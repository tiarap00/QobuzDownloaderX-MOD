using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace QobuzDownloaderX.Shared
{
    internal static class StringTools
    {
        public static string DecodeEncodedNonAsciiCharacters(string value)
        {
            if (value != null)
            {
                return Regex.Replace(
                value,
                @"\\u(?<Value>[a-zA-Z0-9]{4})",
                m =>
                {
                    return ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString();
                });
            }
            else
            {
                return null;
            }
        }

        // For converting illegal filename characters to an underscore.
        public static string GetSafeFilename(string filename)
        {
            if (filename != null)
            {
                return string.Join("_", filename.Split(Path.GetInvalidFileNameChars())).Trim();
            }
            else
            {
                return null;
            }
        }

        public static string FormatDateTimeOffset(DateTimeOffset? dateTimeOffset)
        {
            if (dateTimeOffset != null)
            {
                return dateTimeOffset.GetValueOrDefault().ToString("yyyy-MM-dd");
            }
            else
            {
                return "";
            }
        }

        public static string FormatDurationInSeconds(double durationInSeconds)
        {
            TimeSpan duration = TimeSpan.FromSeconds(durationInSeconds);

            return duration.TotalHours < 1 ?
                $"{duration:mm\\:ss}" :
                $"{duration:hh\\:mm\\:ss}";

        }
    }
}