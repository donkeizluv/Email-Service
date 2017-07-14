using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsvHelper
{
    /// <summary>
    ///     Common exception tasks.
    /// </summary>
    internal static class ExceptionHelper
    {
        /// <summary>
        ///     Adds CsvHelper specific information to <see cref="P:System.Exception.Data" />.
        /// </summary>
        /// <param name="exception">The exception to add the info to.</param>
        /// <param name="parser">The parser.</param>
        /// <param name="type">The type of object that was being created in the <see cref="T:CsvHelper.CsvReader" />.</param>
        /// <param name="namedIndexes">The named indexes in the <see cref="T:CsvHelper.CsvReader" />.</param>
        /// <param name="currentIndex">The current index of the <see cref="T:CsvHelper.CsvReader" />.</param>
        /// <param name="currentRecord">The current record of the <see cref="T:CsvHelper.CsvReader" />.</param>
        public static void AddExceptionDataMessage(Exception exception, ICsvParser parser, Type type,
            Dictionary<string, List<int>> namedIndexes, int? currentIndex, string[] currentRecord)
        {
            try
            {
                exception.Data["CsvHelper"] = GetErrorMessage(parser, type, namedIndexes, currentIndex, currentRecord);
            }
            catch (Exception exception2)
            {
                var exception1 = exception2;
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("An error occurred while creating exception details.");
                stringBuilder.AppendLine();
                stringBuilder.AppendLine(exception1.ToString());
                exception.Data["CsvHelper"] = stringBuilder.ToString();
            }
        }

        /// <summary>
        ///     Gets CsvHelper information to be added to an exception.
        /// </summary>
        /// <param name="parser">The parser.</param>
        /// <param name="type">The type of object that was being created in the <see cref="T:CsvHelper.CsvReader" />.</param>
        /// <param name="namedIndexes">The named indexes in the <see cref="T:CsvHelper.CsvReader" />.</param>
        /// <param name="currentIndex">The current index of the <see cref="T:CsvHelper.CsvReader" />.</param>
        /// <param name="currentRecord">The current record of the <see cref="T:CsvHelper.CsvReader" />.</param>
        /// <returns>The CsvHelper information.</returns>
        public static string GetErrorMessage(ICsvParser parser, Type type, Dictionary<string, List<int>> namedIndexes,
            int? currentIndex, string[] currentRecord)
        {
            var stringBuilder = new StringBuilder();
            if (parser != null)
                stringBuilder.AppendFormat("Row: '{0}' (1 based)", parser.Row).AppendLine();
            if (type != null)
                stringBuilder.AppendFormat("Type: '{0}'", type.FullName).AppendLine();
            if (currentIndex.HasValue)
                stringBuilder.AppendFormat("Field Index: '{0}' (0 based)", currentIndex).AppendLine();
            if (namedIndexes != null)
            {
                string str = (
                    from pair in namedIndexes
                    from index in pair.Value
                    select new {pair, index}).Where(argument0 =>
                {
                    int u003cu003eh_TransparentIdentifier0 = argument0.index;
                    var nullable = currentIndex;
                    if (u003cu003eh_TransparentIdentifier0 != nullable.GetValueOrDefault())
                        return false;
                    return nullable.HasValue;
                }).Select(argument1 => argument1.pair.Key).SingleOrDefault();
                if (str != null)
                    stringBuilder.AppendFormat("Field Name: '{0}'", str).AppendLine();
            }
            if (currentRecord != null)
            {
                var nullable1 = currentIndex;
                if (nullable1.GetValueOrDefault() > -1 ? nullable1.HasValue : false)
                {
                    nullable1 = currentIndex;
                    int length = currentRecord.Length;
                    if ((nullable1.GetValueOrDefault() < length ? nullable1.HasValue : false) &&
                        currentIndex.Value < currentRecord.Length)
                    {
                        string str1 = currentRecord[currentIndex.Value];
                        stringBuilder.AppendFormat("Field Value: '{0}'", str1).AppendLine();
                    }
                }
            }
            return stringBuilder.ToString();
        }
    }
}