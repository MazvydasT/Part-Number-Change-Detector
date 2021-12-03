namespace PartNumberChangeDetector
{
    public struct DSListRecord
    {
        public string DSNumberWithVersion { get; set; }

        private static string iPLMDesignSolution = "iPLMDesignSolution";
        public string IPLMDesignSolution => iPLMDesignSolution;

        public string Maturity { get; set; }

        public string LastModificationDate { get; set; }

        public string ItemType { get; set; }

        public string HasChildren { get; set; }

        public string Changes { get; set; }
    }
}
