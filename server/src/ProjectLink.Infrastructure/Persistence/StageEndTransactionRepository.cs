using Microsoft.EntityFrameworkCore;
using ProjectLink.Domain.Entities;
using ProjectLink.Domain.Interfaces;

namespace ProjectLink.Infrastructure.Persistence;

public class StageEndTransactionRepository : IStageEndTransaction
{
    private readonly AppDbContext _db;

    public StageEndTransactionRepository(AppDbContext db) => _db = db;

    private static void ApplyRecharge(StaminaState state, int maxStamina, int rechargeIntervalMinutes)
    {
        if (state.Current >= maxStamina) return;
        var ticks = (int)((DateTimeOffset.UtcNow - state.LastRechargedAt).TotalMinutes / rechargeIntervalMinutes);
        if (ticks <= 0) return;
        state.Current         = Math.Min(maxStamina, state.Current + ticks);
        state.LastRechargedAt += TimeSpan.FromMinutes(ticks * rechargeIntervalMinutes);
    }

    public async Task<StageEndDbResult> ExecuteAsync(StageEndDbCommand cmd, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // Ensure rows exist before locking
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT IGNORE INTO stage_progress (user_id, stage_id, stars, cleared_at) VALUES ({cmd.UserId}, {cmd.StageId}, 0, NOW())", ct);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT IGNORE INTO stage_best_records (user_id, stage_id, best_clear_time_ms, best_score, cleared_at) VALUES ({cmd.UserId}, {cmd.StageId}, 0, 0, NOW())", ct);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT IGNORE INTO user_ranking_cache (user_id, total_score, stages_cleared, updated_at) VALUES ({cmd.UserId}, 0, 0, NOW())", ct);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT IGNORE INTO user_currency (user_id, soft_amount) VALUES ({cmd.UserId}, 0)", ct);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT IGNORE INTO stamina_state (user_id, current, last_recharged_at) VALUES ({cmd.UserId}, {cmd.MaxStamina}, NOW())", ct);

        // Acquire locks in deterministic order to prevent deadlocks
        var progress = await _db.StageProgress
            .FromSqlInterpolated($"SELECT * FROM stage_progress WHERE user_id = {cmd.UserId} AND stage_id = {cmd.StageId} FOR UPDATE NOWAIT")
            .FirstAsync(ct);
        var bestRecord = await _db.StageBestRecords
            .FromSqlInterpolated($"SELECT * FROM stage_best_records WHERE user_id = {cmd.UserId} AND stage_id = {cmd.StageId} FOR UPDATE NOWAIT")
            .FirstAsync(ct);
        var stamina = await _db.StaminaStates
            .FromSqlInterpolated($"SELECT * FROM stamina_state WHERE user_id = {cmd.UserId} FOR UPDATE")
            .FirstAsync(ct);
        var currency = await _db.UserCurrencies
            .FromSqlInterpolated($"SELECT * FROM user_currency WHERE user_id = {cmd.UserId} FOR UPDATE")
            .FirstAsync(ct);
        var rankingCache = await _db.RankingCaches
            .FromSqlInterpolated($"SELECT * FROM user_ranking_cache WHERE user_id = {cmd.UserId} FOR UPDATE")
            .FirstAsync(ct);

        var isFirstClear  = progress.Stars == 0;
        var prevBestScore = bestRecord.BestScore;
        var isBestRecord  = isFirstClear || cmd.Score > prevBestScore;

        // Update stage_progress if new stars are better
        if (cmd.Stars > progress.Stars)
        {
            progress.Stars     = cmd.Stars;
            progress.ClearedAt = DateTimeOffset.UtcNow;
        }

        // Update best record if this run is better
        if (isBestRecord)
        {
            bestRecord.BestScore       = cmd.Score;
            bestRecord.BestClearTimeMs = cmd.AdjustedMs;
            bestRecord.ClearedAt       = DateTimeOffset.UtcNow;
        }

        // Update ranking cache
        rankingCache.TotalScore    += isBestRecord ? (cmd.Score - prevBestScore) : 0;
        rankingCache.StagesCleared += isFirstClear ? 1 : 0;
        rankingCache.UpdatedAt      = DateTimeOffset.UtcNow;

        // Refund the stamina spent to start this cleared attempt.
        ApplyRecharge(stamina, cmd.MaxStamina, cmd.RechargeIntervalMinutes);
        var staminaBeforeRefund = stamina.Current;
        stamina.Current = Math.Min(cmd.MaxStamina, stamina.Current + cmd.StaminaRefund);
        if (staminaBeforeRefund < cmd.MaxStamina && stamina.Current >= cmd.MaxStamina)
            stamina.LastRechargedAt = DateTimeOffset.UtcNow;

        // Grant soft reward only on first clear
        var softRewardGranted = isFirstClear ? cmd.SoftReward : 0;
        var balanceBefore = currency.SoftAmount;
        currency.SoftAmount += softRewardGranted;

        if (softRewardGranted > 0)
        {
            _db.CurrencyLogs.Add(new CurrencyLog
            {
                UserId        = cmd.UserId,
                TransactionId = Guid.NewGuid().ToString(),
                CurrencyType  = "soft",
                Delta         = softRewardGranted,
                BalanceBefore = balanceBefore,
                BalanceAfter  = currency.SoftAmount,
                Reason        = $"stage_clear:{cmd.StageId}",
                CorrelationId = cmd.CorrelationId,
                CreatedAt     = DateTimeOffset.UtcNow,
            });
        }

        await _db.SaveChangesAsync(ct);

        // Increment daily challenge play count only when stage is in today's daily set
        if (cmd.IsDailyChallengeStage)
        {
            await _db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO daily_challenge_progress (user_id, challenge_date, play_count, completed, streak_days, created_at)
                VALUES ({cmd.UserId}, {cmd.ChallengeDate}, 1, false, 0, NOW())
                ON DUPLICATE KEY UPDATE play_count = play_count + 1
                """, ct);
        }

        _db.ChangeTracker.Clear();
        var dailyRow = cmd.IsDailyChallengeStage
            ? await _db.DailyChallengeProgresses
                .FirstOrDefaultAsync(p => p.UserId == cmd.UserId && p.ChallengeDate == cmd.ChallengeDate, ct)
            : null;

        await tx.CommitAsync(ct);

        return new StageEndDbResult
        {
            IsBestRecord      = isBestRecord,
            SoftBalanceAfter  = currency.SoftAmount,
            SoftRewardGranted = softRewardGranted,
            DailyPlayCount    = dailyRow?.PlayCount ?? 0,
            NextStageUnlocked = isFirstClear && cmd.StageId < cmd.MaxStages,
            TotalScore        = rankingCache.TotalScore,
            StagesCleared     = rankingCache.StagesCleared,
        };
    }
}
