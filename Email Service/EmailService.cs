using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Timers;
using AnEmailService.Csv.Exceptions;
using AnEmailService.EmailComposer;
using AnEmailService.EmailSender;
using AnEmailService.Log;
using AnEmailService.Nini.Config;

namespace AnEmailService
{
    public partial class EmailService : ServiceBase
    {
        private static readonly Uri SomeBaseUri = new Uri(@"C:\somwhere");
        public bool ConsoleMode { get; private set; } = false;
        public EmailService(string[] args)
        {
            InitializeComponent();
        }

        private static string Version => Assembly.GetEntryAssembly().GetName().Version.ToString();

        internal string CcAddresses
        {
            get
            {
                if (!Cc.Any()) return string.Empty;
                var builder = new StringBuilder();
                foreach (string cc in Cc)
                {
                    if (string.IsNullOrEmpty(cc)) continue;
                    builder.Append(CheckSuffix(cc.Trim())).Append(", ");
                }
                //clean head & tail
                return builder.ToString().Trim(',', ' ');
            }
        }

        internal static string ConfigFileName => string.Format(@"{0}\{1}", AssemblyDirectory, CONFIG_FILE_NAME);

        internal static string ProcessedFilePath => string.Format(@"{0}\{1}", AssemblyDirectory, PROCESS_FILE_NAME);

        internal static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        internal void ConsoleStart(string[] args)
        {
            //able to run in 2 modes is cool
            ConsoleMode = true;
            Log("Run in console mode....");
            OnStart(args);
            while(_smtpEmailSender.IsThreadRunning)
            {
                Console.ReadLine();
                Console.WriteLine("Sending thread is running -> kill process to exit.");
            }
            //Console.ReadLine();
            //OnStop();
        }

        private List<string> GetProcessedList()
        {
            try
            {
                return
                    File.ReadAllLines(ProcessedFilePath)
                        .Select(name => string.Format(@"{0}\{1}", ScanFolder, name))
                        .ToList();
            }
            catch (FileNotFoundException) //none exists
            {
                Log("Cant find processed.txt -> assume no processed");
                return new List<string>();
            }
            catch (Exception ex) //other exceptions are fatal to the operation, better stop
            {
                Log(ex.Message);
                throw;
            }
        }
		
		private T GetConfig<T>(IniConfigSource source, string section, string name, bool notNull = false)
        {
            object value = source.Configs[section].GetString(name);
            if(notNull)
            {
                if (string.IsNullOrEmpty(value.ToString())) throw new ArgumentException($"config: {section}->{name} cannot be null");
            }
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                Log($"config: {section}->{name} is invalid");
                Log(ex.Message);
                throw;
            }
            
        }
		
        private void ReadConfig()
        {
            try
            {
                var source = new IniConfigSource(string.Format(@"{0}\{1}", AssemblyDirectory, CONFIG_FILE_NAME));
                //Account
                SenderAccount = source.Configs["Account"].GetString("SenderAccount");
                EmailPwd = source.Configs["Account"].GetString("Pwd");
                EmailServer = source.Configs["Account"].GetString("Server");
                DefaultSuffix = source.Configs["Account"].GetString("DefaultSuffix");
                Port = source.Configs["Account"].GetInt("Port");
                Anon = source.Configs["Account"].GetBoolean("Anon");
                //General
                TestRun = source.Configs["General"].GetBoolean("TestRun");
                TestRecipient = source.Configs["General"].GetString("TestRecipient");
                Limit = source.Configs["General"].GetInt("Limit");

                //Email
                Subject = source.Configs["Email"].GetString("Subject");
                Cc = source.Configs["Email"].GetString("Cc").Split(';');
                SuffixCorrection = source.Configs["Email"].GetBoolean("SuffixCorrection");
                Greeting = source.Configs["Email"].GetString("Greeting");
                Salution = source.Configs["Email"].GetString("Salution");

                //Format
                string delimiter = source.Configs["Format"].GetString("Delimiter");
                if (string.Compare(delimiter, @"\t", true) == 0) Delimiter = Convert.ToChar(9);
                else Delimiter = char.Parse(delimiter);
                GroupBy = source.Configs["Format"].GetString("GroupBy");
                AdditionalRecipient = source.Configs["Format"].GetString("AdditionalRecipient");

                //File
                ScanFolder = source.Configs["File"].GetString("Folder");
                SizeThreshold = source.Configs["File"].GetLong("SizeThreshold");

                //Interval
                ScanInterval = source.Configs["Interval"].GetLong("ScanInterval") * 1000; //in sec
                RetryInterval = source.Configs["Interval"].GetInt("EmailRetry");
                ExitOnSendingThreadCompletion = source.Configs["Interval"].GetBoolean("ExitOnSendingThreadCompletion");
            }
            catch (Exception ex)
            {
                Log("config.ini is not in correct format.");
                Log(ex.Message);
                throw;
            }
        }

