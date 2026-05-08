using System.Net.Http.Headers;
using System.Text;
using MathAPIClient.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;

namespace MathAPIClient.Controllers
{
    public class MathController : Controller
    {
        private static HttpClient? httpClient;
        public MathController(IConfiguration configuration)
        {
            if (httpClient == null)
            {
                var baseUrl = configuration["ApiSettings:BaseUrl"];

                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    throw new InvalidOperationException("ApiSettings:BaseUrl is missing.");
                }

                httpClient = new HttpClient
                {
                    BaseAddress = new Uri(baseUrl)
                };
            }
        }

        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Calculate()
        {
            var token = HttpContext.Session.GetString("MathJWT");

            if (token == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.Operations = GetOperations();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Calculate(decimal? FirstNumber, decimal? SecondNumber, int Operation)
        {
            var token = HttpContext.Session.GetString("MathJWT");
            if (token == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var currentUser = HttpContext.Session.GetString("currentUser");
            decimal? result = 0;
            MathCalculation mathCalculation;

            try
            {
                mathCalculation = MathCalculation.Create(FirstNumber, SecondNumber, Operation, result, currentUser);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                ViewBag.Operations = GetOperations();
                return View();
            }

            StringContent jsonContent = new(
                JsonConvert.SerializeObject(mathCalculation),
                Encoding.UTF8,
                "application/json"
            );

            var request = new HttpRequestMessage(HttpMethod.Post, "api/Math/PostCalculate");
            request.Content = jsonContent;
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await httpClient!.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                MathCalculation? deserialisedResponse = JsonConvert.DeserializeObject<MathCalculation>(jsonResponse);

                if (deserialisedResponse == null)
                {
                    ViewBag.Result = "Invalid response from server.";
                    ViewBag.Operations = GetOperations();
                    return View();
                }

                ViewBag.Result = deserialisedResponse.Result;
                ViewBag.Operations = GetOperations();
                return View();
            }
            else
            {
                ViewBag.Result = await response.Content.ReadAsStringAsync();
                ViewBag.Operations = GetOperations();
                return View();
            }
        }

        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> History()
        {
            var token = HttpContext.Session.GetString("MathJWT");
            if (token == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var request = new HttpRequestMessage(HttpMethod.Get, "api/Math/GetHistory");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await httpClient!.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                List<MathCalculation>? deserialisedResponse =
                    JsonConvert.DeserializeObject<List<MathCalculation>>(jsonResponse);

                if (deserialisedResponse == null || deserialisedResponse.Count == 0)
                {
                    ViewBag.HistoryMessage = "No history exists";
                    return View(new List<MathCalculation>());
                }

                return View(deserialisedResponse);
            }
            else
            {
                ViewBag.HistoryMessage = "No history to show";
                return View(new List<MathCalculation>());
            }
        }

        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Clear()
        {
            var token = HttpContext.Session.GetString("MathJWT");
            if (token == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var request = new HttpRequestMessage(HttpMethod.Delete, "api/Math/DeleteHistory");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await httpClient!.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                TempData["HistoryMessage"] = "Failed to clear history.";
            }

            return RedirectToAction("History");
        }

        private static List<SelectListItem> GetOperations()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "+" },
                new SelectListItem { Value = "2", Text = "-" },
                new SelectListItem { Value = "3", Text = "*" },
                new SelectListItem { Value = "4", Text = "/" }
            };
        }
    }
}