using System.Windows;
using PropertyChanged;

namespace PartNumberChangeDetector
{
    [AddINotifyPropertyChangedInterface]
    public sealed class AppState
    {
        public static AppState State { get; } = new AppState();

        public string PathToCurrentEBOMReport { get; set; }
        public string PathToCurrentEMSEBOMReport { get; set; }

        public RangeObservableCollection<string> PathsToPreviousEBOMReports { get; } = new RangeObservableCollection<string>();

        public double ProgressValue { get; set; }

        public bool Error { get; set; }
        public string Message { get; set; }

        public bool InputControlsAreEnabled => ProgressValue == 0 || ProgressValue == 1 || Error;

        public Visibility ExportButtonVisibility => InputControlsAreEnabled ? Visibility.Visible : Visibility.Collapsed;
        public Visibility CancelExportButtonVisibility => ExportButtonVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

        public Visibility SplitterVisibility => Error ? Visibility.Visible : Visibility.Collapsed;
    }
}
