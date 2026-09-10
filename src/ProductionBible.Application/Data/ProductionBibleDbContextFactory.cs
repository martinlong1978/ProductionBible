using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProductionBible.Application.Data;

public class ProductionBibleDbContextFactory : IDesignTimeDbContextFactory<ProductionBibleDbContext>
{
    public ProductionBibleDbContext CreateDbContext(string[] args)
    {
        var connectionString = "Data Source=design_time.db";
        var options = new DbContextOptionsBuilder<ProductionBibleDbContext>()
            .UseSqlite(connectionString)
            .Options;
        return new ProductionBibleDbContext(options);
    }
}

