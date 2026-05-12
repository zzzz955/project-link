using ProjectLink.Domain.Entities;
using ProjectLink.Domain.Interfaces;

namespace ProjectLink.Application.Progress;

public record UpsertProgressCommand(string UserId, IEnumerable<StageProgress> Records, CancellationToken Ct);

public class UpsertProgressCommandHandler
{
    private readonly IProgressRepository _repo;

    public UpsertProgressCommandHandler(IProgressRepository repo)
    {
        _repo = repo;
    }

    public async Task HandleAsync(UpsertProgressCommand command)
    {
        await _repo.UpsertBatchAsync(command.UserId, command.Records, command.Ct);
    }
}
