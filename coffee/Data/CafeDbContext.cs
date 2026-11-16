using CafeDuaHau.Models;
using Microsoft.EntityFrameworkCore;

namespace CafeDuaHau.Data;

public class CafeDbContext(DbContextOptions<CafeDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<AdminAccount> AdminAccounts => Set<AdminAccount>();

    public DbSet<CustomerAccount> CustomerAccounts => Set<CustomerAccount>();

    public DbSet<MenuPhoto> MenuPhotos => Set<MenuPhoto>();
    public DbSet<MenuReview> MenuReviews => Set<MenuReview>();

    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminAccount>().Property(account => account.UserName).HasMaxLength(64);

        modelBuilder.Entity<CustomerAccount>(account =>
        {
            account.Property(customer => customer.UserName).HasMaxLength(64);
            account.Property(customer => customer.NormalizedUserName).HasMaxLength(64);
            account.HasIndex(customer => customer.NormalizedUserName).IsUnique();
            account.Property(customer => customer.PhoneNumber).HasMaxLength(10);
            account.Property(customer => customer.ExternalProvider).HasMaxLength(16);
            account.Property(customer => customer.ExternalSubject).HasMaxLength(255);
            account.HasIndex(customer => new { customer.ExternalProvider, customer.ExternalSubject }).IsUnique();
        });

        modelBuilder.Entity<MenuItem>(menu =>
        {
            menu.Property(item => item.Id).HasMaxLength(32);
            menu.HasData(MenuData.Items.Select(item => new
            {
                item.Id,
                item.Name,
                item.Description,
                item.CategoryId,
                item.Price,
                item.ImageUrl,
                item.IsHot,
                item.IsSoldOut
            }));
            menu.OwnsMany(item => item.Sizes, sizes =>
            {
                sizes.ToTable("MenuSizes");
                sizes.HasData(MenuData.Items.SelectMany(item => item.Sizes.Select((size, index) => new
                {
                    MenuItemId = item.Id,
                    Id = index + 1,
                    size.Label,
                    size.Price
                })));
            });
        });

        modelBuilder.Entity<MenuPhoto>(photo =>
        {
            photo.HasOne<MenuItem>().WithMany().HasForeignKey(image => image.MenuItemId);
            photo.HasIndex(image => new { image.MenuItemId, image.Position });
            photo.Property(image => image.ContentType).HasMaxLength(20);
        });

        modelBuilder.Entity<MenuReview>(review =>
        {
            review.HasKey(entry => new { entry.MenuItemId, entry.CustomerAccountId });
            review.HasOne<MenuItem>().WithMany().HasForeignKey(entry => entry.MenuItemId);
            review.HasOne<CustomerAccount>().WithMany().HasForeignKey(entry => entry.CustomerAccountId);
            review.Property(entry => entry.Comment).HasMaxLength(1000);
            review.ToTable(table => table.HasCheckConstraint("CK_MenuReviews_Rating", "[Rating] BETWEEN 1 AND 5"));
        });

        modelBuilder.Entity<ChatMessage>(chat =>
        {
            chat.HasOne<CustomerAccount>().WithMany().HasForeignKey(message => message.CustomerAccountId);
            chat.HasIndex(message => new { message.CustomerAccountId, message.Id });
            chat.Property(message => message.Text).HasMaxLength(1000);
        });

        modelBuilder.Entity<Order>(order =>
        {
            order.Property(o => o.Id).HasMaxLength(32);
            order.Property(o => o.CustomerKey).HasMaxLength(64);
            order.HasIndex(o => new { o.CustomerAccountId, o.CreatedAt });
            order.HasOne<CustomerAccount>().WithMany()
                .HasForeignKey(o => o.CustomerAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            order.OwnsMany(o => o.Items, items => items.ToTable("OrderItems"));
        });
    }
}
