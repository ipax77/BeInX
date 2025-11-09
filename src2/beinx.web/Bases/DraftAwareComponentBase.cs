namespace beinx.web.Bases;

using beinx.loc;
using beinx.shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

public abstract class DraftAwareComponentBase<TDto, TEntity> : ComponentBase, IDisposable
{
    [Inject] protected ILogger<DraftAwareComponentBase<TDto, TEntity>> Logger { get; set; } = default!;
    [Inject] protected IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] protected IDraftRepository<TDto> DraftRepository { get; set; } = default!;
    [Inject] protected IStringLocalizer<InvoiceLoc> Loc { get; set; } = default!;

    protected List<TEntity> Items = [];
    protected bool IsLoading = true;
    protected bool IsEditing = false;
    protected DraftState<TDto> CurrentDraft = new();
    protected EditContext EditContext = default!;

    protected override async Task OnInitializedAsync()
    {
        await LoadItemsAsync();

        var draft = await DraftRepository.LoadDraftAsync();
        if (draft is not null)
        {
            bool restore = await JSRuntime.InvokeAsync<bool>("confirm", Loc["DraftRestore"].ToString());
            if (restore)
            {
                CurrentDraft = draft;
                UpdateEditContext();
                IsEditing = true;
                Logger.LogInformation("Draft restored for entity ID {Id}", draft.EntityId);
            }
            else
            {
                await DraftRepository.ClearDraftAsync(draft.EntityId);
            }
        }
    }

    protected abstract Task<List<TEntity>> GetAllAsync();
    protected abstract Task<int> CreateAsync(TDto dto);
    protected abstract Task UpdateAsync(int id, TDto dto);
    protected abstract Task DeleteAsync(int id);

    protected virtual async Task LoadItemsAsync()
    {
        IsLoading = true;
        StateHasChanged();
        Items = await GetAllAsync();
        IsLoading = false;
        StateHasChanged();
    }

    protected virtual void NewItem()
    {
        CurrentDraft = new DraftState<TDto>(null, CreateNewDto());
        UpdateEditContext();
        IsEditing = true;
    }

    protected virtual void EditItem(int id, TDto dto)
    {
        CurrentDraft = new DraftState<TDto>(id, dto);
        UpdateEditContext();
        IsEditing = true;
    }

    protected abstract TDto CreateNewDto();

    protected virtual void UpdateEditContext()
    {
        if (EditContext != null)
            EditContext.OnFieldChanged -= OnFieldChanged;

        ArgumentNullException.ThrowIfNull(CurrentDraft.Data, "Current draft data cannot be null when updating edit context");

        EditContext = new EditContext(CurrentDraft.Data);
        EditContext.OnFieldChanged += OnFieldChanged;
    }

    private void OnFieldChanged(object? sender, FieldChangedEventArgs e)
    {
        _ = SaveDraftAsync();
    }

    protected virtual async Task SaveDraftAsync()
    {
        await DraftRepository.SaveDraftAsync(CurrentDraft);
        Logger.LogDebug("Draft saved for entity ID {Id}", CurrentDraft.EntityId);
    }

    protected virtual async Task SaveItemAsync()
    {
        if (CurrentDraft.IsNew)
        {
            var id = await CreateAsync(CurrentDraft.Data);
            await AfterCreatedAsync(id, CurrentDraft.Data);
        }
        else
        {
            await UpdateAsync(CurrentDraft.EntityId!.Value, CurrentDraft.Data);
            await AfterUpdatedAsync(CurrentDraft.EntityId.Value, CurrentDraft.Data);
        }

        await DraftRepository.ClearDraftAsync(CurrentDraft.EntityId);
        IsEditing = false;
    }

    protected virtual async Task DeleteItemAsync(int id)
    {
        await DeleteAsync(id);
        Items.RemoveAll(i => GetEntityId(i) == id);
    }

    protected abstract int GetEntityId(TEntity entity);

    protected virtual Task AfterCreatedAsync(int id, TDto dto) => Task.CompletedTask;
    protected virtual Task AfterUpdatedAsync(int id, TDto dto) => Task.CompletedTask;

    public void Dispose()
    {
        if (EditContext != null)
            EditContext.OnFieldChanged -= OnFieldChanged;
    }
}

