using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using System.Globalization;
using HashedImageURLScanner.Entities;
using System.Threading;
using Microsoft.Extensions.Configuration;
using HashedImageURLScanner.Loggers;

namespace HashedImageURLScanner.Utilities
{
    public static class WebHelper
    {

        public async static Task<String> GetBookData(IConfigurationRoot config, string bookId, string episodeId)
        {
            string WEBSERVICE_URL = String.Format("{0}?book_id={1}&episode_id={2}", config["webserviceURLRoot"], bookId, episodeId);

            try
            {
                var webRequest = System.Net.WebRequest.Create(WEBSERVICE_URL);
                if (webRequest != null)
                {
                    webRequest.Method = "GET";
                    webRequest.Timeout = 20000;
                    webRequest.ContentType = "application/json";
                    webRequest.Headers.Add(config["webRequestHeaderKey"], config["webRequestHeaderValue"]);

                    using (WebResponse response = await webRequest.GetResponseAsync())
                    {
                        using (Stream s = response.GetResponseStream())
                        {
                            using (StreamReader sr = new StreamReader(s))
                            {
                                var jsonResponse = sr.ReadToEnd();
                                dynamic responseObject = JsonConvert.DeserializeObject(jsonResponse);
                                return "";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                EmailNotifier emailNotifier = new EmailNotifier(config);
                emailNotifier.SendNotificationEmailError("GetTokenQueryString", ex.ToString());
            }

            return null;
        }


        public async static Task<ProductServerData> GetProductData(IConfigurationRoot config, int product_id)
        {
            string WEBSERVICE_URL = String.Format("{0}{1}", config["webserviceURLRoot"], product_id);

            try
            {
                var webRequest = System.Net.WebRequest.Create(WEBSERVICE_URL);
                if (webRequest != null)
                {
                    webRequest.Method = "GET";
                    webRequest.Timeout = 20000;
                    webRequest.ContentType = "text/html";

                    using (WebResponse response = await webRequest.GetResponseAsync())
                    {
                        using (Stream s = response.GetResponseStream())
                        {
                            using (StreamReader sr = new StreamReader(s))
                            {
                                var htmlResponse = sr.ReadToEnd();
                                var jsonResponse = StringHelper.GetJsonResponseFromHTML(htmlResponse);

                                if(jsonResponse != null)
                                {
                                    ProductServerData product = JsonConvert.DeserializeObject<ProductServerData>(jsonResponse);
                                    return product;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                EmailNotifier emailNotifier = new EmailNotifier(config);
                emailNotifier.SendNotificationEmailError("GetTokenQueryString", ex.ToString());
            }

            return null;
        }

        public static async Task<HttpStatusCode> GetUrlStatusCode(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);

                return response.StatusCode;
            }
        }

        public static async Task DownloadURL(IConfigurationRoot config, string url)
        {
            string downloadFolder = @config["localDownloadFolder"]; //Make sure this folder exists or an error will throw. Outputs in ###.jpg format.
            var logger = new Logger(config["localLogLocation"], Convert.ToBoolean(config["logToTextFile"]));
            var emailNotifier = new EmailNotifier(config);

            using (WebClient client = new WebClient())
            {
                try
                {
                    try
                    {
                        logger.Log(String.Format("Downloading specific URL {0}. Current time is {1}", url, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture)));
                        await client.DownloadFileTaskAsync(new Uri(url), downloadFolder + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss-fff", CultureInfo.CurrentCulture).ToString() + ".jpg");
                    }
                    catch (Exception ex)
                    {
                        logger.Log(String.Format("Error in downloading url. Current time is {0}. Error: {1}", DateTime.Now.ToString(), ex.ToString()));
                        emailNotifier.SendNotificationEmailError("DownloadURL", ex.ToString());
                    }
                }
                catch (Exception ex)
                {
                    logger.Log(String.Format("Error in downloading url. Current time is {0}. Error: {1}", DateTime.Now.ToString(), ex.ToString()));
                    emailNotifier.SendNotificationEmailError("DownloadURL", ex.ToString());
                }
            }
        }
        public static async Task DownloadURLNumberedList(IConfigurationRoot config, string url, int pagesToDownload)
        {
            string downloadFolder = @config["localDownloadFolder"] + "/" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss-fff", CultureInfo.CurrentCulture).ToString() + "/"; //Make sure this folder exists or an error will throw. Outputs in ###.jpg format.
            var logger = new Logger(config["localLogLocation"], Convert.ToBoolean(config["logToTextFile"]));
            var emailNotifier = new EmailNotifier(config);
            int errorCount = 0;

            //Create folder based on episodeId if it doesn't exist
            if (!Directory.Exists(downloadFolder))
            {
                Directory.CreateDirectory(downloadFolder);
            }

            using (WebClient client = new WebClient())
            {
                try
                {
                    for (int i = 1; i <= pagesToDownload; i++)
                    {
                        if (errorCount > 5)
                        {
                            logger.Log(String.Format("Got more than 5 errors in a row, probably reached end of the book. Stopping download. Current time is {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture)));
                            return;
                        }

                        try
                        {
                            logger.Log(String.Format("Downloading file {0} of {1}. URL: {2} Current time is {3}", i, pagesToDownload, url, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture)));
                            await client.DownloadFileTaskAsync(new Uri(url), downloadFolder + i + ".jpg");
                            errorCount = 0;
                            //Set URL to next number
                            url = StringHelper.ReplaceLastOccurrence(url, i + ".jpg", (i + 1) + ".jpg");
                        }
                        catch (Exception ex)
                        {
                            logger.Log(String.Format("Error downloading page {0}. URL: {1} Current time is {2}. Error message: {3}", i, url, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture), ex.ToString()));
                            logger.Log(String.Format("Trying again in 2 seconds.", i, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture)));
                            emailNotifier.SendNotificationEmailError("DownloadPage", ex.ToString());
                            Thread.Sleep(2000);
                            i--;
                            errorCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Log(String.Format("Error in downloading Book. Current time is {0}. Error: {1}", DateTime.Now.ToString(), ex.ToString()));
                    emailNotifier.SendNotificationEmailError("DownloadBook", ex.ToString());
                    errorCount++;
                }
            }
        }
    }
}
