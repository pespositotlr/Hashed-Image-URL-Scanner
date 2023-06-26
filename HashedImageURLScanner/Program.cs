using System;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using HashedImageURLScanner.Classes;
using HashedImageURLScanner.Constants;

namespace HashedImageURLScanner
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //URLScanner scanner = new URLScanner(MagazineIssue.Dummy);
            //await scanner.GetHashForIssueParallel(660, 100);

            URLScanner scanner = new URLScanner(Issue.Dummy);
            await scanner.GetNextIssue(ProductURL.Dummy, Issue.Dummy);
        }
    }
}
