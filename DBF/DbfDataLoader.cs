using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DbfTests;

namespace DbfTests
{
    public class DbfDataLoader
    {
        internal bool Load(string root, string searchPattern,
                           List<OutputRow> outputs, List<string> warnings, ref string errorMessage)
        {
            var d = new DirectoryInfo(root);

            var files = d.GetFiles(searchPattern);
            var dbfReader = new DbfReader();
            foreach (FileInfo file in files)
            {
                if (!ProcessFile(file, outputs, dbfReader, warnings, ref errorMessage))
                    return false;
            }

            // Recursively load files from sub-directories
            var directories = d.GetDirectories();
            foreach (var directory in directories)
                if (!Load(directory.FullName, searchPattern, outputs, warnings, ref errorMessage))
                    return false;
            return true;
        }

        private bool ProcessFile(FileInfo file, List<OutputRow> outputs, DbfReader dbfReader, List<string> warnings, ref string errorMessage)
        {
            List<DbfReader.ValueRow> valueRows = null;
            try
            {
                valueRows = dbfReader.ReadValues(file.FullName);
            }
            catch (Exception ex)
            {
                errorMessage = "Error reading values: " + ex.Message;
                return false;
            }

            OutputRow.Headers.Add(file.DirectoryName);
            AddNullValuesAtEnd(outputs);

            foreach (var row in valueRows)
            {
                var newRow = new OutputRow()
                {
                    Timestamp = row.Timestamp,
                };
                var index = outputs.BinarySearch(newRow, new CompareRowTimestamp());
                if (index < 0)   // Not found
                {
                    outputs.Insert(~index, newRow);
                    SetValuesList(newRow, row.Value);
                }
                else
                {
                    var outputRow = outputs[index];
                    if (outputRow.Values.Count < OutputRow.Headers.Count)
                        outputRow.Values.Add(row.Value);
                    else
                    {
                        if (row.Value != outputRow.Values[outputRow.Values.Count - 1])
                        {
                            /* It could be the case that the same timestamp was duplicate in the file. 
                             * Here we are creating a list of warnings to deal with this situation, but we might as well throw an exeception 
                             * or return false and stop the recursive processing - it was a judgement call.
                             * If we knew for sure that the timestamp is a db primary key, that is never duplicated we wouldn't need this verification */
                            if (outputRow.Values[outputRow.Values.Count - 1] != null)
                                warnings.Add("Value overriten. Path=" + file.Directory + "; Timestamp=" + row.Timestamp +
                                             "; old value = " + outputRow.Values[outputRow.Values.Count - 1] + "; new value=" + row.Value);
                            outputRow.Values[outputRow.Values.Count - 1] = row.Value;
                        }
                    }
                }
            }

            return true;
        }

        // For the number of columns to be the same in all rows we need to add null values at the end for the new column
        private void AddNullValuesAtEnd(List<OutputRow> outputs)
        {
            foreach (var outputRow in outputs)
                outputRow.Values.Add(null);
        }

        // Since the new value is at the end, but we are using columns, we need to add null values for the previous columns
        private void SetValuesList(OutputRow newRow, double value)
        {
            for (int i = 0; i < OutputRow.Headers.Count - 1; i++)
                newRow.Values.Add(null);
            newRow.Values.Add(value);

        }


        private class CompareRowTimestamp : IComparer<OutputRow>
        {
            public int Compare(OutputRow x, OutputRow y)
            {
                if (x.Timestamp < y.Timestamp)
                    return -1;
                else if (x.Timestamp > y.Timestamp)
                    return 1;
                else
                    return 0;
            }
        }

    }
}

