using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;

namespace PartNumberChangeDetector
{
    public static class CSVManager
    {
        public static IEnumerable<(EBOMReportRecord EBOMReportRecord, ProgressUpdate ProgressUpdate)> ReadEBOMReport(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var streamReader = new StreamReader(fileStream))
            using (var csvReader = new CsvReader(streamReader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                BadDataFound = null
            }))
            {
                var records = csvReader.GetRecords<EBOMReportRecord>();

                foreach (var record in records)
                {
                    yield return (record, new ProgressUpdate { Max = 1.0, Value = (double)fileStream.Position / fileStream.Length });
                }
            }
        }

        public static IEnumerable<EMSReportDataItem> ReadEMSEBOMReport(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                foreach (var eMSReportDataItem in ReadEMSEBOMReport(fileStream))
                {
                    yield return eMSReportDataItem;
                }
            }
        }

        private static IEnumerable<EMSReportDataItem> ReadEMSEBOMReport(Stream stream)
        {
            CsvReader csvReader = null;
            string header = null;
            int headerLength = 0;

            var brokenRawRecord = "";
            var brokenRecord = new List<string>();
            var brokenRawRecords = new List<string>();

            var csvReaderConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                BadDataFound = null,
                DetectColumnCountChanges = false,
                MissingFieldFound = null,
                ShouldSkipRecord = shouldSkipArgs =>
                {
                    var context = csvReader.Context;
                    var headerRecord = context.Reader.HeaderRecord;
                    var parser = context.Parser;

                    if (header == null) header = parser.RawRecord.Trim();

                    if (headerRecord == null) return false;

                    if (headerRecord != null && headerLength == 0)
                    {
                        headerLength = headerRecord.Length;
                        return false;
                    }

                    var record = shouldSkipArgs.Record;
                    var recordLength = record.Length;

                    if (recordLength == 1) return false;

                    if (record.Length < headerLength)
                    {
                        var brokenRecordCount = brokenRecord.Count;

                        if (brokenRecordCount > 0)
                        {
                            brokenRawRecord += parser.RawRecord.Trim();
                            brokenRecord[brokenRecordCount - 1] = brokenRecord[brokenRecordCount - 1] + (record.FirstOrDefault() ?? "");
                            brokenRecord.AddRange(record.Skip(1));
                        }

                        else
                        {
                            brokenRawRecord = parser.RawRecord.Trim();
                            brokenRecord.AddRange(record);
                        }

                        if (brokenRecord.Count == headerLength)
                        {
                            brokenRawRecords.Add(brokenRawRecord);
                            brokenRecord.Clear();
                            brokenRawRecord = "";
                        }

                        return true;
                    }

                    return false;
                }
            };


            using (var streamReader = new StreamReader(stream))
            using (csvReader = new CsvReader(streamReader, csvReaderConfig))
            {
                var records = csvReader.GetRecords<EMSEBOMReportRecord>();

                EMSEBOMReportRecord? currentDSRecord = null;
                List<(string Number, int Level)> currentDSPartNumbers = null;

                foreach (var record in records)
                {
                    IEnumerable<EMSEBOMReportRecord> recordsToProcess = new[] { record };

                    if (brokenRawRecords.Count > 0)
                    {
                        using (var memoryStream = new MemoryStream())
                        using (var streamWriter = new StreamWriter(memoryStream))
                        {
                            streamWriter.Write(string.Join("\n", brokenRawRecords.Prepend(header)));
                            streamWriter.Flush();
                            memoryStream.Position = 0;

                            using (var r = new StreamReader(memoryStream))
                            using (var c = new CsvReader(r, csvReaderConfig))
                            {
                                recordsToProcess = c.GetRecords<EMSEBOMReportRecord>().Concat(recordsToProcess).ToList();
                            }
                        }

                        brokenRawRecords.Clear();
                    }

                    foreach (var recordToProcess in recordsToProcess)
                    {
                        if (int.TryParse(recordToProcess.Level, out var level))
                        {
                            if (level == 0 || recordToProcess.DSNumber != currentDSRecord?.DSNumber)
                            {
                                if (currentDSRecord.HasValue && currentDSPartNumbers != null)
                                {
                                    yield return new EMSReportDataItem
                                    {
                                        DSNumber = currentDSRecord?.DSNumber,
                                        Version = currentDSRecord?.Version,
                                        JoinedPartNumbers = string.Join("|", currentDSPartNumbers.Select(p => p.Number)),
                                        PartNumbers = currentDSPartNumbers.ToList(),
                                        ProgressUpdate = new ProgressUpdate
                                        {
                                            Max = 1.0,
                                            Value = (double)stream.Position / stream.Length
                                        }
                                    };
                                }

                                currentDSRecord = recordToProcess;
                                currentDSPartNumbers = new List<(string, int)>();

                                if (level == 0) continue;
                            }

                            currentDSPartNumbers.Add((recordToProcess.PartNumber, level));
                        }
                    }
                }

                if (currentDSRecord.HasValue && currentDSPartNumbers != null)
                {
                    yield return new EMSReportDataItem
                    {
                        DSNumber = currentDSRecord?.DSNumber,
                        Version = currentDSRecord?.Version,
                        JoinedPartNumbers = string.Join("|", currentDSPartNumbers.Select(p => p.Number)),
                        PartNumbers = currentDSPartNumbers.ToList(),
                        ProgressUpdate = new ProgressUpdate
                        {
                            Max = 1.0,
                            Value = 1.0
                        }
                    };
                }
            }
        }
    }
}
