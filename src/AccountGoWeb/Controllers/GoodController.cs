using Microsoft.AspNetCore.Mvc;

namespace AccountGoWeb.Controllers
{
    public class GoodController : Controller
    {
        protected IConfiguration? _configuration;

        protected Uri BuildApiUri(string relativeUri)
        {
            var apiBase = _configuration?["ApiUrl"];
            if (string.IsNullOrWhiteSpace(apiBase))
                throw new InvalidOperationException("ApiUrl configuration is not set.");

            var baseUri = new Uri(apiBase, UriKind.Absolute);
            return new Uri(baseUri, relativeUri.TrimStart('/'));
        }

        protected HttpResponseMessage Get(string uri)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                return client.GetAsync(BuildApiUri(uri)).GetAwaiter().GetResult();
            }
        }

        protected async System.Threading.Tasks.Task<HttpResponseMessage> Post(string uri, StringContent data)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                //client.DefaultRequestHeaders.Add("UserName", GetCurrentUserName());

                return await client.PostAsync(BuildApiUri(uri), data);
            }
        }

        protected async System.Threading.Tasks.Task<T> GetAsync<T>(string uri)
        {
            string responseJson = string.Empty;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                var response = await client.GetAsync(BuildApiUri(uri));
                if (response.IsSuccessStatusCode)
                {
                    responseJson = await response.Content.ReadAsStringAsync();
                }
            }

            if (string.IsNullOrWhiteSpace(responseJson))
                return default!;

            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(responseJson)!;
        }

        protected async System.Threading.Tasks.Task<string> PostAsync(string uri, StringContent data)
        {
            string responseJson = string.Empty;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                //client.DefaultRequestHeaders.Add("UserName", GetCurrentUserName());

                var response = await client.PostAsync(BuildApiUri(uri), data);
                if (response.IsSuccessStatusCode)
                {
                    responseJson = await response.Content.ReadAsStringAsync();
                }
            }

            if (string.IsNullOrWhiteSpace(responseJson))
                return string.Empty;

            return Newtonsoft.Json.JsonConvert.DeserializeObject<string>(responseJson) ?? string.Empty;
        }
    }
}
