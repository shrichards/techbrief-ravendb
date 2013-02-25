using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace techbrief_RavenDb
{
    
    public class Product
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double UnitCost { get; set; }
        public int QuantityOnHand { get; set; }
        public DateTime LastOrderDate { get; set; }
        public string SupplierName { get; set; }
        public string WarehouseAddress { get; set; }
    }


    public class Invoice
    {
        public Invoice()
        {
            DateUtc = DateTime.UtcNow;
        }
        public DateTime DateUtc { get; set; }
        public List<LineItem> LineItems { get; set; }
    }

    public class LineItem
    {
        public LineItem()
        {

        }

        public LineItem(Product product, int quantity)
        {
            ProductId = product.Id;
            ProductName = product.Name;
            ProductUnitCost = product.UnitCost;

            Quantity = quantity;
            LineItemCost = Quantity * ProductUnitCost;
        }
        public string ProductId { get; set; }
        public string ProductName { get; protected set; }
        public double ProductUnitCost { get; protected set; }

        public int Quantity { get; protected set; }
        public double LineItemCost { get; protected set; }
    }
}
