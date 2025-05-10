using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LavenderLine.Data
{
    public class EcommerceContext(DbContextOptions<EcommerceContext> options) : IdentityDbContext<ApplicationUser>(options)
    {

        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<CarouselImage> Carousels { get; set; }
        public DbSet<InstagramPost> InstagramPosts { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<NewsletterSubscription> Newsletters { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<OrderHistory> OrderHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            /*  modelBuilder.Entity<OrderItem>()
                  .Property(oi => oi.Price)
                  .HasPrecision(5, 2);*/


            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique()
                .HasFilter(null); 


            // ApplicationUser configuration
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.Orders)
                .WithOne(o => o.User)
                .HasForeignKey(o => o.UserId);



            // Category configuration
            modelBuilder.Entity<Category>()
              .HasMany(c => c.Products)
              .WithOne(p => p.Category)
              .HasForeignKey(p => p.CategoryId)
              .OnDelete(DeleteBehavior.Cascade); 

            // Product configuration
            modelBuilder.Entity<Product>()
                .HasMany(p => p.OrderItems)
                .WithOne(oi => oi.Product)
                .HasForeignKey(oi => oi.ProductId);




            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId);
              

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Payment)
                .WithOne(p => p.Order)
                 .HasForeignKey<Payment>(p => p.OrderId)
                 .OnDelete(DeleteBehavior.Cascade);


        
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.User)
                .WithMany(u => u.Payments)
                .HasForeignKey(p => p.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.ClientSetNull);


            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique();


            modelBuilder.Entity<WishlistItem>()
           .HasKey(w => w.WishlistItemId);

            modelBuilder.Entity<WishlistItem>()
                .HasOne(w => w.User)
                .WithMany(u => u.Wishlists)
                .HasForeignKey(w => w.UserId);


            modelBuilder.Entity<WishlistItem>()
                .HasOne(w => w.Product)
                .WithMany()
                .HasForeignKey(w => w.ProductId);



        }

    }
}
