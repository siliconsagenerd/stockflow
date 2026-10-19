using Microsoft.EntityFrameworkCore;
using StockFlow.Api.Models;

namespace StockFlow.Api.Data;

public class StockFlowDbContext : DbContext
{
    public StockFlowDbContext(DbContextOptions<StockFlowDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<DemoUser> DemoUsers => Set<DemoUser>();
    public DbSet<DemoWorkspace> DemoWorkspaces => Set<DemoWorkspace>();
    public DbSet<DemoSession> DemoSessions => Set<DemoSession>();
    public DbSet<PurchaseRequest> PurchaseRequests => Set<PurchaseRequest>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>().HasIndex(p => p.Sku).IsUnique();
        modelBuilder.Entity<PurchaseOrder>().HasIndex(o => o.PurchaseRequestId).IsUnique();
        modelBuilder.Entity<GoodsReceipt>().HasIndex(g => g.IdempotencyKey).IsUnique();

        modelBuilder.Entity<Product>()
            .HasOne(p => p.PreferredSupplier).WithMany()
            .HasForeignKey(p => p.PreferredSupplierId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<DemoSession>()
            .HasOne(s => s.Workspace).WithMany().HasForeignKey(s => s.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<DemoSession>().HasIndex(s => s.TokenHash).IsUnique();

        modelBuilder.Entity<PurchaseRequest>()
            .HasOne(r => r.Workspace).WithMany().HasForeignKey(r => r.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PurchaseRequest>()
            .HasOne(r => r.Product).WithMany().HasForeignKey(r => r.ProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PurchaseRequest>()
            .HasOne(r => r.Requester).WithMany().HasForeignKey(r => r.RequesterId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PurchaseRequest>()
            .HasOne(r => r.DecidedBy).WithMany().HasForeignKey(r => r.DecidedById).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(o => o.PurchaseRequest).WithOne(r => r.PurchaseOrder).HasForeignKey<PurchaseOrder>(o => o.PurchaseRequestId);
        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(o => o.Supplier).WithMany().HasForeignKey(o => o.SupplierId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(o => o.Warehouse).WithMany().HasForeignKey(o => o.WarehouseId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GoodsReceipt>()
            .HasOne(g => g.PurchaseOrder).WithMany().HasForeignKey(g => g.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<GoodsReceipt>()
            .HasOne(g => g.ReceivedBy).WithMany().HasForeignKey(g => g.ReceivedById).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockMovement>()
            .HasOne(m => m.Product).WithMany().HasForeignKey(m => m.ProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<StockMovement>()
            .HasOne(m => m.Warehouse).WithMany().HasForeignKey(m => m.WarehouseId).OnDelete(DeleteBehavior.Restrict);
    }
}
