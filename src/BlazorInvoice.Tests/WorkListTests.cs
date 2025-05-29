using BlazorInvoice.Db;
using BlazorInvoice.Db.Repository;
using BlazorInvoice.Db.Services;
using BlazorInvoice.Shared;
using BlazorInvoice.Shared.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.XRechnung.NET.AnnotatedDtos;

namespace BlazorInvoice.Tests;

[TestClass]
public class WorkListTests
{
    private readonly ServiceProvider serviceProvider;

    public WorkListTests()
    {
        var services = new ServiceCollection();
        var sqliteConnection = new SqliteConnection("DataSource=:memory:");
        sqliteConnection.Open();
        services.AddDbContext<InvoiceContext>(options => options
            .UseSqlite(sqliteConnection));

        services.AddSingleton<IConfigService, ConfigService>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IWorkListRepository, WorkListRepository>();

        serviceProvider = services.BuildServiceProvider();
    }

    private static async Task<int> CreateTestParty(IServiceScope scope)
    {
        var party = new BuyerAnnotationDto
        {
            Name = "Test Party",
            StreetName = "Test Street",
            City = "Test City",
            PostCode = "12345",
            CountryCode = "DE",
            Email = "test@example.com",
            RegistrationName = "Test Registration",
            BuyerReference = "Test Reference",
        };
        var invoiceRepository = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();
        return await invoiceRepository.CreateParty(party, false);
    }

    private static async Task<WorkEntrySnapShot> CreateTestWorkEntry(IServiceScope scope, int partyId)
    {
        var entry = new WorkEntryDto()
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Job = "Test Job",
            StartTime = new(07, 00),
            EndTime = new(08, 00),
            Billed = false,
            HourlyRate = 100.0,
            PartyId = partyId
        };
        var workListRepository = scope.ServiceProvider.GetRequiredService<IWorkListRepository>();
        var snapshot = new WorkEntrySnapShot
        {
            EntriesByParty = new Dictionary<int, List<WorkEntryDto>>
            {
                { partyId, new List<WorkEntryDto> { entry } }
            }
        };
        await workListRepository.SaveWorkEntries(snapshot);
        return snapshot;
    }

    [TestMethod]
    public async Task T01CanAddEntries()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<InvoiceContext>();
        var workListRepository = scope.ServiceProvider.GetRequiredService<IWorkListRepository>();

        context.Database.EnsureDeleted();
        context.Database.Migrate();

        int partyId = await CreateTestParty(scope);
        await CreateTestWorkEntry(scope, partyId);

        var snapshot = await workListRepository.GetWorkEntries();

        Assert.IsNotNull(snapshot);
        Assert.AreEqual(1, snapshot.EntriesByParty.Values.Count);
    }

    [TestMethod]
    public async Task T02CanEditEntries()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<InvoiceContext>();
        var workListRepository = scope.ServiceProvider.GetRequiredService<IWorkListRepository>();

        context.Database.EnsureDeleted();
        context.Database.Migrate();

        int partyId = await CreateTestParty(scope);
        await CreateTestWorkEntry(scope, partyId);

        var snapshot = await workListRepository.GetWorkEntries();

        var entry = snapshot.EntriesByParty[partyId][0];
        entry.Job = "Updated Job";

        await workListRepository.SaveWorkEntries(snapshot);
        var updatedSnapshot = await workListRepository.GetWorkEntries();
        Assert.IsNotNull(updatedSnapshot);
        Assert.AreEqual(1, updatedSnapshot.EntriesByParty.Values.Count);
        Assert.AreEqual("Updated Job", updatedSnapshot.EntriesByParty[partyId][0].Job);
    }

    [TestMethod]
    public async Task T03CanDeleteEntries()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<InvoiceContext>();
        var workListRepository = scope.ServiceProvider.GetRequiredService<IWorkListRepository>();

        context.Database.EnsureDeleted();
        context.Database.Migrate();

        int partyId = await CreateTestParty(scope);
        var snapshot = await CreateTestWorkEntry(scope, partyId);

        snapshot.EntriesByParty[partyId].Clear();

        await workListRepository.SaveWorkEntries(snapshot);
        var updatedSnapshot = await workListRepository.GetWorkEntries();
        Assert.IsNotNull(updatedSnapshot);
        Assert.AreEqual(0, updatedSnapshot.EntriesByParty.Values.Count);
    }
}
