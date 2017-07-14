using AnEmailService.Log;
using AnEmailService.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnEmailService.EmailComposer
{
    public class BodyBuilder
    {
        private readonly string _openHeadHtml = Resources.OpenHeadHTML;
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(BodyBuilder));

        //CONST
        //format cell here!
        public string[] Header { get; set; }

        public List<string[]> SummaryRows { get; set; }
        public string DetailCsvPath { get; set; }
        public string Recipient { get; set; }
        public int TakeFrom { get; set; }
        public int TakeTo { get; set; }

        public BodyBuilder(string[] headerSummary, List<string[]> summaryRows, int takeFrom, int takeTo)
        {
            Header = headerSummary;
            SummaryRows = summaryRows;
            if (takeFrom < 1 || takeFrom >= Header.Count())
                throw new ArgumentException($"Invalid take from:{takeFrom}, take to:{takeTo}");
            TakeFrom = takeFrom;
            TakeTo = takeTo;
        }

        //NYI: use CSS
        private string BuildHeadersHtml()
        {
            var builder = new StringBuilder();
            builder.Append("<tr>");
            for (int i = TakeFrom - 1; i < Header.Count() && i < TakeTo; i++)
            {
                string val = Header[i] ?? string.Empty;
                //if (val == string.Empty) continue; //should allow empty header
                builder.Append(string.Format("<td><b>{0}</b></td>", val));
            }
            builder.Append("</tr>");
            return builder.ToString();
        }

        /// <summary>
        ///     build email HTML content
        /// </summary>
        /// <param name="greeting">Dear balblabla,</param>
        /// <param name="salution">Regards,</param>
        /// <returns></returns>
        public string BuildContent(string greeting, string salution)
        {
            var builder = new StringBuilder();
            builder.Append("<html>").Append(_openHeadHtml).Append("<body>");
            //insert greeting
            if (!string.IsNullOrEmpty(greeting)) builder.Append(greeting);
            builder.Append("<table>");
            builder.Append(BuildHeadersHtml());
            foreach (var row in SummaryRows)
            {
                builder.Append("<tr>");
                for (int i = TakeFrom - 1; i < Header.Count() && i < TakeTo; i++) //loop Y axis
                {
                    string cellContent = row[i] ?? string.Empty;
                    builder.Append(string.Format("<td>{0}</td>", cellContent));
                }
                builder.Append("</tr>");
                //rowCount++;
            }
            builder.Append("</table>");

            //insert salution
            if (!string.IsNullOrEmpty(salution)) builder.Append(salution);
            //close body & html
            builder.Append("</body>").Append("</html>");
            return builder.ToString();
        }
    }
}