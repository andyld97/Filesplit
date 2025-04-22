namespace Filesplit.Model
{
    public class Progress
    {
        public double TotalProgress { get; set; }

        public double CurrentProgress { get; set; }

        public long TotalBytesProcessed { get; set; }

        public long TotalBytes { get; set; }

        public long CurrentBytesProcessed { get; set; }

        public long CurrentFileBytes { get; set; }  
    }
}