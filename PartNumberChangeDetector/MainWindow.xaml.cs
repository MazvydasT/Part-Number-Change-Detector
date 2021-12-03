using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace PartNumberChangeDetector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        readonly OpenFileDialog openFileFialog = new OpenFileDialog
        {
            Filter = "CSV (Comma delimited) (*.csv)|*.csv",
            CheckFileExists = true
        };

        readonly SaveFileDialog saveFileFialog = new SaveFileDialog
        {
            Filter = "CSV (Comma delimited) (*.csv)|*.csv",
            CheckPathExists = true,
            OverwritePrompt = true,
            ValidateNames = true,
            Title = "Save DS list"
        };

        private void BrowseCurrentEBOMReport_Click(object sender, RoutedEventArgs e)
        {
            openFileFialog.Multiselect = false;
            openFileFialog.Title = "Select current EBOM report";
            openFileFialog.FileName = null;

            if (openFileFialog.ShowDialog() == true)
                AppState.State.PathToCurrentEBOMReport = Utils.PathToUNC(openFileFialog.FileName);
        }

        private void BrowseCurrentEMSEBOMReport_Click(object sender, RoutedEventArgs e)
        {
            openFileFialog.Multiselect = false;
            openFileFialog.Title = "Select current eMS EBOM report";
            openFileFialog.FileName = null;

            if (openFileFialog.ShowDialog() == true)
                AppState.State.PathToCurrentEMSEBOMReport = Utils.PathToUNC(openFileFialog.FileName);
        }

        private void AddPreviousEMSEBOMReports_Click(object sender, RoutedEventArgs e)
        {
            openFileFialog.Multiselect = true;
            openFileFialog.Title = "Select previous eMS EBOM reports";
            openFileFialog.FileName = null;

            if (openFileFialog.ShowDialog() == true)
            {
                var cacheKey = new object();

                AppState.State.PathsToPreviousEBOMReports.AddUnique(openFileFialog.FileNames.Select(v => Utils.PathToUNC(v, cacheKey)));
            }
        }

        private void RemovePreviousEMSEBOMReports_Click(object sender, RoutedEventArgs e) =>
            AppState.State.PathsToPreviousEBOMReports.RemoveRange(PathsToPreviousEMSEBOMReports.SelectedItems.Cast<string>());

        private CancellationTokenSource cancellationTokenSource;
        private async void ExportDSList_Click(object sender, RoutedEventArgs e)
        {
            saveFileFialog.FileName = null;

            if (saveFileFialog.ShowDialog() == true)
            {
                var appState = AppState.State;
                appState.Message = null;
                appState.Error = false;
                appState.ProgressValue = 0;

                ErrorMessageRow.Height = new GridLength(0, GridUnitType.Auto);

                Task task;

                lock (this)
                {
                    cancellationTokenSource = new CancellationTokenSource();

                    task = PartNumberChangeDetector.GetChangedDSs(appState.PathToCurrentEBOMReport, appState.PathToCurrentEMSEBOMReport,
                        appState.PathsToPreviousEBOMReports, Utils.PathToUNC(saveFileFialog.FileName),
                        new Progress<ProgressUpdate>(p =>
                        {
                            if (p.IsError)
                            {
                                appState.Error = true;
                                appState.Message = p.Message;
                            }

                            else
                            {
                                appState.ProgressValue = p.Value / p.Max;
                                appState.Message = p.Message;
                            }
                        }),
                        cancellationTokenSource.Token);
                }

                await task;

                lock (this)
                {
                    if (cancellationTokenSource == null) // Means task has been cancelled
                    {
                        appState.Message = null;
                        appState.ProgressValue = 0;
                    }

                    if (appState.Error) ErrorMessageRow.Height = new GridLength(100, GridUnitType.Star);

                    cancellationTokenSource = null;
                }
            }
        }

        private void CancelExportDSList_Click(object sender, RoutedEventArgs e)
        {
            lock (this)
            {
                cancellationTokenSource?.Cancel();
                cancellationTokenSource = null;
            }
        }
    }
}
