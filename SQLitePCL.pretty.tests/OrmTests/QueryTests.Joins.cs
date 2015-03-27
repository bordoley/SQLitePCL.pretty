using System;

using SQLitePCL.pretty.Orm;
using SQLitePCL.pretty.Orm.Attributes;

using NUnit.Framework;
using RS = SQLitePCL.pretty.Orm.ResultSet;
using System.Linq;

namespace SQLitePCL.pretty.tests
{
    public partial class SqlQueryTests
    {
        public class EmployedBy
        {
            [PrimaryKey]
            public long? Id { get; set; }

            [ForeignKey(typeof(Person))]
            [Indexed(true)]
            public long EmployeeId { get; set; }

            [ForeignKey(typeof(Business))]
            public long BusinessId { get; set; }
        }

        public class Business
        {
            [PrimaryKey]
            public long? Id { get; set; }

            public string Name { get; set; }

            [ForeignKey(typeof(Address))]
            public long AddressId { get; set; }

            public Uri WebSite { get; set; }
        }

        public class Person
        {
            [PrimaryKey]
            public long? Id { get; set; }

            public string FirstName { get; set; }

            public string LastName { get; set; }

            [ForeignKey(typeof(Address))]
            public long AddressId { get; set; }

            public DateTime Birthday { get; set; }
        }

        public class Address
        {
            [PrimaryKey]
            public long? Id { get; set; }
            public uint ZipCode { get; set; }
        }

        [Test]
        public void TestJoins()
        {
            var addressSelector = RS.RowToObject<Address>();
            var personSelector = RS.RowToObject<Person>();
            var businessSelector = RS.RowToObject<Business>();
            var employedBySelector = RS.RowToObject<EmployedBy>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable<Address>();
                db.InitTable<Person>();
                db.InitTable<Business>();
                db.InitTable<EmployedBy>();

                var addresses =
                    db.InsertOrReplaceAll(
                        new Address[]
                        {
                            new Address() { ZipCode = 12345 },
                            new Address() { ZipCode = 67890 }
                        }, addressSelector).Values.ToList();

                var people =
                    db.InsertOrReplaceAll(
                        new Person[]
                        {
                            new Person() { FirstName = "Bob", LastName = "Doe", AddressId = addresses[0].Id.Value },
                            new Person() { FirstName = "Jane", LastName = "Doe", AddressId = addresses[0].Id.Value },
                            new Person() { FirstName = "Doe", LastName = "Adear", AddressId = addresses[1].Id.Value },
                            new Person() { FirstName = "AFemale", LastName = "Dear", AddressId = addresses[1].Id.Value },
                        }, personSelector).Values.ToList();

                var businesses =
                    db.InsertOrReplaceAll(
                        new Business[]
                        {
                            new Business() { Name = "ACompany", AddressId = addresses[0].Id.Value, WebSite = new Uri("http://www.acompany.example.com") },
                            new Business() { Name = "BCompany", AddressId = addresses[1].Id.Value, WebSite = new Uri("http://www.bcompany.example.com") }
                        }, businessSelector).Values.ToList();

                var employeesToBusiness =
                    db.InsertOrReplaceAll(
                        new EmployedBy[]
                        {
                            new EmployedBy() { EmployeeId = people[0].Id.Value, BusinessId = businesses[0].Id.Value },
                            new EmployedBy() { EmployeeId = people[1].Id.Value, BusinessId = businesses[0].Id.Value },
                            new EmployedBy() { EmployeeId = people[2].Id.Value, BusinessId = businesses[1].Id.Value },
                            new EmployedBy() { EmployeeId = people[2].Id.Value, BusinessId = businesses[0].Id.Value },
                        }, employedBySelector).Values.ToList();

                var peopleWhoWorkAtQuery =
                    SqlQuery.From<Person>()
                            .Join<EmployedBy>((person, employedBy) =>
                                person.Id == employedBy.EmployeeId)
                            .Join<Business>((person, employedBy, business) =>
                                business.Id == employedBy.BusinessId)
                            .SelectDistinct()
                            .Where<long>((person, employedBy, business, businessId) =>
                                // FIXME: Would be cool if you could instead use
                                //   business == businesses[0]
                                // and then introspect the primary keys to make it work
                                business.Id == businessId);

                var peopleWhoWorkAt = 
                    db.Query(peopleWhoWorkAtQuery, businesses[0].Id)
                      .Select(x =>
                          Tuple.Create(personSelector(x), businessSelector(x)))
                      .ToList();

                Assert.AreEqual(peopleWhoWorkAt.Count, 3);
            }
        }
    }
}

