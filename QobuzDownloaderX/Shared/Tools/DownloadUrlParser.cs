using QobuzDownloaderX.Models;
using System.Linq;
using System.Text.RegularExpressions;

namespace QobuzDownloaderX.Shared

{
    public static class DownloadUrlParser
    { 
        // Pre-compiled supported download URL patterns
        private static readonly Regex[] DownloadUrlRegExes = {
            new Regex("https:\\/\\/(?:.*?).qobuz.com\\/(?<Type>.*?)\\/(?<id>.*?)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex("https:\\/\\/(?:.*?).qobuz.com\\/(?:.*?)\\/(?<Type>.*?)\\/(?:.*?)\\/(?<id>.*?)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)
        };

        // Supported types of links
        public static readonly string[] LinkTypes = { "album", "track", "artist", "label", "user", "playlist" };

        public static DownloadItem ParseDownloadUrl(string downloadUrl)
        {
            DownloadItem downloadItem = new DownloadItem();

            if (string.IsNullOrEmpty(downloadUrl))
            {
                return downloadItem;
            }

            foreach (Regex regEx in DownloadUrlRegExes)
            {
                Match matches = regEx.Match(downloadUrl);

                if (matches.Success)
                {
                    if (!LinkTypes.Contains(matches.Result("${Type}"))) continue;

                    // Valid Type found, set DownloadItem values
                    downloadItem.Type = matches.Result("${Type}");
                    downloadItem.Id = matches.Result("${id}");

                    break;
                }
            }

            return downloadItem;
        }
    }
}