using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;



namespace Foody
{
    [TestFixture]
    public class FoodyTests
    {

        private RestClient client;
        private static string createdFoodId;
        private const string baseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:86/";


        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJwtToken("vasko8585", "vasko8585");
            
            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };
            client = new RestClient(options);
        }
        private string GetJwtToken(string username, string password)
        {
            var loginClient = new RestClient(baseUrl);
            var request = new RestRequest("api/User/Authentication", Method.Post);
            request.AddJsonBody(new { username, password });
            var response = loginClient.Execute(request);
            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            return json.GetProperty("accessToken").GetString() ?? string.Empty;
        }

        [Test, Order(1)]
        public void CreateFoodShouldReturnCreated()
        {
            var food = new
            {
                name = "NewFood",
                description = "NewDescription",
                url = ""
            };
            var request = new RestRequest("api/Food/Create", Method.Post);
            request.AddJsonBody(food);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            createdFoodId = json.GetProperty("foodId").GetString() ?? string.Empty;
            Assert.That(createdFoodId, Is.Not.Null.And.Not.Empty, "Food ID should not be null or empty.");
        }

        [Test, Order(2)]
        public void EditFoodShouldReturnOk()
        {
            var changes = new[]
            {
                new {path = "/name", op = "replace", value = "EditedFood"},
            };
            var request = new RestRequest($"api/Food/Edit/{createdFoodId}", Method.Patch);
            request.AddJsonBody(changes);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Successfully edited"));
        }

        [Test, Order(3)]
        public void GetAllFoodsShouldReturnList()
        {
            var request = new RestRequest("api/Food/All", Method.Get);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var foods = JsonSerializer.Deserialize<List<object>>(response.Content);
            Assert.That(foods, Is.Not.Empty);
        }
        [Test, Order(4)]
        public void DeleteFoodShouldReturnOk()
        {
            var request = new RestRequest($"api/Food/Delete/{createdFoodId}", Method.Delete);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Deleted successfully!"));

        }

        [Test, Order(5)]
        public void CreateFoodWithoutRequiredFieldsShouldReturnBadRequest()
        {
            var food = new
            {
                name = "",
                description = "NewDescription",
            };
            var request = new RestRequest("api/Food/Create", Method.Post);
            request.AddJsonBody(food);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }
        [Test, Order(6)]
        public void EditNonExistentFoodShouldReturnNotFound()
        {
            string fakeID = "123";
            var changes = new[]
            {
                new {path = "/name", op = "replace", value = "New Title"},
            };
            var request = new RestRequest("api/Food/Edit/{fakeID}", Method.Patch);
            request.AddJsonBody(changes);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.Content, Does.Contain("No food revues..."));
        }
        [Test, Order(7)]
        public void DeleteNonExistentFoodShouldReturnBadRequest()
        {
            string fakeID = "123";
            var request = new RestRequest($"api/Food/Delete/{fakeID}", Method.Delete);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }
        [OneTimeTearDown]
        public void Cleanup()
        {
           client?.Dispose();
        }
    }
}