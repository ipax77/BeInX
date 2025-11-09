namespace beinx.shared;

public class DraftState<T>
{
    public int? EntityId { get; set; }
    public T Data { get; set; } = default!;
    public bool IsNew => !EntityId.HasValue || EntityId <= 0;

    public DraftState() { }
    public DraftState(int? entityId, T data)
    {
        EntityId = entityId;
        Data = data;
    }
}

public interface IDraftRepository<T>
{
    public Task<DraftState<T>?> LoadDraftAsync();
    public Task SaveDraftAsync(DraftState<T> draft);
    public Task ClearDraftAsync(int? entityId);
}