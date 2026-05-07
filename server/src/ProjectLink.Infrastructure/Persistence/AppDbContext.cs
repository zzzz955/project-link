using Microsoft.EntityFrameworkCore;
using ProjectLink.Domain.Entities;

namespace ProjectLink.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Session>          Sessions         => Set<Session>();
    public DbSet<StageProgress>    StageProgress    => Set<StageProgress>();
    public DbSet<ClientMeta>       ClientMeta       => Set<ClientMeta>();
    public DbSet<UserCurrency>     UserCurrencies   => Set<UserCurrency>();
    public DbSet<CurrencyLog>      CurrencyLogs     => Set<CurrencyLog>();
    public DbSet<StaminaState>     StaminaStates    => Set<StaminaState>();
    public DbSet<Inventory>        Inventories      => Set<Inventory>();
    public DbSet<UserProfile>      UserProfiles     => Set<UserProfile>();
    public DbSet<StageBestRecord>  StageBestRecords => Set<StageBestRecord>();
    public DbSet<UserRankingCache> RankingCaches    => Set<UserRankingCache>();
    public DbSet<ActionLog>        ActionLogs       => Set<ActionLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Session>(e =>
        {
            e.ToTable("sessions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            e.Property(x => x.UserId).HasColumnName("user_id").HasMaxLength(36);
            e.Property(x => x.SessionId).HasColumnName("session_id").HasMaxLength(36);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            e.Property(x => x.Active).HasColumnName("active").HasDefaultValue(true);
            e.HasIndex(x => x.UserId).HasDatabaseName("idx_sessions_user_id");
            e.HasIndex(x => x.SessionId).IsUnique();
        });

        modelBuilder.Entity<StageProgress>(e =>
        {
            e.ToTable("stage_progress");
            e.HasKey(x => new { x.UserId, x.StageId });
            e.Property(x => x.UserId).HasColumnName("user_id").HasMaxLength(36);
            e.Property(x => x.StageId).HasColumnName("stage_id");
            e.Property(x => x.Stars).HasColumnName("stars");
            e.Property(x => x.ClearedAt).HasColumnName("cleared_at");
        });

        modelBuilder.Entity<ClientMeta>(e =>
        {
            e.ToTable("client_meta");
            e.HasKey(x => x.ClientVersion);
            e.Property(x => x.ClientVersion).HasColumnName("client_version").HasMaxLength(20);
            e.Property(x => x.MetaHash).HasColumnName("meta_hash");
            e.Property(x => x.ProtocolVersion).HasColumnName("protocol_version").HasMaxLength(20);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<UserCurrency>(e =>
        {
            e.ToTable("user_currency");
            e.HasKey(x => x.UserId);
            e.Property(x => x.UserId).HasColumnName("user_id").HasMaxLength(36);
            e.Property(x => x.SoftAmount).HasColumnName("soft_amount").HasDefaultValue(0L);
        });

        modelBuilder.Entity<CurrencyLog>(e =>
        {
            e.ToTable("currency_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            e.Property(x => x.UserId).HasColumnName("user_id").HasMaxLength(36);
            e.Property(x => x.TransactionId).HasColumnName("transaction_id").HasMaxLength(36);
            e.Property(x => x.CurrencyType).HasColumnName("currency_type").HasMaxLength(32);
            e.Property(x => x.Delta).HasColumnName("delta");
            e.Property(x => x.BalanceBefore).HasColumnName("balance_before");
            e.Property(x => x.BalanceAfter).HasColumnName("balance_after");
            e.Property(x => x.Reason).HasColumnName("reason");
            e.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(36);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasIndex(x => x.UserId).HasDatabaseName("idx_currency_logs_user_id");
        });

        modelBuilder.Entity<StaminaState>(e =>
        {
            e.ToTable("stamina_state");
            e.HasKey(x => x.UserId);
            e.Property(x => x.UserId).HasColumnName("user_id").HasMaxLength(36);
            e.Property(x => x.Current).HasColumnName("current");
            e.Property(x => x.LastRechargedAt).HasColumnName("last_recharged_at");
        });

        modelBuilder.Entity<Inventory>(e =>
        {
            e.ToTable("inventory");
            e.HasKey(x => new { x.UserId, x.ItemId });
            e.Property(x => x.UserId).HasColumnName("user_id").HasMaxLength(36);
            e.Property(x => x.ItemId).HasColumnName("item_id");
            e.Property(x => x.Quantity).HasColumnName("quantity").HasDefaultValue(0);
        });

        modelBuilder.Entity<UserProfile>(e =>
        {
            e.ToTable("user_profiles");
            e.HasKey(x => x.UserId);
            e.Property(x => x.UserId).HasColumnName("user_id").HasMaxLength(36);
            e.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(64);
            e.Property(x => x.AccountCreatedAt).HasColumnName("account_created_at");
            e.Property(x => x.LastLoginAt).HasColumnName("last_login_at");
        });

        modelBuilder.Entity<StageBestRecord>(e =>
        {
            e.ToTable("stage_best_records");
            e.HasKey(x => new { x.UserId, x.StageId });
            e.Property(x => x.UserId).HasColumnName("user_id").HasMaxLength(36);
            e.Property(x => x.StageId).HasColumnName("stage_id");
            e.Property(x => x.BestClearTimeMs).HasColumnName("best_clear_time_ms");
            e.Property(x => x.BestScore).HasColumnName("best_score");
            e.Property(x => x.ClearedAt).HasColumnName("cleared_at");
        });

        modelBuilder.Entity<UserRankingCache>(e =>
        {
            e.ToTable("user_ranking_cache");
            e.HasKey(x => x.UserId);
            e.Property(x => x.UserId).HasColumnName("user_id").HasMaxLength(36);
            e.Property(x => x.TotalScore).HasColumnName("total_score").HasDefaultValue(0L);
            e.Property(x => x.StagesCleared).HasColumnName("stages_cleared").HasDefaultValue(0);
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<ActionLog>(e =>
        {
            e.ToTable("action_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            e.Property(x => x.UserId).HasColumnName("user_id").HasMaxLength(36);
            e.Property(x => x.ActionType).HasColumnName("action_type").HasMaxLength(64);
            e.Property(x => x.Payload).HasColumnName("payload").HasColumnType("json");
            e.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(36);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasIndex(x => x.UserId).HasDatabaseName("idx_action_logs_user_id");
            e.HasIndex(x => x.ActionType).HasDatabaseName("idx_action_logs_action_type");
        });
    }
}
