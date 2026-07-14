using AccountGoWeb.Models.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AccountGoWeb.Controllers
{
    public class AccountController : BaseController
    {
        public AccountController(IConfiguration config)
        {
            _baseConfig = config;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult SignIn(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            // Do not pre-populate real credentials in the login form.
            return View(new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignIn(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var serialize = Newtonsoft.Json.JsonConvert.SerializeObject(model);
            var content = new StringContent(serialize, System.Text.Encoding.UTF8);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            HttpResponseMessage responseSignIn = await Post("account/signin", content);
            string responseContent = await responseSignIn.Content.ReadAsStringAsync();

            Newtonsoft.Json.Linq.JObject resultSignIn;
            try
            {
                resultSignIn = Newtonsoft.Json.Linq.JObject.Parse(responseContent);
            }
            catch (Newtonsoft.Json.JsonException)
            {
                ModelState.AddModelError(string.Empty, "Invalid response from server.");
                return View(model);
            }

            // Explicitly check the value, not just that the key exists.
            var signInSucceeded = resultSignIn["result"]?.Type == Newtonsoft.Json.Linq.JTokenType.Boolean
                && resultSignIn["result"]!.Value<bool>();

            if (!signInSucceeded)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            var user = await GetAsync<Dto.Security.User>("administration/getuser?username=" + Uri.EscapeDataString(model.Email));

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim("RememberMe", model.RememberMe.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Email),
                new Claim(ClaimTypes.Email, user.Email)
            };

            string firstName = user.FirstName ?? "";
            string lastName = user.LastName ?? "";

            claims.Add(new Claim(ClaimTypes.GivenName, firstName));
            claims.Add(new Claim(ClaimTypes.Surname, lastName));
            claims.Add(new Claim(ClaimTypes.Name, $"{firstName} {lastName}"));

            if (user.Roles != null)
            {
                foreach (var role in user.Roles)
                    claims.Add(new Claim(ClaimTypes.Role, role.Name));
            }

            claims.Add(new Claim(ClaimTypes.UserData, Newtonsoft.Json.JsonConvert.SerializeObject(user)));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToLocal(returnUrl!);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(SignedOut));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult SignedOut()
        {
            if (HttpContext.User.Identity!.IsAuthenticated)
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }

            return View();
        }

        // Renamed to avoid hiding ControllerBase.Unauthorized().
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var serialize = Newtonsoft.Json.JsonConvert.SerializeObject(model);
                var content = new StringContent(serialize, System.Text.Encoding.UTF8);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                HttpResponseMessage responseAddNewUser = await Post("account/addnewuser", content);
                string responseContent = await responseAddNewUser.Content.ReadAsStringAsync();

                Newtonsoft.Json.Linq.JObject resultAddNewUser;
                try
                {
                    resultAddNewUser = Newtonsoft.Json.Linq.JObject.Parse(responseContent);
                }
                catch (Newtonsoft.Json.JsonException)
                {
                    ModelState.AddModelError(string.Empty, "Invalid response from server.");
                    return View(model);
                }

                var succeeded = resultAddNewUser["succeeded"]?.Type == Newtonsoft.Json.Linq.JTokenType.Boolean
                    && resultAddNewUser["succeeded"]!.Value<bool>();

                if (succeeded)
                {
                    await Get("administration/initializedcompany");
                    return RedirectToAction(nameof(SignIn));
                }

                string errorMessage = "Registration failed.";
                var errors = resultAddNewUser["errors"] as Newtonsoft.Json.Linq.JArray;
                if (errors != null && errors.Count > 0)
                {
                    var description = errors[0]["description"];
                    if (description != null)
                    {
                        errorMessage = description.ToString();
                    }
                }

                ModelState.AddModelError(string.Empty, errorMessage);
                return View(model);
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty, "Cannot connect to server. Please check if your database is ready/published.");
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An error occurred during registration: " + ex.Message);
                return View(model);
            }
        }

        #region Private Methods
        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(HomeController.Index), "Home");
        }
        #endregion
    }
}