        //public static bool HasWritePermissionOnDir(string path)
        //{
        //    var writeAllow = false;
        //    var writeDeny = false;
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
        //        else if (rule.AccessControlType == AccessControlType.Deny)
        //            writeDeny = true;
        //    }

        //    return writeAllow && !writeDeny;
        //}

        protected override void OnStart(string[] args)
        {
            Log(string.Format("##################### Version: {0} #####################", Version));
            try
            {
                ReadConfig();
            }
            catch (Exception ex)
            {
                Stop();
                return;
            }
            Log("Read config completed.");
            if (TestRun) Log("Test mode activated.");
            _processedList = GetProcessedList();
            if(Anon)
            {
                Log($"Open relay SMTP: {EmailServer}:{Port}");

                _smtpEmailSender = new SmtpMailSender(SenderAccount, EmailServer, Port);
            }
            else
            {
                Log($"Normal SMTP: {EmailServer}:{Port}");
                _smtpEmailSender = new SmtpMailSender(SenderAccount, EmailPwd, EmailServer, Port) { SleepInterval = RetryInterval };
            }
            Log("Service started sucsessfully.");
            StartTimer();
        }

        private void StartTimer()
        {
            _smtpEmailSender.OnEmailSendingThreadExit += _smtpEmailSender_OnEmailSendingThreadExit;
            _timer.Interval = ScanInterval;
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
            _timer_Elapsed(null, null);
        }

        private void _smtpEmailSender_OnEmailSendingThreadExit(object sender, EmailSendingThreadEventArgs e)
        {
            //print stats
            Log(string.Format("Thread exit code: {0}", e.StopOnUnrecoverableException ? "ERROR" : "OK"));
            Log($"Total email sent: {e.TotalSent}");
            Log($"Total retries: {e.TotalRetries}");
            if (ExitOnSendingThreadCompletion)
            {
                //stop service
                Stop();
                if (ConsoleMode)
                    //exit console
                    Environment.Exit(e.StopOnUnrecoverableException ? 1 : 0);
            }
        }

        private void StopTimer()
        {
            _timer.Elapsed -= _timer_Elapsed;
            _timer.Stop();
        }

        private IEnumerable<string> FilterFilesMetSizeThreshold(IEnumerable<string> fileNames)
        {
            try
            {
				if(SizeThreshold == 0) return fileNames;
                return fileNames.TakeWhile(file => new FileInfo(file).Length > SizeThreshold).ToList();
            }
            catch (Exception ex)
            {
                Log("Error getting file info. -> terminate");
                Log(ex.Message);
                return null;
            }
        }

