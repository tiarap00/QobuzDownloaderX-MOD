using QobuzDownloaderX.Models;
using System;
using TagLib;

namespace QobuzDownloaderX.Shared
{
    public static class AudioFileTagger
    {
        // Add Metadata to audiofiles in ID3v2 for mp3 and Vorbis Comment for FLAC
        public static void AddMetaDataTags(DownloadItemInfo fileInfo, string tagFilePath, string tagCoverArtFilePath, DownloadLogger logger)
        {
            // Set file to tag
            var tfile = TagLib.File.Create(tagFilePath);
            tfile.RemoveTags(TagTypes.Id3v1);

            // Use ID3v2.4 as default mp3 tag version
            TagLib.Id3v2.Tag.DefaultVersion = 4;
            TagLib.Id3v2.Tag.ForceDefaultVersion = true;

            switch (Globals.AudioFileType)
            {
                case ".mp3":

                    // For custom / troublesome tags.
                    TagLib.Id3v2.Tag customId3v2 = (TagLib.Id3v2.Tag)tfile.GetTag(TagTypes.Id3v2, true);

                    // Saving cover art to file(s)
                    if (Globals.TaggingOptions.WriteCoverImageTag)
                    {
                        try
                        {
                            // Define cover art to use for MP3 file(s)
                            TagLib.Id3v2.AttachmentFrame pic = new TagLib.Id3v2.AttachmentFrame
                            {
                                TextEncoding = TagLib.StringType.Latin1,
                                MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg,
                                Type = TagLib.PictureType.FrontCover,
                                Data = TagLib.ByteVector.FromPath(tagCoverArtFilePath)
                            };

                            // Save cover art to MP3 file.
                            tfile.Tag.Pictures = new TagLib.IPicture[1] { pic };
                            tfile.Save();
                        }
                        catch
                        {
                            logger.AddDownloadLogErrorLine($"Cover art tag failed, .jpg still exists?...{Environment.NewLine}", true, true);
                        }
                    }

                    // Track Title tag, version is already added to name if available
                    if (Globals.TaggingOptions.WriteTrackTitleTag) { tfile.Tag.Title = fileInfo.TrackName; }

                    // Album Title tag, version is already added to name if available
                    if (Globals.TaggingOptions.WriteAlbumNameTag) { tfile.Tag.Album = fileInfo.AlbumName; }

                    // Album Artits tag
                    if (Globals.TaggingOptions.WriteAlbumArtistTag) { tfile.Tag.AlbumArtists = new string[] { fileInfo.AlbumArtist }; }

                    // Track Artist tag
                    if (Globals.TaggingOptions.WriteTrackArtistTag) { tfile.Tag.Performers = new string[] { fileInfo.PerformerName }; }

                    // Composer tag
                    if (Globals.TaggingOptions.WriteComposerTag) { tfile.Tag.Composers = new string[] { fileInfo.ComposerName }; }

                    // Release Date tag
                    if (Globals.TaggingOptions.WriteReleaseYearTag) 
                    {
                        fileInfo.ReleaseDate = fileInfo.ReleaseDate.Substring(0, 4);
                        tfile.Tag.Year = UInt32.Parse(fileInfo.ReleaseDate);
                    }

                    // Genre tag
                    if (Globals.TaggingOptions.WriteGenreTag) { tfile.Tag.Genres = new string[] { fileInfo.Genre }; }

                    // Disc Number tag
                    if (Globals.TaggingOptions.WriteDiskNumberTag) { tfile.Tag.Disc = Convert.ToUInt32(fileInfo.DiscNumber); }

                    // Total Discs tag
                    if (Globals.TaggingOptions.WriteDiskTotalTag) { tfile.Tag.DiscCount = Convert.ToUInt32(fileInfo.DiscTotal); }

                    // Total Tracks tag
                    if (Globals.TaggingOptions.WriteTrackTotalTag) { tfile.Tag.TrackCount = Convert.ToUInt32(fileInfo.TrackTotal); }

                    // Track Number tag
                    // !! Set Track Number after Total Tracks to prevent taglib-sharp from re-formatting the field to a "two-digit zero-filled value" !!
                    if (Globals.TaggingOptions.WriteTrackNumberTag)
                    {
                        // Set TRCK tag manually to prevent using "two-digit zero-filled value"
                        // See https://github.com/mono/taglib-sharp/pull/240 where this change was introduced in taglib-sharp v2.3
                        // Original command: tfile.Tag.Track = Convert.ToUInt32(TrackNumber);
                        customId3v2.SetNumberFrame("TRCK", Convert.ToUInt32(fileInfo.TrackNumber), tfile.Tag.TrackCount);
                    }

                    // Comment tag
                    if (Globals.TaggingOptions.WriteCommentTag) { tfile.Tag.Comment = Globals.TaggingOptions.CommentTag; }

                    // Copyright tag
                    if (Globals.TaggingOptions.WriteCopyrightTag) { tfile.Tag.Copyright = fileInfo.Copyright; }

                    // ISRC tag
                    if (Globals.TaggingOptions.WriteIsrcTag) { customId3v2.SetTextFrame("TSRC", fileInfo.Isrc); }

                    // Release Type tag
                    if (fileInfo.MediaType != null && Globals.TaggingOptions.WriteMediaTypeTag) { customId3v2.SetTextFrame("TMED", fileInfo.MediaType); }

                    // Save all selected tags to file
                    tfile.Save();

                    break;

                case ".flac":

                    // For custom / troublesome tags.
                    TagLib.Ogg.XiphComment custom = (TagLib.Ogg.XiphComment)tfile.GetTag(TagLib.TagTypes.Xiph);

                    // Saving cover art to file(s)
                    if (Globals.TaggingOptions.WriteCoverImageTag)
                    {
                        try
                        {
                            // Define cover art to use for FLAC file(s)
                            TagLib.Id3v2.AttachmentFrame pic = new TagLib.Id3v2.AttachmentFrame
                            {
                                TextEncoding = TagLib.StringType.Latin1,
                                MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg,
                                Type = TagLib.PictureType.FrontCover,
                                Data = TagLib.ByteVector.FromPath(tagCoverArtFilePath)
                            };

                            // Save cover art to FLAC file.
                            tfile.Tag.Pictures = new TagLib.IPicture[1] { pic };
                            tfile.Save();
                        }
                        catch
                        {
                            logger.AddDownloadLogErrorLine($"Cover art tag failed, .jpg still exists?...{Environment.NewLine}", true, true);
                        }
                    }

                    // Track Title tag, version is already added to name if available
                    if (Globals.TaggingOptions.WriteTrackTitleTag) { tfile.Tag.Title = fileInfo.TrackName; }

                    // Album Title tag, version is already added to name if available
                    if (Globals.TaggingOptions.WriteAlbumNameTag) { tfile.Tag.Album = fileInfo.AlbumName; }

                    // Album Artist tag
                    if (Globals.TaggingOptions.WriteAlbumArtistTag) { custom.SetField("ALBUMARTIST", fileInfo.AlbumArtist); }

                    // Track Artist tag
                    if (Globals.TaggingOptions.WriteTrackArtistTag) { custom.SetField("ARTIST", fileInfo.PerformerName); }

                    // Composer tag
                    if (Globals.TaggingOptions.WriteComposerTag) { custom.SetField("COMPOSER", fileInfo.ComposerName); }

                    // Release Date tag
                    if (Globals.TaggingOptions.WriteReleaseYearTag) { custom.SetField("YEAR", fileInfo.ReleaseDate); }

                    // Genre tag
                    if (Globals.TaggingOptions.WriteGenreTag) { tfile.Tag.Genres = new string[] { fileInfo.Genre }; }

                    // Track Number tag
                    if (Globals.TaggingOptions.WriteTrackNumberTag)
                    {
                        tfile.Tag.Track = Convert.ToUInt32(fileInfo.TrackNumber);
                        // Override TRACKNUMBER tag again to prevent using "two-digit zero-filled value"
                        // See https://github.com/mono/taglib-sharp/pull/240 where this change was introduced in taglib-sharp v2.3
                        custom.SetField("TRACKNUMBER", Convert.ToUInt32(fileInfo.TrackNumber));
                    }

                    // Disc Number tag
                    if (Globals.TaggingOptions.WriteDiskNumberTag) { tfile.Tag.Disc = Convert.ToUInt32(fileInfo.DiscNumber); }

                    // Total Discs tag
                    if (Globals.TaggingOptions.WriteDiskTotalTag) { tfile.Tag.DiscCount = Convert.ToUInt32(fileInfo.DiscTotal); }

                    // Total Tracks tag
                    if (Globals.TaggingOptions.WriteTrackTotalTag) { tfile.Tag.TrackCount = Convert.ToUInt32(fileInfo.TrackTotal); }

                    // Comment tag
                    if (Globals.TaggingOptions.WriteCommentTag) { custom.SetField("COMMENT", Globals.TaggingOptions.CommentTag); }

                    // Copyright tag
                    if (Globals.TaggingOptions.WriteCopyrightTag) { custom.SetField("COPYRIGHT", fileInfo.Copyright); }
                    // UPC tag
                    if (Globals.TaggingOptions.WriteUpcTag) { custom.SetField("UPC", fileInfo.Upc); }

                    // ISRC tag
                    if (Globals.TaggingOptions.WriteIsrcTag) { custom.SetField("ISRC", fileInfo.Isrc); }

                    // Release Type tag
                    if (fileInfo.MediaType != null && Globals.TaggingOptions.WriteMediaTypeTag)
                    {
                        custom.SetField("MEDIATYPE", fileInfo.MediaType);
                    }

                    // Explicit tag
                    if (Globals.TaggingOptions.WriteExplicitTag)
                    {
                        if (fileInfo.Advisory == true) { custom.SetField("ITUNESADVISORY", "1"); } else { custom.SetField("ITUNESADVISORY", "0"); }
                    }

                    // Save all selected tags to file
                    tfile.Save();

                    break;
            }
        }
    }
}
