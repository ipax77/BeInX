using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace BlazorInvoice.Db;

public class WorkEntry
{
    public int WorkEntryId { get; set; }
    public Guid EntryGuid { get; set; } = Guid.NewGuid();
    public DateTime Date { get; set; }
    [MaxLength(500)]
    public string Job { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool Billed { get; set; }
    public decimal HourlyRate { get; set; }
    public int InvoicePartyId { get; set; }
    public InvoiceParty? InvoiceParty { get; set; }
}

public class TempWorkEntry
{
    public int TempWorkEntryId { get; set; }
    [Precision(0)]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public byte[] WorkEntriesBlob { get; set; } = Array.Empty<byte>();
    public int? InvoicePartyId { get; set; }
}
