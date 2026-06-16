using System.Data.Entity;
using LuxuryCar.Identity;
using LuxuryCar.Models;
using Microsoft.AspNet.Identity.EntityFramework;

namespace LuxuryCar.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext() : base("DefaultConnection", throwIfV1Schema: false)
    {
    }

    public static ApplicationDbContext Create()
    {
        return new ApplicationDbContext();
    }

    public DbSet<Airport> Airports { get; set; }
    public DbSet<CarVehicleType> CarVehicleTypes { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<CarBookingAddon> CarBookingAddons { get; set; }
    public DbSet<CarBookingAddonSelection> CarBookingAddonSelections { get; set; }
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
    public DbSet<MediaAsset> MediaAssets { get; set; }
    public DbSet<AppSetting> AppSettings { get; set; }
    public DbSet<CmsPage> CmsPages { get; set; }
    public DbSet<CmsPageTranslation> CmsPageTranslations { get; set; }
    public DbSet<BlogPost> BlogPosts { get; set; }
    public DbSet<BlogPostTranslation> BlogPostTranslations { get; set; }
    public DbSet<RedirectRule> RedirectRules { get; set; }
    public DbSet<EmailLog> EmailLogs { get; set; }
    public DbSet<BookingCounter> BookingCounters { get; set; }
    public DbSet<PopularRoute> PopularRoutes { get; set; }

    protected override void OnModelCreating(DbModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Airport>().HasIndex(x => x.Code).IsUnique();
        builder.Entity<CmsPage>().HasIndex(x => x.Slug).IsUnique();
        builder.Entity<CmsPage>().HasIndex(x => x.PublicPath).IsUnique();
        builder.Entity<RedirectRule>().HasIndex(x => x.SourcePath).IsUnique();
        builder.Entity<BlogPostTranslation>().HasIndex(x => new { x.Culture, x.Slug }).IsUnique();
        builder.Entity<CmsPageTranslation>().HasIndex(x => new { x.CmsPageId, x.Culture }).IsUnique();
        builder.Entity<Booking>().HasIndex(x => x.BookingNumber).IsUnique();
        builder.Entity<AppSetting>().HasIndex(x => x.Key).IsUnique();
        builder.Entity<MediaAsset>().HasIndex(x => x.PublicId);

        builder.Entity<Booking>().ToTable("Bookings");
        builder.Entity<CarVehicleType>().ToTable("CarVehicleTypes");
        builder.Entity<CarBookingAddon>().ToTable("CarBookingAddons");
        builder.Entity<CarBookingAddonSelection>().ToTable("CarBookingAddonSelections");

        builder.Entity<BlogPostTranslation>()
            .HasRequired(x => x.BlogPost)
            .WithMany(x => x.Translations)
            .HasForeignKey(x => x.BlogPostId)
            .WillCascadeOnDelete(true);
        builder.Entity<BlogPost>()
            .HasOptional(x => x.FeaturedMediaAsset)
            .WithMany()
            .HasForeignKey(x => x.FeaturedMediaAssetId)
            .WillCascadeOnDelete(false);
        builder.Entity<BlogPost>()
            .HasOptional(x => x.OgMediaAsset)
            .WithMany()
            .HasForeignKey(x => x.OgMediaAssetId)
            .WillCascadeOnDelete(false);
        builder.Entity<BlogPostTranslation>()
            .HasOptional(x => x.OgMediaAsset)
            .WithMany()
            .HasForeignKey(x => x.OgMediaAssetId)
            .WillCascadeOnDelete(false);
        builder.Entity<CmsPageTranslation>()
            .HasRequired(x => x.CmsPage)
            .WithMany(x => x.Translations)
            .HasForeignKey(x => x.CmsPageId)
            .WillCascadeOnDelete(true);
        builder.Entity<CmsPage>()
            .HasOptional(x => x.FeaturedMediaAsset)
            .WithMany()
            .HasForeignKey(x => x.FeaturedMediaAssetId)
            .WillCascadeOnDelete(false);
        builder.Entity<CmsPage>()
            .HasOptional(x => x.OgMediaAsset)
            .WithMany()
            .HasForeignKey(x => x.OgMediaAssetId)
            .WillCascadeOnDelete(false);
        builder.Entity<CmsPageTranslation>()
            .HasOptional(x => x.OgMediaAsset)
            .WithMany()
            .HasForeignKey(x => x.OgMediaAssetId)
            .WillCascadeOnDelete(false);
        builder.Entity<CarVehicleType>()
            .HasOptional(x => x.Image)
            .WithMany()
            .HasForeignKey(x => x.MediaAssetId)
            .WillCascadeOnDelete(false);
        builder.Entity<CarBookingAddon>()
            .HasOptional(x => x.Image)
            .WithMany()
            .HasForeignKey(x => x.MediaAssetId)
            .WillCascadeOnDelete(false);
        builder.Entity<CarBookingAddonSelection>()
            .HasRequired(x => x.Booking)
            .WithMany(x => x.CarAddonSelections)
            .HasForeignKey(x => x.BookingId)
            .WillCascadeOnDelete(true);
        builder.Entity<CarBookingAddonSelection>()
            .HasRequired(x => x.CarBookingAddon)
            .WithMany()
            .HasForeignKey(x => x.CarBookingAddonId)
            .WillCascadeOnDelete(false);
        builder.Entity<PopularRoute>()
            .HasOptional(x => x.Image)
            .WithMany()
            .HasForeignKey(x => x.MediaAssetId)
            .WillCascadeOnDelete(false);

        builder.Entity<CarVehicleType>().Property(x => x.BaseFareUsd).HasPrecision(10, 2);
        builder.Entity<CarVehicleType>().Property(x => x.PricePerKmUsd).HasPrecision(10, 2);
        builder.Entity<CarBookingAddon>().Property(x => x.PriceUsd).HasPrecision(10, 2);
        builder.Entity<CarBookingAddonSelection>().Property(x => x.PriceUsd).HasPrecision(10, 2);
        builder.Entity<CarBookingAddonSelection>().Property(x => x.UnitPriceUsd).HasPrecision(10, 2);
        builder.Entity<Booking>().Property(x => x.DistanceKm).HasPrecision(10, 2);
        builder.Entity<Booking>().Property(x => x.EstimatedPriceUsd).HasPrecision(10, 2);
        builder.Entity<Booking>().Property(x => x.BasePriceUsd).HasPrecision(10, 2);
        builder.Entity<Booking>().Property(x => x.AddonTotalUsd).HasPrecision(10, 2);
        builder.Entity<Booking>().Property(x => x.DiscountUsd).HasPrecision(10, 2);
        builder.Entity<Booking>().Property(x => x.TaxFeeUsd).HasPrecision(10, 2);
        builder.Entity<Booking>().Property(x => x.TotalPriceUsd).HasPrecision(10, 2);
        builder.Entity<PaymentTransaction>().Property(x => x.AmountUsd).HasPrecision(10, 2);
    }
}
