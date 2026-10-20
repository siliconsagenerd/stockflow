using Microsoft.EntityFrameworkCore;
using StockFlow.Api.Models;

namespace StockFlow.Api.Data;

public static class SeedData
{
    public static async Task SeedAsync(StockFlowDbContext db)
    {
        if (await db.Products.AnyAsync()) return;

        var supplierA = new Supplier { Id = Guid.NewGuid(), Name = "Nordic Industrial Supply", ContactEmail = "orders@nordic.example" };
        var supplierB = new Supplier { Id = Guid.NewGuid(), Name = "Rhein Filtertechnik", ContactEmail = "vertrieb@rheinfilter.example" };
        db.Suppliers.AddRange(supplierA, supplierB);

        var warehouse = new Warehouse { Id = Guid.NewGuid(), Name = "Hauptlager Saarbrücken", Location = "Saarbrücken, DE" };
        db.Warehouses.Add(warehouse);

        var employee = new DemoUser { Id = Guid.NewGuid(), DisplayName = "Anna Beispiel (Employee)", Role = "Employee" };
        var manager = new DemoUser { Id = Guid.NewGuid(), DisplayName = "Markus Beispiel (Manager)", Role = "Manager" };
        var warehouseOp = new DemoUser { Id = Guid.NewGuid(), DisplayName = "Werner Beispiel (Warehouse)", Role = "Warehouse" };
        db.DemoUsers.AddRange(employee, manager, warehouseOp);

        var products = new[]
        {
            new Product { Id = Guid.NewGuid(), Sku = "FIL-100", Name = "Replacement Filter F-100", Unit = "pcs", PreferredSupplierId = supplierB.Id, CurrentStock = 24 },
            new Product { Id = Guid.NewGuid(), Sku = "GAS-220", Name = "Sealing Gasket G-220", Unit = "pcs", PreferredSupplierId = supplierA.Id, CurrentStock = 0 },
            new Product { Id = Guid.NewGuid(), Sku = "BLT-M8", Name = "Hex Bolt M8 (box of 50)", Unit = "box", PreferredSupplierId = supplierA.Id, CurrentStock = 6 },
            new Product { Id = Guid.NewGuid(), Sku = "LUB-5L", Name = "Machine Lubricant 5L", Unit = "can", PreferredSupplierId = supplierA.Id, CurrentStock = 3 },
            new Product { Id = Guid.NewGuid(), Sku = "BELT-V", Name = "Drive Belt Type V", Unit = "pcs", PreferredSupplierId = supplierB.Id, CurrentStock = 0 },
        };
        db.Products.AddRange(products);

        await db.SaveChangesAsync();
    }
}
