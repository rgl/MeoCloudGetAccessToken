// developed by Rui Lopes (ruilopes.com). licensed under GPLv3.

using log4net;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.StaticFiles;
using Microsoft.Owin.StaticFiles.Infrastructure;
using Newtonsoft.Json.Serialization;
using Owin;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Web.Http;

namespace MeoCloudGetAccessToken
{
    public class HttpHost
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(HttpHost));
        private readonly string _listenUrl;
        private IDisposable _webApp;

        public HttpHost(string listenUrl)
        {
            _listenUrl = listenUrl;
        }

        public void Start()
        {
            // NB if WebApp.Start fails with:
            //
            //  An exception of type 'System.MissingMemberException' occurred in Microsoft.Owin.Hosting.dll but was not handled in user code
            //
            //  Additional information: The server factory could not be located for the given input: Microsoft.Owin.Host.HttpListener
            //
            // You need to explicitly add the Microsoft.Owin.Host.HttpListener.dll assembly to the main application project. Or better, add
            // the Microsoft ASP.NET Web API 2 OWIN Self Host nuget package.

            _webApp = WebApp.Start(_listenUrl, Startup);

            Log.InfoFormat("Listening at {0}", _listenUrl);
        }

        public void Stop()
        {
            _webApp.Dispose();
        }

        private void Startup(IAppBuilder app)
        {
            var config = new HttpConfiguration();

            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute("DefaultApi", "api/{controller}/{id}", new { id = RouteParameter.Optional });

            // use JSON by default.
            var appXmlType = config.Formatters.XmlFormatter.SupportedMediaTypes.FirstOrDefault(t => t.MediaType == "application/xml");
            config.Formatters.XmlFormatter.SupportedMediaTypes.Remove(appXmlType);

            // automatically convert JSON attribute names to camelCase.
            var json = config.Formatters.JsonFormatter;
            json.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

            app.UseWebApi(config);

            var staticFilesSharedOptions = new SharedOptions
                {
                    FileSystem = new PhysicalFileSystem(GetStaticFilesPath())
                };
            app.UseDefaultFiles(new DefaultFilesOptions(staticFilesSharedOptions) { DefaultFileNames = new[] { "index.html" } });
            app.UseStaticFiles(new StaticFileOptions(staticFilesSharedOptions));
        }

        // on DEBUG return the Public sub-directory where the current cs file is. otherwise, use the assembly location.
        private string GetStaticFilesPath([CallerFilePath] string filePath = null)
        {
#if DEBUG
            var basePath = Path.GetDirectoryName(filePath);
#else
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
#endif

            return Path.Combine(basePath, "Public");
        }
    }
}
