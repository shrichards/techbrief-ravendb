using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Indexes;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace techbrief_RavenDb
{
     public class Invoice
    {
         public Invoice()
         {
             DateUtc = DateTime.UtcNow;
         }
        public DateTime DateUtc { get; set; }
        public List<LineItem> LineItems {get; set; }
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

    
    class Program
    {
        static string databaseName = "invoice-sample";

        static void Main(string[] args)
        {
            IDocumentStore ravenStore = new DocumentStore() { DefaultDatabase = databaseName, Url = "http://localhost:8080" };
            ravenStore.Initialize();

            #region Empty Database (Pay no attention to the man behind the curtain)
            // delete all existing documents
            ravenStore.DatabaseCommands.DeleteByIndex("Raven/DocumentsByEntityName",
            new IndexQuery
            {
                Query = "Tag:*"
            }, allowStale: false);

            // Delete the hilo docs to reset numbering
            ravenStore.DatabaseCommands.Delete("Raven/Hilo/products", null);
            ravenStore.DatabaseCommands.Delete("Raven/Hilo/invoices", null);
            ravenStore.DatabaseCommands.Delete("Raven/Hilo/employees", null);

            ravenStore.DatabaseCommands.DeleteIndex("Products/BySupplierName");
            #endregion

            #region Introduction to Storing and Retrieving typed objects
            // Raven implements the unit of work pattern with IDocumentSession
            using (IDocumentSession session = ravenStore.OpenSession())
            {
                // Product has an Id property, but we don't set it (Raven will do it for us)
                Product widget = new Product()
                {
                    Name = "Widget",
                    UnitCost = 3.49,
                    QuantityOnHand = 1000,
                    SupplierName = "Widgets 'r Us",
                    LastOrderDate = DateTime.UtcNow.AddDays(-33),
                    WarehouseAddress = "490 Boston Post Rd, Sudbury, MA"
                };

                // Passing the object into the session's Store method - you guessed it - stores the document
                // signatures: Store(dynamic toStore), Store(object toStore)
                session.Store(widget);

                // We can see in the console that we haven't actually communicated with the RavenDb service yet.
                
                // What do you think the ID of the Product we just added is?

                Product whatzit = new Product()
                {
                    Name = "Whatzit",
                    UnitCost = 44.99,
                    QuantityOnHand = 180,
                    SupplierName = "House of Whatzits",
                    LastOrderDate = DateTime.UtcNow.AddDays(-19),
                    WarehouseAddress = "490 Boston Post Rd, Sudbury, MA"
                };
                session.Store(whatzit);

                // We still haven't send any data to the RavenDb service yet.

                // By default, RavenDb uses the HiLo algorithm to provide each session with a set of ids that can be freely used.
                // A new set of Ids is negotiated between the session and the server if a session's batch is expended.

                // Default Id format is "collectionName/docNumber"; Raven default is to use the plural entity type name as the collection name
                // "Collection" is only a convention, not an enforced rule.
                // completely possible for a Genre to be stored with Id "artists/99" that will be presented in the artists collection
                // RavenDb _only stores documents_, as such collection is not a first class citizen in Raven.
                // collection is a convenience that groups _potentially_ similar documents within the management studio (can also be used to query for docs)

                // Id can also be numeric/Guid.  Strong-typed in C# entity, RavenDb automatically does conversion to/from string when ser/des
                
                // Id can be assigned to any string value.  Can also plug-in custom id generators.

                // Can we load by Id?
                var loadedWidget = session.Load<Product>("products/1");

                // Will query return the Product?
                var queriedWidget = session.Query<Product>()
                    .Where(g => g.Name == "Widget")
                    .FirstOrDefault();

         
                // SaveChanges makes a single remote call write the batch to the RavenDb server
                session.SaveChanges();


                // Session is "safe by default":
                //      * If a page size of query isn't specified (think Linq's "Take(x)" method), max of 128 results (configurable) are returned. 
                //        Hard limit of 1024 (configurable) set on the server
                //      * A session is limited to 30 remote calls (configurable).  If RavenDb is being used "properly", the number of remote calls
                //        executed within a session should be as close to 1 as possible.  If more than 30 calls are being made, the code is probably
                //        in an N+1 situation


                // Different results once changes have been saved?
                queriedWidget = session.Query<Product>()
                    .Where(g => g.Name == "Widget")
                    .FirstOrDefault();

                // Queries are executed against the Lucene indexes that RavenDb maintains.  Even though entities can be loaded immediately after
                // they've been stored in the session, they won't be included in queries until they've been saved to the RavenDb server.  Promotes
                // proper unit of work, IMO.
            }
            #endregion

            #region References

            // As a document store, the prefered mechanism is embedding references; simply store a copy of the data with the entity being stored.

            // This works great for unchanging entities beneath an "aggregate root".  For example, a line item of an invoice.  The invoice is the 
            // aggregate root, and it contains many line items.
 
            // Line items are immutable (cost of an item on an invoice won't change over time), and line items would only be
            // accessed via the invoice, not directly. No need to have a separate line item document; embed it right in the invoice.

            // Convenient, but not always appropriate.  Ex: Referenced data changes frequently; would need to update all copies everywhere.
            // Also, perhaps the aggregate root only requires a subset of the data; perhaps just enough to display on a web page.
            // A line item is generated for the sale of a product.  Product entities could track a lot of information (supplier, order history,
            // warehouse location, etc) that an invoice doesn't directly care about.  In this situation, the line item could contain a reference
            // to the Product, and keep a copy of the product's  Name and Unit Cost.  
 
            // This way, the Invoice has enough information about the product to display the line item, but because it also has the Id reference,
            // the Product can be loaded if additional details are required.

            using (IDocumentSession session = ravenStore.OpenSession())
            {
                var invoice = new Invoice()
                {
                    DateUtc = DateTime.UtcNow,
                    LineItems = new List<LineItem>()
                };

                // Load all the products
                var products = session.Load<Product>(1, 2) // Applies conventions for the entity to generate the full id
                              .ToList();

                // create a line item for each product in this invoice
                invoice.LineItems.Add(new LineItem(products[0], 11));
                invoice.LineItems.Add(new LineItem(products[1], 3));

                session.Store(invoice);
                session.SaveChanges();
            }

            using (IDocumentSession session = ravenStore.OpenSession())
            {
                var invoice = session.Load<Invoice>("invoices/1");

                // Each line-item in the invoice has basic product data, as well as the product id
                // Using the product ids, we can get the products
                var prods = new List<Product>();
                foreach (var lineItem in invoice.LineItems)
                {
                    prods.Add(session.Load<Product>(lineItem.ProductId));
                }
                
                // There's that N+1 situation we've been trying to avoid. We can do better
            }

            using(IDocumentSession session = ravenStore.OpenSession())
            {
                var invoice = session.Load<Invoice>("invoices/1");

                var products = session.Load<Product>(invoice.LineItems.Select(li => li.ProductId)).ToList();

                // 2 calls to RavenDb.  No more N+1, but... we can do even better
            }

            using (IDocumentSession session = ravenStore.OpenSession())
            {
                var invoice = session
                    .Include<Invoice>(i => i.LineItems.Select(li => li.ProductId))
                    .Load("invoices/1");

                var products = session.Load<Product>(invoice.LineItems.Select(li => li.ProductId)).ToList();

                // Invoice and related products loaded in one remote call
            }

            // Unlike EF (and other ORMs), there's no concept of a navigation property.  RavenDb forces the dev to explicitly
            // load related entities.
            #endregion

            #region Dynamic Entities

            using (IDocumentSession session = ravenStore.OpenSession())
            {
                // We added a document with Id manual/1 in Raven Studio.
                dynamic empl = new ExpandoObject();
                empl.FirstName = "Seth";
                empl.LastName = "Richards";
                empl.Id = "employees/1";

                session.Store(empl);
                session.SaveChanges();
            }

            using (IDocumentSession session = ravenStore.OpenSession())
            {
                dynamic empl = session.Load<dynamic>("employees/1");
                session.Delete<dynamic>(empl);
                session.SaveChanges();
            }
            #endregion

            #region Indexes
            // Executing a query against a session generates a temporary Dynamic Index.
            // The first time a query is executed, a Lucene index is created.  This can be an
            // _expensive_ process, depending on complexity and document volume.
            // If the same temporary index is queried enough times, Raven will automatically
            // promote it to a permanent index.

            // Static indexes are defined explicitly and provide far more capability.  Because they're
            // pre-defined, they can also greatly reduce (or eliminate) the latency encountered
            // on first query.
            ravenStore.DatabaseCommands.PutIndex("Products/BySupplierName",
                new IndexDefinitionBuilder<Product>{
                    Map = prods => from prod in prods
                                   select new { SupplierName = prod.SupplierName }
                });

            using (IDocumentSession session = ravenStore.OpenSession())
            {
                var fromWidgets = session.Query<Product>("Products/BySupplierName")
                                    .Where(x => x.SupplierName == "Widgets 'r Us")
                                    .ToList();
            }

            // Can also aggregate data using map/reduce
            // Add a few more invoices
            using (IDocumentSession session = ravenStore.OpenSession())
            {
                var prods = session.Load<Product>(1, 2).ToList();

                session.Store(
                    new Invoice()
                    {
                        LineItems = new List<LineItem>(){ new LineItem(prods[0], 7), new LineItem(prods[1], 1)}
                    }
                );

                session.Store(
                    new Invoice()
                    {
                        LineItems = new List<LineItem>() { new LineItem(prods[0], 11) }
                    }
                );

                session.Store(
                    new Invoice()
                    {
                        LineItems = new List<LineItem>() { new LineItem(prods[1], 5) }
                    }
                );
                session.SaveChanges();
            }

            IndexCreation.CreateIndexes(typeof(InvoicedProductTotals).Assembly, ravenStore);
            using (IDocumentSession session = ravenStore.OpenSession())
            {

            }
            #endregion
        }
    }

    public class ProductTotals
    {
        public string ProductId { get; set; }
        public string ProductName{ get; set; }
        public int TotalUnitsSold{ get; set; }
        public double TotalSaleCost { get; set; } 
    }

    public class InvoicedProductTotals : AbstractMultiMapIndexCreationTask<ProductTotals>
    {
        public InvoicedProductTotals()
            : base()
        {
            
            AddMap<Product>(products => from prod in products
                                        select new
                                        {
                                            ProductId = prod.Id,
                                            ProductName = prod.Name,
                                            TotalUnitsSold = 0,
                                            TotalSaleCost = 0.0
                                        });

            AddMap<Invoice>(invoices => from inv in invoices
                                        from lineItem in inv.LineItems
                                        select new
                                        {
                                            ProductId = lineItem.ProductId,
                                            ProductName = lineItem.ProductName,
                                            TotalUnitsSold = lineItem.Quantity,
                                            TotalSaleCost = lineItem.LineItemCost
                                        });

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

            Index(x => x.ProductId, Raven.Abstractions.Indexing.FieldIndexing.NotAnalyzed);
            Index(x => x.ProductName, Raven.Abstractions.Indexing.FieldIndexing.Analyzed);
        }
    }
}