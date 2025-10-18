using ChatWS.Entities;
using Microsoft.EntityFrameworkCore;

namespace PingWS.Data
{
    public class UserDbContext(DbContextOptions<UserDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
    }
}
