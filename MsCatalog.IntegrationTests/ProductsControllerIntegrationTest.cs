using Newtonsoft.Json;
using System.Text;

namespace MsCatalog.IntegrationTests
{
    public class ProductsControllerIntegrationTest : IClassFixture<TestingWebAppFactory<Program>>
    {
        private readonly HttpClient _client;

        public ProductsControllerIntegrationTest(TestingWebAppFactory<Program> factory) => _client = factory.CreateClient();

        [Fact]
        public async Task GetProducts_Success()
        {
            var response = await _client.GetAsync("/catalog/products");
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task CreateProduct_Success()
        {
            var postRequest = new HttpRequestMessage(HttpMethod.Post, "/catalog/products");

            var formModel = new Dictionary<string, dynamic>
            {
                { "label", "TestLabel" },
                { "description", "TestDesc" },
                { "price", 20 },
                { "taxPercent", 0 },
                { "specialPrice", 20 },
                { "visible", true }
            };


            var jsonString = JsonConvert.SerializeObject(formModel);

            postRequest.Content = new StringContent(jsonString, Encoding.UTF8, "text/json");

            var response = await _client.SendAsync(postRequest);

            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            Assert.Contains("TestLabel", responseString);
            Assert.Contains("TestDesc", responseString);
        }

        [Fact]
        public async Task CreateProduct_EnsureBadLabel()
        {
            var postRequest = new HttpRequestMessage(HttpMethod.Post, "/catalog/products");

            var formModel = new Dictionary<string, dynamic>
            {
                { "label", "TestLabel" },
                { "description", "TestDesc" },
                { "price", 20 },
                { "taxPercent", 0 },
                { "specialPrice", 20 },
                { "visible", true }
            };


            var jsonString = JsonConvert.SerializeObject(formModel);

            postRequest.Content = new StringContent(jsonString, Encoding.UTF8, "text/json");

            var response = await _client.SendAsync(postRequest);

            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            Assert.DoesNotContain("RandomString", responseString);
        }
    }
}