        //scan & send email routine
        //very careful when change this
        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //start scanning
            Log("++++++++ Routine start ++++++++");
            try
            {
                var files = ScanNewFilesToProcess(ScanFolder)?.ToList();
                if (files == null) return; //error occurred
                if (files.Count < 1) return; //no files in the folder
                //take file with met size
                var fileToBeProcessed = FilterFilesMetSizeThreshold(files)?.ToList();
                if (fileToBeProcessed == null) return; //cant get file info -> skip this scan now

                Log(string.Format("{0} files met defined threshold", fileToBeProcessed.Count));
                //if 0 file met, expected -> add all file to processed
                //write to processed list
                if (!WriteToProcessedFile(files)) //doesnt matter met condition or not, add to skip
                {
                    Log("Failed to write to processed -> skip routine");
                    return;
                }
                //update list
                _processedList.AddRange(files);
                if (fileToBeProcessed.Count < 1) return; //added to processed now skip this scan
                //to emails message
                //test new algorithm
                //var emails = ParseFilesToEmail(fileToBeProcessed)?.ToList();
                var emails = ParseFilesToEmail_2(fileToBeProcessed)?.ToList();
                if (emails == null || emails.Count <= 0) return;
                for (int i = 0; i < emails.Count; i++)
                {
                    _smtpEmailSender.EnqueueEmail(emails[i]);
                    //uhmmmm weird?
                    if (i + 1 != Limit || Limit <= 0 || !TestRun) continue;
                    Log(string.Format("{0} emails limit reached -> skip the rest", i + 1));
                    break;
                }
                //emails.ForEach(mail => _exSender.EnqueueEmail(mail));
            }
            finally
            {
                Log("++++++++ Routine completed. ++++++++");
                Log(string.Format("Next scan: {0}", DateTime.Now.AddSeconds(ScanInterval / 1000)));
            }
            Log("Start sending thread....");
            _smtpEmailSender.StartSending();
        }

        //private IEnumerable<MailMessage> ParseFilesToEmail(IEnumerable<string> files)
        //{
        //    var emails = new List<MailMessage>();
        //    foreach (string file in files)
        //    {
        //        try
        //        {
        //            Log(string.Format("Reading: {0}", file));
        //            string[] headers;
        //            string[,] content;
        //            CsvToArray.ReadCsvTo2DArray(file, Delimiter, out headers, out content);
        //            Log("Innit email builder...");
        //            var builder = new EmailBuilder(headers, content);
        //            Log("Building maps...");
        //            var maps = builder.GetMap(GroupBy);
        //            Log("Write emails...");
        //            foreach (var map in maps)
        //            {
        //                //string to = string.Empty;
        //                try
        //                {
        //                    var mail = new MailMessage()
        //                    {
        //                        From = new MailAddress(CheckSuffix(_exSender.EmailAccount)),
        //                        Subject = Subject,
        //                        Body = builder.BuildContent(map, Greeting, Salution),
        //                        IsBodyHtml = true
        //                    };
        //                    //add default CC
        //                    if (!string.IsNullOrEmpty(CcAddresses))
        //                        mail.CC.Add(CcAddresses);
        //                    var sendTo = new List<MailAddress>();
        //                    //add recipients
        //                    //if TestRun then route all emails to TestRecipient
        //                    if (TestRun)
        //                    {
        //                        sendTo.Add(new MailAddress(CheckSuffix(TestRecipient)));
        //                    }
        //                    else
        //                    {
        //                        sendTo.Add(new MailAddress(CheckSuffix(map.Group)));
        //                        //add extra recipients
        //                        var extraRecipients = builder.BuildAdditionalRecipients(AdditionalRecipient, map);
        //                        foreach (var extraRecipient in extraRecipients)
        //                        {
        //                            sendTo.Add(new MailAddress(CheckSuffix(extraRecipient)));
        //                        }
        //                    }
        //                    foreach (var recipient in sendTo)
        //                    {
        //                        mail.To.Add(recipient);
        //                    }
        //                    emails.Add(mail);
        //                }
        //                //catch MailAddress ex
        //                catch (FormatException)
        //                {
        //                    Log("Email address is not valid:");
        //                    Log("From: " + CheckSuffix(TestRecipient));
        //                    Log("To: " + CheckSuffix(map.Group));
        //                    Log("Cc: " + CcAddresses);
        //                }
        //            }
        //            if (emails.Count < 1) throw new InvalidDataException("Cant parse to any email from data, possibly corrupted data.");
        //        }
        //        catch (UnauthorizedAccessException ex)
        //        {
        //            //account maybe locked -> abort this scan routine, may try again later.
        //            Log(ex.Message ?? string.Empty);
        //            Log(">>>Abort routine.");
        //            return null;
        //        }
        //        //CsvToArray.ReadCsvTo2DArray throws these ex
        //        catch (InvalidDataException ex)
        //        {
        //            Log(ex.Message ?? string.Empty);
        //            Log(">>>Skip this file");
        //        }
        //        catch (MissingFieldCsvException ex)
        //        {
        //            Log(string.Format("Csv fields and columns are not even!", file));
        //            Log(string.Format(ex.Message ?? string.Empty));
        //            Log(">>>Skip this file");
        //        }
        //        catch (Exception ex)
        //        {
        //            Log(string.Format("Unhandled exception on reading {0}", file));
        //            Log(string.Format(ex.Message ?? string.Empty));
        //            Log(">>>Skip this file");
        //        }
        //    }
        //    Log(string.Format("Total email(s): {0} ", emails.Count));
        //    return emails;
        //}

        //a much faster algorithm

        private IEnumerable<MailMessage> ParseFilesToEmail_2(IEnumerable<string> files)
        {
            var emails = new List<MailMessage>();
            foreach (string file in files)
                try
                {
                    Log(string.Format("Reading: {0}", file));
                    //test new CSV reader
                    CsvToArray.ReadCsv(file, Delimiter, out string[] headers, out List<string[]> content);
                    Log(string.Format("Rows: {0}", content.Count));
                    //this one randomly pauses with no exception thrown
                    //CsvToArray.ReadCsvTo2DArray(file, Delimiter, out headers, out content);
                    Log("Innitializing email builder...");
                    var builder = new EmailBuilder(headers, content);
                    Log("Building maps...");
                    var maps = builder.GetMap_2(GroupBy);
                    Log("Preparing emails...");
                    foreach (var contentMap in maps)
                        try
                        {
                            //NYI: lazy this class body to reduce memory
                            var mail = new MailMessage
                            {
                                From = new MailAddress(CheckSuffix(_smtpEmailSender.EmailAccount)),
                                Subject = Subject,
                                Body = builder.BuildContent(contentMap, Greeting, Salution),
                                IsBodyHtml = true
                            };
                            //add default CC
                            if (!string.IsNullOrEmpty(CcAddresses))
                                mail.CC.Add(CcAddresses);
                            var sendTo = new List<MailAddress>();
                            //add recipients
                            //if TestRun then route all emails to TestRecipient
                            if (TestRun)
                            {
                                sendTo.Add(new MailAddress(CheckSuffix(TestRecipient)));
                            }
                            else
                            {
                                sendTo.Add(new MailAddress(CheckSuffix(contentMap.Group)));
                                //add extra recipients
                                var extraRecipients = builder.BuildAdditionalRecipients(AdditionalRecipient, contentMap);
                                sendTo.AddRange(extraRecipients.Select(extraRecipient => new MailAddress(CheckSuffix(extraRecipient))));
                            }
                            foreach (var recipient in sendTo)
                                mail.To.Add(recipient);
                            emails.Add(mail);
                        }
                        //catch MailAddress ex
                        catch (FormatException)
                        {
                            Log("Email address is not valid:");
                            Log("From: " + CheckSuffix(TestRecipient));
                            Log("To: " + CheckSuffix(contentMap.Group));
                            Log("Cc: " + CcAddresses);
                        }
                    if (emails.Count < 1)
                        throw new InvalidDataException("Cant parse to any email from data, possibly corrupted data.");
                    //release, idk if this makes GC collecting faster...?
                    //content.Clear();
                    //maps = null;
                    //builder.Dispose();
                }
                catch (UnauthorizedAccessException ex)
                {
                    //account maybe locked -> abort this scan routine, may try again later.
                    Log(ex.Message);
                    Log(">>>Abort routine.");
                    return null;
                }
                //CsvToArray.ReadCsvTo2DArray throws these ex
                catch (InvalidDataException ex)
                {
                    Log(ex.Message);
                    Log(">>>Skip this file");
                }
                catch (MissingFieldCsvException ex)
                {
                    Log(string.Format("Csv fields and columns are not even!", file));
                    Log(string.Format(ex.Message));
                    Log(">>>Skip this file");
                }
                catch (Exception ex)
                {
                    Log(string.Format("Unhandled exception on reading {0}", file));
                    Log(string.Format(ex.Message));
                    Log(">>>Skip this file");
                }
            Log(string.Format("Total email(s): {0} ", emails.Count));
            return emails;
        }

        private IEnumerable<string> ScanNewFilesToProcess(string folderPath)
        {
            Log("Start scanning at " + folderPath);
            List<string> files;
            try
            {
                files = Directory.GetFiles(folderPath, "*.csv").Except(_processedList).ToList();
                if (files.Count < 1)
                {
                    Log("Nothing new to process...back to sleep Zzz");
                    return null;
                }
            }
            catch (DirectoryNotFoundException) //folder may be available next time -> skip this scan
            {
                Log("Cannot find " + folderPath);
                return null;
            }
            catch (IOException ex) //cant connect FTP, folder may be available next time -> skip this scan
            {
                //Log("Error on get filenames in " + fileName);
                Log(ex.Message);
                return null;
            }
            catch (UnauthorizedAccessException ex) //in case log on account got locked, try again later
            {
                Log("Access to the folder is unauthorized!");
                Log(ex.Message);
                return null;
            }
            catch (Exception ex) //no
            {
                Log("Unhandled exception on accessing scanned folder. -> terminate");
                Log(ex.Message);
                throw;
            }
            return files;
        }

        protected override void OnStop()
        {
            StopTimer();
            Log("Service stopped.");
        }

        private bool WriteToProcessedFile(IEnumerable<string> list)
        {
            Log("Writing to processed list.");
            try
            {
                File.AppendAllLines(ProcessedFilePath, list.Select(GetFileNameFromUrl));
                return true;
            }
            catch (Exception ex)
            {
                Log("Failed write processed.txt \n" + ex.Message);
                return false;
            }
        }

        private string CheckSuffix(string address)
            => SuffixCorrection ? (address.Contains("@") ? address : address + DefaultSuffix) : address;

        private static void Log(string log) => Logger.Log(log);

        //this seems ugly af :/
        private static string GetFileNameFromUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                uri = new Uri(SomeBaseUri, url);

            return Path.GetFileName(uri.LocalPath);
        }

        #region Variables

        private string SenderAccount;
        private string EmailServer;
        private string EmailPwd;
        private int Port;
        private bool Anon;
        private bool TestRun;
        private string TestRecipient;
        private int Limit;
        private string ScanFolder;
        private string DefaultSuffix;
        private char Delimiter;
        private long SizeThreshold;
        private string GroupBy;
        private string AdditionalRecipient;
        private int RetryInterval;
        private long ScanInterval;
        private bool ExitOnSendingThreadCompletion;
        private string Subject;
        private string[] Cc;
        private string Greeting;
        private string Salution;
        private bool SuffixCorrection;
        private const string CONFIG_FILE_NAME = "config.ini";
        private const string PROCESS_FILE_NAME = "processed.txt";
        private SmtpMailSender _smtpEmailSender;
        private readonly Timer _timer = new Timer();
        private List<string> _processedList;
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(EmailService));

        #endregion
    }
}