using System.Collections.Generic;

namespace Filesplit.Model
{
    public class Info
    {
        public string TargetFileName { get; set; } = string.Empty;

        public List<string> Files { get; set; } = new List<string>();

        public string Hash { get; set; } = string.Empty;

        public long Length { get; set; } = 0L;
    }
}
