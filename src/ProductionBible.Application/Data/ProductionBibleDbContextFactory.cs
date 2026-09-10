using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProductionBible.Application.Data;

public class ProductionBibleDbContextFactory : IDesignTimeDbContextFactory<ProductionBibleDbContext>
{
    public ProductionBibleDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ProductionBibleDbContext>()
            .UseSqlite("Data Source=design_time.db")
            .Options;
        return new ProductionBibleDbContext(options);
    }
}

