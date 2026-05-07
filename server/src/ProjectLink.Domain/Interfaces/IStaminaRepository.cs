using ProjectLink.Domain.Entities;

namespace ProjectLink.Domain.Interfaces;

public interface IStaminaRepository
{
    // Reads row with lazy recharge applied (no lock — for display only)
    Task<StaminaState> GetComputedAsync(string userId, int maxStamina, int rechargeIntervalMinutes, CancellationToken ct);
    // SELECT FOR UPDATE → apply recharge → deduct 1 → save (throws InsufficientStaminaException if 0)
    Task<StaminaState> DeductAsync(string userId, int maxStamina, int rechargeIntervalMinutes, CancellationToken ct);
    // SELECT FOR UPDATE → apply recharge → add toAdd (capped at max) → save
    Task<(StaminaState state, int added)> AddAsync(string userId, int maxStamina, int rechargeIntervalMinutes, int toAdd, CancellationToken ct);
}
