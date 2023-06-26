using System;
using System.Collections.Generic;
using System.Text;

namespace HashedImageURLScanner.Entities
{
    public class ScannerSettings
    {
        public string BookId { get; set; }
        public string PreviousEpisodeId { get; set; }
        public string CurrentEpisodeId { get; set; }
        public string MiddleId { get; set; }
        public int MaximumPagesToDownload { get; set; }
        public int MaximumAttempts { get; set; }
        public int TimeBetweenAttemptsMilliseconds { get; set; }
    }
}
