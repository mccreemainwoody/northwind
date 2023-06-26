using System;
using System.Collections.Generic;

namespace Northwind.Models
{
    public partial class Orderdetail
    {
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public float Discount { get; set; }
        
        public virtual Order? OrderIdNavigation { get; set; }
        public virtual Product? ProductIdNavigation { get; set; }

        public override string ToString() => $"{OrderId} - {Quantity} x {ProductId} - {UnitPrice}";
    }
}
