using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AnEmailService
{
    public partial class EmailService
    {
        private static string Version => Assembly.GetEntryAssembly().GetName().Version.ToString();

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

        private List<string> GetProcessedList()
        {
            try
            {
                return
                    File.ReadAllLines(ProcessedFilePath)
                        .Select(name => string.Format(@"{0}\{1}", Folder, name))
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
        internal string HardCc
        {
            get
            {
                if (!Cc.Any()) return string.Empty;
                var builder = new StringBuilder();
                foreach (string address in Cc)
                {
                    if (string.IsNullOrEmpty(address)) continue;
                    builder.Append(CheckSuffix(address.Trim())).Append(", ");
                }
                //clean head & tail
                return builder.ToString().Trim(',', ' ');
            }
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

        private bool WriteToProcessedFile(string fileName)
        {
            return WriteToProcessedFile(new List<string>() { fileName });
        }

        private string CheckSuffix(string address)
            => SuffixCorrection ? (address.Contains("@") ? address : address + DefaultSuffix) : address;

        private static void Log(string log) => Logger.Log(log);
        private static void PrintLine() => Log("----------------------------------------------");
        //this seems ugly af :/
        private static string GetFileNameFromUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                uri = new Uri(SomeBaseUri, url);

            return Path.GetFileName(uri.LocalPath);
        }

        private Dictionary<string, string> ParseToRecipientMap(string map)
        {
            var dict = new Dictionary<string, string>();
            var split = map.Replace(" ", null).Split(','); //trim spaces then split pair
            if (split.Count() == 0) throw new ArgumentException($"Map is invalid: {map}");
            foreach (var s in split)
            {
                var pair = s.Split(':');
                if (pair.Count() != 2) throw new ArgumentException($"Invalid map string {s}");
                dict.Add(pair.First(), pair.Last());
                Logger.Log($"{pair.First()} -> {pair.Last()}");
            }
            //if (dict.Count < 1) throw new InvalidDataException("readinng RecipientMap result zero recipients");
            return dict;
        }
    }
}