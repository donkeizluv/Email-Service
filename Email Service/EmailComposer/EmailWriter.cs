using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AnEmailService.Log;
using AnEmailService.Properties;

namespace AnEmailService.EmailComposer
{
    public class EmailBuilder : IDisposable
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(EmailBuilder));
        //CONST
        //format cell here!
        private readonly string _openHeadHtml = Resources.OpenHeadHTML;
        private string[,] _content; //X, Y

        private string[] _headers;

        public EmailBuilder(string[] headers, string[,] content)
        {
            _headers = headers;
            _content = content;
            LastColIndex = _headers.Count() - 1;
            LastRowIndex = _content.GetLength(0) - 1; //get len of X;
        }

        public EmailBuilder(string[] headers, List<string[]> content)
        {
            _headers = headers;
            _content = new string[content.Count, headers.Count()];
            int rowIndex = 0;
            foreach (var row in content)
            {
                for (int i = 0; i < _headers.Count(); i++)
                    _content[rowIndex, i] = row[i];
                rowIndex++;
            }
            LastColIndex = _headers.Count() - 1;
            LastRowIndex = _content.GetLength(0) - 1; //get len of X;
        }

        public int LastRowIndex { get; }
        public int LastColIndex { get; }

        public void Dispose()
        {
            _content = null;
            _headers = null;
        }

        //NYI: use CSS
        private string BuildHeadersHtml()
        {
            var builder = new StringBuilder();
            builder.Append("<tr>");
            for (int i = 0; i <= LastColIndex; i++)
            {
                string val = _headers[i] ?? string.Empty;
                //if (val == string.Empty) continue; //should allow empty header
                builder.Append(string.Format("<td><b>{0}</b></td>", val));
            }
            builder.Append("</tr>");
            return builder.ToString();
        }

        /// <summary>
        ///     build email HTML content
        /// </summary>
        /// <param name="map">content map</param>
        /// <param name="greeting">Dear balblabla,</param>
        /// <param name="salution">Regards,</param>
        /// <returns></returns>
        public string BuildContent(ContentMap map, string greeting, string salution)
        {
            //int rowCount = 0;
            //int occurrent = CountOccurrent(headerIndex, uniqueName);
            //insert HTML defination, table headers
            var builder = new StringBuilder();
            builder.Append("<html>").Append(_openHeadHtml).Append("<body>");
            //insert greeting
            if (!string.IsNullOrEmpty(greeting)) builder.Append(greeting);
            //insert header
            builder.Append("<table>");
            builder.Append(BuildHeadersHtml());

            //row iteration
            foreach (int rowIndex in map.Map)
            {
                builder.Append("<tr>");
                for (int colIndex = 0; colIndex <= LastColIndex; colIndex++) //loop Y axis
                {
                    string cellContent = _content[rowIndex, colIndex] ?? string.Empty;
                    builder.Append(string.Format("<td>{0}</td>", cellContent));
                }
                builder.Append("</tr>");
                //rowCount++;
            }
            //close table
            builder.Append("</table>");
            //insert salution
            if (!string.IsNullOrEmpty(salution)) builder.Append(salution);
            //close body & html
            builder.Append("</body>").Append("</html>");
            return builder.ToString();
        }

        /// <summary>
        ///     Map content kinda like grouping data
        /// </summary>
        /// <param name="groupBy">header name to map to, like grouping data.</param>
        /// <returns></returns>
        //public IEnumerable<ContentMap> GetMap(string groupBy)
        //{
        //    var listContentMap = new List<ContentMap>();
        //    int colIndex = FindHeaderIndex(groupBy);
        //    if (colIndex < 0) throw new InvalidDataException("Cannot find header " + groupBy);
        //    for (int i = 0; i <= LastRowIndex; i++)
        //    {
        //        string groupName = _content[i, colIndex] ?? string.Empty;
        //        if (groupName == string.Empty) continue;
        //        var found = listContentMap.FirstOrDefault(item => item.Group == groupName);
        //        if (found.Group == null) //new name & map
        //        {
        //            var map = new ContentMap
        //            {
        //                Group = groupName,
        //                Map = new List<int> {i}
        //            };
        //            listContentMap.Add(map);
        //        }
        //        else //write location(index) to map
        //        {
        //            found.Map.Add(i);
        //        }
        //    }
        //    return listContentMap;
        //}
        /// <summary>
        ///     Map content kinda like grouping data
        /// </summary>
        /// <param name="groupBy">header name to map to, like grouping data.</param>
        /// <returns></returns>
        public IEnumerable<ContentMap> GetMap_2(string groupBy)
        {
            var dict = new Dictionary<string, ContentMap>();
            int colIndex = FindHeaderIndex(groupBy);
            if (colIndex < 0) throw new InvalidDataException("Cannot find header: " + groupBy);
            for (int i = 0; i <= LastRowIndex; i++)
            {
                string groupName = _content[i, colIndex] ?? string.Empty;
                if (groupName == string.Empty) continue;
                if (!dict.ContainsKey(groupName))
                    dict.Add(groupName, new ContentMap
                    {
                        Group = groupName,
                        Map = new List<int> {i}
                    });
                else
                    dict[groupName].Map.Add(i);
            }
            return dict.Values.ToList();
        }

        private int FindHeaderIndex(string headerName)
        {
            for (int i = 0; i <= LastColIndex; i++)
            {
                string val = _headers[i] ?? string.Empty;
                if (val == string.Empty) continue;
                if (string.Compare(val, headerName, true) == 0)
                    return i;
            }
            return -1;
        }

        /// <summary>
        ///     build recipient list
        /// </summary>
        /// <param name="headers">columns to get more recipients ex: "rep1; rep2"</param>
        /// <returns></returns>
        public List<string> BuildAdditionalRecipients(string headers, ContentMap map)
        {
            if (headers == string.Empty) return new List<string>();
            var emailList = new List<string>();
            var colIndexList = new List<int>();
            var split = headers.Split(';');
            foreach (string header in split)
            {
                if (header == string.Empty) continue;
                int headerIndex = FindHeaderIndex(header.Trim());
                if (headerIndex == -1)
                {
                    Logger.Log(string.Format("Could not find header: {0}", header));
                    continue;
                }
                colIndexList.Add(headerIndex);
            }
            foreach (int colIndex in colIndexList)
            foreach (int rowIndex in map.Map)
            {
                string email = _content[rowIndex, colIndex].Trim();
                if (!emailList.Contains(email) && email != string.Empty) emailList.Add(email);
            }
            return emailList;
        }
    }
}