using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AnEmailService.Log;
using AnEmailService.Properties;
using System.Net.Mail;
using System.Net.Mime;

namespace AnEmailService.EmailComposer
{
    public class EmailBuilder
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(EmailBuilder));
        //CONST
        //format cell here!
        private readonly string _openHeadHtml = Resources.OpenHeadHTML;
        private string[,] _content; //X, Y

        private string[] _headers;

        //send attatchment
        public EmailBuilder() { }
        public string BuildContent(string content, string greeting, string salution)
        {
            var builder = new StringBuilder();
            builder.Append("<html>").Append(_openHeadHtml).Append("<body>").AppendFormat("<p>{0}</p>", greeting);
            //insert content
            builder.AppendFormat("<p>{0}</p>", content);
            //insert salution
            if (!string.IsNullOrEmpty(salution)) builder.Append(salution);
            //close body & html
            builder.Append("</body>").Append("</html>");
            return builder.ToString();
        }

        /// <summary>
        ///     build email HTML content
        /// </summary>
        /// <param name="map">content map</param>
        /// <param name="greeting">Dear balblabla,</param>
        /// <param name="salution">Regards,</param>
        /// <returns></returns>
        //public string BuildContent(ContentMap map, string greeting, string salution)
        //{
        //    //int rowCount = 0;
        //    //int occurrent = CountOccurrent(headerIndex, uniqueName);
        //    //insert HTML defination, table headers
        //    var builder = new StringBuilder();
        //    builder.Append("<html>").Append(_openHeadHtml).Append("<body>");
        //    //insert greeting
        //    if (!string.IsNullOrEmpty(greeting)) builder.Append(greeting);
        //    //insert header
        //    builder.Append("<table>");
        //    builder.Append(BuildHeadersHtml());

        //    //row iteration
        //    foreach (int rowIndex in map.Map)
        //    {
        //        builder.Append("<tr>");
        //        for (int colIndex = 0; colIndex <= LastColIndex; colIndex++) //loop Y axis
        //        {
        //            string cellContent = _content[rowIndex, colIndex] ?? string.Empty;
        //            builder.Append(string.Format("<td>{0}</td>", cellContent));
        //        }
        //        builder.Append("</tr>");
        //        //rowCount++;
        //    }
        //    //close table
        //    builder.Append("</table>");
        //    //insert salution
        //    if (!string.IsNullOrEmpty(salution)) builder.Append(salution);
        //    //close body & html
        //    builder.Append("</body>").Append("</html>");
        //    return builder.ToString();
        //}
    }
}