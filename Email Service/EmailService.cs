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
using System.Net.Mime;

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
            if (_smtpEmailSender == null) return;
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

        //pretty good
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
                //Port = source.Configs["Account"].GetInt("Port");
                Port = GetConfig<int>(source, "Account", "Port", true);
                Anon = source.Configs["Account"].GetBoolean("Anon");
                //General
                TestRun = source.Configs["General"].GetBoolean("TestRun");
                TestRecipient = source.Configs["General"].GetString("TestRecipient");
                Limit = source.Configs["General"].GetInt("Limit");

                //Email
                Subject = source.Configs["Email"].GetString("Subject");
                Cc = source.Configs["Email"].GetString("Cc").Split(';');
                SuffixCorrection = source.Configs["Email"].GetBoolean("SuffixCorrection");
                Content = source.Configs["Email"].GetString("Content");
                Greeting = source.Configs["Email"].GetString("Greeting");
                Salution = source.Configs["Email"].GetString("Salution");

                //Format
                RecipientMap = GetConfig<string>(source, "Format", "RecipientMap", true);
                ParseToRecipientMap(RecipientMap);


                //File
                ScanFolder = source.Configs["File"].GetString("Folder");
                SizeThreshold = source.Configs["File"].GetLong("SizeThreshold");
                
                //Interval
                ScanInterval = source.Configs["Interval"].GetLong("ScanInterval") * 1000; //in sec
                RetryInterval = source.Configs["Interval"].GetInt("EmailRetry");
                ExitOnSendingThreadCompletion = source.Configs["Interval"].GetBoolean("ExitOnSendingThreadCompletion");
            }
            catch (NullReferenceException ex)
            {
                Log("config.ini is not in correct format.");
                Log(ex.Message);
                throw;
            }
            catch(InvalidOperationException ex)
            {
                Log(ex.Message);
                throw;
            }
            catch(ArgumentException ex)
            {
                Log(ex.Message);
                throw;
            }
        }
        //exp: 2W:luu.nhat-hong, SV:luu.nhat-hong
        private void ParseToRecipientMap(string map)
        {
            var split = map.Replace(" ", null).Split(','); //trim spaces then split pair
            if (split.Count() == 0) throw new ArgumentException($"RecipientMap is invalid: {map}");
            foreach (var s in split)
            {
                var pair = s.Split(':');
                if (pair.Count() != 2) throw new ArgumentException($"invalid recipient map string {s}");
                RecipientDict.Add(pair.First(), pair.Last());
                Logger.Log($"{pair.First()} -> {pair.Last()}");
            }
            if (RecipientDict.Count < 1) throw new InvalidDataException("readinng RecipientMap result zero recipients");
        }

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
                if (SizeThreshold == 0) return fileNames;
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
                List<MailMessage> emails;
                //use map or not
                emails = FilesToEmail(fileToBeProcessed)?.ToList();

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

        private MailMessage CreateMailMessage(EmailBuilder builder)
        {
            var mail = new MailMessage
            {
                From = new MailAddress(CheckSuffix(_smtpEmailSender.EmailAccount)),
                Subject = Subject,
                Body = builder.BuildContent(Content, Greeting, Salution),
                IsBodyHtml = true
            };
            return mail;
        }

        private List<MailAddress> GetRecipientList(string fileName)
        {
            //add recipients
            var sendTo = new List<MailAddress>();
            //if TestRun then route all emails to TestRecipient
            if (TestRun)
            {
                sendTo.Add(new MailAddress(CheckSuffix(TestRecipient)));
            }
            else
            {
                var match = new List<string>();
                //find recipient email
                foreach (var pair in RecipientDict)
                {
                    if(fileName.Split('.').First().Contains(pair.Key)) //find string in name part
                    {
                        match.Add(pair.Value);
                    }
                }
                
                if(match.Count > 0)
                {
                    match = match.Distinct().ToList(); //trim duplicate
                    foreach (var address in match)
                    {
                        sendTo.Add(new MailAddress(CheckSuffix(address)));
                    }
                }
                else
                {
                    //Log($"file: {fileName} doesnt have a match recipient.");
                    return null;
                }
            }
            return sendTo;
        }

        private void AddAttachment(string fileName, MailMessage mail)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new InvalidOperationException("attatment file name is null or empty");
            var attachment = new Attachment(fileName, MediaTypeNames.Application.Octet);
            ContentDisposition disposition = attachment.ContentDisposition;
            disposition.CreationDate = File.GetCreationTime(fileName);
            disposition.ModificationDate = File.GetLastWriteTime(fileName);
            disposition.ReadDate = File.GetLastAccessTime(fileName);
            disposition.FileName = Path.GetFileName(fileName);
            disposition.Size = new FileInfo(fileName).Length;
            disposition.DispositionType = DispositionTypeNames.Attachment;
            mail.Attachments.Add(attachment);
        }
        //a much faster algorithm
        private IEnumerable<MailMessage> FilesToEmail(IEnumerable<string> files)
        {
            var emails = new List<MailMessage>();
            foreach (string fileName in files)
            {
                try
                {
                    var name = fileName.Split('\\').Last();
                    Log(string.Format("File: {0}", name));
                    var builder = new EmailBuilder();
                    var mail = CreateMailMessage(builder);
                    //add default CC
                    if (!string.IsNullOrEmpty(CcAddresses))
                    {
                        mail.CC.Add(CcAddresses);
                    }
                    var recipient = GetRecipientList(name); //recipent base on file name mapping
                    if(recipient == null || recipient.Count < 1)
                    {
                        Log($"^ doesnt have recipients");
                        continue;
                    }
                    //add recipents
                    Log("^ Recipient:");
                    foreach (var r in recipient)
                    {
                        mail.To.Add(r);
                        Log(r.Address);
                    }
                    AddAttachment(fileName, mail);
                    emails.Add(mail);
                }
                catch (UnauthorizedAccessException ex)
                {
                    //account maybe locked -> abort this scan routine, may try again later.
                    Log(ex.Message);
                    Log(">>>Abort routine.");
                    return null;
                }
                catch (Exception ex)
                {
                    Log(string.Format("Unhandled exception on reading {0}", fileName));
                    Log(string.Format(ex.Message));
                    Log(">>>Skip this file");
                }
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
        //General
        private bool TestRun;
        private string TestRecipient;
        private int Limit;   
        private string DefaultSuffix;

        //Account
        private string SenderAccount;
        private string EmailServer;
        private string EmailPwd;
        private int Port;
        private bool Anon;

        //Email
        private string Subject;
        private string[] Cc;
        private string Content;
        private string Greeting;
        private string Salution;
        private bool SuffixCorrection;
        //private bool ContentAsCSV;

        //Format
        //private char Delimiter;
        //private string GroupBy;
        //private string AdditionalRecipient;
        //private bool RecipientMap;
        private string RecipientMap;
        private Dictionary<string, string> RecipientDict = new Dictionary<string, string>();

        //File
        private string ScanFolder;
        private long SizeThreshold;
        //private string CSVTempFolder;
        //Interval
        private int RetryInterval;
        private long ScanInterval;
        private bool ExitOnSendingThreadCompletion;


        //Others
        private const string CONFIG_FILE_NAME = "config.ini";
        private const string PROCESS_FILE_NAME = "processed.txt";
        //private const string CSV_TEMP_FOLDER = "tempCSV";
        private SmtpMailSender _smtpEmailSender;
        private readonly Timer _timer = new Timer();
        private List<string> _processedList;
        //private Dictionary<ContentMap, List<string>> _csvDict = new Dictionary<ContentMap, List<string>>();
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(EmailService));

        #endregion
    }
}