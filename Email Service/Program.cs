using System;
using System.ServiceProcess;
using AnEmailService.Log;

namespace AnEmailService
{
    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        private static void Main(string[] args)
        {
            if (Environment.UserInteractive || ForceConsole(args))
            {
                var emailService = new EmailService(args);
                LogManager.IsConsole = true;
                emailService.ConsoleStart(args);
            }
            else
            {
                var servicesToRun = new ServiceBase[]
                {
                    new EmailService(args)
                };
                ServiceBase.Run(servicesToRun);
            }
        }
        private static bool ForceConsole(string [] args)
        {
            foreach (var a in args)
            {
                return string.Compare(a, "-console", true) == 0;
            }
            return false;
        }
        //public static bool HasWritePermissionOnDir(string path)
        //{
        //    var writeAllow = false;
        //    var accessControlList = Directory.GetAccessControl(path);
        //    if (accessControlList == null)
        //        return false;
        //    var accessRules = accessControlList.GetAccessRules(true, true,
        //                                typeof(System.Security.Principal.SecurityIdentifier));
        //    if (accessRules == null)
        //        return false;

        //    foreach (FileSystemAccessRule rule in accessRules)
        //    {
        //        if ((FileSystemRights.Write & rule.FileSystemRights) != FileSystemRights.Write)
        //            continue;

        //        if (rule.AccessControlType == AccessControlType.Allow)
        //            writeAllow = true;
        //    }

        //    return writeAllow;
        //}
        //internal static string AssemblyDirectory
        //{
        //    get
        //    {
        //        string codeBase = Assembly.GetExecutingAssembly().CodeBase;
        //        var uri = new UriBuilder(codeBase);
        //        string path = Uri.UnescapeDataString(uri.Path);
        //        return Path.GetDirectoryName(path);
        //    }
        //}
    }
}