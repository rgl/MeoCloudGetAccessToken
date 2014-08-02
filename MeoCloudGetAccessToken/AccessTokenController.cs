// developed by Rui Lopes (ruilopes.com). licensed under GPLv3.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace MeoCloudGetAccessToken
{
    public class AccessTokenController : ApiController
    {
        private readonly static Provider[] Providers = new[]
            {
                new Provider
                    {
                        Name = "meo-dev",
                        AuthorizeUrl = "https://disco.dev.sapo.pt/oauth2/authorize",
                        TokenUrl = "https://disco.dev.sapo.pt/oauth2/token",
                    },
                new Provider
                    {
                        Name = "meo",
                        AuthorizeUrl = "https://meocloud.pt/oauth2/authorize",
                        TokenUrl = "https://meocloud.pt/oauth2/token",
                    },
                new Provider
                    {
                        Name = "dropbox",
                        AuthorizeUrl = "https://www.dropbox.com/1/oauth2/authorize",
                        TokenUrl = "https://api.dropbox.com/1/oauth2/token",
                    }
            };

        private class Provider
        {
            public string Name { get; set; }
            public string AuthorizeUrl { get; set; }
            public string TokenUrl { get; set; }
        }

        public class DanceArguments
        {
            public string Provider { get; set; }

            public Credentials Credentials { get; set; }
        }

        public class Credentials
        {
            public string ClientId { get; set; }
            public string ClientSecret { get; set; }
        }

        // e.g. {"access_token": "blablablablablablablabla-blablablabl-ablablablablablablablablabl", "token_type": "bearer", "uid": "20421111"}
        private class TokenSuccessfulResponse
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }

            [JsonProperty("token_type")]
            public string TokenType { get; set; }

            public string Uid { get; set; }
        }

        // e.g. {"error_description": "code has already been used", "error": "invalid_grant"}
        private class TokenErrorResponse
        {
            public string Error { get; set; }

            [JsonProperty("error_description")]
            public string ErrorDescription { get; set; }
        }


        [Route("dance")]
        [HttpPost]
        public HttpResponseMessage Post([FromBody] DanceArguments arguments)
        {
            var provider = Providers.First(p => p.Name == arguments.Provider);

            var credentials = arguments.Credentials;

            var csrfToken = GenerateCsrfToken();

            var authorizeConsumerUrl = string.Format(
                provider.AuthorizeUrl +
                    "?client_id={0}"+
                    "&redirect_uri={1}" +
                    "&response_type=code" +
                    "&state={2}",
                Uri.EscapeDataString(credentials.ClientId),
                Uri.EscapeDataString(new Uri(Request.RequestUri, Url.Route("callback", null)).AbsoluteUri),
                Uri.EscapeDataString(csrfToken)
            );

            var response = Request.CreateResponse(HttpStatusCode.Found);
            response.Headers.AddCookies(new[]
                {
                    new CookieHeaderValue("csfr", csrfToken),
                    new CookieHeaderValue("clientId", credentials.ClientId),
                    new CookieHeaderValue("clientSecret", credentials.ClientSecret),
                    new CookieHeaderValue("provider", provider.Name),
                }
            );
            response.Headers.Location = new Uri(authorizeConsumerUrl);
            return response;
        }

        [Route("callback", Name="callback")]
        [HttpGet]
        public async Task<HttpResponseMessage> Get(string code, string state)
        {
            var cookies = Request.Headers.GetCookies().SelectMany(all => all.Cookies).ToList();


            var csrfTokenCookie = cookies.FirstOrDefault(c => c.Name == "csfr");

            if (csrfTokenCookie == null || csrfTokenCookie.Value != state)
            {
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Possible CSRF attack.");
            }


            var clientIdCookie = cookies.FirstOrDefault(c => c.Name == "clientId");
            var clientSecretCookie = cookies.FirstOrDefault(c => c.Name == "clientSecret");

            if (clientIdCookie == null || clientSecretCookie == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "No credentials.");
            }


            var providerCookie = cookies.FirstOrDefault(c => c.Name == "provider");

            if (providerCookie == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "No provider.");                
            }

            var provider = Providers.First(p => p.Name == providerCookie.Value);


            var credentials = new Credentials
                {
                    ClientId = clientIdCookie.Value,
                    ClientSecret = clientSecretCookie.Value,
                };

            using (var http = new HttpClient(new LegacyJsonMediaTypeConverterDelegatingHandler()))
            {
                var data = new Dictionary<string, string>
                    {
                        {"redirect_uri", new Uri(Request.RequestUri, Url.Route("callback", null)).AbsoluteUri},
                        {"code", code},
                        {"grant_type", "authorization_code"}
                    };

                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Basic",
                    Convert.ToBase64String(
                        Encoding.ASCII.GetBytes(
                            string.Format("{0}:{1}", credentials.ClientId, credentials.ClientSecret)
                        )
                    )
                );

                var response = await http.PostAsync(provider.TokenUrl, new FormUrlEncodedContent(data));

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var result = await response.Content.ReadAsAsync<TokenSuccessfulResponse>();

                    return Request.CreateResponse(
                        HttpStatusCode.OK,
                        new
                            {
                                credentials.ClientId,
                                credentials.ClientSecret,
                                result.TokenType,
                                result.AccessToken
                            }
                    );
                }
                else
                {
                    var result = await response.Content.ReadAsAsync<TokenErrorResponse>();

                    return Request.CreateResponse(HttpStatusCode.InternalServerError, result);
                }
            }
        }

        private static string GenerateCsrfToken()
        {
            var bytes = new byte[18];

            using (var provider = new RNGCryptoServiceProvider())
            {
                provider.GetBytes(bytes);
            }

            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_");
        }
    }
}
