
using BlazorInvoice.Shared;
using BlazorInvoice.Shared.Interfaces;

namespace BlazorInvoice.IndexedDb.Services;

public class StatsService(IIndexedDbService indexedDbService, IConfigService configService) : IStatsRepository
{
    public async Task<StatsResponse> GetStats(int year)
    {
        var invoices = await indexedDbService.GetAllInvoices();
        var invoiceBlobs = invoices
            .Select(s => new InvoiceStatsRecord(s.Info.InvoiceDto.IssueDate,
                 (decimal)(s.Info.InvoiceDto.PayableAmount - s.Info.InvoiceDto.GlobalTax), s.IsPaid))
            .ToList();
        var config = await configService.GetConfig();
        DateTime start = new(year, 1, 1);
        DateTime end = new(year + 1, 1, 1);

        var monthStep = config.StatsIsMonthNotQuater ? 1 : 3;
        var monthEndDay = config.StatsMonthEndDay == 0 ? 1 : config.StatsMonthEndDay;
        var steps = GetSteps(invoiceBlobs
                .Where(x => x.IsPaid && x.IssueDate >= start && x.IssueDate < end)
            , year, monthStep, monthEndDay);

        return new StatsResponse
        {
            Start = start,
            End = end.AddDays(-1),
            Steps = steps,
            UnpaidAmount = invoiceBlobs.Where(x => !x.IsPaid).Sum(s => s.TotalAmountWithoutVat),
            TotalInvoices = invoiceBlobs.Count,
            PaidInvoices = invoiceBlobs.Count(x => x.IsPaid)
        };
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
}

internal record InvoiceStatsRecord(DateTime IssueDate, decimal TotalAmountWithoutVat, bool IsPaid);