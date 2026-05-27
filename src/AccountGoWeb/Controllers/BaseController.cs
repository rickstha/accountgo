using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace AccountGoWeb.Controllers
{
    public class BaseController : Controller
    {
        protected IConfiguration? _baseConfig;
        private static readonly HttpClient _httpClient = new HttpClient();

        protected async System.Threading.Tasks.Task<T> GetAsync<T>(string uri)
        {
            string responseJson = string.Empty;
            try
            {
                if (string.IsNullOrEmpty(_baseConfig?["ApiUrl"]))
                    return default(T)!;
                
                var baseUri = _baseConfig!["ApiUrl"];
                var fullUri = new Uri(new Uri(baseUri!), uri);
                var response = await _httpClient.GetAsync(fullUri);
                if (response.IsSuccessStatusCode)
                {
                    responseJson = await response.Content.ReadAsStringAsync();
                }
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"HTTP request error: {ex.Message}");
                return default(T)!;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetAsync: {ex.Message}");
                return default(T)!;
            }
            
            if (string.IsNullOrEmpty(responseJson))
                return default(T)!;
            
            try
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(responseJson)!;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JSON deserialization error: {ex.Message}");
                return default(T)!;
            }
        }

        protected async System.Threading.Tasks.Task<HttpResponseMessage> Get(string uri)
        {
            try
            {
                if (string.IsNullOrEmpty(_baseConfig?["ApiUrl"]))
                    throw new InvalidOperationException("ApiUrl configuration is not set");
                
                var baseUri = _baseConfig!["ApiUrl"];
                var fullUri = new Uri(new Uri(baseUri!), uri);
                var response = await _httpClient.GetAsync(fullUri);
                return response;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Get: {ex.Message}");
                throw;
            }
        }

        protected async System.Threading.Tasks.Task<string> PostAsync(string uri, StringContent data)
        {
            string responseJson = string.Empty;
            try
            {
                if (string.IsNullOrEmpty(_baseConfig?["ApiUrl"]))
                    return string.Empty;
                
                var baseUri = _baseConfig!["ApiUrl"];
                var fullUri = new Uri(new Uri(baseUri!), uri);
                var request = new HttpRequestMessage(HttpMethod.Post, fullUri)
                {
                    Content = data
                };
                request.Headers.Add("UserName", GetCurrentUserName());
                
                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    responseJson = await response.Content.ReadAsStringAsync();
                }
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"HTTP request error: {ex.Message}");
                return string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in PostAsync: {ex.Message}");
                return string.Empty;
            }

            return responseJson ?? string.Empty;
        }

        protected async System.Threading.Tasks.Task<HttpResponseMessage> Post(string uri, StringContent data)
        {
            try
            {
                if (string.IsNullOrEmpty(_baseConfig?["ApiUrl"]))
                    throw new InvalidOperationException("ApiUrl configuration is not set");
                
                var baseUri = _baseConfig!["ApiUrl"];
                var fullUri = new Uri(new Uri(baseUri!), uri);
                var request = new HttpRequestMessage(HttpMethod.Post, fullUri)
                {
                    Content = data
                };
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.Add("UserName", GetCurrentUserName());

                var response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Post: {ex.Message}");
                throw;
            }
        }

        protected bool HasPermission(string permission)
        {
            if (HttpContext.User.Identity!.IsAuthenticated)
            {
                System.Collections.Generic.IList<string> permissions = new System.Collections.Generic.List<string>();

                foreach (var claim in HttpContext.User.Claims)
                {
                    if (claim.Type == System.Security.Claims.ClaimTypes.UserData)
                    {
                        Newtonsoft.Json.Linq.JObject userData = Newtonsoft.Json.Linq.JObject.Parse(claim.Value);
                        if (userData["Roles"] != null)
                        {
                            foreach (var r in userData["Roles"])
                            {
                                if (r["Permissions"] != null)
                                {
                                    foreach (var p in r["Permissions"])
                                    {
                                        if (p["Name"] != null)
                                            permissions.Add(p["Name"]!.ToString());
                                    }
                                }
                            }
                        }
                    }
                }

                if (permissions.Contains(permission))
                    return true;
            }
            return false;
        }

        protected string GetCurrentUserName()
        {
            if (HttpContext.User.Identity!.IsAuthenticated)
            {
                var emailClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email);
                return emailClaim?.Value ?? string.Empty;
            }
            return string.Empty;
        }
    }
}
