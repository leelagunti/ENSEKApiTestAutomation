using ENSEKApiTests.Models;
using ENSEKApiTests.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Reqnroll;
using RestSharp;
using System.Text.RegularExpressions;

namespace ENSEKApiTests.StepDefinitions
{
    [Binding]
    public class ENSEKAPISteps
    {
        private RestResponse _response;
        private List<JObject> _orders;
        private readonly ScenarioContext _scenarioContext;

        public ENSEKAPISteps(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        [Given("the base API URL is configured")]
        public void GivenTheBaseAPIURL()
        {
            Console.WriteLine("Base URL is loaded from appsettings.json via ApiHelper.");
        }        

        [When("I place an order for (.*) unit of (.*)")]
        public void WhenIPlaceOrderForFuel(int quantity, string fuel)
        {
            if (!_scenarioContext.TryGetValue($"FuelId_{fuel}", out var fuelIdObj))
                throw new KeyNotFoundException($"FuelId for '{fuel}' not found in context.");

            string endpoint = $"/buy/{fuelIdObj}/{quantity}";
            _response = ApiHelper.ExecuteRequest(endpoint, Method.Put);
            _scenarioContext[$"Order_{fuel}"] = _response;
        }

        [Then("the order for (.*) should return status code (.*)")]
        [When("the order for (.*) should return status code (.*)")]
        public void WhenTheOrderForShouldReturnStatusCode(string fuel, int expectedStatusCode)
        {
            if (!_scenarioContext.TryGetValue($"Order_{fuel}", out var responseObj))
                throw new KeyNotFoundException($"Order response for '{fuel}' not found in ScenarioContext.");

            var response = (RestResponse)responseObj;

            Assert.That((int)response.StatusCode, Is.EqualTo(expectedStatusCode),
                $"Expected status code {expectedStatusCode} but got {(int)response.StatusCode} for '{fuel}'.");

            var message = JObject.Parse(response.Content)["message"]?.ToString();
            Assert.That(message, Is.Not.Null.And.Not.Empty, $"API response message is missing for '{fuel}'.");

            //  whitespace
            string normalizedMessage = Regex.Replace(message, @"\s+", " ");

            // Case-insensitive match for both formats
            var regex = new Regex(@"your order\s?id is\s?([a-f0-9\-]{36})", RegexOptions.IgnoreCase);
            var match = regex.Match(normalizedMessage);

            Assert.That(match.Success, Is.True,
                $"Message did not contain any expected order ID markers: Your order id is  / Your orderid is \nActual message: {message}");

            string orderId = match.Groups[1].Value.Trim().TrimEnd('.');
            Assert.That(Guid.TryParse(orderId, out _), $"Extracted value is not a valid GUID: {orderId}");

            Console.WriteLine($"Valid Order ID for '{fuel}': {orderId}");
            _scenarioContext[$"OrderId_{fuel}"] = orderId;
        }        


        [Then("the purchase message should create an order")]
        public void ThenThePurchaseMessageShouldCreateAnOrder()
        {
            var responseContent = JObject.Parse(_response.Content);
            var message = responseContent["message"]?.ToString();

            Assert.That(message, Is.Not.Null.And.Not.Empty, "API response message is missing.");

            // all kinds of whitespace become ' '
            string normalizedMessage = Regex.Replace(message, @"\s+", " "); 

            // Case-insensitive regex
            var regex = new Regex(@"Your order\s?id is ([a-f0-9\-]+)", RegexOptions.IgnoreCase);
            var match = regex.Match(normalizedMessage);

            Assert.That(match.Success, Is.True,
                $"Message did not contain any expected order ID markers: Your order id is  / Your orderid is \nActual message: {message}");

            string orderId = match.Groups[1].Value.Trim().TrimEnd('.');
            Assert.That(Guid.TryParse(orderId, out _), $"Extracted value is not a valid GUID: {orderId}");

            Console.WriteLine($"✅ Valid Order ID extracted: {orderId}");
            _scenarioContext["LastOrderId"] = orderId;
        }



        [When("I fetch the list of available energy types")]
        public void WhenIFetchEnergyTypes()
        {
            _response = ApiHelper.ExecuteRequest("/energy", Method.Get);
            Assert.That((int)_response.StatusCode, Is.EqualTo(200));
        }

        [Then("I should store the energy response")]
        public void ThenIStoreEnergyResponse()
        {
            var energyData = JsonConvert.DeserializeObject<EnergyResponse>(_response.Content);
            _scenarioContext[$"FuelId_electric"] = energyData.electric.energy_id;
            _scenarioContext[$"FuelId_gas"] = energyData.gas.energy_id;
            _scenarioContext[$"FuelId_oil"] = energyData.oil.energy_id;
            _scenarioContext[$"FuelId_nuclear"] = energyData.nuclear.energy_id;
        }

        [When("I fetch the order list")]
        [Then("I fetch the order list")]
        public void ThenIFetchOrders()
        {
            _response = ApiHelper.ExecuteRequest("/orders", Method.Get);
            _orders = JArray.Parse(_response.Content).Select(x => (JObject)x).ToList();
        }

        [Then(@"the placed order for (.*) should appear in today's order list")]
        public void ThenPlacedOrderForFuelShouldAppearInTodaysOrderList(string fuel)
        {
            // Fetch orders
            _response = ApiHelper.ExecuteRequest("/orders", Method.Get);
            var orders = JArray.Parse(_response.Content).Select(x => (JObject)x).ToList();

            // Filter only today's orders
            var today = DateTime.UtcNow.Date;
            var todaysOrders = orders.Where(o =>
            {
                var timeStr = o["time"]?.ToString();
                return DateTime.TryParse(timeStr, out var dt) && dt.Date == today;
            }).ToList();

            // Get the placed order for this fuel
            if (!_scenarioContext.TryGetValue($"Order_{fuel}", out var responseObj))
            {
                Assert.Fail($"Order response for '{fuel}' not found in ScenarioContext.");
            }

            var message = JObject.Parse(((RestResponse)responseObj).Content)?["message"]?.ToString()?.Replace('\u00A0', ' ')?.Trim();
            var marker = message.Contains("Your order id is ") ? "Your order id is " : "Your orderid is ";
            var rawOrderId = message.Split(marker).LastOrDefault()?.Trim();
            var orderId = rawOrderId?.TrimEnd('.', '\"');

            Assert.That(Guid.TryParse(orderId, out _), Is.True, $"'{orderId}' is not a valid GUID.");

            // Look for orderId in today's orders
            var found = todaysOrders.Any(o =>
                string.Equals(o["id"]?.ToString() ?? o["Id"]?.ToString(), orderId, StringComparison.OrdinalIgnoreCase));

            Assert.That(found, Is.True, $"Order ID '{orderId}' for '{fuel}' not found in today's order list.");
            Console.WriteLine($"✅ Verified order for '{fuel}' with ID: {orderId}");
        }

        [Then(@"I should count orders placed before today")]
        public void ThenCountOldOrders()
        {
            if (_orders == null)
            {
                Console.WriteLine("⚠️ Orders not fetched yet. Fetching now...");
                _response = ApiHelper.ExecuteRequest("/orders", Method.Get);
                _orders = JArray.Parse(_response.Content).Select(x => (JObject)x).ToList();
            }

            int count = _orders.Count(o =>
            {
                var timestamp = o["time"]?.ToString();
                return DateTime.TryParse(timestamp, out var dt) && dt.Date < DateTime.UtcNow.Date;
            });

            Console.WriteLine($"✅ Orders placed before today: {count}");
            Assert.That(count, Is.GreaterThanOrEqualTo(0));
        }


        [When("I query an order with invalid fuel type")]
        public void WhenIQueryInvalidOrder()
        {
            var body = new { quantity = 1, fuel = "InvalidFuel" };
            _response = ApiHelper.ExecuteRequest("/orders", Method.Post, body);
        }

        [Then("the response status code should be {int}")]
        public void ThenTheResponseStatusCodeShouldBe(int statusCode)
        {
            Assert.That((int)_response.StatusCode, Is.EqualTo(statusCode));
        }
    }
}