using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;

namespace WebClient.Pages
{
    public class IndexModel : PageModel
    {
        static HttpClient client = new();
        private readonly ILogger<IndexModel> _logger;
        /// <summary>
        /// Collection representing all customer records.
        /// Used for iterative page display.
        /// </summary>
        [BindProperty]
        public List<CustomerData> AllCustomers { get; set; } = new();
        /// <summary>
        /// Single customer instance selected in webclient interface
        /// </summary>
        [BindProperty]
        public CustomerData SelectedCustomer { get; set; } = new();
        /// <summary>
        /// Text response displayed to end user in webclient
        /// </summary>
        [BindProperty]
        public string FormResponse { get; set; } = "";

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
            FormResponse = "";
            Load();
        }

        /// <summary>
        /// Reset SelectedCustomer, clearing form edit field in webclient
        /// </summary>
        public void ClearForm()
        {
            SelectedCustomer = new();
        }

        /// <summary>
        /// Request to open RavenDB web interface.
        /// Load in a new window.
        /// </summary>
        public async void OnGetOpenDBMS()
        {
            await client.GetAsync($"https://localhost:44397/customer/0");
        }

        /// <summary>
        /// Get all customers
        /// </summary>
        private void Load()
        {
            HttpResponseMessage response = client.GetAsync($"https://localhost:44397/customer/").Result;
            response.EnsureSuccessStatusCode();
            AllCustomers = response.Content.ReadFromJsonAsync<List<CustomerData>>().Result;
        }

        /// <summary>
        /// Add or updated customer record width duplicate detection.
        /// </summary>
        /// <param name="id">Required for Update</param>
        /// <param name="name">Required</param>
        /// <param name="address"></param>
        public void OnPostModifyCustomer(string id, string name, string address)
        {
            if (id == null || SelectedCustomer.Id != id)
            {
                SelectedCustomer = new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Created = DateTime.Now,
                };
            }
            SelectedCustomer.Name = name;
            SelectedCustomer.Address = address;
            HttpResponseMessage response = client.PostAsJsonAsync($"https://localhost:44397/customer/", JsonConvert.SerializeObject(SelectedCustomer)).Result;
            response.EnsureSuccessStatusCode();
            FormResponse = response.Content.ReadAsStringAsync().Result;
            if (!FormResponse.Contains("Duplicate"))
                ClearForm();
            Load();
        }

        /// <summary>
        /// Get customer record by ID
        /// </summary>
        /// <param name="id">Required, GUID</param>
        public void OnGetSelectCustomer(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            HttpResponseMessage response = client.GetAsync($"https://localhost:44397/customer/" + id).Result;
            response.EnsureSuccessStatusCode();
            SelectedCustomer = response.Content.ReadFromJsonAsync<CustomerData>().Result;
        }

        /// <summary>
        /// Remove customer record from database
        /// </summary>
        /// <param name="id">Required</param>
        public void OnGetDeleteCustomer(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            HttpResponseMessage response = client.DeleteAsync($"https://localhost:44397/customer/" + id).Result;
            response.EnsureSuccessStatusCode();
            FormResponse = response.Content.ReadAsStringAsync().Result;
            ClearForm();
            Load();
        }
    }
}