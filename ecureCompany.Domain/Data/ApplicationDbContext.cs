using ecureCompany.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using SecureCompany.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace SecureCompany.Domain.Data
{
    public class ApplicationDbContext :
        IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options
        ) : base(options) { }

        public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(rt => rt.Id);

                entity.Property(rt => rt.TokenHash)
                .IsRequired()
                .HasMaxLength(64);

                entity.HasIndex(rt => rt.TokenHash)
                .IsUnique();

                entity.Property(rt => rt.UserId)
                .IsRequired();

                entity.HasIndex(rt => rt.UserId);

                entity.HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(u => u.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}