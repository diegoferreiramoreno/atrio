using Microsoft.EntityFrameworkCore;

namespace Atrio.Infrastructure.Persistence;

public sealed class AtrioDbContext : DbContext
{
    public AtrioDbContext(DbContextOptions<AtrioDbContext> options)
        : base(options)
    {
    }
}
