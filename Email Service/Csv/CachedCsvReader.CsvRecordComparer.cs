using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using AnEmailService.Csv.Resources;

namespace AnEmailService.Csv
{
    public partial class CachedCsvReader
        : CsvReader
    {
        /// <summary>
        ///     Represents a CSV record comparer.
        /// </summary>
        private class CsvRecordComparer
            : IComparer<string[]>
        {
            #region Constructors

            /// <summary>
            ///     Initializes a new instance of the CsvRecordComparer class.
            /// </summary>
            /// <param name="field">The field index of the values to compare.</param>
            /// <param name="direction">The sort direction.</param>
            public CsvRecordComparer(int field, ListSortDirection direction)
            {
                if (field < 0)
                    throw new ArgumentOutOfRangeException("field", field,
                        string.Format(CultureInfo.InvariantCulture, ExceptionMessage.FieldIndexOutOfRange, field));

                _field = field;
                _direction = direction;
            }

            #endregion

            #region IComparer<string[]> Members

            public int Compare(string[] x, string[] y)
            {
                Debug.Assert(x != null && y != null && x.Length == y.Length && _field < x.Length);

                int result = string.Compare(x[_field], y[_field], StringComparison.CurrentCulture);

                return _direction == ListSortDirection.Ascending ? result : -result;
            }

            #endregion

            #region Fields

            /// <summary>
            ///     Contains the field index of the values to compare.
            /// </summary>
            private readonly int _field;

            /// <summary>
            ///     Contains the sort direction.
            /// </summary>
            private readonly ListSortDirection _direction;

            #endregion
        }
    }
}