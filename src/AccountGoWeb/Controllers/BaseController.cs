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
                var apiUrl = _baseConfig?["ApiUrl"];
                if (string.IsNullOrWhiteSpace(apiUrl))
                    return default(T)!;

                var fullUri = new Uri(new Uri(apiUrl), uri);
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
                var apiUrl = _baseConfig?["ApiUrl"];
                if (string.IsNullOrWhiteSpace(apiUrl))
                    throw new InvalidOperationException("ApiUrl configuration is not set");

                var fullUri = new Uri(new Uri(apiUrl), uri);
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
                var apiUrl = _baseConfig?["ApiUrl"];
                if (string.IsNullOrWhiteSpace(apiUrl))
                    return string.Empty;

                var fullUri = new Uri(new Uri(apiUrl), uri);
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
                var apiUrl = _baseConfig?["ApiUrl"];
                if (string.IsNullOrWhiteSpace(apiUrl))
                    throw new InvalidOperationException("ApiUrl configuration is not set");

                var fullUri = new Uri(new Uri(apiUrl), uri);
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
            var user = HttpContext?.User;
            if (user?.Identity is { IsAuthenticated: true })
            {
                System.Collections.Generic.IList<string> permissions = new System.Collections.Generic.List<string>();

                foreach (var claim in user.Claims)
                {
                    if (claim.Type == System.Security.Claims.ClaimTypes.UserData)
                    {
                        if (string.IsNullOrWhiteSpace(claim.Value))
                            continue;

                        Newtonsoft.Json.Linq.JObject userData = Newtonsoft.Json.Linq.JObject.Parse(claim.Value);
                        var roles = userData["Roles"] as Newtonsoft.Json.Linq.JArray;
                        if (roles != null)
                        {
                            foreach (var r in roles.Children())
                            {
                                var role = r as Newtonsoft.Json.Linq.JObject;
                                if (role == null)
                                    continue;

                                var permissionsList = role["Permissions"] as Newtonsoft.Json.Linq.JArray;
                                if (permissionsList == null)
                                    continue;

                                foreach (var p in permissionsList.Children())
                                {
                                    var permissionName = p["Name"]?.ToString();
                                    if (!string.IsNullOrWhiteSpace(permissionName))
                                        permissions.Add(permissionName);
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
            var user = HttpContext?.User;
            if (user?.Identity is { IsAuthenticated: true })
            {
                var emailClaim = user.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email);
                return emailClaim?.Value ?? string.Empty;
            }
            return string.Empty;
        }
    }
}
