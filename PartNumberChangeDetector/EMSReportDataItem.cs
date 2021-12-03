using System.Collections.Generic;

namespace PartNumberChangeDetector
{
    public struct EMSReportDataItem
    {
        public string DSNumber { get; set; }
        public string Version { get; set; }
        public string JoinedPartNumbers { get; set; }
        public IList<(string Number, int Level)> PartNumbers { get; set; }
        public ProgressUpdate ProgressUpdate { get; set; }
    }
}
