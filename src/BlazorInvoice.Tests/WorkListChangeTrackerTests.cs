using BlazorInvoice.Shared;
using BlazorInvoice.Weblib.Services;

namespace BlazorInvoice.Tests;

[TestClass]
public class WorkListChangeTrackerTests
{
    private WorkEntryDto CreateTestEntry(int partyId = 1, string? job = "Test Job")
    {
        return new WorkEntryDto
        {
            EntryGuid = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(DateTime.Today),
            Job = job!,
            StartTime = new TimeOnly(7, 0),
            EndTime = new TimeOnly(8, 0),
            Billed = false,
            HourlyRate = 100.0,
            PartyId = partyId
        };
    }

    [TestMethod]
    public void CanAddEntry()
    {
        var tracker = new WorkListService(new());
        var entry = CreateTestEntry();
        tracker.EntryCreated(entry);
        var entries = tracker.GetEntriesByParty(1);
        Assert.AreEqual(1, entries.Count);
        Assert.AreEqual(entry, entries[0]);
    }

    [TestMethod]
    public void CanEditEntry()
    {
        var tracker = new WorkListService(new());
        var entry = CreateTestEntry();
        tracker.EntryCreated(entry);

        var updatedEntry = entry with { Job = "Updated Job" };
        tracker.EntryChanged(updatedEntry);

        var entries = tracker.GetEntriesByParty(1);
        Assert.AreEqual("Updated Job", entries[0].Job);
    }

    [TestMethod]
    public void CanDeleteEntry()
    {
        var tracker = new WorkListService(new());
        var entry = CreateTestEntry();
        tracker.EntryCreated(entry);

        tracker.EntryDeleted(entry);
        var entries = tracker.GetEntriesByParty(1);
        Assert.AreEqual(0, entries.Count);
    }

    [TestMethod]
    public void UndoCreate_RemovesEntry()
    {
        var tracker = new WorkListService(new());
        var entry = CreateTestEntry();
        tracker.EntryCreated(entry);

        var result = tracker.UndoLastChange(1);
        Assert.IsNotNull(result);

        var entries = tracker.GetEntriesByParty(1);
        Assert.AreEqual(0, entries.Count);
    }

    [TestMethod]
    public void UndoEdit_RevertsToPreviousState()
    {
        var tracker = new WorkListService(new());
        var entry = CreateTestEntry(job: "Original Job");
        tracker.EntryCreated(entry);

        var updated = entry with { Job = "Edited Job" };
        tracker.EntryChanged(updated);
        tracker.UndoLastChange(1);

        var entries = tracker.GetEntriesByParty(1);
        Assert.AreEqual("Original Job", entries[0].Job);
    }

    [TestMethod]
    public void UndoDelete_RestoresEntry()
    {
        var tracker = new WorkListService(new());
        var entry = CreateTestEntry();
        tracker.EntryCreated(entry);
        tracker.EntryDeleted(entry);

        tracker.UndoLastChange(1);
        var entries = tracker.GetEntriesByParty(1);
        Assert.AreEqual(1, entries.Count);
    }

    [TestMethod]
    public void RedoAfterUndoCreate_ReAddsEntry()
    {
        var tracker = new WorkListService(new());
        var entry = CreateTestEntry();
        tracker.EntryCreated(entry);
        tracker.UndoLastChange(1);

        var result = tracker.RedoLastChange(1);
        Assert.IsNotNull(result);

        var entries = tracker.GetEntriesByParty(1);
        Assert.AreEqual(1, entries.Count);
    }

    [TestMethod]
    public void RedoAfterUndoEdit_ReappliesEdit()
    {
        var tracker = new WorkListService(new());
        var entry = CreateTestEntry(job: "Before");
        tracker.EntryCreated(entry);

        var edited = entry with { Job = "After" };
        tracker.EntryChanged(edited);
        tracker.UndoLastChange(1);
        tracker.RedoLastChange(1);

        var entries = tracker.GetEntriesByParty(1);
        Assert.AreEqual("After", entries[0].Job);
    }

    [TestMethod]
    public void RedoAfterUndoDelete_RemovesAgain()
    {
        var tracker = new WorkListService(new());
        var entry = CreateTestEntry();
        tracker.EntryCreated(entry);
        tracker.EntryDeleted(entry);
        tracker.UndoLastChange(1);
        tracker.RedoLastChange(1);

        var entries = tracker.GetEntriesByParty(1);
        Assert.AreEqual(0, entries.Count);
    }

    [TestMethod]
    public void RedoFailsWhenNoUndoHappened()
    {
        var tracker = new WorkListService(new());
        var result = tracker.RedoLastChange(1);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void UndoFailsWhenNoChanges()
    {
        var tracker = new WorkListService(new());
        var result = tracker.UndoLastChange(1);
        Assert.IsNull(result);
    }
}
