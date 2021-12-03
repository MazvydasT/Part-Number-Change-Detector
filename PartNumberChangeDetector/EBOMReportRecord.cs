using CsvHelper.Configuration.Attributes;

namespace PartNumberChangeDetector
{

    public struct EBOMReportRecord
    {
        [Name("PART NUMBER")]
        public string DSNumber { get; set; }

        [Name("VERSION")]
        public string Version { get; set; }

        [Name("ITEM_REV_LAST_MOD_DATE")]
        public string LastModificationDate { get; set; }

        [Name("ITEM_TYPE")]
        public string ItemType { get; set; }

        [Name("HAS_CHILDREN?")]
        public string HasChildren { get; set; }

        [Name("CONFIGURED_MATURITY")]
        public string Maturity { get; set; }
    }
}