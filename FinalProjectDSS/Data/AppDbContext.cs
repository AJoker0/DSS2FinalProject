using FinalProjectDSS.Models;
using Microsoft.EntityFrameworkCore;

namespace FinalProjectDSS.Data
{
    public class AppDbContext : DbContext
    {
        // constrcutor that takes settings to connection
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // this properties will be used to create tables in the database
        public DbSet<User> Users { get; set; }
        public DbSet<TodoItem> Todos { get; set; }

        // Here we set additional rules (restrictions)
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Email of user must be unique
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

            // Title is neccesery and max 100 characters
            modelBuilder.Entity<TodoItem>().Property(t => t.Title).IsRequired().HasMaxLength(100);

            //Details max 1000 characters
            modelBuilder.Entity<TodoItem>().Property(t => t.Details).HasMaxLength(1000);

            // Connectivity (one to many) (one user can have many tasks)
            modelBuilder.Entity<TodoItem>().HasOne(t => t.User).WithMany(u => u.Todos).HasForeignKey(t => t.UserId);


        }
    }
}
