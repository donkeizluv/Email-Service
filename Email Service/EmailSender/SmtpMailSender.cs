using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Threading;
using AnEmailService.Log;

namespace AnEmailService.EmailSender
{
    public delegate void EmailSendingThreadExitEventHandler(object sender, EmailSendingThreadEventArgs e);
    public class SmtpMailSender
    {
        public event EmailSendingThreadExitEventHandler OnEmailSendingThreadExit;
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(SmtpMailSender));
        private readonly SmtpClient _client;
        public bool IsThreadRunning { get; private set; }
        //public readonly Queue<MailMessage> _queueMail = new Queue<MailMessage>();
        private Thread _sendingThread;

        /// <summary>
        /// anon
        /// </summary>
        /// <param name="senderName"></param>
        /// <param name="server"></param>
        /// <param name="port"></param>
        public SmtpMailSender(string senderName, string server, int port)
        {
            EmailAccount = senderName;
            Server = server;
            _client = new SmtpClient
            {
                UseDefaultCredentials = false,
                Port = port,
                Host = server,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = Timeout,
                EnableSsl = false
            };
        }

        public SmtpMailSender(string emailAccount, string pwd, string server, int port)
        {
            EmailAccount = emailAccount;
            Server = server;
            _client = new SmtpClient
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(EmailAccount, pwd),
                Port = port,
                Host = Server,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = Timeout,
                EnableSsl = false //seems faster with this shit off
            };
            // port 587, 25..
            // 25 seems to do the trick
        }

        public string EmailAccount { get; }
        public string Server { get; }
        public int SleepInterval { get; set; } = 5000;
        public int Timeout { get; set; } = 30000;
        public ConcurrentQueue<MailMessage> MailQueue { get; set; } = new ConcurrentQueue<MailMessage>();

        public void EnqueueEmail(MailMessage mail)
        {
            MailQueue.Enqueue(mail);
        }

        public void EnqueueEmail(IEnumerable<MailMessage> mailCollection)
        {
            if (MailQueue.Count > 0)
                foreach (var mail in mailCollection)
                    MailQueue.Enqueue(mail);
            else
                MailQueue = new ConcurrentQueue<MailMessage>(mailCollection);
        }

        public void StartSending()
        {
            if (IsThreadRunning) return; //already running
            _sendingThread = new Thread(SendingThread) {IsBackground = true};

            try
            {
                _sendingThread.Start();
                IsThreadRunning = true;
            }
            catch (Exception ex)
            {
                Log("Start thread exception: " + ex.Message ?? string.Empty);
                //whatever, retry next time. this is safemeasure, dont think we can reach this.
            }
        }

        protected virtual void RaiseOnSendingThreadExit(EmailSendingThreadEventArgs e)
        {
            OnEmailSendingThreadExit?.Invoke(this, e);
        }

        private void SendingThread()
        {
            int sleep = 0;
            int emailCount = 0;
            int retries = 0;
            bool unrecoverableEx = false;
            try
            {
                while (true)
                {
                    Thread.Sleep(sleep);
                    MailMessage anEmail;
                    if (!MailQueue.TryDequeue(out anEmail))
                    {
                        Log("All emails processed -> stop thread");
                        return;
                    }
                    try
                    {
                        //_client.Timeout = 1;
                        Log(string.Format("Start sending email to {0}, total recipients: {1}", anEmail.To[0],
                            anEmail.To.Count));
                        _client.Send(anEmail);
                        Log("Sent sucessfully.");
                        sleep = 0;
                        emailCount++;
                    }
                    catch (SmtpException ex) when (ex.Message.Contains("timed out"))
                    {
                        //The operation has timed out.
                        //enqueue to retry later
                        MailQueue.Enqueue(anEmail);
                        //wait a moment to retry
                        sleep = SleepInterval;
                        Log("Timed out sending email -> wait then retry");
                        retries++;
                    }
                    catch (SmtpException ex) when (ex.Message.Contains("Message submission rate"))
                    {
                        //limit reached error
                        //SmtpException
                        //Service not available, closing transmission channel. 
                        //The server response was: 4.4.2 Message submission rate for this client has exceeded the configured limit

                        //enqueue to retry later
                        MailQueue.Enqueue(anEmail);
                        //wait a moment to retry
                        sleep = SleepInterval;
                        Log("Limit reached. -> wait then retry");
                        retries++;
                    }
                    catch (SmtpException ex)
                        when (
                            ex.InnerException != null &&
                            ex.InnerException.Message.Contains("The remote name could not be resolved"))
                    {
                        //ex.InnerException = {"The remote name could not be resolved: 'mail.hdsaison.com.vn'"}
                        MailQueue.Enqueue(anEmail);
                        sleep = SleepInterval;
                        Log("Could not resolve server name. -> wait then retry");
                        retries++;
                    }
                    catch (SmtpException ex)
                        when (
                            ex.InnerException != null &&
                            ex.InnerException.Message.Contains("Unable to connect to the remote server"))
                    {
                        //ex.InnerException = {"The remote name could not be resolved: 'mail.hdsaison.com.vn'"}
                        MailQueue.Enqueue(anEmail);
                        sleep = SleepInterval;
                        Log("Could not connect to server -> wait then retry");
                        retries++;
                    }
                    catch (SmtpException ex) when (ex.Message.Contains("Client was not authenticated"))
                    {
                        unrecoverableEx = true;
                        Log("Unauthenticated");
                        return;
                    }
                    catch (SmtpFailedRecipientsException ex)
                    {
                        Log("Couldnt send to: " + ex.FailedRecipient);
                    }
                    catch (Exception ex)
                    {
                        unrecoverableEx = true;
                        Log("Unhandled exception on sending email.");
                        Log(ex.Message ?? string.Empty);
                        if (ex.InnerException != null)
                            Log(" Inner ex: " + ex.InnerException.Message ?? string.Empty);

                        return;
                    }
                }
            }
            finally
            {
                IsThreadRunning = false;
                RaiseOnSendingThreadExit(new EmailSendingThreadEventArgs(emailCount, retries, unrecoverableEx));
                Log("Thread stopped.");
                //GC.Collect();
            }
        }

        //public void StopSending()
        //{
        //    if (_sendingThread == null) return;
        //    //check for state first!
        //    try
        //    {
        //        _sendingThread.Abort();
        //    }
        //    catch (Exception ex)
        //    {
        //        Log("Stop thread exception: " + ex.Message ?? string.Empty);
        //    }

        //}
        private static void Log(string log) => Logger.Log(log);
    }
    public class EmailSendingThreadEventArgs : EventArgs
    {
        public int TotalSent { get; private set; }
        public int TotalRetries { get; private set; }
        public bool StopOnUnrecoverableException { get; private set; }
        public EmailSendingThreadEventArgs(int sent, int retries, bool unrecoverableException)
        {
            TotalSent = sent;
            TotalRetries = retries;
            StopOnUnrecoverableException = unrecoverableException;
        }
    }
}