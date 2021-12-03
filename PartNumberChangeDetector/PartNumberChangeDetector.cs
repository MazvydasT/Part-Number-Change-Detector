using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;

namespace PartNumberChangeDetector
{
    public static class PartNumberChangeDetector
    {
        public static Task GetChangedDSs(string ebomReportPath, string eMSEBOMReportPath, IReadOnlyList<string> prevEMSEBOMReportPaths,
        string pathToDSList,
        IProgress<ProgressUpdate> progress = null,
        CancellationToken cancellationToken = default) => Task.Factory.StartNew(() =>
        {
            using (var fileStream = new FileStream(pathToDSList, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
            using (var streamWriter = new StreamWriter(fileStream, new UTF8Encoding(false)))
            {
                try
                {
                    GetChangedDSs(ebomReportPath, eMSEBOMReportPath, prevEMSEBOMReportPaths, streamWriter, progress, cancellationToken).Wait();
                }

                catch (Exception dsListException)
                {
                    progress?.Report(new ProgressUpdate { IsError = true, Message = $"DS list output file: {pathToDSList}\n\n{dsListException.Message}" });
                    return;
                }

                finally
                {
                    fileStream?.SetLength(fileStream?.Position ?? 0); // Trunkates existing file
                }
            }
        }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        public static Task GetChangedDSs(string ebomReportPath, string eMSEBOMReportPath, IReadOnlyList<string> prevEMSEBOMReportPaths,
            TextWriter dsListWriter,
            IProgress<ProgressUpdate> progress = null,
            CancellationToken cancellationToken = default) => Task.Factory.StartNew(() =>
            {
                var maxProgress = 2.0 + prevEMSEBOMReportPaths.Count;

                var ebomReportRecords = new Dictionary<string, EBOMReportRecord>();

                try
                {
                    foreach (var ebomReportItem in CSVManager.ReadEBOMReport(ebomReportPath))
                    {
                        if (cancellationToken.IsCancellationRequested) return;

                        var ebomReportRecord = ebomReportItem.EBOMReportRecord;
                        var ebomReportProgressUpdate = ebomReportItem.ProgressUpdate;

                        ebomReportRecords[ebomReportRecord.DSNumber] = ebomReportRecord;

                        var value = ebomReportProgressUpdate.Value / ebomReportProgressUpdate.Max;
                        progress?.Report(new ProgressUpdate { Max = maxProgress, Value = value, Message = $"Reading current EBOM report: {value:P2}" });
                    }
                }

                catch (Exception exception)
                {
                    progress?.Report(new ProgressUpdate { IsError = true, Message = $"Current EBOM report file: {ebomReportPath}\n\n{exception.Message}" });
                    return;
                }



                var checkSet = new Dictionary<string, (EBOMReportRecord EBOMReportRecord, string JoinedPartNumbers, IList<(string Number, int Level)> PartNumbers, double Version)>();

                try
                {
                    foreach (var eMSDataItem in CSVManager.ReadEMSEBOMReport(eMSEBOMReportPath))
                    {
                        if (cancellationToken.IsCancellationRequested) return;

                        if (ebomReportRecords.TryGetValue(eMSDataItem.DSNumber, out var ebomReportRecord) &&
                            double.TryParse(ebomReportRecord.Version, out var ebomReportVersion) &&
                            double.TryParse(eMSDataItem.Version, out var eMSReportVersion) &&
                            Math.Floor(ebomReportVersion) == Math.Floor(eMSReportVersion))
                        {
                            checkSet[eMSDataItem.DSNumber] = (ebomReportRecord, eMSDataItem.JoinedPartNumbers, eMSDataItem.PartNumbers, eMSReportVersion);
                        }

                        var eMSReportProgressUpdate = eMSDataItem.ProgressUpdate;

                        var value = eMSReportProgressUpdate.Value / eMSReportProgressUpdate.Max;
                        progress?.Report(new ProgressUpdate { Max = maxProgress, Value = 1.0 + value, Message = $"Reading current eMS EBOM report: {value:P2}" });
                    }
                }

                catch (Exception exception)
                {
                    progress?.Report(new ProgressUpdate { IsError = true, Message = $"Current eMS EBOM report file: {eMSEBOMReportPath}\n\n{exception.Message}" });
                    return;
                }

                var changedDSCounter = 0;

                using (var csvWriter = new CsvWriter(dsListWriter, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Encoding = new UTF8Encoding(false),
                    LeaveOpen = true
                }))
                {
                    for (int i = 0, c = prevEMSEBOMReportPaths?.Count ?? 0; i < c; ++i)
                    {
                        var prevEMSEBOMReportPath = prevEMSEBOMReportPaths[i];

                        try
                        {
                            foreach (var prevEMSDataItem in CSVManager.ReadEMSEBOMReport(prevEMSEBOMReportPath))
                            {
                                if (cancellationToken.IsCancellationRequested) return;

                                var dsNumber = prevEMSDataItem.DSNumber;

                                if (checkSet.TryGetValue(dsNumber, out var checkSetDataItem) &&
                                    double.TryParse(prevEMSDataItem.Version, out var prevEMSReportVersion) &&
                                    prevEMSReportVersion == checkSetDataItem.Version &&
                                    prevEMSDataItem.JoinedPartNumbers != checkSetDataItem.JoinedPartNumbers)
                                {
                                    var ebomReportRecord = checkSetDataItem.EBOMReportRecord;

                                    var currentPartNumberCount = checkSetDataItem.PartNumbers.Where((v, index) => checkSetDataItem.PartNumbers.Count == index + 1 || checkSetDataItem.PartNumbers[index + 1].Level <= v.Level)
                                        .GroupBy(v => v.Number).ToDictionary(g => g.Key, g => g.Count());
                                    var prevPartNumberCount = prevEMSDataItem.PartNumbers.Where((v, index) => prevEMSDataItem.PartNumbers.Count == index + 1 || prevEMSDataItem.PartNumbers[index + 1].Level <= v.Level)
                                        .GroupBy(v => v.Number).ToDictionary(g => g.Key, g => g.Count());

                                    var changes = new List<string>();

                                    foreach (var currentPair in currentPartNumberCount)
                                    {
                                        var currentNumber = currentPair.Key;
                                        var currentCount = currentPair.Value;

                                        if (!prevPartNumberCount.TryGetValue(currentNumber, out var previousCount))
                                        {
                                            changes.Add($"+ {currentNumber} x{currentCount}");
                                        }
                                    }

                                    if (changes.Count == 0) continue;

                                    ++changedDSCounter;

                                    csvWriter.WriteRecord(new DSListRecord
                                    {
                                        DSNumberWithVersion = $"{dsNumber}_{(int)Math.Floor(checkSetDataItem.Version)}.1",
                                        Maturity = ebomReportRecord.Maturity,
                                        LastModificationDate = ebomReportRecord.LastModificationDate,
                                        ItemType = ebomReportRecord.ItemType,
                                        HasChildren = $"{ebomReportRecord.HasChildren.Trim().ToUpper() == "Y"}".ToUpper(),
                                        Changes = string.Join("\n", changes)
                                    });
                                    csvWriter.NextRecord();

                                    checkSet.Remove(dsNumber);

                                    if (checkSet.Count == 0) goto checkSetIsEmpty;
                                }

                                var prevEMSDataProgressUpdate = prevEMSDataItem.ProgressUpdate;

                                var value = prevEMSDataProgressUpdate.Value / prevEMSDataProgressUpdate.Max;
                                progress?.Report(new ProgressUpdate { Max = maxProgress, Value = 2.0 + i + value, Message = $"Reading {i + 1} of {c} previous eMS EBOM report{(c > 1 ? "s" : "")} and writing DS list: {value:P2}" });
                            }
                        }

                        catch (Exception eMSReportException)
                        {
                            progress?.Report(new ProgressUpdate { IsError = true, Message = $"Previous eMS EBOM report file {i + 1}: {prevEMSEBOMReportPath}\n\n{eMSReportException.Message}" });
                            return;
                        }
                    }
                }

            checkSetIsEmpty:
                progress?.Report(new ProgressUpdate { Max = maxProgress, Value = maxProgress, Message = $"Done: {changedDSCounter} DS" + (changedDSCounter == 1 ? "" : "s") + " listed" });

            }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }
}
