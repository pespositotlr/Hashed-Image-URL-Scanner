using HashedImageURLScanner.Entities;
using HashedImageURLScanner.Loggers;
using HashedImageURLScanner.Utilities;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HashedImageURLScanner.Classes
{

    /// <summary>
    /// Scans specific urls for if a file is found. Does a brute-force search through different ids and hashes to find the latest files.
    /// </summary>
    public class URLScanner
    {
        private ScannerSettings settings;
        IConfigurationRoot config;
        Logger logger;
        EmailNotifier emailNotifier;
        int attemptNumber;
        bool issueDone;
        string url;
        bool hashFound = false;
        int remainingTasks = 0;

        public URLScanner(string urlToCheck, int maximumAttempts = 100000000, int timeBetweenAttemptsMilliseconds = 3)
        {
            settings = new ScannerSettings();
            var builder = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true)
                .AddEnvironmentVariables();
            config = builder.Build();
            logger = new Logger(config["localLogLocation"], Convert.ToBoolean(config["logToTextFile"]));
            emailNotifier = new EmailNotifier(config);

            url = urlToCheck;
            settings.MaximumAttempts = maximumAttempts;
            settings.TimeBetweenAttemptsMilliseconds = timeBetweenAttemptsMilliseconds;
        }

        public async Task GetHashForIssue(int startingHashInt = 0, int endingHashInt = 65536, int maxLocalAttempts = 65536)
        {
            //await bot.LogToDiscord();
            attemptNumber = 1;
            int localAttemptNumber = 1;
            TimeSpan t = TimeSpan.FromMilliseconds(settings.TimeBetweenAttemptsMilliseconds);
            string timeBetweenAttemptsString = t.Minutes.ToString();
            string startingHashString = StringHelper.GetHashFromIntWithPaddedZeroes(startingHashInt);
            string endingHashString = StringHelper.GetHashFromIntWithPaddedZeroes(endingHashInt);
            string currentUrl = StringHelper.SetUrlToSpecificHash(url, startingHashString);
            int currentHashInt = startingHashInt;
            bool hashDone = false;

            logger.Log(String.Format("Beginning scan for issue URL {0} from hash {1} to {2}. Maximum number of attempts is {3}. The current time is {4}", url, startingHashString, endingHashString, settings.MaximumAttempts, DateTime.Now.ToString()));

            while (!hashDone)
            {
                if (attemptNumber % 1000 == 0)
                    logger.Log(attemptNumber + ": " + currentUrl);

                try
                {

                    HttpStatusCode currentStatusCode = await WebHelper.GetUrlStatusCode(currentUrl);

                    switch (currentStatusCode)
                    {
                        case HttpStatusCode.OK:
                            //Found what we're looking for, download the whole book
                            hashDone = true;
                            hashFound = true;
                            await RunFoundUrlProcess(currentUrl);
                            break;
                        case HttpStatusCode.Forbidden:
                        case HttpStatusCode.NotFound:
                            //Try again later (Units in milliseconds)
                            await Wait(settings.TimeBetweenAttemptsMilliseconds);
                            currentHashInt++;
                            localAttemptNumber++;
                            currentUrl = StringHelper.SetUrlToHashFromInt(currentUrl, currentHashInt);
                            if (currentUrl == "" || currentHashInt >= endingHashInt)
                            {
                                hashDone = true;
                                logger.Log(String.Format("Reached ending hash of {0}", endingHashInt));
                            }
                            break;
                        default:
                            //Log that we somehow got some other error
                            logger.Log(String.Format("Got weird response code: {0}. Current time is: {1}", currentStatusCode.ToString(), DateTime.Now.ToString()));
                            //Wait and then try this URL again
                            await Wait(settings.TimeBetweenAttemptsMilliseconds);
                            //emailNotifier.SendNotificationEmailError("Main", "Got weird response code: " + currentStatusCode.ToString());
                            break;
                    }

                    if (hashFound || localAttemptNumber == maxLocalAttempts || attemptNumber == settings.MaximumAttempts)
                        hashDone = true;
                    else
                        attemptNumber++;

                }
                catch (Exception ex)
                {
                    logger.Log(String.Format("Got an error trying to get status code. Error: {0}. Current time is: {1}", ex.ToString(), DateTime.Now.ToString()));
                    await Wait(1000); //Sleep for 1 second before trying again.
                }

            }

            try
            {
                if (hashDone)
                {
                    remainingTasks--;
                    logger.Log(String.Format("All done! (Hash {0} to {1}) | Remaining Tasks: {2} | Hash Found: {3}", startingHashString, endingHashString, remainingTasks, hashFound));
                }
            }
            catch (Exception ex)
            {
                await Wait(100);
            }

            return;
        }

        public async Task GetHashForIssueParallel(int numOfTasks = 660, int hashesToCheckPerTask = 100)
        {
            var tasks = new List<Task>();
            tasks.Add(GetHashForIssue(0, 0));
            remainingTasks = numOfTasks;

            for (int i = 0; i < numOfTasks; i++)
            {
                int startingHash = i * hashesToCheckPerTask + 1;
                int endingHash = (i * hashesToCheckPerTask) + hashesToCheckPerTask;

                //Don't go higher than ffff
                if (endingHash > 65535)
                    endingHash = 65535;

                if (startingHash <= 65535)
                {
                    tasks.Add(GetHashForIssue(startingHash, endingHash, hashesToCheckPerTask));
                }
            }

            await Task.WhenAll(tasks);

            if (hashFound)
                logger.Log(String.Format("All done! Hash Found: {0}", hashFound));
        }

        public async Task GetNextIssue(string productUrl)
        {
            //await bot.LogToDiscord();
            attemptNumber = 1;
            TimeSpan t = TimeSpan.FromMilliseconds(settings.TimeBetweenAttemptsMilliseconds);
            string timeBetweenAttemptsString = t.Minutes.ToString();
            int currentProductId = Convert.ToInt32(StringHelper.GetProductId(productUrl));
            bool latestIssueFound = false;
            string imageUrlPrefix = StringHelper.GetIssueMiniPrefix(url);
            string currentProductUrl = String.Format("{0}{1}", config["webserviceProductURLRoot"], currentProductId);
            string latestImageUrl = "";
            int NotFoundErrorCount = 0;
            int MaxNotFoundErrorCount = 1000;
            List<string> foundIssueIDs = new List<string>();

            logger.Log(String.Format("Beginning scan for next issue for URL {0}. Maximum number of attempts is {1}. The current time is {2}", url, settings.MaximumAttempts, DateTime.Now.ToString()));

            try
            {
                while (!latestIssueFound)
                {
                    logger.Log("Current ProjectId: " + currentProductId + " NotFoundErrorCount: " + NotFoundErrorCount);

                    HttpStatusCode currentStatusCode = await WebHelper.GetUrlStatusCode(currentProductUrl);

                    switch (currentStatusCode)
                    {
                        case HttpStatusCode.OK:
                            //Search for that productId
                            var product = await WebHelper.GetProductData(config, currentProductId);

                            //If that product is the type you're searching for, then save that value and overwrite it with a later one
                            if (product != null && product.S3_key.Contains(imageUrlPrefix))
                            {
                                latestImageUrl = config["imageBaseURL"] + product.S3_key + "1.jpg";
                                foundIssueIDs.Add(StringHelper.GetIssueIDFromS3Key(product.S3_key));
                                logger.Log("Current Latest Image URL: " + latestImageUrl);
                            }
                            currentProductId++;
                            NotFoundErrorCount = 0;
                            await Wait(settings.TimeBetweenAttemptsMilliseconds);
                            break;
                        case HttpStatusCode.Forbidden:
                        case HttpStatusCode.NotFound:
                        case HttpStatusCode.InternalServerError:
                            //You've reached the end of published products
                            NotFoundErrorCount++;
                            //There can be not-founds for one id but then a greater id IS found
                            if (NotFoundErrorCount > MaxNotFoundErrorCount)
                                latestIssueFound = true;
                            else
                                currentProductId++;
                            break;
                        default:
                            currentProductId++;
                            logger.Log(String.Format("Got weird response code: {0}. Current time is: {1}", currentStatusCode.ToString(), DateTime.Now.ToString()));
                            break;
                    }

                    currentProductUrl = String.Format("{0}{1}", config["webserviceProductURLRoot"], currentProductId);
                }

                //Start looking for the next issue to be published with that prefix
                logger.Log(String.Format("Latest Issue fond."));
                logger.Log(String.Format("Last Released ProductID: " + currentProductId));
                logger.Log(String.Format("Latest Image URL for Prefix: " + latestImageUrl));

                logger.Log(String.Format("Found IDs:"));
                foreach (var issueID in foundIssueIDs)
                {
                    logger.Log(issueID);
                }

                //Search through all hashes for the next issue with that prefix
                url = StringHelper.SetUrlToNextIssue(latestImageUrl);
                settings.MaximumAttempts = 65536; //Hash is 4 digits so this is 16^4

                //Now start searching for this issue by checking all hashes
                //while(!issueDone)
                //{
                //    await GetHashForIssueParallel(660, 100);
                //    logger.Log(String.Format("Done searching all hashes for: " + latestImageUrl));
                //    attemptNumber++;
                //    if (hashFound)
                //        issueDone = true;
                //}

            }
            catch (Exception ex)
            {
                logger.Log(String.Format("Got an error trying to get status code. Error: {0}. Current time is: {1}", ex.ToString(), DateTime.Now.ToString()));
                await Wait(10000); //Sleep for 10 seconds before trying again.
            }

            return;
        }

        private async Task Wait(int milliseconds)
        {
            await Task.Delay(milliseconds);
        }

        private async Task RunFoundUrlProcess(string foundURL)
        {
            logger.Log(String.Format("URL found! Found {0} found at {1}", foundURL, DateTime.Now.ToString()));
            emailNotifier.SendNotificationUrlFound(foundURL);
            //await bot.PostMessage(String.Format("URL found! Found {0} found at {1}", url, DateTime.Now.ToString()));

            await WebHelper.DownloadURLNumberedList(config, foundURL, 999);
            return;
        }

    }
}
