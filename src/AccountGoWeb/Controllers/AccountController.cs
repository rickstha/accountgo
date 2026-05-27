using AccountGoWeb.Models.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
            return View(new LoginViewModel() { Email = "admin@accountgo.ph", Password = "P@ssword1" });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> SignIn(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var serialize = Newtonsoft.Json.JsonConvert.SerializeObject(model);
                var content = new StringContent(serialize);
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

                if (resultSignIn["result"] != null)
                {
                    var user = await GetAsync<Dto.Security.User>("administration/getuser?username=" + Uri.EscapeDataString(model.Email));
                    
                    if (user == null)
                    {
                        ModelState.AddModelError(string.Empty, "User not found.");
                        return View(model);
                    }

                    var claims = new List<Claim>();
                    claims.Add(new Claim("RememberMe", model.RememberMe.ToString()));
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Email));
                    claims.Add(new Claim(ClaimTypes.Email, user.Email));

                    //new code added for fiscalYears.cs-- for sulav chitrakar 2026/4/26

                    string firstName = user.FirstName != null ? user.FirstName : "";
                    string lastName = user.LastName != null ? user.LastName : "";

                    claims.Add(new Claim(ClaimTypes.GivenName, firstName));
                    claims.Add(new Claim(ClaimTypes.Surname, lastName));
                    claims.Add(new Claim(ClaimTypes.Name, firstName + " " + lastName));

                    if (user.Roles != null)
                    {
                        foreach(var role in user.Roles)
                            claims.Add(new Claim(ClaimTypes.Role, role.Name));
                    }

                    claims.Add(new Claim(ClaimTypes.UserData, Newtonsoft.Json.JsonConvert.SerializeObject(user)));

                    var identity = new ClaimsIdentity(claims, "AuthCookie");

                    ClaimsPrincipal principal = new ClaimsPrincipal(new[] { identity });

                    HttpContext.User = principal;

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    return RedirectToLocal(returnUrl!);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        public async Task<IActionResult> SignOut()
        {
            await HttpContext.SignOutAsync();

            return View();
        }

        public IActionResult SignedOut()
        {
            if (HttpContext.User.Identity!.IsAuthenticated)
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }

            return View();
        }
        public IActionResult Unauthorized()
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
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            try
            {
                if (ModelState.IsValid)
                {
                    var serialize = Newtonsoft.Json.JsonConvert.SerializeObject(model);
                    var content = new StringContent(serialize);
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

                    if (resultAddNewUser["succeeded"] != null && (bool)resultAddNewUser["succeeded"])
                    {
                        HttpResponseMessage responseInitialized = await Get("administration/initializedcompany");
                        return RedirectToAction(nameof(AccountController.SignIn), "Account");
                    }
                    else
                    {
                        string errorMessage = "Registration failed.";
                        if (resultAddNewUser["errors"] != null && resultAddNewUser["errors"].Count() > 0)
                        {
                            var firstError = resultAddNewUser["errors"][0];
                            if (firstError["description"] != null)
                            {
                                errorMessage = firstError["description"].ToString();
                            }
                        }
                        ModelState.AddModelError(string.Empty, errorMessage);
                        return View(model);
                    }
                }
            }
            catch(HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, "Cannot connect to server. Please check if your database is ready/published.");
                return View(model);
            }
            catch(Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An error occurred during registration: " + ex.Message);
                return View(model);
            }
            return View(model);
        }

        #region Private Methods
        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }
        #endregion
    }
}
