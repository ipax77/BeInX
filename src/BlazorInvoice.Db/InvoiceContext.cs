using Microsoft.EntityFrameworkCore;

namespace BlazorInvoice.Db;

public sealed class InvoiceContext : DbContext
{
    public DbSet<Invoice> Invoices { get; set; } = null!;
    public DbSet<InvoiceParty> InvoiceParties { get; set; } = null!;
    public DbSet<PaymentMeans> PaymentMeans { get; set; } = null!;
    public DbSet<InvoiceLine> InvoiceLines { get; set; } = null!;
    public DbSet<AdditionalDocumentReference> AdditionalDocumentReferences { get; set; } = null!;
    public DbSet<TempInvoice> TempInvoices { get; set; } = null!;
    public DbSet<AppConfig> AppConfigs { get; set; } = null!;
    public DbSet<WorkEntry> WorkEntries { get; set; } = null!;
    public DbSet<TempWorkEntry> TempWorkEntries { get; set; } = null!;

    public InvoiceContext(DbContextOptions<InvoiceContext> options)
    : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasIndex(i => i.Id);
            entity.HasIndex(i => i.IssueDate);
        });

        modelBuilder.Entity<InvoiceParty>(entity =>
        {
            entity.HasIndex(i => i.Email);
            entity.HasIndex(i => i.IsSeller);
        });

        modelBuilder.Entity<WorkEntry>(entity =>
        {
            entity.HasIndex(i => i.EntryGuid).IsUnique();
        });

        base.OnModelCreating(modelBuilder);
    }
}
