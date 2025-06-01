using BlazorInvoice.Shared;

namespace BlazorInvoice.Weblib.Services;

public class WorkListService
{
    private readonly WorkEntrySnapShot _snapshot = new();
    private readonly Dictionary<int, Stack<WorkEntryDelta>> _undoStacks = [];
    private readonly Dictionary<int, Stack<WorkEntryDelta>> _redoStacks = [];

    public WorkListService(WorkEntrySnapShot snapShot)
    {
        _snapshot = snapShot;
    }

    public WorkEntrySnapShot GetSnapshot()
    {
        return _snapshot;
    }

    public void SetSnapshot(WorkEntrySnapShot snapShot)
    {
        _snapshot.EntriesByParty = snapShot.EntriesByParty;
        _undoStacks.Clear();
        _redoStacks.Clear();
    }

    public Dictionary<int, Stack<WorkEntryDelta>> GetUndoStacks()
    {
        return _undoStacks;
    }

    public List<WorkEntryDto> GetEntriesByParty(int partyId)
    {
        if (_snapshot.EntriesByParty.TryGetValue(partyId, out var entries))
        {
            return entries;
        }
        return [];
    }

    public bool CanUndoParty(int partyId)
    {
        if (_undoStacks.TryGetValue(partyId, out var undoStack) && undoStack.Count > 0)
        {
            return true;
        }
        return false;
    }

    public bool CanRedoParty(int partyId)
    {
        if (_redoStacks.TryGetValue(partyId, out var redoStack) && redoStack.Count > 0)
        {
            return true;
        }
        return false;
    }

    public void EntryCreated(WorkEntryDto entry)
    {
        if (!_snapshot.EntriesByParty.TryGetValue(entry.PartyId, out var entries)
            || entries is null)
        {
            entries = _snapshot.EntriesByParty[entry.PartyId] = [];
        }
        entries.Add(entry);
        if (_undoStacks.TryGetValue(entry.PartyId, out var changes)
            || changes is null)
        {
            changes = _undoStacks[entry.PartyId] = new();
        }
        changes.Push(new WorkEntryDelta
        {
            ChangeType = WorkEntryChangeType.Create,
            After = entry with { },
            PartyId = entry.PartyId
        });
        _redoStacks.Remove(entry.PartyId);
    }

    public void EntryChanged(WorkEntryDto entry)
    {
        if (_snapshot.EntriesByParty.TryGetValue(entry.PartyId, out var entries))
        {
            var existingEntry = entries.FirstOrDefault(e => e.EntryGuid == entry.EntryGuid);
            if (existingEntry is not null)
            {
                var delta = new WorkEntryDelta
                {
                    ChangeType = WorkEntryChangeType.Edit,
                    Before = existingEntry with { },
                    PartyId = entry.PartyId
                };
                existingEntry.Date = entry.Date;
                existingEntry.Job = entry.Job;
                existingEntry.StartTime = entry.StartTime;
                existingEntry.EndTime = entry.EndTime;
                existingEntry.HourlyRate = entry.HourlyRate;
                if (_undoStacks.TryGetValue(entry.PartyId, out var changes)
                    || changes is null)
                {
                    changes = _undoStacks[entry.PartyId] = new();
                }
                changes.Push(delta with { After = existingEntry with { } });
            }
        }
        _redoStacks.Remove(entry.PartyId);
    }

    public void EntryDeleted(WorkEntryDto entry)
    {
        if (_snapshot.EntriesByParty.TryGetValue(entry.PartyId, out var entries) == true)
        {
            var removed = entries.RemoveAll(e => e.EntryGuid == entry.EntryGuid) > 0;
            if (removed)
            {
                if (_undoStacks.TryGetValue(entry.PartyId, out var changes)
                    || changes is null)
                {
                    changes = _undoStacks[entry.PartyId] = new();
                }
                changes.Push(new WorkEntryDelta
                {
                    ChangeType = WorkEntryChangeType.Delete,
                    Before = entry with { },
                    PartyId = entry.PartyId
                });
            }
        }
        _redoStacks.Remove(entry.PartyId);
    }

    public WorkEntryDelta? UndoLastChange(int partyId)
    {
        if (_undoStacks.TryGetValue(partyId, out var undoStack) && undoStack.TryPop(out var delta))
        {
            if (!_snapshot.EntriesByParty.TryGetValue(partyId, out var entries))
            {
                entries = _snapshot.EntriesByParty[partyId] = new();
            }

            if (!_redoStacks.TryGetValue(partyId, out var redoStack))
            {
                redoStack = _redoStacks[partyId] = new Stack<WorkEntryDelta>();
            }

            switch (delta.ChangeType)
            {
                case WorkEntryChangeType.Create:
                    entries.RemoveAll(e => e.EntryGuid == delta.After!.EntryGuid);
                    redoStack.Push(delta);
                    break;

                case WorkEntryChangeType.Edit:
                    var index = entries.FindIndex(e => e.EntryGuid == delta.After!.EntryGuid);
                    if (index >= 0)
                    {
                        entries[index] = delta.Before!;
                        redoStack.Push(delta);
                    }
                    break;

                case WorkEntryChangeType.Delete:
                    entries.Add(delta.Before!);
                    redoStack.Push(delta);
                    break;
            }

            return delta;
        }
        return null;
    }

    public WorkEntryDelta? RedoLastChange(int partyId)
    {
        if (_redoStacks.TryGetValue(partyId, out var redoStack) && redoStack.TryPop(out var delta))
        {
            if (!_snapshot.EntriesByParty.TryGetValue(partyId, out var entries))
            {
                entries = _snapshot.EntriesByParty[partyId] = new();
            }

            if (!_undoStacks.TryGetValue(partyId, out var undoStack))
            {
                undoStack = _undoStacks[partyId] = new Stack<WorkEntryDelta>();
            }

            switch (delta.ChangeType)
            {
                case WorkEntryChangeType.Create:
                    entries.Add(delta.After!);
                    undoStack.Push(delta);
                    break;

                case WorkEntryChangeType.Edit:
                    var index = entries.FindIndex(e => e.EntryGuid == delta.Before!.EntryGuid);
                    if (index >= 0)
                    {
                        entries[index] = delta.After!;
                        undoStack.Push(delta);
                    }
                    break;

                case WorkEntryChangeType.Delete:
                    entries.RemoveAll(e => e.EntryGuid == delta.Before!.EntryGuid);
                    undoStack.Push(delta);
                    break;
            }

            return delta;
        }

        return null;
    }
}
