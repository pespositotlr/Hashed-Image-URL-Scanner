using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashedImageURLScanner.Utilities
{
    public static class StringHelper
    {
        public static string GetTranslatedTitle(string sourceLanguageTitle)
        {
            var translatedTitle = sourceLanguageTitle.Replace("TEST_INPUT", "TEST_RESULT");

            return translatedTitle;
        }

        public static string GetTranslatedAuthor(string sourceLanguageAuthor)
        {
            sourceLanguageAuthor = sourceLanguageAuthor.Replace("TEST_INPUT", "TEST_RESULT");

            return sourceLanguageAuthor;
        }
        public static string PadZeroes(string input, int totalDigitCount)
        {
            int numberOfZeroes = (totalDigitCount - input.Length);

            StringBuilder zeroesBuilder = new StringBuilder();

            for (int i = 0; i < numberOfZeroes; i++)
            {
                zeroesBuilder.Append('0');
            }

            return zeroesBuilder.ToString() + input;
        }

        public static string SetUrlTo0thHash(string url)
        {
            //Set hash to 0000
            string currentHash = GetCurrentHash(url);

            return url.Replace(currentHash, "0000");
        }

        public static string GetCurrentHash(string url)
        {
            //Loops through the hash numbers for a given url. A 4 digit hexidecimal number.

            int startIndex = 38;
            int length = 4;
            return url.Substring(startIndex, length);
        }

        public static string SetUrlToNextHash(string url)
        {
            //Increments the hash numbers for a given url by 1. A 4 digit hexidecimal number.

            string currentHash = GetCurrentHash(url);

            if (currentHash == "ffff")
                return "";

            int intFromHex = int.Parse(currentHash, System.Globalization.NumberStyles.HexNumber) + 1;

            string newHash = PadZeroes(intFromHex.ToString("X"), 4);

            //Use the /'s to make sure you replace the right part of the string as it goes through every possible 4-digit hash
            return url.Replace("/" + currentHash + "/", "/" + newHash.ToLower() + "/");
        }
        public static string SetUrlToHashFromInt(string url, int intValue)
        {
            if (intValue >= 65536)
                return "";

            string currentHash = GetCurrentHash(url);
            string newHash = PadZeroes(intValue.ToString("X"), 4);

            //Use the /'s to make sure you replace the right part of the string as it goes through every possible 4-digit hash
            return url.Replace("/" + currentHash + "/", "/" + newHash.ToLower() + "/");
        }

        public static string SetUrlToSpecificHash(string url, string hash)
        {
            string currentHash = GetCurrentHash(url);
            string newHash = PadZeroes(hash, 4);

            //Use the /'s to make sure you replace the right part of the string as it goes through every possible 4-digit hash
            return url.Replace("/" + currentHash + "/", "/" + newHash.ToLower() + "/");
        }

        public static string GetCurrentIssue(string url)
        {
            int startIndex = 65;
            int length = 9;
            return url.Substring(startIndex, length);
        }
        public static string SetUrlToNextIssue(string url)
        {
            //Sets hash numbers for a given url after the .net.

            string currentIssue = GetCurrentIssue(url);
            int newIssue = Convert.ToInt32(currentIssue) + 1;

            return url.Replace(currentIssue, newIssue.ToString());
        }


        public static string GetProductId(string url)
        {

            int startIndex = url.IndexOf("product_id=") + 11;
            int length = 7;
            return url.Substring(startIndex, length);
        }
        public static string GetIssuePrefix(string url)
        {
            //Gets the prefix before the issue number

            int startIndex = 51;
            int length = 14;
            return url.Substring(startIndex, length);
        }

        public static string ReplaceLastOccurrence(string source, string find, string replace)
        {
            int place = source.LastIndexOf(find);

            if (place == -1)
                return source;

            return source.Remove(place, find.Length).Insert(place, replace);
        }


        public static string GetJsonResponseFromHTML(string htmlResponse)
        {
            int startIndex = htmlResponse.IndexOf("var book_data=") + 14;
            int endIndex = htmlResponse.IndexOf(";\n            var pages_data =");
            int length = endIndex - startIndex;
            return htmlResponse.Substring(startIndex, length);
        }

        public static int GetIntFromHash(string hash)
        {
            return int.Parse(hash, System.Globalization.NumberStyles.HexNumber);
        }
        public static string GetHashFromInt(int intValue)
        {
            return intValue.ToString("X");
        }
        public static string GetHashFromIntWithPaddedZeroes(int intValue)
        {
            return PadZeroes(intValue.ToString("X"), 4);
        }
        public static string GetIssueIDFromS3Key(string S3Key)
        {
            //Ex: 2f94/browser/ABS_mbj_27537_125514648_001_001_trial/1/

            int startIndex = S3Key.IndexOf("ABS_mbj_") + 14; ;
            int length = 9;
            return S3Key.Substring(startIndex, length);
        }
    }
}
