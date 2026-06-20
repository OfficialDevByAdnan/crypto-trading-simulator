using CryptoSim.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CryptoSim.Core.Entities;

namespace CryptoSim.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<CryptoPrice> CryptoPrices { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Portfolio> Portfolios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unique constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Seed initial data
            modelBuilder.Entity<CryptoPrice>().HasData(
                new CryptoPrice
                {
                    Id = 1,
                    Symbol = "BTC",
                    Name = "Bitcoin",
                    Price = 45000m,
                    Change24h = 2.5m,
                    Volume24h = 25000000000m,
                    MarketCap = 850000000000L
                },
                new CryptoPrice
                {
                    Id = 2,
                    Symbol = "ETH",
                    Name = "Ethereum",
                    Price = 3200m,
                    Change24h = 3.2m,
                    Volume24h = 15000000000m,
                    MarketCap = 380000000000L
                }
            );
        }
    }
}
