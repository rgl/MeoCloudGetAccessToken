// developed by Rui Lopes (ruilopes.com). licensed under GPLv3.

using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MeoCloudGetAccessToken
{
    public class MeoCloudClient
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(MeoCloudClient));

        private const string AccountInfoUrl = "https://publicapi.meocloud.pt/1/Account/Info";

        private const string CreateFolderUrl = "https://publicapi.meocloud.pt/1/Fileops/CreateFolder";

        private string FilesUrl { get; set; }
        private string MetadataUrl { get; set; }

        private readonly string _root;
        private readonly AuthenticationHeaderValue _authenticationHeaderValue;

        public MeoCloudClient(string accessToken, bool sandbox)
        {
            _root = sandbox ? "sandbox" : "meocloud";

            if (sandbox)
            {
                FilesUrl = "https://api-content.meocloud.pt/1/Files/sandbox";
                MetadataUrl = "https://publicapi.meocloud.pt/1/Metadata/sandbox";
            }
            else
            {
                FilesUrl = "https://api-content.meocloud.pt/1/Files/meocloud";
                MetadataUrl = "https://publicapi.meocloud.pt/1/Metadata/meocloud";
            }

            _authenticationHeaderValue = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        public async Task<AccountInfo> GetAccountInfo()
        {
            using (var http = CreateHttpClient())
            {
                var response = await http.GetAsync(AccountInfoUrl);

                response.EnsureSuccessStatusCode();

                var body = await response.Content.ReadAsStringAsync();

                // we have to remove the uid from the json response because its send as a number that does not fit in a long...
                body = Regex.Replace(body, @"""uid"":\s*\d+\s*,", "");

                var accountInfo = JsonConvert.DeserializeObject<AccountInfo>(body);

                return accountInfo;
            }
        }

        public async Task CreateDirectory(string path, bool createParentDirectories = false)
        {
            if (!path.StartsWith("/"))
            {
                throw new ArgumentOutOfRangeException("path", "Must be an absolute path");
            }

            if (path.EndsWith("/"))
            {
                throw new ArgumentOutOfRangeException("path", "Cannot end with a forward slash");
            }

            if (createParentDirectories)
            {
                var segments = path.Split('/').Reverse().Skip(1).Reverse().ToArray();

                for (var i = 2; i <= segments.Length; ++i)
                {
                    var parentDirectoryPath = string.Join("/", segments.Take(i));

                    await CreateDirectory(parentDirectoryPath);
                }
            }

            var data = new FormUrlEncodedContent(
                new[]
                    {
                        new KeyValuePair<string, string>("root", _root),
                        new KeyValuePair<string, string>("path", path),
                    }
            );

            using (var http = CreateHttpClient())
            {
                var response = await http.PostAsync(CreateFolderUrl, data);

                // CreateFolder successful response:
                //
                //  {
                //      "is_owner": true,
                //      "rev": "c84abd1e-9e79-11e3-8426-e0db55018fa4",
                //      "thumb_exists": false,
                //      "bytes": 0,
                //      "modified": "Wed, 26 Feb 2014 00:06:03 +0000",
                //      "cursor": "action_create_dir,event_c84a7a2a-9e79-11e3-8426-e0db55018fa4,8ee2a682-a196-45b6-81fd-0c71acca374e_c84b0f26-9e79-11e3-8426-e0db55018fa4",
                //      "client_mtime": "Wed, 26 Feb 2014 00:06:03 +0000",
                //      "path": "/public/a",
                //      "is_dir": true,
                //      "size": "0 bytes",
                //      "root": "app_folder",
                //      "hash": "c84a5da6-9e79-11e3-8426-e0db55018fa4",
                //      "icon": "folder"
                //  }

                // 200 OK means the directory didn't yet exists, and was just created.
                // 403 Forbidden means the directory already existed, which is fine for us.
                if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return;
                }

                var body = await response.Content.ReadAsStringAsync();

                throw new Exception(string.Format("Failed to create the directory {0} with StatusCode={1} Body={2}", path, response.StatusCode, body));
            }
        }

        public async Task<Metadata> Upload(string path, HttpContent content, bool createParentDirectories = false, bool overwrite = false, string parentRevision = null)
        {
            if (!path.StartsWith("/"))
            {
                throw new ArgumentOutOfRangeException("path", "Must be an absolute path");
            }

            if (path.EndsWith("/"))
            {
                throw new ArgumentOutOfRangeException("path", "Cannot end with a forward slash");
            }

            if (createParentDirectories)
            {
                var directoryPath = string.Join("/", path.Split('/').Reverse().Skip(1).Reverse());

                if (directoryPath.Length != 0)
                {
                    await CreateDirectory(directoryPath, true);
                }
            }

            if (overwrite && parentRevision == null)
            {
                var metadata = await GetMetadata(path);

                if (metadata != null)
                {
                    parentRevision = metadata.Revision;
                }
                else
                {
                    // no metadata found. the file didn't exist before, so no need to overwrite.
                    overwrite = false;
                }
            }

            var url = overwrite
                ? string.Format("{0}{1}?overwrite=true&parent_rev={2}", FilesUrl, Uri.EscapeUriString(path), Uri.EscapeDataString(parentRevision))
                : string.Format("{0}{1}", FilesUrl, Uri.EscapeUriString(path));

            // TODO remove me url = string.Format("{0}{1}?overwrite=true", FilesUrl, Uri.EscapeUriString(path));

            using (var http = CreateHttpClient())
            {
                var response = await http.PutAsync(url, content);

                // File upload successful response:
                //
                //  {
                //      "bytes": 13,
                //      "thumb_exists": false,
                //      "rev": "fcd87d32-9e79-11e3-9aba-e0db55018fa4",
                //      "modified": "Wed, 26 Feb 2014 00:07:32 +0000",
                //      "cursor": "action_upload,event_fcd83bc4-9e79-11e3-9aba-e0db55018fa4,8ee2a682-a196-45b6-81fd-0c71acca374e_fcd8e1b4-9e79-11e3-9aba-e0db55018fa4",
                //      "mime_type": "text/plain",
                //      "path": "/public/a/test.txt",
                //      "is_dir": false,
                //      "icon": "text_txt",
                //      "root": "app_folder",
                //      "client_mtime": "Wed, 26 Feb 2014 00:07:32 +0000",
                //      "size": "13 bytes"
                //  }

                response.EnsureSuccessStatusCode();

                var metadata = await response.Content.ReadAsAsync<Metadata>();

                return metadata;
            }
        }

        private async Task<Metadata> GetMetadata(string path)
        {
            if (!path.StartsWith("/"))
            {
                throw new ArgumentOutOfRangeException("path", "Must be an absolute path");
            }

            if (path.Length > 1 && path.EndsWith("/"))
            {
                throw new ArgumentOutOfRangeException("path", "Cannot end with a forward slash");
            }

            using (var http = CreateHttpClient())
            {
                var response = await http.GetAsync(MetadataUrl + path);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var metadata = await response.Content.ReadAsAsync<Metadata>();

                return metadata;
            }
        }

        private HttpClient CreateHttpClient()
        {
            var http = new HttpClient();

            http.DefaultRequestHeaders.Authorization = _authenticationHeaderValue;

            return http;
        }
    }

    // e.g.
    //
    //  {
    //      "referral_link": "https://db.tt/Qn6Z5TdN",
    //      "display_name": "Rui Lopes",
    //      "uid": 20421111,
    //      "country": "PT",
    //      "quota_info": {
    //          "datastores": 0,
    //          "shared": 0,
    //          "quota": 34225520640,
    //          "normal": 11646663417
    //      },
    //      "email": "rgl@ruilopes.com"
    //  }
    //
    // See https://www.dropbox.com/developers/core/docs#account-info
    public class AccountInfo
    {
        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        public long Uid { get; set; }

        public string Email { get; set; }

        [JsonProperty("quota_info")]
        public QuotaInfo QuotaInfo { get; set; }
    }

    public class QuotaInfo
    {
        public long Quota { get; set; }
        public long Normal { get; set; }
    }

    // e.g.
    //  {
    //     "hash": "36a8306b-1a13-11e2-859b-3c0754179fed",
    //     "bytes": 0,
    //     "thumb_exists": false,
    //     "rev": "509fc400-2f65-11e2-9501-3c0754179fed",
    //     "modified": "Fri, 12 Oct 2012 15:36:59 +0000",
    //     "client_mtime": "Mon, 18 Jul 2011 18:04:35 +0000",
    //     "is_link": true,
    //     "path": "/stuff/Téstâção",
    //     "is_dir": false,
    //     "root": "meocloud",
    //     "size": "0 bytes"
    //  }
    public class Metadata
    {
        [JsonProperty("bytes")]
        public long Size { get; set; }

        [JsonProperty("rev")]
        public string Revision{ get; set; }
    }
}
