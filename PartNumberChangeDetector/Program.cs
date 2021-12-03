using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using NDesk.Options;

namespace PartNumberChangeDetector
{
    class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                string pathToCurrentEBOMReport = null;
                string pathToCurrentEMSEBOMReport = null;
                List<string> pathsToPreviousEMSEBOMReports = new List<string>();

                string pathToDSList = null;

                bool showHelp = false;

                var cacheKey = new object();

                var options = new OptionSet()
                {
                    { "r|EBOMReport=", "{PATH} to current EBOM Report", v => pathToCurrentEBOMReport = Utils.PathToUNC(v, cacheKey) },
                    { "e|EMSReport=", "{PATH} to current eMS EBOM Report", v => pathToCurrentEMSEBOMReport = Utils.PathToUNC(v, cacheKey) },

                    { "p|PrevEMSReport=", "At least one {PATH} to previous eMS EBOM Report", v => pathsToPreviousEMSEBOMReports.Add(Utils.PathToUNC(v, cacheKey)) },

                    { "d|DSList:", "Optional: {PATH} to DS list output", v => pathToDSList = Utils.PathToUNC(v, cacheKey) },

                    { "h|?|help", "Shows this help message", v => showHelp = v != null }
                };

                try
                {
                    options.Parse(args);
                }

                catch (OptionException e)
                {
                    Console.Error.WriteLine($"Error:\n{Process.GetCurrentProcess().ProcessName} {string.Join(" ", args)}");
                    Console.Error.WriteLine(e.Message);

                    Console.Error.WriteLine();
                    ShowHelp(options);

                    return;
                }

                if (showHelp)
                {
                    ShowHelp(options);
                    return;
                }

                if (string.IsNullOrWhiteSpace(pathToCurrentEBOMReport))
                {
                    Console.Error.WriteLine("Error:\nPath to current EBOM Report is not provided.\n");
                    ShowHelp(options);
                    return;
                }

                if (string.IsNullOrWhiteSpace(pathToCurrentEMSEBOMReport))
                {
                    Console.Error.WriteLine("Error:\nPath to current eMS EBOM Report is not provided.\n");
                    ShowHelp(options);
                    return;
                }

                if (pathsToPreviousEMSEBOMReports.Count == 0)
                {
                    Console.Error.WriteLine("Error:\nPath to at least one previous eMS EBOM Report is not provided.\n");
                    ShowHelp(options);
                    return;
                }

                var pathToDSListIsBlank = string.IsNullOrWhiteSpace(pathToDSList);

                ConsoleProgress<ProgressUpdate> progress = null;

                if (!pathToDSListIsBlank || (pathToDSListIsBlank && Console.IsOutputRedirected))
                {
                    progress = new ConsoleProgress<ProgressUpdate>((progressUpdate) =>
                    {
                        if (progressUpdate.IsError) Console.Error.Write($"\nError:\n\n{progressUpdate.Message}");
                        else Console.Error.Write($"\rProgress: {progressUpdate.Value / progressUpdate.Max:P2}");
                    });
                }

                try
                {
                    if (pathToDSListIsBlank)
                    {
                        Console.OutputEncoding = new UTF8Encoding(false);

                        PartNumberChangeDetector.GetChangedDSs(pathToCurrentEBOMReport, pathToCurrentEMSEBOMReport, pathsToPreviousEMSEBOMReports, Console.Out, progress).Wait();
                    }

                    else
                    {
                        PartNumberChangeDetector.GetChangedDSs(pathToCurrentEBOMReport, pathToCurrentEMSEBOMReport, pathsToPreviousEMSEBOMReports, pathToDSList, progress).Wait();
                    }
                }

                finally
                {
                    Console.Error.WriteLine();
                }
            }

            else
            {
                var handle = GetConsoleWindow();
                ShowWindow(handle, 0); // To hide

                new Application().Run(new MainWindow());
            }
        }

        public static void ShowHelp(OptionSet options)
        {
            Console.Error.WriteLine("Usage:\n");

            var start = $"{Process.GetCurrentProcess().ProcessName} ";
            var padding = "".PadLeft(start.Length + 1);

            Console.Error.WriteLine(" To compare against multiple previous reports:\n");
            Console.Error.WriteLine($" {start}-r=EBOM_Report.csv" +
                $"\n{padding}-e=eMS_EBOM_Report.csv" +
                $"\n{padding}-p=previous_eMS_EBOM_Report_1.csv" +
                $"\n{padding}-p=previous_eMS_EBOM_Report_2.csv" +
                $"\n{padding}-d=ds_list.csv");
            Console.Error.WriteLine("\n");

            Console.Error.WriteLine(" To output ds_list.csv by redirection:\n");
            Console.Error.WriteLine($" {start}-r EBOM_Report.csv" +
                $"\n{padding}-e eMS_EBOM_Report.csv" +
                $"\n{padding}-p \"previous eMS EBOM Report.csv\"" +
                $"\n{padding}> ds_list.csv");
            Console.Error.WriteLine("\n");

            Console.Error.WriteLine(" To output DS list to console:\n");
            Console.Error.WriteLine($" {start}-r \"EBOM Report.csv\"" +
                $"\n{padding}-e \"eMS EBOM Report.csv\"" +
                $"\n{padding}-p=previous_eMS_EBOM_Report.csv");
            Console.Error.WriteLine("\n");

            Console.Error.WriteLine("Options:");
            options.WriteOptionDescriptions(Console.Error);
        }
    }
}
