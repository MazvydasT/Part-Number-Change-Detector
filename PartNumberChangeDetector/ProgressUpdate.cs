namespace PartNumberChangeDetector
{
    public struct ProgressUpdate
    {
        public double Max { get; set; }
        public double Value { get; set; }
        public string Message { get; set; }
        public bool IsError { get; set; }
    }
}
