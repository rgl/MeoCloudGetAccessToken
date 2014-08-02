// developed by Rui Lopes (ruilopes.com). licensed under GPLv3.

using log4net;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace MeoCloudGetAccessToken
{
    internal class MeoCloud
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(MeoCloud));

        public static void Use()
        {
            TestMeoCloudAccess();
            //TestDropboxAccess();
        }

        private static void TestMeoCloudAccess()
        {
            const string accessToken = "TODO";

            var client = new MeoCloudClient(accessToken, true);

            var accountInfo = client.GetAccountInfo().Result;

            Log.DebugFormat(
                "Meo Cloud Account Info DisplayName={0} TotalSpace={1} UsedSpace={2} ({3} %)",
                accountInfo.DisplayName,
                accountInfo.QuotaInfo.Quota.ToHumanReadableBytes(),
                accountInfo.QuotaInfo.Normal.ToHumanReadableBytes(),
                Math.Round(accountInfo.QuotaInfo.Normal * 100.0 / accountInfo.QuotaInfo.Quota, 3)
            );

            client.Upload(
                "/public/a/b/c/d/test.txt",
                new StringContent("Hello, World 33!", Encoding.UTF8, "text/plain"),
                createParentDirectories:true,
                overwrite:true
            ).Wait();
        }

        private static void TestDropboxAccess()
        {
            const string AccessToken = "TODO";
            const string AccountInfoUrl = "https://api.dropbox.com/1/account/info";

            using (var http = new HttpClient(new LegacyJsonMediaTypeConverterDelegatingHandler()))
            {
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

                var response = http.GetAsync(AccountInfoUrl).Result;

                response.EnsureSuccessStatusCode();

                var accountInfo = response.Content.ReadAsAsync<AccountInfo>().Result;

                Log.DebugFormat(
                    "Dropbox Account Info DisplayName={0} TotalSpace={1} UsedSpace={2} ({3} %)",
                    accountInfo.DisplayName,
                    accountInfo.QuotaInfo.Quota.ToHumanReadableBytes(),
                    accountInfo.QuotaInfo.Normal.ToHumanReadableBytes(),
                    Math.Round(accountInfo.QuotaInfo.Normal * 100.0 / accountInfo.QuotaInfo.Quota, 3)
                );
            }
        }

    }
}