using QobuzApiSharp.Models.Content;
using System.Collections.Generic;

namespace QobuzDownloaderX.Shared
{
    public static class QualityStringMappings
    {
        private static readonly Dictionary<string, (string, string)> qualityMappings = new Dictionary<string, (string, string)>()
        {
            {"5", ("MP3 320kbps CBR", "MP3")},
            {"6", ("FLAC (16bit/44.1kHz)", "FLAC (16bit-44.1kHz)")},
            {"7", ("FLAC (24bit/96kHz)", "FLAC (24bit-96kHz)")},
            {"27", ("FLAC (24bit/192kHz)", "FLAC (24bit-196kHz)")}
        };

        private static readonly Dictionary<string, (double, double)> maximumBitDepthAndSampleRateMappings = new Dictionary<string, (double, double)>()
        {
            {"5", (0, 0)}, // N/A, using 0 for easy calculation check.
            {"6", (16, 44.1)},
            {"7", (24, 96)},
            {"27", (24, 196)}
        };
        public static double GetMaxBitDepth(string formatIdString)
        {
            if (maximumBitDepthAndSampleRateMappings.TryGetValue(formatIdString, out var value))
                return value.Item1;
            else
                throw new KeyNotFoundException($"QualityFormatID '{formatIdString}' not found.");
        }

        public static double GetMaxSampleRate(string formatIdString)
        {
            if (maximumBitDepthAndSampleRateMappings.TryGetValue(formatIdString, out var value))
                return value.Item2;
            else
                throw new KeyNotFoundException($"QualityFormatID '{formatIdString}' not found.");
        }

        public static (string quality, string qualityPath) GetQualityStrings(string formatIdString)
        {
            if (qualityMappings.TryGetValue(formatIdString, out var value))
                return value;
            else
                throw new KeyNotFoundException($"QualityFormatID '{formatIdString}' not found.");
        }

        public static (string quality, string qualityPath) GetQualityStrings(string formatIdString, Album qobuzAlbum)
        {
            // Get Max bitDepth & sampleRate from API result.
            double bitDepth = qobuzAlbum.MaximumBitDepth.GetValueOrDefault();
            double sampleRate = qobuzAlbum.MaximumSamplingRate.GetValueOrDefault();

            double maxSelectedQuality = GetMaxBitDepth(formatIdString) * GetMaxSampleRate(formatIdString);
            double maxItemQuality = bitDepth * sampleRate;

            // Limit to selected quality if album quality is higher.
            if (maxSelectedQuality <= maxItemQuality)
            {
                return GetQualityStrings(formatIdString);
            }

            var quality = "FLAC (" + bitDepth + "bit/" + sampleRate + "kHz)";
            var qualityPath = quality.Replace(@"\", "-").Replace("/", "-");

            return (quality, qualityPath);
        }
    }
}