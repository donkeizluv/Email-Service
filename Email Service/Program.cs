using AnEmailService.Log;
using System;
using System.ServiceProcess;

namespace AnEmailService
{
    internal static partial class Program
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

        private static bool ForceConsole(string[] args)
        {
            foreach (var a in args)
            {
                return string.Compare(a, "-console", true) == 0;
            }
            return false;
        }
    }
}