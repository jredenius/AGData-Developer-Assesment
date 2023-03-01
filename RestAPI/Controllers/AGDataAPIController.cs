using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Raven.Embedded;
using System.Xml.Linq;

namespace RestAPI.Controllers
{
    [ApiController]
    [Route("customer")]
    public class AGDataAPIController : ControllerBase
    {
        private readonly ILogger<AGDataAPIController> _logger;

        public AGDataAPIController(ILogger<AGDataAPIController> logger)
        {
            _logger = logger;
            int dbID = 0;
            try
            {
                dbID = EmbeddedServer.Instance.GetServerProcessIdAsync().Result;
            }
            catch (Exception)
            {
                //string DataBaseDirectory = Directory.GetCurrentDirectory() + @"\RavenDB"; //Mock DB instances for Production vs. Unit Testing
                string DataBaseDirectory = Directory.GetCurrentDirectory().Replace("UnitTesting", "RestAPI") + @"\RavenDB"; //Single instance db location using relative path
                EmbeddedServer.Instance.StartServer(new ServerOptions
                {
                    DataDirectory = DataBaseDirectory,
                });
            }
        }

        /// <summary>
        /// Retrieve all customer records
        /// </summary>
        /// <returns>Enumerable list of customers</returns>
        [HttpGet()]
        public IEnumerable<CustomerData> Get()
        {
            using (var store = EmbeddedServer.Instance.GetDocumentStore("Embedded"))
            {
                using (var session = store.OpenSession())
                {
                    return session
                    .Query<CustomerData>()
                    .OrderBy(r => r.Name)
                    .ToList();
                }
            }
        }
        /// <summary>
        /// Get customer by ID
        /// </summary>
        /// <param name="id">Required, GUID</param>
        /// <returns>Single instance of customer</returns>
        [HttpGet("{id}")]
        public CustomerData Get(string id) // Get Single Customer
        {
            if (id == "0")
            {
                EmbeddedServer.Instance.OpenStudioInBrowser();
                return null;
            }
            else
            {
                using (var store = EmbeddedServer.Instance.GetDocumentStore("Embedded"))
                {
                    using (var session = store.OpenSession())
                    {
                        return session
                        .Query<CustomerData>()
                        .Where(r => r.Id == id)
                        .FirstOrDefault<CustomerData>();
                    }
                }
            }
        }
        /// <summary>
        /// Add or Update customer record with duplicate detection.
        /// Duplicates negate any action.
        /// </summary>
        /// <param name="value">Json Serialized Customer Object</param>
        /// <returns>Text status message</returns>
        [HttpPost]
        public string Post([FromBody]string value)
        {
            CustomerData customer = JsonConvert.DeserializeObject<CustomerData>(value);
            if (customer == null) return "Invalid record.";
            using (var store = EmbeddedServer.Instance.GetDocumentStore("Embedded"))
            {
                using (var session = store.OpenSession())
                {
                    CustomerData checkCustomer = session
                    .Query<CustomerData>()
                    .Customize(x => x.WaitForNonStaleResults(TimeSpan.FromSeconds(5)))
                    .Where(r => r.Id == customer.Id || r.Name == customer.Name)
                    .FirstOrDefault<CustomerData>();

                    if (checkCustomer == null) //No customer match found
                    {
                        //Add new customer record
                        string newId = Guid.NewGuid().ToString();
                        session.Store(new CustomerData()
                        {
                            Id = newId,
                            Name = customer.Name,
                            Address = customer.Address,
                            Created = DateTime.Now,
                            Updated = DateTime.Now,
                        }, newId);
                        
                        session.SaveChanges(); //Commit Changes
                        return "New record added.";
                    }
                    else if(checkCustomer.Id != customer.Id && checkCustomer.Name == customer.Name) //Duplicate customer record detected
                    {
                        //Data not committed
                        return "Duplicate record found. Add record failed.";
                    }
                    else
                    {
                        //Update customer record
                        CustomerData existingCustomer = session.Load<CustomerData>(customer.Id);
                        existingCustomer.Name = customer.Name;
                        existingCustomer.Address = customer.Address;
                        existingCustomer.Updated = DateTime.Now;
                        
                        session.SaveChanges(); //Commit Changes
                        return "Record updated.";
                    }
                }
            }
        }
        /// <summary>
        /// Delete customer record from database.
        /// </summary>
        /// <param name="id">Required, GUID</param>
        /// <returns>Text status message</returns>
        [HttpDelete("{id}")]
        public string Delete(string id)
        {
            using (var store = EmbeddedServer.Instance.GetDocumentStore("Embedded"))
            {
                using (var session = store.OpenSession())
                {
                    try
                    {
                        session.Delete(id);
                        session.SaveChanges();
                        return "Record removed.";
                    }
                    catch (Exception)
                    {
                        return "Record deletion failed.";
                    }
                }
            }
        }

    }
}