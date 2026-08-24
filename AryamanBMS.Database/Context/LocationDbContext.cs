using AryamanBMS.Models;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Database.Context
{
    public class LocationDbContext : DbContext
    {
        public LocationDbContext(
            DbContextOptions<LocationDbContext> options)
            : base(options)
        {
        }

        public DbSet<CountryModel> Countries { get; set; }
        public DbSet<StateModel> States { get; set; }
        public DbSet<CityModel> Cities { get; set; }
        public DbSet<PincodeModel> Pincodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CountryModel>()
                .ToTable("TableCountry");

            modelBuilder.Entity<CountryModel>()
                .HasIndex(x => x.Iso2Code)
                .IsUnique();

            modelBuilder.Entity<CountryModel>()
                .HasIndex(x => x.Iso3Code)
                .IsUnique();

            modelBuilder.Entity<CountryModel>()
                .HasIndex(x => x.CountryName)
                .IsUnique();

            modelBuilder.Entity<StateModel>()
                .ToTable("TableState");

            modelBuilder.Entity<StateModel>()
                .HasIndex(x => x.StateName)
                .IsUnique();

            modelBuilder.Entity<CityModel>()
                .ToTable("TableCity");

            modelBuilder.Entity<CityModel>()
                .HasIndex(x => new
                {
                    x.StateId,
                    x.CityName
                })
                .IsUnique();

            modelBuilder.Entity<CityModel>()
                .HasOne(x => x.State)
                .WithMany(x => x.Cities)
                .HasForeignKey(x => x.StateId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PincodeModel>()
                .ToTable("TablePincode");

            modelBuilder.Entity<PincodeModel>()
                .HasOne(x => x.City)
                .WithMany(x => x.Pincodes)
                .HasForeignKey(x => x.CityId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}