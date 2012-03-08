using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace ResPack
{
    internal static class Program
    {
        const string PATH_PROP = "CLOJURE_COMPILE_PATH";

        static Program()
        {
            AppDomain.CurrentDomain.UnhandledException +=
                (sender, args) => LogErrorAndExit(args.ExceptionObject as Exception);
        }

        private static void LogErrorAndExit(Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Environment.Exit(ExitCode.Error);
        }

        private static void Main(string[] args)
        {
            switch (args.Length)
            {
                case 0:
                    throw new Exception("missing resource file name");

                case 1:
                    throw new Exception("missing content file name");
            }

            var dir = Environment.GetEnvironmentVariable(PATH_PROP) ?? Environment.CurrentDirectory;
            args.Skip(1).PackResources(dir, args[0]);
        }

        private static void PackResources(this IEnumerable<string> resources, string directory, string fileName)
        {
            var entries = new Dictionary<string, byte[]>();
            
            foreach (var res in resources.Where(r => !String.IsNullOrWhiteSpace(r)))
            {
                var contentFile = Path.Combine(directory, res.Trim());
                var data = File.ReadAllBytes(contentFile);
                entries.Add(res, data);
            }

            var resFile = Path.Combine(directory, fileName);
            entries.WriteResourceFile(resFile);
        }
    }

    internal static class ExitCode
    {
        public const int Error = -1;
        public const int Success = 0;
    }
}