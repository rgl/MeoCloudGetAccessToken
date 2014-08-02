// developed by Rui Lopes (ruilopes.com). licensed under GPLv3.

using log4net;
using log4net.Config;
using System;
using System.IO;
using System.Reflection;

namespace MeoCloudGetAccessToken
{
    class Program
    {
        static void Main(string[] args)
        {
            InitializeLog4Net();

            //MeoCloud.Use();

            RunHttpHost();
        }

        private static void RunHttpHost()
        {
            // TODO let the user pass the listen address on the command line.
            var httpHost = new HttpHost("http://+:8008");

            httpHost.Start();

            Console.WriteLine("Press ENTER to quit");
            Console.ReadLine();

            httpHost.Stop();
        }

        private static void InitializeLog4Net()
        {
            var logConfigPath = new FileInfo(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "log4net.config"));

            GlobalContext.Properties["app"] = ((AssemblyProductAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false)[0]).Product;

            var logsBasePath = new DirectoryInfo(Path.Combine(logConfigPath.Directory.FullName, "Logs"));
            if (!logsBasePath.Exists)
                logsBasePath.Create();
            GlobalContext.Properties["logsBasePath"] = logsBasePath.FullName;

            XmlConfigurator.ConfigureAndWatch(logConfigPath);
        }
    }
}
