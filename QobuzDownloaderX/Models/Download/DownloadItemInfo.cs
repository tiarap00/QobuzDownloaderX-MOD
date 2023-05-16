using QobuzApiSharp.Models.Content;
using QobuzDownloaderX.Shared;

namespace QobuzDownloaderX.Models
{
    public class DownloadItemInfo
    {
        public string DisplayQuality { get; set; }

        // Important strings
        public string DowloadItemID { get; set; }
        public string Stream { get; set; }

        // Info for creating paths
        public DownloadItemPaths CurrentDownloadPaths { get; set; }

        // Info / Tagging strings
        public string TrackVersionName { get; set; }
        public bool? Advisory { get; set; }
        public string AlbumArtist { get; set; }
        public string AlbumName { get; set; }
        public string PerformerName { get; set; }
        public string ComposerName { get; set; }
        public string TrackName { get; set; }
        public string Copyright { get; set; }
        public string Genre { get; set; }
        public string ReleaseDate { get; set; }
        public string Isrc { get; set; }
        public string Upc { get; set; }
        public string FrontCoverImgUrl { get; set; }
        public string FrontCoverImgTagUrl { get; set; }
        public string FrontCoverImgBoxUrl { get; set; }
        public string MediaType { get; set; }

        // Info / Tagging ints
        public int DiscNumber { get; set; }
        public int DiscTotal { get; set; }
        public int TrackNumber { get; set; }
        public int TrackTotal { get; set; }

        public DownloadItemInfo()
        {
            CurrentDownloadPaths = new DownloadItemPaths();
        }

        public void SetAlbumDownloadInfo(Album qobuzAlbum)
        {
            SetAlbumCoverArtUrls(qobuzAlbum);
            SetAlbumTaggingInfo(qobuzAlbum);
            SetAlbumPaths(qobuzAlbum);
        }

        private void ClearAlbumTaggingInfo()
        {
            // Clear tag strings
            AlbumArtist = null;
            AlbumName = null;
            Genre = null;
            ReleaseDate = null;
            Upc = null;
            MediaType = null;

            // Clear tag ints
            TrackTotal = 0;
            DiscTotal = 0;

            // Clear tagbased Paths
            CurrentDownloadPaths.AlbumArtistPath = null;
            CurrentDownloadPaths.AlbumNamePath = null;
        }

        private void ClearTrackTaggingInfo()
        {
            // Clear tag strings
            PerformerName = null;
            ComposerName = null;
            TrackName = null;
            TrackVersionName = null;
            Advisory = null;
            Copyright = null;
            Isrc = null;

            // Clear tag ints
            TrackNumber = 0;
            DiscNumber = 0;

            // Clear tagbased Paths
            CurrentDownloadPaths.TrackNamePath = null;
        }

        private void SetAlbumCoverArtUrls(Album qobuzAlbum)
        {
            // Grab cover art link
            FrontCoverImgUrl = qobuzAlbum.Image.Large;
            // Get 150x150 artwork for cover art box
            FrontCoverImgBoxUrl = FrontCoverImgUrl.Replace("_600.jpg", "_150.jpg");
            // Get selected artwork size for tagging
            FrontCoverImgTagUrl = FrontCoverImgUrl.Replace("_600.jpg", $"_{Globals.TaggingOptions.ArtSize}.jpg");
            // Get max sized artwork ("_org.jpg" is compressed version of the original "_org.jpg")
            FrontCoverImgUrl = FrontCoverImgUrl.Replace("_600.jpg", "_org.jpg");
        }

        // Set Album tagging info
        public void SetAlbumTaggingInfo(Album qobuzAlbum)
        {
            ClearAlbumTaggingInfo();

            AlbumArtist = StringTools.DecodeEncodedNonAsciiCharacters(qobuzAlbum.Artist.Name);
            AlbumName = StringTools.DecodeEncodedNonAsciiCharacters(qobuzAlbum.Title.Trim());
            string albumVersionName = StringTools.DecodeEncodedNonAsciiCharacters(qobuzAlbum.Version?.Trim());
            // Add album version to AlbumName if present
            AlbumName += (albumVersionName == null ? "" : " (" + albumVersionName + ")");

            Genre = StringTools.DecodeEncodedNonAsciiCharacters(qobuzAlbum.Genre.Name);
            ReleaseDate = StringTools.FormatDateTimeOffset(qobuzAlbum.ReleaseDateStream);
            Upc = qobuzAlbum.Upc;
            MediaType = qobuzAlbum.ReleaseType;

            // Grab tag ints
            TrackTotal = qobuzAlbum.TracksCount.GetValueOrDefault();
            DiscTotal = qobuzAlbum.MediaCount.GetValueOrDefault();
        }

        // Set Album tagbased Paths
        private void SetAlbumPaths(Album qobuzAlbum)
        {
            // Grab sample rate and bit depth for album.
            (string displayQuality, string qualityPathLocal) = QualityStringMappings.GetQualityStrings(Globals.FormatIdString, qobuzAlbum);
            DisplayQuality = displayQuality;
            CurrentDownloadPaths.QualityPath = qualityPathLocal;

            // If AlbumArtist or AlbumName goes over set MaxLength number of characters, limit them to the MaxLength
            CurrentDownloadPaths.AlbumArtistPath = StringTools.TrimToMaxLength(StringTools.GetSafeFilename(AlbumArtist), Globals.MaxLength);
            CurrentDownloadPaths.AlbumNamePath = StringTools.TrimToMaxLength(StringTools.GetSafeFilename(AlbumName), Globals.MaxLength);
        }

        // Set Track tagging info
        public void SetTrackTaggingInfo(Track qobuzTrack)
        {
            ClearTrackTaggingInfo();

            PerformerName = StringTools.DecodeEncodedNonAsciiCharacters(qobuzTrack.Performer?.Name);
            // If no performer name, use album artist
            if (string.IsNullOrEmpty(PerformerName))
            {
                PerformerName = StringTools.DecodeEncodedNonAsciiCharacters(qobuzTrack.Album?.Artist?.Name);
            }
            ComposerName = StringTools.DecodeEncodedNonAsciiCharacters(qobuzTrack.Composer?.Name);
            TrackName = StringTools.DecodeEncodedNonAsciiCharacters(qobuzTrack.Title.Trim());
            TrackVersionName = StringTools.DecodeEncodedNonAsciiCharacters(qobuzTrack.Version?.Trim());
            // Add track version to TrackName
            TrackName += (TrackVersionName == null ? "" : " (" + TrackVersionName + ")");

            Advisory = qobuzTrack.ParentalWarning;
            Copyright = StringTools.DecodeEncodedNonAsciiCharacters(qobuzTrack.Copyright);
            Isrc = qobuzTrack.Isrc;

            // Grab tag ints
            TrackNumber = qobuzTrack.TrackNumber.GetValueOrDefault();
            DiscNumber = qobuzTrack.MediaNumber.GetValueOrDefault();

            // Paths
            SetTrackPaths();
        }

        // Set Track tagbased Paths
        private void SetTrackPaths()
        {
            CurrentDownloadPaths.PerformerNamePath = StringTools.TrimToMaxLength(StringTools.GetSafeFilename(PerformerName), Globals.MaxLength);
            CurrentDownloadPaths.TrackNamePath = StringTools.GetSafeFilename(TrackName);
        }
    }
}
