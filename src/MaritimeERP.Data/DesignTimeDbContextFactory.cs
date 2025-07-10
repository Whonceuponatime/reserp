using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MaritimeERP.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MaritimeERPContext>
    {
        public MaritimeERPContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<MaritimeERPContext>();
            optionsBuilder.UseSqlite("Data Source=maritime_erp.db");

            return new MaritimeERPContext(optionsBuilder.Options);
        }
    }
} 