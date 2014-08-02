// developed by Rui Lopes (ruilopes.com). licensed under GPLv3.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MeoCloudGetAccessToken
{
    // NB this is needed for dropbox APIs. they still return JSON using text/javascript mime media type.
    public class LegacyJsonMediaTypeConverterDelegatingHandler : DelegatingHandler
    {
        public LegacyJsonMediaTypeConverterDelegatingHandler()
            : base(new HttpClientHandler())
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (response.Content.Headers.ContentType.MediaType == "text/javascript")
                response.Content.Headers.ContentType.MediaType = "application/json";

            return response;
        }
    }
}
