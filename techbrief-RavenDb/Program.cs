using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Client.Document;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace techbrief_RavenDb
{
    public class Genre
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class Album
    {
        public string Id { get; set; }
        public string AlbumArtUrl { get; set; }
        public string Title { get; set; }
        public int CountSold { get; set; }
        public double Price { get; set; }

        public AlbumGenre Genre { get; set; }
        public AlbumArtist Artist { get; set; }
    }

    public class AlbumGenre
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class AlbumArtist
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class Invoice
    {
        public DateTime DateUtc { get; set; }
        public List<LineItem> LineItems {get; set; }
    }

    public class LineItem
    {
        public LineItem(string productName, double unitCost, int quantity)
        {
            ProductName = productName;
            Quantity = quantity;
            UnitCost = unitCost;
            LineItemCost = Quantity * UnitCost;
        }

        public string ProductName { get; protected set; }
        public int Quantity { get; protected set; }
        public double UnitCost { get; protected set; }
        public double LineItemCost { get; protected set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            IDocumentStore ravenStore = new DocumentStore() { DefaultDatabase = "music-simple", Url = "http://localhost:8080" };
            ravenStore.Initialize();

            #region Empty Database (Pay no attention to the man behind the curtain)
            // delete all existing documents
            ravenStore.DatabaseCommands.DeleteByIndex("Raven/DocumentsByEntityName",
            new IndexQuery
            {
                Query = "Tag:*"
            }, allowStale: false);

            // Delete the hilo docs to reset numbering
            ravenStore.DatabaseCommands.Delete("Raven/Hilo/genres", null);
            ravenStore.DatabaseCommands.Delete("Raven/Hilo/albums", null);
            ravenStore.DatabaseCommands.Delete("Raven/Hilo/artists", null);
            #endregion

            #region Introduction to Storing and Retrieving typed objects
            // Raven implements the unit of work pattern with IDocumentSession
            using (IDocumentSession session = ravenStore.OpenSession())
            {
                // Genre has an Id property, but we don't set it (Raven will do it for us)
                Genre rock = new Genre()
                {
                  Name = "Rock",
                  Description = "Rock and Roll is a form of rock music developed in the 1950s and 1960s. Rock music combines many kinds of music from the United States, such as country music, folk music, church music, work songs, blues and jazz."
                };

                // Passing the object into the session's Store method - you guessed it - stores the document
                // signatures: Store(dynamic toStore), Store(object toStore)
                session.Store(rock);

                // We can see in the console that we haven't actually communicated with the RavenDb service yet.
                
                // What do you think the ID of the Genre we just added is?


                Genre jazz = new Genre()
                {
                    Name = "Jazz",
                    Description = "Jazz is a type of music which was invented in the United States. Jazz music combines African-American music with European music. Some common jazz instruments include the saxophone, trumpet, piano, double bass, and drums."
                };
                session.Store(jazz);

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
                var loadedRock = session.Load<Genre>("genres/1");

                // Will query return the Genre?
                var queriedRock = session.Query<Genre>()
                    .Where(g => g.Name == "Rock")
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
                queriedRock = session.Query<Genre>()
                    .Where(g => g.Name == "Rock")
                    .FirstOrDefault();

                // Queries are executed against the Lucene indexes that RavenDb maintains.  Even though entities can be loaded immediately after
                // they've been stored in the session, they won't be included in queries until they've been saved to the RavenDb server.  Promotes
                // proper unit of work, IMO.
            }
            #endregion

            #region References

            // As a document store, the prefered mechanism is embedding references; simply store a copy of the data with the entity being stored.
            // Works great for static entities beneath an "aggregate root".  For example, a line item of an invoice -> Invoice is aggregate root, and it
            // contains many line items. Line items are immutable (cost of an item on an invoice won't change over time), and line items would only be
            // accessed via the invoice, not directly. No need to have a separate line item document; embed it right in the invoice
            // Convenient, but not always appropriate.  Ex: Referenced data changes frequently; would need to update all copies everywhere.
            
           



            #endregion

        }
    }
}