using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbfTests
{
    internal class DbfReader
    {
        internal class ValueRow
        {
            public ValueRow(double value, DateTime timestamp)
            {
                Value = value;
                Timestamp = timestamp;
            }

            public double Value { get; }
            public DateTime Timestamp { get; }
        }

        public const string ColumnAttType = "ATT_TYPE";
        public const string ColumnValInt = "VALINT";
        public const string ColumnValReal = "VALREAL";
        public const string ColumnValBool = "VALBOOL";
        public const string ColumnDate = "DATE_NDX";
        public const string ColumnTime = "TIME_NDX";

        public List<ValueRow> ReadValues(string filePath)
        {
            var valueRows = new List<ValueRow>();
            using (var connection = new OleDbConnection($"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={Path.GetDirectoryName(filePath)};Extended Properties=dBASE IV;User ID=;Password=;"))
            {
                if (connection.State == ConnectionState.Closed)
                    connection.Open();

                using (var dataAdapter = new OleDbDataAdapter($"select ATT_TYPE, VALINT, VALREAL, VALBOOL, DATE_NDX, TIME_NDX from {Path.GetFileName(filePath)} where VALID=1 and RELIABLE=1", connection))
                {
                    var dataset = new DataSet();
                    dataAdapter.Fill(dataset);

                    if (dataset.Tables.Count == 1) // only one table should exist anyway
                    {
                        var relevantTable = dataset.Tables[0];
                        valueRows = relevantTable.Rows.Cast<DataRow>().Select(r => GetValueRow(r)).ToList();
                    }
                    else
                    {
                        Console.WriteLine($"File {filePath} has been ignored. 1 table must exist within the file!");
                    }
                }
            }
            return valueRows;
        }

        private ValueRow GetValueRow(DataRow dataRow)
        {
            var attType = (double)dataRow[ColumnAttType];
            double value = 0;

            switch (attType)
            {
                case 1:
                    value = (double)dataRow[ColumnValInt];
                    break;
                case 2:
                    value = (double)dataRow[ColumnValReal];
                    break;
                case 3:
                    value = (double)dataRow[ColumnValBool];
                    break;
            }

            var date = ((double)dataRow[ColumnDate]).ToString();
            string timestring = $"0000{dataRow[ColumnTime]}";
            var time = timestring.Substring(timestring.Length - 4);
            DateTime timestamp = DateTime.ParseExact($"{date}{time}", "yyyMMddHHmm", CultureInfo.InvariantCulture);
            timestamp = timestamp.AddYears(1900);
            return new ValueRow(value, timestamp);
        }
    }
}
