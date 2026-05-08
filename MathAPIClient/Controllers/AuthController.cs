using System.Text;
using MathAPIClient.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace MathAPIClient.Controllers
{
    public class AuthController : Controller
    {
        private static HttpClient? httpClient;
        public AuthController(IConfiguration configuration)
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

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(LoginModel login)
        {
            StringContent jsonContent = new(
                JsonConvert.SerializeObject(login),
                Encoding.UTF8,
                "application/json"
            );

            HttpResponseMessage response = await httpClient!.PostAsync("api/Auth/Register", jsonContent);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                AuthResponse? deserialisedResponse = JsonConvert.DeserializeObject<AuthResponse>(jsonResponse);

                if (deserialisedResponse == null)
                {
                    ViewBag.Result = "Invalid response from server.";
                    return View();
                }

                HttpContext.Session.SetString("currentUser", deserialisedResponse.UserId);
                HttpContext.Session.SetString("MathJWT", deserialisedResponse.Token);

                return RedirectToAction("Calculate", "Math");
            }
            else
            {
                ViewBag.Result = await response.Content.ReadAsStringAsync();
                return View();
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginModel login)
        {
            StringContent jsonContent = new(
                JsonConvert.SerializeObject(login),
                Encoding.UTF8,
                "application/json"
            );

            HttpResponseMessage response = await httpClient!.PostAsync("api/Auth/Login", jsonContent);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                AuthResponse? deserialisedResponse = JsonConvert.DeserializeObject<AuthResponse>(jsonResponse);

                if (deserialisedResponse == null)
                {
                    ViewBag.Result = "Invalid response from server.";
                    return View();
                }

                HttpContext.Session.SetString("currentUser", deserialisedResponse.UserId);
                HttpContext.Session.SetString("MathJWT", deserialisedResponse.Token);

                return RedirectToAction("Calculate", "Math");
            }
            else
            {
                ViewBag.Result = await response.Content.ReadAsStringAsync();
                return View();
            }
        }

        [HttpGet]
        public IActionResult LogOut()
        {
            HttpContext.Session.Clear();

            if (httpClient != null)
            {
                httpClient.DefaultRequestHeaders.Authorization = null;
            }

            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return RedirectToAction("Login", "Auth");
        }
    }
}