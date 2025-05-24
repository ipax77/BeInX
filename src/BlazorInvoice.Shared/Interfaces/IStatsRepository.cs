namespace BlazorInvoice.Shared.Interfaces;

public interface IStatsRepository
{
    Task<StatsResponse> GetStats(int year);
}