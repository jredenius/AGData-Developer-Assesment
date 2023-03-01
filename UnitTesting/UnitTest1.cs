using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Raven.Client.Documents.BulkInsert;
using Raven.Client.Documents.Linq.Indexing;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Queries;
using Raven.Client.Documents.Session;
using Raven.Embedded;
using RestAPI;
using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace UnitTesting
{
    /// <summary>
    /// Unit Testing for API.
    /// </summary>
    [TestClass]
    public class AGDataAPIControllerTests
    {
        /// <summary>
        /// Instance of RestAPI controller.
        /// </summary>
        readonly AGDataAPIController api = new AGDataAPIController(new Logger<AGDataAPIController>(new LoggerFactory()));
        /// <summary>
        /// Tests database connectivity.
        /// Checks count of all customer records.
        /// </summary>
        [TestMethod]
        public void GetAll_TestDBConnection_ReturnsResults()
        {
            List<CustomerData> records = null;
            try
            {
                records = api.Get().ToList();
            }
            catch (Exception)
            {
                Assert.Fail("Database Connection Failed");
            }
            Assert.IsNotNull(records);
            Assert.IsTrue(records.Count > 0, "Record Count: " + records.Count.ToString());
        }
        /// <summary>
        /// Adds mock customer record.
        /// Checks success.
        /// Removes mock record.
        /// </summary>
        [TestMethod]
        public void AddNew_withDuplicateValidation()
        {
            string TestRecordName = "New Test Customer 1";
            string response = api.Post(JsonConvert.SerializeObject(new CustomerData()
            {
                Id = "",
                Name = TestRecordName,
                Address = "Test Address 1",
                Created = DateTime.Now,
                Updated = DateTime.Now,
            }));

            using (var store = EmbeddedServer.Instance.GetDocumentStore("Embedded"))
            {
                using (var session = store.OpenSession())
                {
                    CustomerData checkCustomer = session
                    .Query<CustomerData>()
                    .Customize(x => x.WaitForNonStaleResults(TimeSpan.FromSeconds(5)))
                    .Where(r => r.Name == TestRecordName)
                    .FirstOrDefault<CustomerData>();

                    Assert.IsNotNull(checkCustomer, "Test Record Not Found [1]: " + response);
                    Assert.AreEqual(TestRecordName, checkCustomer.Name, "Test Record Not Found [2]: " + response);

                    api.Delete(checkCustomer.Id);
                }
            }
        }
        /// <summary>
        /// Adds mock customer record.
        /// Checks success.
        /// Adds second mock customer record with the same name.
        /// Checks success.
        /// Removes mock record.
        /// </summary>
        [TestMethod]
        public void AddNew_withForcedDuplicateFailure()
        {
            string TestRecordName = "New Test Customer 2";
            string response1 = api.Post(JsonConvert.SerializeObject(new CustomerData()
            {
                Id = "",
                Name = TestRecordName,
                Address = "Test Address 2",
                Created = DateTime.Now,
                Updated = DateTime.Now,
            }));

            Assert.AreEqual("New record added.", response1, "Dirty Record, Remove Before Retry");

            string response2 = api.Post(JsonConvert.SerializeObject(new CustomerData()
            {
                Id = "",
                Name = TestRecordName,
                Address = "Test Address 2",
                Created = DateTime.Now,
                Updated = DateTime.Now,
            }));

            Assert.AreEqual("Duplicate record found. Add record failed.", response2, "Remove Dirty Record");

            using (var store = EmbeddedServer.Instance.GetDocumentStore("Embedded"))
            {
                using (var session = store.OpenSession())
                {
                    CustomerData checkCustomer = session
                    .Query<CustomerData>()
                    .Customize(x => x.WaitForNonStaleResults(TimeSpan.FromSeconds(5)))
                    .Where(r => r.Name == TestRecordName)
                    .FirstOrDefault<CustomerData>();

                    api.Delete(checkCustomer.Id);
                }
            }
        }
        /// <summary>
        /// Adds mock customer record.
        /// Checks success.
        /// Modifies same mock customer record with the new name.
        /// Checks success.
        /// Removes mock record.
        /// </summary>
        [TestMethod]
        public void ModifyRecord_check()
        {
            string TestRecordName = "New Test Customer 3";
            string TestRecordNameModified = "New Test Customer 3 - Modified";
            CustomerData TestObj = new CustomerData()
            {
                Id = "",
                Name = TestRecordName,
                Address = "Test Address 3",
                Created = DateTime.Now,
                Updated = DateTime.Now,
            };
            string response1 = api.Post(JsonConvert.SerializeObject(TestObj));
            using (var store = EmbeddedServer.Instance.GetDocumentStore("Embedded"))
            {
                using (var session = store.OpenSession())
                {
                    CustomerData checkCustomer = session
                    .Query<CustomerData>()
                    .Customize(x => x.WaitForNonStaleResults(TimeSpan.FromSeconds(5)))
                    .Where(r => r.Name == TestRecordName)
                    .FirstOrDefault<CustomerData>();

                    Assert.IsNotNull(checkCustomer, "Test Record Not Found [1]");

                    TestObj.Id = checkCustomer.Id;
                    TestObj.Name += TestRecordNameModified;
                    string response2 = api.Post(JsonConvert.SerializeObject(TestObj));
                    Assert.AreNotEqual("Duplicate record found. Add record failed.", response2, "Dirty Record, Delete Record Before Retrying");
                }
            }
            using (var store = EmbeddedServer.Instance.GetDocumentStore("Embedded"))
            {
                using (var session = store.OpenSession())
                {
                    CustomerData existingCustomer = session.Load<CustomerData>(TestObj.Id);

                    Assert.IsNotNull(existingCustomer, "Modified Test Record Not Found [2]");
                    Assert.AreEqual(TestObj.Id, existingCustomer.Id, "Record Modification Failed [1]");
                    Assert.AreEqual(TestObj.Name, existingCustomer.Name, "Record Modification Failed [2]");

                    api.Delete(TestObj.Id);
                }
            }
        }
        /// <summary>
        /// Adds mock customer record.
        /// Checks success.
        /// Deletes same mock customer record.
        /// Checks success.
        /// </summary>
        [TestMethod]
        public void DeleteRecord_check()
        {
            string TestRecordName = "New Test Customer 4";
            CustomerData TestObj = new CustomerData()
            {
                Id = "",
                Name = TestRecordName,
                Address = "Test Address 4",
                Created = DateTime.Now,
                Updated = DateTime.Now,
            };
            string response1 = api.Post(JsonConvert.SerializeObject(TestObj));
            using (var store = EmbeddedServer.Instance.GetDocumentStore("Embedded"))
            {
                using (var session = store.OpenSession())
                {
                    CustomerData checkCustomer = session
                    .Query<CustomerData>()
                    .Customize(x => x.WaitForNonStaleResults(TimeSpan.FromSeconds(5)))
                    .Where(r => r.Name == TestRecordName)
                    .FirstOrDefault<CustomerData>();

                    api.Delete(checkCustomer.Id);

                    checkCustomer = session
                    .Query<CustomerData>()
                    .Customize(x => x.WaitForNonStaleResults(TimeSpan.FromSeconds(5)))
                    .Where(r => r.Name == TestRecordName)
                    .FirstOrDefault<CustomerData>();

                    Assert.IsNull(checkCustomer, "Record Found, Deletion Failed");

                }
            }
        }
        /// <summary>
        /// For development testing only
        /// Removes all customer records from database.
        /// Populates database with records from data source file.
        /// Ignored by default, remove the [Ignore] attribute to execute.
        /// </summary>
        [TestMethod]
        [Ignore]
        public void ResetAllTestRecords_IgnoredByDefault()
        {
            int newRecordCount = 0;
            using (var store = EmbeddedServer.Instance.GetDocumentStore("Embedded"))
            {
                store.Operations.Send(new DeleteByQueryOperation(new IndexQuery
                {
                    Query = "from CustomerDatas"
                }));
                using (BulkInsertOperation bulkInsert = store.BulkInsert(new BulkInsertOptions
                {
                    SkipOverwriteIfUnchanged = true
                }))
                {
                    using (StreamReader sr = new StreamReader(@"TestData.csv"))
                    {
                        String line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            string[] parts = line.Split(',', 3, StringSplitOptions.RemoveEmptyEntries);
                            string ID = parts[0];
                            bulkInsert.Store(new CustomerData
                            {
                                Id = ID,
                                Name = parts[1],
                                Address = parts[2].Replace("\"", ""),
                                Created = DateTime.Now,
                                Updated = DateTime.Now,
                            }, ID);
                        }
                    }
                }
                using (var session = store.OpenSession())
                {
                    newRecordCount = session
                    .Query<CustomerData>()
                    .ToList()
                    .Count;
                }
            }
            Assert.IsTrue(newRecordCount >= 100, "Records Import Failed");
        }
    }
}