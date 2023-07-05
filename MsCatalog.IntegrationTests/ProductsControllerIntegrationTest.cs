using Newtonsoft.Json;
using System.Text;
using System.Web;

namespace MsCatalog.IntegrationTests
{
    public class ProductsControllerIntegrationTest : IClassFixture<TestingWebAppFactory<Program>>
    {
        private readonly HttpClient _client;

        public ProductsControllerIntegrationTest(TestingWebAppFactory<Program> factory) => _client = factory.CreateClient();

        //[Fact]
        //public async Task GetProducts_Success()
        //{
        //    var endpoint = "catalog/products?RestaurantId=1";

        //    var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_client.BaseAddress, endpoint));

        //    var uriBuilder = new UriBuilder(request.RequestUri);
        //    var query = HttpUtility.ParseQueryString(uriBuilder.Query);
        //    query["PageNumber"] = "1";
        //    query["PageSize"] = "1";
        //    query["RestaurantId"] = "1";
        //    uriBuilder.Query = query.ToString();
        //    request.RequestUri = uriBuilder.Uri;

        //    var response = await _client.SendAsync(request);
        //    response.EnsureSuccessStatusCode();
        //}

        [Fact]
        public async Task CreateProduct_Success()
        {
            var postRequest = new HttpRequestMessage(HttpMethod.Post, "catalog/1/products");

            var formModel = new Dictionary<string, dynamic>
            {
                { "label", "TestLabel" },
                { "description", "TestDesc" },
                { "price", 20 },
                { "taxPercent", 0 },
                { "specialPrice", 20 },
                { "visible", true },
                { "quantity", 20 },
                { "restaurantId", 1 }
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
            var postRequest = new HttpRequestMessage(HttpMethod.Post, "/catalog/1/products");

            var formModel = new Dictionary<string, dynamic>
            {
                { "label", "TestLabel" },
                { "description", "TestDesc" },
                { "price", 20 },
                { "taxPercent", 0 },
                { "specialPrice", 20 },
                { "visible", true },
                { "quantity", 20 },
                { "restaurantId", 1 }
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