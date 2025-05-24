using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BlazorInvoice.Db;

public class DbContextFactory : IDesignTimeDbContextFactory<InvoiceContext>
{
    public InvoiceContext CreateDbContext(string[] args)
    {
        var connectionString = "Data Source=./test.db";

        var optionsBuilder = new DbContextOptionsBuilder<InvoiceContext>();
        optionsBuilder.UseSqlite(connectionString, x =>
        {
            x.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
        });

        return new InvoiceContext(optionsBuilder.Options);
    }
}