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

        //MAN specific
        //filenames
        private string SummaryFileKeyword;

        private string DetailFileKeyword;
        private string OutputCsvDelimitor;

        //column names
        private string FilterEmailColumnName; //MAN_EMAIL

        private string FilterTitleColumnName; //TITLE
        private string TitleToDetailColumn; //exp: RSM:ASM_EMAIL, BDS_EMAIL:BDS_EMAIL

        //summary take
        private int SummaryTakeFrom;

        private int SummaryTakeTo;

        //detail take
        private int DetailTakeFrom;

        private int DetailTakeTo;

        //empty row config
        private string BLSummaryColumnName; //BL in summary

        private string BLDetailColumnName; //BL in detail
        private string EmptyEmailReceiver;
        private string RestOfEmptyReceiver;

        //move file
        private bool MoveToDoneFolder;

        private string DoneFolderPath;
        private bool CleanTempFolder;

        //private string GroupBy;

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

                //MAN specific
                //Filenames
                SummaryFileKeyword = GetConfig<string>(source, "MAN_Spec", "SummaryFileKeyword");
                DetailFileKeyword = GetConfig<string>(source, "MAN_Spec", "DetailFileKeyword");
                OutputCsvDelimitor = GetConfig<string>(source, "MAN_Spec", "OutputCsvDelimitor");
                //column names
                FilterEmailColumnName = GetConfig<string>(source, "MAN_Spec", "FilterEmailColumnName");
                FilterTitleColumnName = GetConfig<string>(source, "MAN_Spec", "FilterTitleColumnName");
                TitleToDetailColumn = GetConfig<string>(source, "MAN_Spec", "TitleToDetailColumn");
                Log("Title summary to detail column map:");
                TitleToDetailsColumnMap = ParseToRecipientMap(TitleToDetailColumn); //to map
                SummaryTakeFrom = GetConfig<int>(source, "MAN_Spec", "SummaryTakeFrom");
                SummaryTakeTo = GetConfig<int>(source, "MAN_Spec", "SummaryTakeTo");
                ////detail take
                DetailTakeFrom = GetConfig<int>(source, "MAN_Spec", "DetailTakeFrom");
                DetailTakeTo = GetConfig<int>(source, "MAN_Spec", "DetailTakeTo");
                //empty row config
                BLSummaryColumnName = GetConfig<string>(source, "MAN_Spec", "BLSummaryColumnName");
                BLDetailColumnName = GetConfig<string>(source, "MAN_Spec", "BLDetailColumnName");
                EmptyEmailReceiver = GetConfig<string>(source, "MAN_Spec", "EmptyEmailReceiver");
                Log("Empty email receiver map:");
                EmptyEmailMap = ParseToRecipientMap(EmptyEmailReceiver); //to map
                RestOfEmptyReceiver = GetConfig<string>(source, "MAN_Spec", "RestOfEmptyReceiver");
                if (string.IsNullOrEmpty(RestOfEmptyReceiver)) throw new ArgumentNullException("RestOfEmptyReceiver must be set.");
                Log($"Not mapped BL send to: {RestOfEmptyReceiver}");
                //move file
                MoveToDoneFolder = GetConfig<bool>(source, "MAN_Spec", "MoveToDoneFolder");
                //CleanTempFolder = GetConfig<bool>(source, "MAN_Spec", "CleanTempFolder");
                DoneFolderPath = GetConfig<string>(source, "MAN_Spec", "DoneFolderPath");

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