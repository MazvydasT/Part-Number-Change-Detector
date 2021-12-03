using CsvHelper.Configuration.Attributes;

namespace PartNumberChangeDetector
{
    public struct EMSEBOMReportRecord
    {
        [Name("MP")]
        public string DSNumber { get; set; }

        [Name("LEVEL")]
        public string Level { get; set; }

        [Name("VERSION")]
        public string Version { get; set; }

        [Name("PART_NUMBER")]
        public string PartNumber { get; set; }
    }
}