using beinx.shared;

namespace beinx.db.Services;

public partial class InvoiceService
{
    public async Task<StatsResponse> GetStats(int year)
    {
        InvoicesRequest request = new()
        {
            Year = year,
            Page = 0,
            PageSize = 10_000,
        };

        var invoices = await invoiceRepository.GetFilteredListAsync(request);
        var config = await configService.GetConfig();
        var monthStep = config.StatsIsMonthNotQuater ? 1 : 3;
        var monthEndDay = config.StatsMonthEndDay == 0 ? 1 : config.StatsMonthEndDay;

        return AggregateStats(invoices, year, monthStep, monthEndDay);
    }

    internal static StatsResponse AggregateStats(
        IReadOnlyList<InvoiceListItem> invoices,
        int year,
        int monthStep,
        int monthEndDay)
    {
        DateTime start = new(year, 1, 1);
        DateTime end = new(year + 1, 1, 1);
        var periods = GetPeriods(year, monthStep, monthEndDay);
        var totalsWithVat = new double[periods.Count];
        var totalsWithoutVat = new double[periods.Count];
        List<InvoiceListItem> unpaidInvoices = [];

        foreach (var invoice in invoices)
        {
            if (!invoice.IsPaid)
            {
                unpaidInvoices.Add(invoice);
                continue;
            }

            if (invoice.IssueDate < start || invoice.IssueDate >= end)
            {
                continue;
            }

            for (var index = 0; index < periods.Count; index++)
            {
                var period = periods[index];
                if (invoice.IssueDate <= period.StartExclusive || invoice.IssueDate > period.EndInclusive)
                {
                    continue;
                }

                totalsWithVat[index] += invoice.PayableAmount;
                totalsWithoutVat[index] += invoice.TaxExclusiveAmount;
                break;
            }
        }

        var steps = new List<StatsStepResponse>(periods.Count);
        for (var index = 0; index < periods.Count; index++)
        {
            var period = periods[index];
            steps.Add(new StatsStepResponse
            {
                Start = period.StartExclusive.AddDays(1),
                End = period.EndInclusive,
                TotalAmountWithVat = totalsWithVat[index],
                TotalAmountWithoutVat = totalsWithoutVat[index]
            });
        }

        return new StatsResponse
        {
            Start = start,
            End = end.AddDays(-1),
            Steps = steps,
            TotalInvoices = invoices.Count,
            UnpaidInvoices = unpaidInvoices
        };
    }

    private static List<StatsPeriod> GetPeriods(int year, int monthStep, int monthEndDay)
    {
        var periods = new List<StatsPeriod>(12 / monthStep);
        var endDate = new DateOnly(year + 1, 1, 1).AddDays(-1);

        // JANUARY
        {
            var periodStart = new DateOnly(year, 1, 1).AddDays(-1);
            var periodEnd = SafeDateOnly(year, 1 + monthStep, monthEndDay);
            periods.Add(CreatePeriod(periodStart, periodEnd));
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

            periods.Add(CreatePeriod(periodStart, periodEnd));
        }

        return periods;
    }

    private static StatsPeriod CreatePeriod(DateOnly start, DateOnly end)
        => new(start.ToDateTime(TimeOnly.MinValue), end.ToDateTime(TimeOnly.MinValue));

    private static DateOnly SafeDateOnly(int year, int month, int day)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        return new DateOnly(year, month, Math.Min(day, daysInMonth));
    }

    private readonly record struct StatsPeriod(DateTime StartExclusive, DateTime EndInclusive);
}

