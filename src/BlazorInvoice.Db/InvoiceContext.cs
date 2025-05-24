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

        base.OnModelCreating(modelBuilder);
    }
}

public class TempInvoice
{
    public int TempInvoiceId { get; set; }
    [Precision(0)]
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public byte[] InvoiceBlob { get; set; } = [];
    public int? InvoiceId { get; set; }
    public int? SellerPartyId { get; set; }
    public int? BuyerPartyId { get; set; }
    public int? PaymentMeansId { get; set; }
}