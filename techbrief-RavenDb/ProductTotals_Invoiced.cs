using Raven.Abstractions.Indexing;
using Raven.Client.Indexes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace techbrief_RavenDb
{
    public class ProductTotals
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public int TotalUnitsSold { get; set; }
        public double TotalSaleCost { get; set; }
    }

    public class ProductTotals_Invoiced : AbstractIndexCreationTask<Invoice, ProductTotals>
    {
        public ProductTotals_Invoiced()
            : base()
        {


            Map = invoices => from inv in invoices
                              from lineItem in inv.LineItems
                              select new
                              {
                                  ProductId = lineItem.ProductId,
                                  ProductName = lineItem.ProductName,
                                  TotalUnitsSold = lineItem.Quantity,
                                  TotalSaleCost = lineItem.LineItemCost
                              };

            Reduce = results => from res in results
                                group res by res.ProductId
                                    into g
                                    select new
                                    {
                                        ProductId = g.Key,
                                        ProductName = g.Select(x => x.ProductName).First(),
                                        TotalUnitsSold = g.Sum(x => x.TotalUnitsSold),
                                        TotalSaleCost = g.Sum(x => x.TotalSaleCost)
                                    };

            Index(x => x.ProductId, FieldIndexing.NotAnalyzed);
            Index(x => x.ProductName, FieldIndexing.Analyzed);
            Index(x => x.TotalSaleCost, FieldIndexing.NotAnalyzed);
        }
    }

}
