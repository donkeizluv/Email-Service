using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnEmailService.Csv;
using CsvHelper.Configuration;

namespace AnEmailService.EmailComposer
{
    internal static class CsvToArray
    {
        public static void ReadCsvTo2DArray(string path, char delimiter, out string[] headers,
            out string[,] contentArray)
        {
            StreamReader streamReader = null;
            try
            {
                streamReader = new StreamReader(path, true);
            }
            catch (IOException)
            {
                //try open read-only
                var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                streamReader = new StreamReader(fs, true);
            }
            if (streamReader == null) throw new IOException("Cannot access the data file.");
            using (var csv = new CachedCsvReader(streamReader, true, delimiter))
            {
                contentArray = new string[csv.Count(), csv.FieldCount];
                csv.MoveToStart();
                headers = csv.GetFieldHeaders();
                int fieldCount = csv.FieldCount;

                //check
                if (contentArray.GetLength(1) == 0)
                    throw new InvalidDataException("CSV doesnt have headers.");
                if (contentArray.GetLength(0) == 0)
                    throw new InvalidDataException("CSV doesnt contain data -> skip");

                int row = 0;
                while (csv.ReadNextRecord())
                {
                    for (int i = 0; i < fieldCount; i++)
                        contentArray[row, i] = csv[i];
                    row++;
                }
            }
        }

        public static void ReadCsvTo2DArray(string path, char delimiter, out string[] headers,
            out List<string[]> content)
        {
            StreamReader streamReader = null;
            try
            {
                streamReader = new StreamReader(path, true);
            }
            catch (IOException)
            {
                //try open read-only
                var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                streamReader = new StreamReader(fs, true);
            }
            if (streamReader == null) throw new IOException("Cannot access the data file.");
            using (var csv = new CsvReader(streamReader, true, delimiter))
            {
                content = new List<string[]>();
                headers = csv.GetFieldHeaders();
                int fieldCount = csv.FieldCount;
                while (csv.ReadNextRecord())
                {
                    var aRow = new string[fieldCount];
                    for (int i = 0; i < fieldCount; i++)
                        aRow[i] = csv[i];
                    content.Add(aRow);
                }
                if (fieldCount < 1)
                    throw new InvalidDataException("CSV doesnt have headers.");
                if (content.Count < 1)
                    throw new InvalidDataException("CSV doesnt contain data -> skip");
            }
            streamReader.Dispose();
        }

        public static void ReadCsv(string path, char delimiter, out string[] headers, out List<string[]> content)
        {
            content = new List<string[]>();
            using (TextReader reader = File.OpenText(path))
            {
                var parser = new CsvHelper.CsvReader(reader, new CsvConfiguration
                {
                    Delimiter = delimiter.ToString(),
                    HasHeaderRecord = true,
                    TrimFields = true,
                    TrimHeaders = true,
                    SkipEmptyRecords = true,
                });
                parser.ReadHeader();
                headers = parser.FieldHeaders;
                while (parser.Read())
                    content.Add(parser.CurrentRecord);
            }
            if (content.Count < 1)
                throw new InvalidDataException("CSV doesnt contain data -> skip");
        }
    }
}