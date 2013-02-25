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
using Raven.Abstractions.Indexing;

namespace techbrief_RavenDb
{
    public class Supplier
    {
        public string Id { get; set; }
        public List<SupplierAttribute> Attributes { get; set; }
    }

    public class SupplierAttribute
    {
        public SupplierAttribute(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; set; }
        public string Value { get; set; }
    }

    // http://ravendb.net/docs/2.0/client-api/advanced/dynamic-fields

    public class Suppliers_ByAttribute : AbstractIndexCreationTask<Supplier>
    {
        public Suppliers_ByAttribute()
        {
            Map = suppliers => from s in suppliers
                               select new
                               {
                                   _ = s.Attributes
                                      .Select(attribute =>
                                          // Name, value, stored, analyzed
                                          CreateField(attribute.Key, attribute.Value, false, true))
                               };
        }
    }
}
