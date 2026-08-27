using beinx.db.Services;
using beinx.shared;

namespace beinx.Tests;

[TestClass]
public sealed class StatsAggregationTests
{
    [TestMethod]
    public void AggregateStats_ComputesBothTaxModesOnceAndKeepsUnpaidSeparate()
    {
        InvoiceListItem[] invoices =
        [
            CreateInvoice(new DateTime(2025, 1, 1), true, 119, 100),
            CreateInvoice(new DateTime(2025, 2, 1), true, 59.5, 50),
            CreateInvoice(new DateTime(2025, 2, 2), true, 23.8, 20),
            CreateInvoice(new DateTime(2025, 12, 31), true, 10, 10),
            CreateInvoice(new DateTime(2025, 1, 15), false, 238, 200),
            CreateInvoice(new DateTime(2024, 12, 31), true, 999, 999)
        ];

        var result = InvoiceService.AggregateStats(invoices, 2025, 1, 1);

        Assert.AreEqual(6, result.TotalInvoices);
        Assert.HasCount(1, result.UnpaidInvoices);
        Assert.HasCount(12, result.Steps);
        Assert.AreEqual(new DateTime(2025, 1, 1), result.Steps[0].Start);
        Assert.AreEqual(new DateTime(2025, 2, 1), result.Steps[0].End);
        Assert.AreEqual(178.5, result.Steps[0].TotalAmountWithVat, 0.001);
        Assert.AreEqual(150, result.Steps[0].TotalAmountWithoutVat, 0.001);
        Assert.AreEqual(23.8, result.Steps[1].TotalAmountWithVat, 0.001);
        Assert.AreEqual(20, result.Steps[1].TotalAmountWithoutVat, 0.001);
        Assert.AreEqual(0, result.Steps[2].TotalAmountWithVat);
        Assert.AreEqual(212.3, result.Steps.Sum(step => step.TotalAmountWithVat), 0.001);
        Assert.AreEqual(180, result.Steps.Sum(step => step.TotalAmountWithoutVat), 0.001);
    }

    [TestMethod]
    public void AggregateStats_PreservesQuarterBoundaryBehavior()
    {
        InvoiceListItem[] invoices =
        [
            CreateInvoice(new DateTime(2024, 4, 26), true, 119, 100),
            CreateInvoice(new DateTime(2024, 4, 27), true, 50, 50)
        ];

        var result = InvoiceService.AggregateStats(invoices, 2024, 3, 26);

        Assert.HasCount(4, result.Steps);
        Assert.AreEqual(119, result.Steps[0].TotalAmountWithVat);
        Assert.AreEqual(100, result.Steps[0].TotalAmountWithoutVat);
        Assert.AreEqual(50, result.Steps[1].TotalAmountWithVat);
        Assert.AreEqual(50, result.Steps[1].TotalAmountWithoutVat);
    }

    private static InvoiceListItem CreateInvoice(
        DateTime issueDate,
        bool isPaid,
        double payableAmount,
        double taxExclusiveAmount)
        => new()
        {
            IssueDate = issueDate,
            IsPaid = isPaid,
            PayableAmount = payableAmount,
            TaxExclusiveAmount = taxExclusiveAmount
        };
}
