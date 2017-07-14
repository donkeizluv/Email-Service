using AnEmailService.EmailComposer;
using AnEmailService.EmailSender;
using AnEmailService.Log;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.ServiceProcess;
using System.Text;
using System.Timers;

namespace AnEmailService
{
    public partial class EmailService : ServiceBase
    {
        private SmtpMailSender _smtpEmailSender;
        private readonly Timer _timer = new Timer();
        private List<string> _processedList;
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(EmailService));
        private static readonly Uri SomeBaseUri = new Uri(@"C:\somwhere");

        public bool ConsoleMode { get; private set; } = false;

        public EmailService(string[] args)
        {
            InitializeComponent();
        }

        internal void ConsoleStart(string[] args)
        {
            //able to run in 2 modes is cool
            ConsoleMode = true;
            Log("Run in console mode....");
            OnStart(args);
            while (_smtpEmailSender.IsThreadRunning)
            {
                Console.ReadLine();
                Console.WriteLine("Sending thread is running -> kill process to exit.");
            }
            //Console.ReadLine();
            //OnStop();
        }

        protected override void OnStart(string[] args)
        {
            Log(string.Format("##################### Version: {0} #####################", Version));
            if (!ReadConfig())
            {
                Stop();
                return;
            }
            Log("Read config completed.");
            if (TestRun) Log("Test mode activated.");
            _processedList = GetProcessedList();
            if (Anon)
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

        //scan & send email routine
        //very careful when change this
        private void StartRoutine()
        {
            Log("++++++++ Routine start ++++++++");
            //if (CleanTempFolder)
            //{
            //    CleanTempFolderFiles();
            //}
            try
            {
                if (!ScanNewFilesToProcess(Folder, out var summary, out var details))
                {
                    Log("Failed to get summary & details files -> skip");
                    Log($"Summary: {summary}, Details: {details}");
                    return;
                }

                if (!WriteToProcessedFile(summary) || !WriteToProcessedFile(details)) //doesnt matter met condition or not, add to skip
                {
                    Log("Failed to write to processed -> skip routine");
                    return;
                }
                //update list
                _processedList.Add(summary);
                _processedList.Add(details);
                //to emails message
                var emails = ParseFilesToEmail(summary, details).ToList();
                if (emails == null || emails.Count <= 0) return;
                //move file to done folder
                if (MoveToDoneFolder)
                {
                    Log("Move files to done...");
                    MoveFileToDone(summary);
                    MoveFileToDone(details);
                }
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

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //start scanning
            StartRoutine();
        }

        //a much faster algorithm
        //fucking procedural shit
        private IEnumerable<MailMessage> ParseFilesToEmail(string summaryFilename, string detailsFilename)
        {
            var emails = new List<MailMessage>();
            try
            {
                //read CSV
                Log(string.Format("Reading summary: {0}", GetFileNameFromUrl(summaryFilename)));
                CsvToArray.ReadCsv(summaryFilename, Delimiter, out string[] summaryHeader, out List<string[]> summaryContent);
                if (summaryContent.Count < 1) throw new InvalidDataException("Summary doesnt have any data.");
                Log(string.Format("Rows: {0}", summaryContent.Count));
                Log(string.Format("Reading details: {0}", GetFileNameFromUrl(detailsFilename)));
                CsvToArray.ReadCsv(detailsFilename, Delimiter, out string[] detailHeader, out List<string[]> detailContent);
                Log(string.Format("Rows: {0}", detailContent.Count));
                //get indexes
                int summaryGroupIndex = Array.FindIndex(summaryHeader, (s => string.Compare(SummaryColumnToGroup, s, true) == 0));
                if (summaryGroupIndex == -1) throw new InvalidDataException($"Summary file doesnt contain column: {SummaryColumnToGroup}");

                //map detail and content
                var summaryMapDict = Helper.MapRow(summaryHeader, SummaryColumnToGroup, summaryContent);
                var detailContentMapDict = Helper.MapRow(detailHeader, DetailsColumnToGroup, detailContent);
                foreach (var summaryRow in summaryMapDict)
                {
                    //Mapping now takes empty rows to send to BL specific address
                    //so check here
                    if (string.IsNullOrEmpty(summaryRow.Value.Group)) continue;

                    MailMessage mail;
                    string emailAddress = summaryRow.Value.Group;
                    if(SummaryAsCSV)
                    {
                        mail = CreateEmail();
                        Log($"Making CSV summary for: {emailAddress}");

                    }
                    else
                    {
                        mail = CreateEmail(summaryHeader, summaryRow.Value.GetContentArray());
                    }
                    

                    //write CSV
                    //CSV content
                    Log($"Making CSV detail for: {emailAddress}");
                    if (detailContentMapDict.ContainsKey(emailAddress))
                    {
                        var csvContent = detailContentMapDict[emailAddress].GetContentArray();
                        if (csvContent.Count > 0)
                        {
                            Log($"Email: {emailAddress} has {csvContent.Count} rows for detail.");
                            string csvName = string.Format("{0}_details.csv", StripEmail(emailAddress));
                            string fullFilename = string.Format("{0}\\{1}", TempCSVFolder, csvName);
                            if (!WriteCSV(fullFilename, detailHeader, csvContent))
                            {
                                Log($"Fail to make csv for {emailAddress}");
                                //continue; //??? should i?
                            }
                            else
                            {
                                AddAttachment(fullFilename, mail);
                            }
                        }
                    }
                    else
                    {
                        Log($"Email: {emailAddress} has 0 details.");
                    }
                    //add recipient
                    AddRecipient(mail, emailAddress);
                    emails.Add(mail);
                }
                return emails;
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
            //catch (MissingFieldCsvException ex)
            //{
            //	Log(string.Format("Csv fields and columns are not even!", file));
            //	Log(string.Format(ex.Message));
            //	Log(">>>Skip this file");
            //}
            catch (Exception ex)
            {
                Log(string.Format("Unhandled exception on parsing files to emails"));
                Log(string.Format(ex.Message));
                Log(ex.StackTrace);
                Log(">>>Skip this file");
            }
            Log(string.Format("Total email(s): {0} ", emails.Count));
            return emails;
        }

        private MailMessage CreateEmail(string[] header, List<string[]> summaryRowContent)
        {
            var mail = new MailMessage
            {
                From = new MailAddress(CheckSuffix(_smtpEmailSender.EmailAccount)),
                Subject = Subject,
                Body = new BodyBuilder(header, summaryRowContent)
                       .BuildContent(Greeting, Salution),
                IsBodyHtml = true
            };
            return mail;
        }

        private MailMessage CreateEmail()
        {
            var builder = new StringBuilder();
            builder.Append($"<p>{Greeting}</p>");
            builder.Append($"<p>{Salution}</p>");
            var mail = new MailMessage
            {
                From = new MailAddress(CheckSuffix(_smtpEmailSender.EmailAccount)),
                Subject = Subject,
                Body = builder.ToString(),
                IsBodyHtml = true
            };
            return mail;
        }

        private bool WriteCSV(string fullFilename, string[] header, List<string[]> content)
        {
            var joined = new List<string>();
            foreach (var item in content)
            {
                joined.Add(string.Join(OutputCsvDelimitor, item));
            }
            return WriteCSV(fullFilename, header, joined);
        }

        private bool WriteCSV(string fullFilename, string[] header, List<string> content)
        {
            //create folder
            try
            {
                var file = new FileInfo(fullFilename);
                file.Directory.Create(); // If the directory already exists, this method does nothing.
                var csvContent = new List<string>();
                csvContent.Add(string.Join(OutputCsvDelimitor, header));
                csvContent.AddRange(content);
                File.WriteAllLines(file.FullName, csvContent, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                Log(ex.StackTrace);
                return false;
            }
        }

        private void AddRecipient(MailMessage mail, string recipient)
        {
            if (TestRun)
            {
                mail.To.Add(CheckSuffix(TestRecipient));
            }
            else
            {
                mail.To.Add(CheckSuffix(recipient));
            }
            //suffix is handled in HardCc
            mail.CC.Add(HardCc);
        }

        private bool ScanNewFilesToProcess(string folderPath, out string summary, out string details)
        {
            summary = string.Empty;
            details = string.Empty;
            Log("Start scanning at " + folderPath);
            List<string> files;
            try
            {
                files = Directory.GetFiles(folderPath, "*.csv").Except(_processedList).ToList();
                if (files.Count < 1)
                {
                    Log("Nothing new to process...back to sleep Zzz");
                    return false;
                }
                bool foundSummary = false;
                bool foundDetails = false;
                if (files.Count > 2) Log("Warning: folder contains more than a pair of summary & detail");
                if (files.Count < 2) Log("Warning: folder does not contain 2 files for summary & detail");

                foreach (var file in files)
                {
                    var name = file.Split('\\').Last();
                    if (name.Contains(DetailFileKeyword))
                    {
                        if (foundDetails)
                        {
                            Log($"Warning: >1 detail file: {DetailFileKeyword} is found! -> skip");
                            continue;
                        }
                        details = file;
                        foundDetails = true;
                    }
                    if (name.Contains(SummaryFileKeyword))
                    {
                        if (foundSummary)
                        {
                            Log($"Warning: >1 summary file: {SummaryFileKeyword} is found! -> skip");
                            continue;
                        }
                        summary = file;
                        foundSummary = true;
                    }
                }
                return foundSummary && foundDetails;
            }
            catch (DirectoryNotFoundException) //folder may be available next time -> skip this scan
            {
                Log("Cannot find " + folderPath);
                return false;
            }
            catch (IOException ex) //cant connect FTP, folder may be available next time -> skip this scan
            {
                //Log("Error on get filenames in " + fileName);
                Log(ex.Message);
                return false;
            }
            catch (UnauthorizedAccessException ex) //in case log on account got locked, try again later
            {
                Log("Access to the folder is unauthorized!");
                Log(ex.Message);
                return false;
            }
            catch (Exception ex) //no
            {
                Log("Unhandled exception on accessing scanned folder. -> terminate");
                Log(ex.Message);
                throw;
            }
        }

        //private void CleanTempFolderFiles()
        //{
        //    //foreach (var file in CsvFiles)
        //    //{
        //    //    File.Delete(file);
        //    //}
        //    //Log($"Deleted {CsvFiles.Count} temp csv files");
        //    int count = 0;
        //    foreach (var file in Directory.GetFiles(TempCSVFolder, "*.csv"))
        //    {
        //        try
        //        {
        //            File.Delete(file);
        //            count++;
        //        }
        //        catch (IOException)
        //        {
        //        }
        //    }
        //    Log($"Deleted {count} temp csv files");
        //}

        private void MoveFileToDone(string fullFilename)
        {
            Directory.CreateDirectory(DoneFolderPath);
            string doneFilename = string.Format(@"{0}\{1}", DoneFolderPath, GetFileNameFromUrl(fullFilename));
            if (File.Exists(doneFilename))
            {
                File.Delete(doneFilename);
            }
            File.Move(fullFilename, doneFilename);
        }

        private void AddAttachment(string fileName, MailMessage mail)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new InvalidOperationException("attatment file name is null or empty");
            var attachment = new Attachment(fileName, MediaTypeNames.Application.Octet);
            var disposition = attachment.ContentDisposition;
            disposition.CreationDate = File.GetCreationTime(fileName);
            disposition.ModificationDate = File.GetLastWriteTime(fileName);
            disposition.ReadDate = File.GetLastAccessTime(fileName);
            disposition.FileName = Path.GetFileName(fileName);
            disposition.Size = new FileInfo(fileName).Length;
            disposition.DispositionType = DispositionTypeNames.Attachment;
            mail.Attachments.Add(attachment);
        }

        public string TempCSVFolder
        {
            get
            {
                return $"{AssemblyDirectory}\\{TEMP_FOLDER_NAME}";
            }
        }

        //public string DoneFolder
        //{
        //    get
        //    {
        //        return $"{AssemblyDirectory}\\{DONE_FOLDER_NAME}";
        //    }
        //}

        protected override void OnStop()
        {
            StopTimer();
            Log("Service stopped.");
        }

        private string StripEmail(string email)
        {
            string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            string returnString = email.Replace(DefaultSuffix, "").Replace("-", "").Replace(".", "");
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                returnString = returnString.Replace(c, '_');
            }
            return returnString;
        }
    }
}