using BlazorInvoice.Shared;
using BlazorInvoice.Shared.Interfaces;
using Microsoft.JSInterop;
using pax.XRechnung.NET.XmlModels;
using System.Text;
using System.Xml.Serialization;

namespace BlazorInvoice.Pwa.Services;

public class StatsRepository(IJSRuntime _js) : IStatsRepository
{
    public async Task<StatsResponse> GetStats(int year)
    {
        return await Task.FromResult(new StatsResponse
        {
            Start = DateTime.Today,
            End = DateTime.Today.AddDays(-1),
            Steps = [],
            UnpaidAmount = 0,
            TotalInvoices = 0,
            PaidInvoices = 0
        });
    }

    private static List<StatsStepResponse> GetSteps(IEnumerable<InvoiceStatsRecord> records, int year, int monthStep, int monthEndDay)
    {
        var steps = new List<StatsStepResponse>();
        var endDate = new DateOnly(year + 1, 1, 1).AddDays(-1);

        // JANUARY
        {
            var periodStart = new DateOnly(year, 1, 1).AddDays(-1);
            var periodEnd = SafeDateOnly(year, 1 + monthStep, monthEndDay);
            steps.Add(GetStep(records, periodStart, periodEnd));
        }

        // INTERMEDIATE PERIODS
        for (int month = monthStep + 1; month <= 12; month += monthStep)
        {
            var periodStart = SafeDateOnly(year, month, monthEndDay);
            var temp = periodStart.AddMonths(monthStep);
            var periodEnd = SafeDateOnly(temp.Year, temp.Month, monthEndDay);
            if (periodEnd > endDate)
            {
                periodEnd = endDate;
            }
            if (periodStart >= endDate) break;

            steps.Add(GetStep(records, periodStart, periodEnd));
        }

        return steps;
    }

    private static StatsStepResponse GetStep(IEnumerable<InvoiceStatsRecord> records, DateOnly start, DateOnly end)
    {
        var invoicesInPeriod = records
            .Where(x => x.IssueDate > start.ToDateTime(new()) && x.IssueDate <= end.ToDateTime(new()))
            .ToList();

        var total = invoicesInPeriod.Sum(s => s.TotalAmountWithoutVat);

        return new()
        {
            Start = start.AddDays(1).ToDateTime(TimeOnly.MinValue),
            End = end.ToDateTime(TimeOnly.MinValue),
            TotalAmountWithoutVat = total
        };
    }

    private static DateOnly SafeDateOnly(int year, int month, int day)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        return new DateOnly(year, month, Math.Min(day, daysInMonth));
    }

    private static XmlInvoice? GetXmlInvoice(byte[]? blob, XmlSerializer serializer)
    {
        if (blob == null)
        {
            return null;
        }
        try
        {
            using var stream = new MemoryStream(blob);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return (XmlInvoice?)serializer.Deserialize(stream);
        }
        catch
        {
            return null;
        }
    }

    private static decimal GetTotalAmountWithoutVat(XmlInvoice xmlInvoice)
    {
        return xmlInvoice.LegalMonetaryTotal.TaxExclusiveAmount.Value;
    }
}

internal record InvoiceStatsRecord(DateTime IssueDate, decimal TotalAmountWithoutVat, bool IsPaid);