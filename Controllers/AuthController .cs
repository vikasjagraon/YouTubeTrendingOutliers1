using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using YouTubeTrendingOutliers.Models;

namespace YouTubeTrendingOutliers.Controllers
{
    public class AuthController : Controller
    {
            private readonly IWebHostEnvironment _env;

            public AuthController(IWebHostEnvironment env)
            {
                _env = env;
            }

            [HttpGet]
            public IActionResult Login()
            {
                return View();
            }

        [HttpPost]
        public async Task<IActionResult> Login(UserLogin login)
        {
            if (string.IsNullOrWhiteSpace(login.Username) || string.IsNullOrWhiteSpace(login.Password))
            {
                ViewBag.Error = "Username and Password are required.";
                return View();
            }

            string filePath = Path.Combine(_env.ContentRootPath, "Data", "users.json");

            var json = await System.IO.File.ReadAllTextAsync(filePath);
            var users = JsonSerializer.Deserialize<List<UserLogin>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var matchedUser = users.FirstOrDefault(u => u.Username == login.Username && u.Password == login.Password);

            if (matchedUser != null)
            {
                // ✅ This is where you set the authentication cookie
                var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, matchedUser.Username)
            };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                return RedirectToAction("Index", "YouTube");
            }

            ViewBag.Error = "Invalid credentials.";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
    }

