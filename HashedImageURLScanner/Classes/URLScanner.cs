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

        public async Task GetHashForIssue(string startingHash = "0000", string endingHash = "ffff")
        {
            //await bot.LogToDiscord();
            attemptNumber = 1;
            TimeSpan t = TimeSpan.FromMilliseconds(settings.TimeBetweenAttemptsMilliseconds);
            string timeBetweenAttemptsString = t.Minutes.ToString();
            string currentUrl = StringHelper.SetUrlToSpecificHash(url, startingHash);
            bool hashDone = false;

            logger.Log(String.Format("Beginning scan for issue URL {0} from hash {1} to {2}. Maximum number of attempts is {3}. The current time is {4}", url, startingHash, endingHash, settings.MaximumAttempts, DateTime.Now.ToString()));

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
                            await RunFoundUrlProcess();
                            break;
                        case HttpStatusCode.Forbidden: case HttpStatusCode.NotFound:
                            //Try again later (Units in milliseconds)
                            await Wait(settings.TimeBetweenAttemptsMilliseconds);
                            currentUrl = StringHelper.SetUrlToNextHash(currentUrl);
                            if (currentUrl == "" || endingHash == StringHelper.GetCurrentHash(currentUrl))
                            {
                                hashDone = true;
                                logger.Log(String.Format("Reached ending hash of {0}", endingHash));
                            }
                            break;
                        default:
                            //Log that we somehow got some other error
                            logger.Log(String.Format("Got weird response code: {0}. Current time is: {0}", currentStatusCode.ToString(), DateTime.Now.ToString()));
                            emailNotifier.SendNotificationEmailError("Main", "Got weird response code: " + currentStatusCode.ToString());
                            break;
                    }

                    if (hashFound || attemptNumber == settings.MaximumAttempts)
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
                    logger.Log(String.Format("All done! (Hash {0} to {1}) Hash Found: {2}", startingHash, endingHash, hashFound));
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
            tasks.Add(GetHashForIssue("0000", "0000"));

            for (int i = 0; i < numOfTasks; i++)
            {
                int startingHash = i * hashesToCheckPerTask + 1;
                int endingHash = (i * hashesToCheckPerTask) + hashesToCheckPerTask;

                //Don't go higher than ffff
                if (endingHash > 65535)
                    endingHash = 65535;

                if(startingHash <= 65535)
                {
                    string startingHashString = StringHelper.PadZeroes(startingHash.ToString("X"), 4);
                    string endingHashString = StringHelper.PadZeroes(endingHash.ToString("X"), 4);

                    tasks.Add(GetHashForIssue(startingHashString, endingHashString));
                }
            }

            await Task.WhenAll(tasks);

            if (hashFound)
                logger.Log(String.Format("All done! Hash Found: {2}", hashFound));
        }

        public async Task GetNextIssue(string productUrl, string imageUrl)
        {
            //await bot.LogToDiscord();
            attemptNumber = 1;
            TimeSpan t = TimeSpan.FromMilliseconds(settings.TimeBetweenAttemptsMilliseconds);
            string timeBetweenAttemptsString = t.Minutes.ToString();
            int currentProductId = Convert.ToInt32(StringHelper.GetProductId(productUrl));
            bool latestIssueFound = false;
            string imageUrlPrefix = StringHelper.GetIssuePrefix(imageUrl);
            string currentProductUrl = String.Format("{0}{1}", config["webserviceURLRoot"], currentProductId);
            string latestImageUrl = "";
            int NotFoundErrorCount = 0;
            int MaxNotFoundErrorCount = 1000;

            logger.Log(String.Format("Beginning scan for next issue for URL {0}. Maximum number of attempts is {1}. The current time is {2}", url, settings.MaximumAttempts, DateTime.Now.ToString()));

            try
            {
                while(!latestIssueFound)
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
                                logger.Log("Current Latest Image URL: " + latestImageUrl);
                            }
                            currentProductId++;
                            NotFoundErrorCount = 0;
                            await Wait(settings.TimeBetweenAttemptsMilliseconds);
                            break;
                        case HttpStatusCode.Forbidden: case HttpStatusCode.NotFound: case HttpStatusCode.InternalServerError:
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

                    currentProductUrl = String.Format("{0}{1}", config["webserviceURLRoot"], currentProductId);
                }

                //Start looking for the next issue to be published with that prefix
                logger.Log(String.Format("Latest Issue fond."));
                logger.Log(String.Format("Last Released ProductID: " + currentProductId));
                logger.Log(String.Format("Latest Image URL for Prefix: " + latestImageUrl));

                //Search through all hashes for the next issue with that prefix
                url = StringHelper.SetUrlToNextIssue(latestImageUrl);
                settings.MaximumAttempts = 65536; //Hash is 4 digits so this is 16^4

                //Now start searching for this issue by checking all hashes
                //while(!issueDone)
                //{
                    await GetHashForIssueParallel(660, 100);
                    logger.Log(String.Format("Done searching all hashes for: " + latestImageUrl));
                    attemptNumber++;
                    if (hashFound)
                        issueDone = true;
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

        private async Task RunFoundUrlProcess()
        {
            logger.Log(String.Format("URL found! Found {0} found at {1}", url, DateTime.Now.ToString()));
            emailNotifier.SendNotificationUrlFound(url);
            //await bot.PostMessage(String.Format("URL found! Found {0} found at {1}", url, DateTime.Now.ToString()));

            await WebHelper.DownloadURL(config, url);
            return;
        }

    }
}
