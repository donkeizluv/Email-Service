using AnEmailService.EmailComposer;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AnEmailService
{
    public static class Helper
    {
        public static Dictionary<string, ContentMap> MapRow(string[] header, string groupBy, List<string[]> content)
        {
            int columnCount = header.Count();
            string[,] contentArray = new string[content.Count, columnCount];

            //list string[] to 2D array
            int rowIndex = 0;
            foreach (var row in content)
            {
                for (int i = 0; i < columnCount; i++)
                    contentArray[rowIndex, i] = row[i];
                rowIndex++;
            }
            //map to dict
            var dict = new Dictionary<string, ContentMap>();
            int colIndex = FindHeaderIndex(header, groupBy, columnCount);
            if (colIndex < 0) throw new InvalidDataException("Cannot find header: " + groupBy);
            for (int i = 0; i < content.Count; i++)
            {
                string groupName = contentArray[i, colIndex] ?? string.Empty;
                //if (groupName == string.Empty) continue; //skip blank
                if (!dict.ContainsKey(groupName))
                {
                    var map = new ContentMap(groupName);
                    map.Add(i, content[i]);
                    dict.Add(groupName, map);

                }
                else
                {
                    dict[groupName].Add(i, content[i]);
                }
            }
            return dict;
        }

        //public static List<int> GetIndexWithEmptyFields(string[] header, string[] columnNames, List<string[]> content)
        //{
        //    int columnCount = header.Count();
        //    string[,] contentArray = new string[content.Count, columnCount];

        //    //list string[] to 2D array
        //    int rowIndex = 0;
        //    foreach (var row in content)
        //    {
        //        for (int i = 0; i < columnCount; i++)
        //            contentArray[rowIndex, i] = row[i];
        //        rowIndex++;
        //    }
        //    //map to dict
        //    var emptyIndexList = new List<int>();
        //    //get column indexes
        //    var columnIndexList = new List<int>();
        //    foreach (var colName in columnNames)
        //    {
        //        int colIndex = FindHeaderIndex(header, colName, columnCount);
        //        if (colIndex < 0) continue;
        //        columnIndexList.Add(colIndex);
        //    }

        //    for (int i = 0; i < content.Count; i++)
        //    {
        //        bool empty = true;
        //        foreach (var index in columnIndexList)
        //        {
        //            if (contentArray[i, index] != string.Empty)
        //                empty = false;
        //        }
        //        if (empty) emptyIndexList.Add(i);
        //    }
        //    return emptyIndexList;
        //}

        private static int FindHeaderIndex(string[] header, string headerNameToFind, int columnCount)
        {
            for (int i = 0; i <= header.Count() - 1; i++)
            {
                string val = header[i] ?? string.Empty;
                if (val == string.Empty) continue;
                if (string.Compare(val, headerNameToFind, true) == 0)
                    return i;
            }
            return -1;
        }
    }
}