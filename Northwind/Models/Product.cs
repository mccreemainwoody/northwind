using System;
using System.Collections.Generic;

namespace Northwind.Models
{
    public partial class Product
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public int? SupplierId { get; set; }
        public int? CategoryId { get; set; }
        public string? QuantityPerUnit { get; set; }
        public decimal? UnitPrice { get; set; }
        public int? UnitsInStock { get; set; }
        public int? UnitsOnOrder { get; set; }
        public int? ReorderLevel { get; set; }
        public bool? Discontinued { get; set; }
        
        public virtual Category? CategoryIdNavigation { get; set; }
        public virtual Supplier? SupplierIdNavigation { get; set; }

        public override string ToString() => $"{ProductId} - {ProductName}";
    }
}
