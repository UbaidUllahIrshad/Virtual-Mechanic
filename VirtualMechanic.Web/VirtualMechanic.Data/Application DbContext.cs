using Microsoft.EntityFrameworkCore;
using VirtualMechanic.Core.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace VirtualMechanic.Data
{
   
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Mechanic> Mechanics { get; set; }
        public DbSet<ServiceRequest> ServiceRequests { get; set; }

       
        public DbSet<User> AppUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
          
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().ToTable("AppUsers");

            modelBuilder.Entity<ServiceRequest>()
                .HasOne(r => r.User)
                .WithMany(u => u.ServiceRequests)
                .HasForeignKey(r => r.UserId);

            modelBuilder.Entity<ServiceRequest>()
                .HasOne(r => r.Mechanic)
                .WithMany(m => m.AssignedRequests)
                .HasForeignKey(r => r.MechanicId)
                .IsRequired(false);
        }
    }
}