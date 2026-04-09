using Microsoft.EntityFrameworkCore;

using ProductStore.Api.Models;



namespace ProductStore.Api.Data;



public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)

{

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();



    protected override void OnModelCreating(ModelBuilder modelBuilder)

    {

        modelBuilder.Entity<Category>(e =>

        {

            e.HasKey(c => c.Id);

            e.Property(c => c.Name).HasMaxLength(128).IsRequired();

        });



        modelBuilder.Entity<Product>(e =>

        {

            e.HasKey(p => p.Id);

            e.Property(p => p.Sku).HasMaxLength(64).IsRequired();

            e.HasIndex(p => p.Sku).IsUnique();

            e.Property(p => p.Name).HasMaxLength(256).IsRequired();

            e.Property(p => p.Description).HasMaxLength(2000);

            e.Property(p => p.CosmosMetadataJson).HasColumnType("TEXT");

            e.Property(p => p.CosmosCommercialDescription).HasMaxLength(2000);

            e.Property(p => p.CosmosGtin).HasMaxLength(32);

            e.Property(p => p.CosmosThumbnailUrl).HasMaxLength(2048);

            e.Property(p => p.CosmosBrandName).HasMaxLength(256);

            e.Property(p => p.CosmosBrandPictureUrl).HasMaxLength(2048);

            e.Property(p => p.CosmosAvgPrice).HasPrecision(18, 2);

            e.Property(p => p.CosmosMaxPrice).HasPrecision(18, 2);

            e.Property(p => p.CosmosMinPrice).HasPrecision(18, 2);

            e.Property(p => p.CosmosPriceLabel).HasMaxLength(128);

            e.Property(p => p.CosmosNcmCode).HasMaxLength(16);

            e.Property(p => p.CosmosNcmDescription).HasMaxLength(2000);

            e.Property(p => p.CosmosGpcCode).HasMaxLength(64);

            e.Property(p => p.CosmosGpcDescription).HasMaxLength(2000);

            e.Property(p => p.Price).HasPrecision(18, 2);

            e.HasOne(p => p.Category)

                .WithMany(c => c.Products)

                .HasForeignKey(p => p.CategoryId)

                .OnDelete(DeleteBehavior.Restrict);

        });

    }

}

