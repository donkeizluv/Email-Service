using AnEmailService.Nini.Config;
using System;

namespace AnEmailService
{
    public partial class EmailService
    {
        #region Config

        private string SenderAccount;
        private string EmailServer;
        private string EmailPwd;
        private int Port;
        private bool Anon;
        private bool TestRun;
        private string TestRecipient;
        private int Limit;
        private string Folder;
        private string DefaultSuffix;
        private char Delimiter;
        //private long SizeThreshold;

        //CA specific
        //filenames
        private string SummaryFileKeyword;

        private string DetailFileKeyword;
        private string OutputCsvDelimitor;
        private bool SummaryAsCSV;

        //move file
        private bool MoveToDoneFolder;

        private string DoneFolderPath;
        private bool CleanTempFolder;
        //columns name
        private string SummaryColumnToGroup;
        private string DetailsColumnToGroup;
        private string EmptyEmailReceiver;
        //private string AdditionalRecipient;
        private int RetryInterval;

        private long ScanInterval;
        private bool ExitOnSendingThreadCompletion;
        private string Subject;
        private string Greeting;
        private string Salution;
        private string[] Cc;
        private bool SuffixCorrection;
        private const string CONFIG_FILE_NAME = "config.ini";
        private const string PROCESS_FILE_NAME = "processed.txt";
        private const string TEMP_FOLDER_NAME = "temp";

        #endregion Config


        //use nameof
        private bool ReadConfig()
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
                SuffixCorrection = source.Configs["Email"].GetBoolean("SuffixCorrection");
                Greeting = source.Configs["Email"].GetString("Greeting");
                Salution = source.Configs["Email"].GetString("Salution");
                Cc = source.Configs["Email"].GetString("CC").Split(';');

                //Format
                string delimiter = source.Configs["Format"].GetString("Delimiter");
                if (string.Compare(delimiter, @"\t", true) == 0) Delimiter = Convert.ToChar(9);
                else Delimiter = char.Parse(delimiter);

                //CA specific
                //Filenames
                SummaryFileKeyword = GetConfig<string>(source, "CA_Spec", "SummaryFileKeyword");
                DetailFileKeyword = GetConfig<string>(source, "CA_Spec", "DetailFileKeyword");
                OutputCsvDelimitor = GetConfig<string>(source, "CA_Spec", "OutputCsvDelimitor");
                SummaryAsCSV = GetConfig<bool>(source, "CA_Spec", nameof(SummaryAsCSV));
                //column to group
                SummaryColumnToGroup = GetConfig<string>(source, "CA_Spec", "SummaryColumnToGroup");
                DetailsColumnToGroup = GetConfig<string>(source, "CA_Spec", "DetailsColumnToGroup");

                //empty email
                EmptyEmailReceiver = GetConfig<string>(source, "CA_Spec", nameof(EmptyEmailReceiver));

                //move file
                MoveToDoneFolder = GetConfig<bool>(source, "CA_Spec", "MoveToDoneFolder");
                //CleanTempFolder = GetConfig<bool>(source, "CA_Spec", "CleanTempFolder");
                DoneFolderPath = GetConfig<string>(source, "CA_Spec", "DoneFolderPath");

                //File
                Folder = source.Configs["File"].GetString("Folder");
                //SizeThreshold = source.Configs["File"].GetLong("SizeThreshold");

                //Interval
                ScanInterval = source.Configs["Interval"].GetLong("ScanInterval") * 1000; //in sec
                RetryInterval = source.Configs["Interval"].GetInt("EmailRetry");
                ExitOnSendingThreadCompletion = source.Configs["Interval"].GetBoolean("ExitOnSendingThreadCompletion");
                return true;
            }
            catch (Exception ex)
            {
                Log("config.ini is not in correct format.");
                Log(ex.Message);
                return false;
            }
        }

        private T GetConfig<T>(IniConfigSource source, string section, string name, bool notNull = false)
        {
            object value = source.Configs[section].GetString(name);
            if (notNull)
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
    }
}