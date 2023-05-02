namespace QobuzDownloaderX.Models
{
    public class DownloadItem
    {
        public string Type { get; set; }
        public string Id { get; set; }

        public bool IsEmpty()
        {
            return Type == null || Id == null;
        }

    }
}
