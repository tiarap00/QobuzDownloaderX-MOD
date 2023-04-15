using QobuzApiSharp.Exceptions;
using QobuzApiSharp.Models.Content;
using QobuzApiSharp.Service;
using QobuzDownloaderX.Models.Content;
using QobuzDownloaderX.Properties;
using QobuzDownloaderX.Shared;
using QobuzDownloaderX.View;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using TagLib;

namespace QobuzDownloaderX
{
    public partial class QobuzDownloaderX : HeadlessForm
    {
        private readonly string downloadErrorLog = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Download_Errors.log");

        public QobuzDownloaderX()
        {
            InitializeComponent();

            // Remove previous download error log
            if (System.IO.File.Exists(downloadErrorLog))
            {
                System.IO.File.Delete(downloadErrorLog);
            }
        }

        public string artSize { get; set; }
        public string fileNameTemplateString { get; set; }
        public string finalTrackNamePath { get; set; }
        public string finalTrackNameVersionPath { get; set; }
        public string qualityPath { get; set; }
        public int MaxLength { get; set; }
        public int devClickEggThingValue { get; set; }
        public int debugMode { get; set; }

        // Important strings
        public string DowloadItemID { get; set; }

        public string stream { get; set; }

        // Info strings for creating paths
        public string albumArtistPath { get; set; }

        public string performerNamePath { get; set; }
        public string albumNamePath { get; set; }
        public string trackNamePath { get; set; }
        public string versionNamePath { get; set; }
        public string path1Full { get; set; }
        public string path2Full { get; set; }
        public string path3Full { get; set; }
        public string path4Full { get; set; }
        public string path5Full { get; set; }
        public string path6Full { get; set; }

        // Info / Tagging strings
        public string trackIdString { get; set; }

        public string trackVersionName { get; set; }
        public bool? advisory { get; set; }
        public string albumArtist { get; set; }
        public string albumName { get; set; }
        public string performerName { get; set; }
        public string composerName { get; set; }
        public string trackName { get; set; }
        public string copyright { get; set; }
        public string genre { get; set; }
        public string releaseDate { get; set; }
        public string isrc { get; set; }
        public string upc { get; set; }
        public string playlistCoverImg { get; set; }
        public string frontCoverImg { get; set; }
        public string frontCoverImgBox { get; set; }
        public string type { get; set; }

        // Info / Tagging ints
        public int discNumber { get; set; }

        public int discTotal { get; set; }
        public int trackNumber { get; set; }
        public int trackTotal { get; set; }

        // Supported types of links
        private static readonly string[] LinkTypes = { "album", "track", "artist", "label", "user", "playlist" };

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Set main form size on launch and bring to center.
            this.Height = 533;
            this.CenterToScreen();

            // Grab profile image
            string profilePic = Convert.ToString(Globals.Login.User.Avatar);
            profilePictureBox.ImageLocation = profilePic.Replace(@"\", null).Replace("s=50", "s=20");

            // Welcome the user after successful login.
            output.Invoke(new Action(() => output.Text = String.Empty));
            output.Invoke(new Action(() => output.AppendText("Welcome " + Globals.Login.User.DisplayName + " (" + Globals.Login.User.Email + ") !\r\n")));
            output.Invoke(new Action(() => output.AppendText("User Zone - " + Globals.Login.User.Zone + "\r\n\r\n")));
            output.Invoke(new Action(() => output.AppendText("Qobuz Credential Description - " + Globals.Login.User.Credential.Description + "\r\n")));
            output.Invoke(new Action(() => output.AppendText("\r\n")));
            output.Invoke(new Action(() => output.AppendText("Qobuz Subscription Details\r\n")));
            output.Invoke(new Action(() => output.AppendText("==========================\r\n")));

            if (Globals.Login.User.Subscription != null)
            {
                output.Invoke(new Action(() => output.AppendText("Offer Type - " + Globals.Login.User.Subscription.Offer + "\r\n")));
                output.Invoke(new Action(() => output.AppendText("Start Date - ")));
                output.Invoke(new Action(() => output.AppendText(Globals.Login.User.Subscription.StartDate != null ? ((DateTimeOffset)Globals.Login.User.Subscription.StartDate).ToString("dd-MM-yyyy") : "?")));
                output.Invoke(new Action(() => output.AppendText("\r\n")));
                output.Invoke(new Action(() => output.AppendText("End Date - ")));
                output.Invoke(new Action(() => output.AppendText(Globals.Login.User.Subscription.StartDate != null ? ((DateTimeOffset)Globals.Login.User.Subscription.EndDate).ToString("dd-MM-yyyy") : "?")));
                output.Invoke(new Action(() => output.AppendText("\r\n")));
                output.Invoke(new Action(() => output.AppendText("Periodicity - " + Globals.Login.User.Subscription.Periodicity + "\r\n")));
                output.Invoke(new Action(() => output.AppendText("==========================\r\n\r\n")));
            }
            else
            {
                output.Invoke(new Action(() => output.AppendText("No active subscriptions, only sample downloads possible!\r\n")));
                output.Invoke(new Action(() => output.AppendText("==========================\r\n\r\n")));
            }

            output.Invoke(new Action(() => output.AppendText("Your user_auth_token has been set for this session!")));

            // Get and display version number.
            verNumLabel.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            // Set a placeholder image for Cover Art box.
            albumArtPicBox.ImageLocation = Globals.DEFAULT_COVER_ART_URL;

            // Change account info for logout button
            string oldText = logoutLabel.Text;
            logoutLabel.Text = oldText.Replace("%name%", Globals.Login.User.DisplayName);

            // Set saved settings to correct places.
            folderBrowserDialog.SelectedPath = Settings.Default.savedFolder;
            albumCheckbox.Checked = Settings.Default.albumTag;
            albumArtistCheckbox.Checked = Settings.Default.albumArtistTag;
            artistCheckbox.Checked = Settings.Default.artistTag;
            commentCheckbox.Checked = Settings.Default.commentTag;
            commentTextbox.Text = Settings.Default.commentText;
            composerCheckbox.Checked = Settings.Default.composerTag;
            copyrightCheckbox.Checked = Settings.Default.copyrightTag;
            discNumberCheckbox.Checked = Settings.Default.discTag;
            discTotalCheckbox.Checked = Settings.Default.totalDiscsTag;
            genreCheckbox.Checked = Settings.Default.genreTag;
            isrcCheckbox.Checked = Settings.Default.isrcTag;
            typeCheckbox.Checked = Settings.Default.typeTag;
            explicitCheckbox.Checked = Settings.Default.explicitTag;
            trackTitleCheckbox.Checked = Settings.Default.trackTitleTag;
            trackNumberCheckbox.Checked = Settings.Default.trackTag;
            trackTotalCheckbox.Checked = Settings.Default.totalTracksTag;
            upcCheckbox.Checked = Settings.Default.upcTag;
            releaseCheckbox.Checked = Settings.Default.yearTag;
            imageCheckbox.Checked = Settings.Default.imageTag;
            mp3Checkbox.Checked = Settings.Default.quality1;
            flacLowCheckbox.Checked = Settings.Default.quality2;
            flacMidCheckbox.Checked = Settings.Default.quality3;
            flacHighCheckbox.Checked = Settings.Default.quality4;
            Globals.FormatIdString = Settings.Default.qualityFormat;
            Globals.AudioFileType = Settings.Default.audioType;
            artSizeSelect.SelectedIndex = Settings.Default.savedArtSize;
            filenameTempSelect.SelectedIndex = Settings.Default.savedFilenameTemplate;
            fileNameTemplateString = Settings.Default.savedFilenameTemplateString;
            MaxLength = Settings.Default.savedMaxLength;

            customFormatIDTextbox.Text = Globals.FormatIdString;

            artSize = artSizeSelect.Text;
            maxLengthTextbox.Text = MaxLength.ToString();

            // Check if there's no selected path saved.
            if (folderBrowserDialog.SelectedPath == null || folderBrowserDialog.SelectedPath == "")
            {
                // If there is NOT a saved path.
                output.Invoke(new Action(() => output.AppendText("\r\n\r\n")));
                output.Invoke(new Action(() => output.AppendText("No default path has been set! Remember to Choose a Folder!\r\n")));
            }
            else
            {
                // If there is a saved path.
                output.Invoke(new Action(() => output.AppendText("\r\n\r\n")));
                output.Invoke(new Action(() => output.AppendText("Using the last folder you've selected as your selected path!\r\n")));
                output.Invoke(new Action(() => output.AppendText("\r\n")));
                output.Invoke(new Action(() => output.AppendText("Default Folder:\r\n")));
                output.Invoke(new Action(() => output.AppendText(folderBrowserDialog.SelectedPath + "\r\n")));
            }

            // Run anything put into the debug events (For Testing)
            debuggingEvents(sender, e);
        }

        private void debuggingEvents(object sender, EventArgs e)
        {
            devClickEggThingValue = 0;

            // Debug mode for things that are only for testing, or shouldn't be on public releases. At the moment, does nothing.
            if (!Debugger.IsAttached)
            {
                debugMode = 0;
            }
            else
            {
                debugMode = 1;
            }

            // Show app_secret value.
            //output.Invoke(new Action(() => output.AppendText("\r\n\r\napp_secret = " + Globals.AppSecret)));

            // Show format_id value.
            //output.Invoke(new Action(() => output.AppendText("\r\n\r\nformat_id = " + Globals.FormatIdString)));
        }

        private void OpenSearch_Click(object sender, EventArgs e)
        {
            Globals.SelectedDownloadUrl = null;
            Globals.SearchForm.ShowDialog(this);
            this.downloadUrl.Text = Globals.SelectedDownloadUrl;
        }

        private void downloadButton_Click(object sender, EventArgs e)
        {
            getLinkTypeBG.RunWorkerAsync();
        }

        private void downloadUrl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                getLinkTypeBG.RunWorkerAsync();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private T ExecuteApiCall<T>(Func<QobuzApiService, T> apiCall)
        {
            try
            {
                return apiCall(QobuzApiServiceManager.GetApiService());
            }
            catch (Exception ex)
            {
                // If connection to API fails, or something is incorrect, show error info + log details.
                List<string> errorLines = new List<string>();

                output.Invoke(new Action(() => output.AppendText("\r\n")));
                output.Invoke(new Action(() => output.AppendText($"{ex.Message} Details saved to error log\r\n")));

                switch (ex)
                {
                    case ApiErrorResponseException erEx:
                        errorLines.Add($"Failed API request: {erEx.RequestContent}");
                        errorLines.Add($"Api response code: {erEx.ResponseStatusCode}");
                        errorLines.Add($"Api response status: {erEx.ResponseStatus}");
                        errorLines.Add($"Api response reason: {erEx.ResponseReason}");
                        break;
                    case ApiResponseParseErrorException pEx:
                        errorLines.Add($"Api response content: {pEx.ResponseContent}");
                        break;
                    default:
                        errorLines.Add($"{ex}");
                        break;
                }

                // Write detailed info to log
                System.IO.File.AppendAllLines(downloadErrorLog, errorLines);
            }

            return default;
        }

        private void DisableBoxes()
        {
            mp3Checkbox.Invoke(new Action(() => mp3Checkbox.Visible = false));
            flacLowCheckbox.Invoke(new Action(() => flacLowCheckbox.Visible = false));
            flacMidCheckbox.Invoke(new Action(() => flacMidCheckbox.Visible = false));
            flacHighCheckbox.Invoke(new Action(() => flacHighCheckbox.Visible = false));
            downloadButton.Invoke(new Action(() => downloadButton.Enabled = false));
        }

        private void EnableBoxes()
        {
            mp3Checkbox.Invoke(new Action(() => mp3Checkbox.Visible = true));
            flacLowCheckbox.Invoke(new Action(() => flacLowCheckbox.Visible = true));
            flacMidCheckbox.Invoke(new Action(() => flacMidCheckbox.Visible = true));
            flacHighCheckbox.Invoke(new Action(() => flacHighCheckbox.Visible = true));
            downloadButton.Invoke(new Action(() => downloadButton.Enabled = true));
        }

        private void SelectFolder_Click(object sender, EventArgs e)
        {
            Thread t = new Thread((ThreadStart)(() =>
            {
                // Open Folder Browser to select path & Save the selection
                folderBrowserDialog.ShowDialog();
                Settings.Default.savedFolder = folderBrowserDialog.SelectedPath;
                Settings.Default.Save();
            }));

            // Run your code from a thread that joins the STA Thread
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
        }

        private void OpenFolderButton_Click(object sender, EventArgs e)
        {
            // Open selcted folder
            if (folderBrowserDialog.SelectedPath == null || folderBrowserDialog.SelectedPath == "")
            {
                // If there's no selected path.
                MessageBox.Show("No path selected!", "ERROR",
                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                EnableBoxes();
            }
            else
            {
                // If selected path doesn't exist, create it. (Will be ignored if it does)
                System.IO.Directory.CreateDirectory(folderBrowserDialog.SelectedPath);
                // Open selcted folder
                Process.Start(@folderBrowserDialog.SelectedPath);
            }
        }

        private void GetLinkTypeBG_DoWork(object sender, DoWorkEventArgs e)
        {
            DisableBoxes();

            // Check if there's no selected path.
            if (folderBrowserDialog.SelectedPath == null || folderBrowserDialog.SelectedPath == "")
            {
                // If there is NOT a saved path.
                output.Invoke(new Action(() => output.Text = String.Empty));
                output.Invoke(new Action(() => output.AppendText("No path has been set! Remember to Choose a Folder!\r\n")));
                EnableBoxes();
                return;
            }

            string downloadItemLink = downloadUrl.Text;

            var downloadItemIdGrab = Regex.Match(downloadItemLink, "https:\\/\\/(?:.*?).qobuz.com\\/(?<type>.*?)\\/(?<id>.*?)$").Groups;
            var linkType = downloadItemIdGrab[1].Value;
            var downloadItemId = downloadItemIdGrab[2].Value;

            // Fallback to check if full store link is used
            if (!LinkTypes.Contains(linkType))
            {
                downloadItemIdGrab = Regex.Match(downloadItemLink, "https:\\/\\/(?:.*?).qobuz.com\\/(?:.*?)\\/(?<type>.*?)\\/(?:.*?)\\/(?<id>.*?)$").Groups;
                linkType = downloadItemIdGrab[1].Value;
                downloadItemId = downloadItemIdGrab[2].Value;
            }

            DowloadItemID = downloadItemId;

            if (linkType == "track")
            {
                downloadTrackBG.RunWorkerAsync();
            }
            else if (linkType == "album")
            {
                downloadAlbumBG.RunWorkerAsync();
            }
            else if (linkType == "artist")
            {
                downloadDiscogBG.RunWorkerAsync();
            }
            else if (linkType == "label")
            {
                downloadLabelBG.RunWorkerAsync();
            }
            else if (linkType == "user")
            {
                if (DowloadItemID == @"library/favorites/albums")
                {
                    downloadFaveAlbumsBG.RunWorkerAsync();
                }
                //else if (DowloadItemID == @"library/favorites/artists")
                //{
                //    downloadFaveArtistsBG.RunWorkerAsync();
                //}
                else
                {
                    output.Invoke(new Action(() => output.Text = String.Empty));
                    output.Invoke(new Action(() => output.AppendText("Downloading favorites only works on favorite albums at the moment. More options will be added in the future.\r\n\r\nIf you'd like to go ahead and grab your favorite albums, paste this link in the URL section - https://play.qobuz.com/user/library/favorites/albums")));
                    EnableBoxes();
                }
            }
            else if (linkType == "playlist")
            {
                downloadPlaylistBG.RunWorkerAsync();
            }
            else
            {
                // Say what isn't available at the moment.
                output.Invoke(new Action(() => output.Text = String.Empty));
                output.Invoke(new Action(() => output.AppendText("URL not understood. Is there a typo?")));
                EnableBoxes();
            }
        }

        // Add Metadata to audiofiles in ID3v2 for mp3 and Vorbis Comment for FLAC
        private void AddMetaDataTags(string tagFilePath, string tagCoverArtFilePath)
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
                    TagLib.Id3v2.Tag t = (TagLib.Id3v2.Tag)tfile.GetTag(TagLib.TagTypes.Id3v2);

                    // Saving cover art to file(s)
                    if (imageCheckbox.Checked)
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
                            output.Invoke(new Action(() => output.AppendText("Cover art tag fail, .jpg still exists?...")));
                        }
                    }

                    // Track Title tag, version is already added to name if available
                    if (trackTitleCheckbox.Checked) { tfile.Tag.Title = trackName; }

                    // Album Title tag, version is already added to name if available
                    if (albumCheckbox.Checked) { tfile.Tag.Album = albumName; }

                    // Album Artits tag
                    if (albumArtistCheckbox.Checked) { tfile.Tag.AlbumArtists = new string[] { albumArtist }; }

                    // Track Artist tag
                    if (artistCheckbox.Checked) { tfile.Tag.Performers = new string[] { performerName }; }

                    // Composer tag
                    if (composerCheckbox.Checked) { tfile.Tag.Composers = new string[] { composerName }; }

                    // Release Date tag
                    if (releaseCheckbox.Checked) { releaseDate = releaseDate.Substring(0, 4); tfile.Tag.Year = UInt32.Parse(releaseDate); }

                    // Genre tag
                    if (genreCheckbox.Checked) { tfile.Tag.Genres = new string[] { genre }; }

                    // Track Number tag
                    if (trackNumberCheckbox.Checked) { tfile.Tag.Track = Convert.ToUInt32(trackNumber); }

                    // Disc Number tag
                    if (discNumberCheckbox.Checked) { tfile.Tag.Disc = Convert.ToUInt32(discNumber); }

                    // Total Discs tag
                    if (discTotalCheckbox.Checked) { tfile.Tag.DiscCount = Convert.ToUInt32(discTotal); }

                    // Total Tracks tag
                    if (trackTotalCheckbox.Checked) { tfile.Tag.TrackCount = Convert.ToUInt32(trackTotal); }

                    // Comment tag
                    if (commentCheckbox.Checked) { tfile.Tag.Comment = commentTextbox.Text; }

                    // Copyright tag
                    if (copyrightCheckbox.Checked) { tfile.Tag.Copyright = copyright; }

                    // ISRC tag
                    if (isrcCheckbox.Checked) { TagLib.Id3v2.Tag tag = (TagLib.Id3v2.Tag)tfile.GetTag(TagTypes.Id3v2, true); tag.SetTextFrame("TSRC", isrc); }

                    // Release Type tag
                    if (type != null && typeCheckbox.Checked)
                    {
                        TagLib.Id3v2.Tag tag = (TagLib.Id3v2.Tag)tfile.GetTag(TagTypes.Id3v2, true); tag.SetTextFrame("TMED", type);
                    }

                    // Save all selected tags to file
                    tfile.Save();

                    break;

                case ".flac":

                    // For custom / troublesome tags.
                    var custom = (TagLib.Ogg.XiphComment)tfile.GetTag(TagLib.TagTypes.Xiph);

                    // Saving cover art to file(s)
                    if (imageCheckbox.Checked)
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
                            output.Invoke(new Action(() => output.AppendText("Cover art tag fail, .jpg still exists?...")));
                        }
                    }

                    // Track Title tag, version is already added to name if available
                    if (trackTitleCheckbox.Checked) { tfile.Tag.Title = trackName; }

                    // Album Title tag, version is already added to name if available
                    if (albumCheckbox.Checked) { tfile.Tag.Album = albumName; }

                    // Album Artist tag
                    if (albumArtistCheckbox.Checked) { custom.SetField("ALBUMARTIST", albumArtist); }

                    // Track Artist tag
                    if (artistCheckbox.Checked) { custom.SetField("ARTIST", performerName); }

                    // Composer tag
                    if (composerCheckbox.Checked) { custom.SetField("COMPOSER", composerName); }

                    // Release Date tag
                    if (releaseCheckbox.Checked) { custom.SetField("YEAR", releaseDate); }

                    // Genre tag
                    if (genreCheckbox.Checked) { tfile.Tag.Genres = new string[] { genre }; }

                    // Track Number tag
                    if (trackNumberCheckbox.Checked) { tfile.Tag.Track = Convert.ToUInt32(trackNumber); }

                    // Disc Number tag
                    if (discNumberCheckbox.Checked) { tfile.Tag.Disc = Convert.ToUInt32(discNumber); }

                    // Total Discs tag
                    if (discTotalCheckbox.Checked) { tfile.Tag.DiscCount = Convert.ToUInt32(discTotal); }

                    // Total Tracks tag
                    if (trackTotalCheckbox.Checked) { tfile.Tag.TrackCount = Convert.ToUInt32(trackTotal); }

                    // Comment tag
                    if (commentCheckbox.Checked) { custom.SetField("COMMENT", commentTextbox.Text); }

                    // Copyright tag
                    if (copyrightCheckbox.Checked) { custom.SetField("COPYRIGHT", copyright); }
                    // UPC tag
                    if (upcCheckbox.Checked) { custom.SetField("UPC", upc); }

                    // ISRC tag
                    if (isrcCheckbox.Checked) { custom.SetField("ISRC", isrc); }

                    // Release Type tag
                    if (type != null && typeCheckbox.Checked)
                    {
                        custom.SetField("MEDIATYPE", type);
                    }

                    // Explicit tag
                    if (explicitCheckbox.Checked)
                    {
                        if (advisory == true) { custom.SetField("ITUNESADVISORY", "1"); } else { custom.SetField("ITUNESADVISORY", "0"); }
                    }

                    // Save all selected tags to file
                    tfile.Save();

                    break;
            }
        }

        private void CreateTrackDirectories(string basePath, string qualityPath, string albumPathSuffix = "", bool forPlaylist = false)
        {
            if (forPlaylist)
            {
                path1Full = basePath;
                path2Full = path1Full;
                path3Full = Path.Combine(basePath, qualityPath);
                path4Full = path3Full;
            }
            else
            {
                path1Full = Path.Combine(basePath, albumArtistPath);
                path2Full = Path.Combine(basePath, albumArtistPath, albumNamePath + albumPathSuffix);
                path3Full = Path.Combine(basePath, albumArtistPath, albumNamePath + albumPathSuffix, qualityPath);

                // If more than 1 disc, create folders for discs. Otherwise, strings will remain null
                // Pad discnumber with minimum of 2 integer positions based on total number of disks
                if (discTotal > 1)
                {
                    // Create strings for disc folders
                    string discFolder = "CD " + discNumber.ToString().PadLeft(Math.Max(2, (int)Math.Floor(Math.Log10(discTotal) + 1)), '0');
                    path4Full = Path.Combine(basePath, albumArtistPath, albumNamePath + albumPathSuffix, qualityPath, discFolder);
                }
                else
                {
                    path4Full = path3Full;
                }
            }

            System.IO.Directory.CreateDirectory(path4Full);
        }

        // Set Track tagging info, tagbased Paths
        private void GetTrackTaggingInfo(Track qobuzTrack)
        {
            ClearTrackTaggingInfo();

            performerName = StringTools.DecodeEncodedNonAsciiCharacters(qobuzTrack.Performer.Name);
            composerName = StringTools.DecodeEncodedNonAsciiCharacters(qobuzTrack.Composer?.Name);
            trackName = StringTools.DecodeEncodedNonAsciiCharacters(qobuzTrack.Title.Trim());
            trackVersionName = StringTools.DecodeEncodedNonAsciiCharacters(qobuzTrack.Version?.Trim());
            // Add track version to trackName
            trackName += (trackVersionName == null ? "" : " (" + trackVersionName + ")");

            advisory = qobuzTrack.ParentalWarning;
            copyright = StringTools.DecodeEncodedNonAsciiCharacters(qobuzTrack.Copyright);
            isrc = qobuzTrack.Isrc;

            // Grab tag ints
            trackNumber = qobuzTrack.TrackNumber.GetValueOrDefault();
            discNumber = qobuzTrack.MediaNumber.GetValueOrDefault();

            // Paths
            performerNamePath = StringTools.GetSafeFilename(performerName);
            trackNamePath = StringTools.GetSafeFilename(trackName);
        }

        // Set Album tagging info, tagbased Paths
        private void GetAlbumTaggingInfo(Album qobuzAlbum)
        {
            ClearAlbumTaggingInfo();

            albumArtist = StringTools.DecodeEncodedNonAsciiCharacters(qobuzAlbum.Artist.Name);
            albumName = StringTools.DecodeEncodedNonAsciiCharacters(qobuzAlbum.Title.Trim());
            string albumVersionName = StringTools.DecodeEncodedNonAsciiCharacters(qobuzAlbum.Version?.Trim());
            // Add album version to albumName if present
            albumName += (albumVersionName == null ? "" : " (" + albumVersionName + ")");

            genre = StringTools.DecodeEncodedNonAsciiCharacters(qobuzAlbum.Genre.Name);
            releaseDate = StringTools.FormatDateTimeOffset(qobuzAlbum.ReleaseDateStream);
            upc = qobuzAlbum.Upc;
            type = qobuzAlbum.ReleaseType;

            // Grab tag ints
            trackTotal = qobuzAlbum.TracksCount.GetValueOrDefault();
            discTotal = qobuzAlbum.MediaCount.GetValueOrDefault();

            // Paths
            albumArtistPath = StringTools.GetSafeFilename(albumArtist);
            albumNamePath = StringTools.GetSafeFilename(albumName);
        }

        private void UpdateAlbumTagsUI()
        {
            // Update UI
            albumArtistTextBox.Invoke(new Action(() => albumArtistTextBox.Text = albumArtist));
            albumTextBox.Invoke(new Action(() => albumTextBox.Text = albumName));
            releaseDateTextBox.Invoke(new Action(() => releaseDateTextBox.Text = releaseDate));
            upcTextBox.Invoke(new Action(() => upcTextBox.Text = upc));
            totalTracksTextbox.Invoke(new Action(() => totalTracksTextbox.Text = trackTotal.ToString()));
        }

        private void ClearTrackTaggingInfo()
        {
            // Clear tag strings
            performerName = null;
            composerName = null;
            trackName = null;
            trackVersionName = null;
            advisory = null;
            copyright = null;
            isrc = null;

            // Clear tag ints
            trackNumber = 0;
            discNumber = 0;

            // Clear tagbased Paths
            trackNamePath = null;
        }

        private void ClearAlbumTaggingInfo()
        {
            // Clear tag strings
            albumArtist = null;
            albumName = null;
            genre = null;
            releaseDate = null;
            upc = null;
            type = null;

            // Clear tag ints
            trackTotal = 0;
            discTotal = 0;

            // Clear tagbased Paths
            albumArtistPath = null;
            albumNamePath = null;
        }

        private void GetAlbumCoverArtUrls(Album qobuzAlbum)
        {
            // Grab cover art link
            frontCoverImg = qobuzAlbum.Image.Large;
            // Get 150x150 artwork for cover art box
            frontCoverImgBox = frontCoverImg.Replace("_600.jpg", "_150.jpg");
            // Get max sized artwork ("_org.jpg" is compressed version of the original "_org.jpg")
            frontCoverImg = frontCoverImg.Replace("_600.jpg", "_org.jpg");

            albumArtPicBox.Invoke(new Action(() => albumArtPicBox.ImageLocation = frontCoverImgBox));
        }

        private bool IsStreamable(Track qobuzTrack, bool inPlaylist = false)
        {
            bool tryToStream = true;

            if (qobuzTrack.Streamable == false)
            {
                switch (streamableCheckbox.Checked)
                {
                    case true:
                        string trackReference = inPlaylist ? $"{qobuzTrack.Performer?.Name} - {qobuzTrack.Title}" : qobuzTrack.TrackNumber.GetValueOrDefault().ToString();
                        trackName = StringTools.DecodeEncodedNonAsciiCharacters(qobuzTrack.Title.Trim());
                        output.Invoke(new Action(() => output.AppendText($"Track {trackReference} is not available for streaming. Unable to download.\r\n")));
                        tryToStream = false;
                        break;

                    default:
                        output.Invoke(new Action(() => output.AppendText("Track is not available for streaming. But streamable check is being ignored for debugging, or messed up releases. Attempting to download...\r\n")));
                        break;
                }
            }

            return tryToStream;
        }

        public bool DownloadTrack(Track qobuzTrack, string basePath, bool isPartOfPlaylist, bool isPartOfAlbum, bool removeTagArtFileAfterDownload = false, string albumPathSuffix = "")
        {
            trackIdString = qobuzTrack.Id.GetValueOrDefault().ToString();

            GetTrackTaggingInfo(qobuzTrack);

            // If track is downloaded as part of Album, Album related processings should already be done.
            // Only handle Album related processing when downloading a single track.
            if (!isPartOfAlbum) PrepareAlbumDownload(qobuzTrack.Album);

            // Check if available for streaming.
            if (!IsStreamable(qobuzTrack)) return false;

            // If albumArtist, performerName or albumName goes over set MaxLength number of characters, limit them to the MaxLength
            albumArtistPath = albumArtistPath.Substring(0, Math.Min(albumArtistPath.Length, MaxLength)).TrimEnd();
            performerNamePath = performerNamePath.Substring(0, Math.Min(performerNamePath.Length, MaxLength)).TrimEnd();
            albumNamePath = albumNamePath.Substring(0, Math.Min(albumNamePath.Length, MaxLength)).TrimEnd();

            // Create directories if they don't exist yet
            // Add Album ID to Album Path if requested (to avoid conflicts for similar albums with trimmed long names)
            CreateTrackDirectories(basePath, qualityPath, albumPathSuffix, isPartOfPlaylist);

            // Set trackPath to the created directories
            string trackPath = path4Full;

            // Create padded track number string with minimum of 2 integer positions based on number of total tracks
            string paddedTrackNumber = trackNumber.ToString().PadLeft(Math.Max(2, (int)Math.Floor(Math.Log10(trackTotal) + 1)), '0');

            // Create full track filename
            if (isPartOfPlaylist)
            {
                finalTrackNamePath = string.Concat(performerNamePath, fileNameTemplateString, trackNamePath).TrimEnd();
            }
            else
            {
                finalTrackNamePath = string.Concat(paddedTrackNumber, fileNameTemplateString, trackNamePath).TrimEnd();
            }

            // Shorten full filename if over MaxLength to avoid errors with file names being too long
            finalTrackNamePath = finalTrackNamePath.Substring(0, Math.Min(finalTrackNamePath.Length, MaxLength)).TrimEnd();

            // Notify UI of starting track download.
            output.Invoke(new Action(() => output.AppendText("Downloading - " + finalTrackNamePath + " ...... ")));

            // Check if the file already exists
            string checkFile = Path.Combine(trackPath, finalTrackNamePath + Globals.AudioFileType);

            if (System.IO.File.Exists(checkFile))
            {
                string message = "File for \"" + finalTrackNamePath + "\" already exists. Skipping.\r\n";
                output.Invoke(new Action(() => output.AppendText(message)));
                System.Threading.Thread.Sleep(100);
                return false;
            }

            // Get track streaming URL, abort if failed.
            string streamUrl = ExecuteApiCall(apiService => apiService.GetTrackFileUrl(trackIdString, Globals.FormatIdString))?.Url;

            if (string.IsNullOrEmpty(streamUrl))
            {
                // Can happen with free accounts trying to download non-previewable tracks. 
                output.Invoke(new Action(() => output.AppendText("No streaming URL found. Skipping.\r\n")));
                return false;
            }

            try
            {
                // Create file path strings
                string filePath = Path.Combine(trackPath, finalTrackNamePath + Globals.AudioFileType);
                string coverArtFilePath = Path.Combine(path3Full, "Cover.jpg");
                string coverArtTagFilePath = Path.Combine(path3Full, artSize + ".jpg");

                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", Globals.USER_AGENT);

                    using (HttpResponseMessage streamResponse = httpClient.GetAsync(streamUrl, HttpCompletionOption.ResponseHeadersRead).Result)
                    {
                        // Save streamed file from link
                        using (Stream streamToReadFrom = streamResponse.Content.ReadAsStreamAsync().Result)
                        {
                            using (Stream streamToWriteTo = System.IO.File.Create(filePath))
                            {
                                streamToReadFrom.CopyTo(streamToWriteTo);
                            }
                        }
                    }

                    // Download selected cover art size for tagging files (if not exists)
                    if (!System.IO.File.Exists(coverArtTagFilePath))
                    {
                        try
                        {
                            using (Stream imageStream = httpClient.GetStreamAsync(frontCoverImg.Replace("_org", "_" + artSize)).Result)
                            {
                                using (FileStream fileStream = new FileStream(coverArtTagFilePath, FileMode.CreateNew))
                                {
                                    imageStream.CopyTo(fileStream);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Qobuz servers throw a 404 as if the image doesn't exist.
                            string[] errorLines = { "Error downloading image file for tagging.", ex.Message, "\r\n" };
                            System.IO.File.AppendAllLines(downloadErrorLog, errorLines);
                        }
                    }

                    // Download max quality Cover Art to "Cover.jpg" file in chosen path (if requested & not exists).
                    if (!isPartOfPlaylist && !System.IO.File.Exists(coverArtFilePath))
                    {
                        try
                        {
                            using (Stream imageStream = httpClient.GetStreamAsync(frontCoverImg).Result)
                            {
                                using (FileStream fileStream = new FileStream(coverArtFilePath, FileMode.CreateNew))
                                {
                                    imageStream.CopyTo(fileStream);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Qobuz servers throw a 404 as if the image doesn't exist.
                            string[] errorLines = { "Error downloading full size cover image file.", ex.Message, "\r\n" };
                            System.IO.File.AppendAllLines(downloadErrorLog, errorLines);
                        }
                    }
                }

                // Tag metadata to downloaded track.
                AddMetaDataTags(filePath, coverArtTagFilePath);

                // Remove temp tagging art file if requested and exists.
                if (removeTagArtFileAfterDownload && System.IO.File.Exists(coverArtTagFilePath))
                {
                    System.IO.File.Delete(coverArtTagFilePath);
                }

                output.Invoke(new Action(() => output.AppendText("Track Download Done!\r\n")));
            }
            catch (Exception downloadError)
            {
                // If there is an issue trying to, or during the download, show error info.
                string error = downloadError.ToString();
                output.Invoke(new Action(() => output.AppendText("\r\n\r\n")));
                output.Invoke(new Action(() => output.AppendText("Track Download ERROR. Information below.\r\n\r\n")));
                output.Invoke(new Action(() => output.AppendText(error)));
                return false;
            }

            return true;
        }

        public bool DownloadAlbum(Album qobuzAlbum, string basePath, bool loadTracks = false, string albumPathSuffix = "")
        {
            if (loadTracks)
            {
                // Get Album model object with tracks for when tracks aren't already loaded in Album model
                qobuzAlbum = ExecuteApiCall(apiService => apiService.GetAlbum(qobuzAlbum.Id, true));
            }

            // If API call failed or empty Album was provided, abort
            if (qobuzAlbum == null || string.IsNullOrEmpty(qobuzAlbum.Id)) { EnableBoxes(); return false; }

            PrepareAlbumDownload(qobuzAlbum);

            // Download all tracks of the Album, clean albumArt tag file after last track
            int trackCount = qobuzAlbum.TracksCount ?? 0;

            for (int i = 0; i < trackCount; i++)
            {
                Track qobuzTrack = qobuzAlbum.Tracks.Items[i];

                // Nested Album objects in Tracks are not always fully populated, inject current qobuzAlbum in Track to be downloaded
                qobuzTrack.Album = qobuzAlbum;

                DownloadTrack(qobuzTrack, basePath, false, true, i == trackCount - 1, albumPathSuffix);
            }

            // Look for digital booklet(s) in "Goodies"
            List<Goody> booklets = qobuzAlbum.Goodies?.Where(g => g.FileFormatId == (int)GoodiesFileType.BOOKLET).ToList();

            if (booklets?.Any() == true)
            {
                output.Invoke(new Action(() => output.AppendText("Goodies found, downloading...")));
                using (HttpClient client = new HttpClient())
                {
                    int counter = 1;

                    foreach (Goody booklet in booklets)
                    {
                        HttpResponseMessage response = client.GetAsync(booklet.Url).Result;

                        if (response.IsSuccessStatusCode)
                        {
                            using (Stream bookletStream = response.Content.ReadAsStreamAsync().Result)
                            {
                                string fileName = counter == 1 ? "Digital Booklet.pdf" : $"Digital Booklet {counter}.pdf";
                                string filePath = Path.Combine(path3Full, fileName);

                                using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    bookletStream.CopyTo(fileStream);
                                }

                                counter++;
                            }
                        }
                    }
                }
            }

            return true;
        }

        private void PrepareAlbumDownload(Album qobuzAlbum)
        {
            // Grab sample rate and bit depth for album track is from.
            (string quality, string qualityPathLocal) = QualityStringMappings.GetQualityStrings(Globals.FormatIdString, qobuzAlbum);
            qualityPath = qualityPathLocal;

            // Display album quality in quality textbox.
            qualityTextbox.Invoke(new Action(() => qualityTextbox.Text = quality));

            GetAlbumCoverArtUrls(qobuzAlbum);
            GetAlbumTaggingInfo(qobuzAlbum);
            UpdateAlbumTagsUI();
        }

        // For downloading "track" links
        private void DownloadTrackBG_DoWork(object sender, DoWorkEventArgs e)
        {
            // Empty output, then say Starting Downloads.
            output.Invoke(new Action(() => output.Text = String.Empty));
            output.Invoke(new Action(() => output.AppendText("Starting Downloads...\r\n\r\n")));

            // Set "basePath" as the selected path.
            string downloadBasePath = folderBrowserDialog.SelectedPath;

            Track qobuzTrack = ExecuteApiCall(apiService => apiService.GetTrack(DowloadItemID, true));

            // If API call failed, abort
            if (qobuzTrack == null) { EnableBoxes(); return; }

            bool fileDownloaded = DownloadTrack(qobuzTrack, downloadBasePath, false, false, true);

            // If download failed, abort
            if (!fileDownloaded) { EnableBoxes(); return; }

            // Say that downloading is completed.
            output.Invoke(new Action(() => output.AppendText("Track Download Done!\r\n\r\n")));
            output.Invoke(new Action(() => output.AppendText("File will be located in your selected path.")));
            EnableBoxes();
        }

        // For downloading "album" links
        private void DownloadAlbumBG_DoWork(object sender, DoWorkEventArgs e)
        {
            // Empty output, then say Starting Downloads.
            output.Invoke(new Action(() => output.Text = String.Empty));
            output.Invoke(new Action(() => output.AppendText("Starting Downloads...\r\n\r\n")));

            // Set "basePath" as the selected path.
            String downloadBasePath = folderBrowserDialog.SelectedPath;

            try
            {
                // Get Album model object
                Album qobuzAlbum = ExecuteApiCall(apiService => apiService.GetAlbum(DowloadItemID, true));

                // If API call failed, abort
                if (qobuzAlbum == null) { EnableBoxes(); return; }

                bool albumDownloaded = DownloadAlbum(qobuzAlbum, downloadBasePath);

                // If download failed, abort
                if (!albumDownloaded) { EnableBoxes(); return; }

                // Say that downloading is completed.
                output.Invoke(new Action(() => output.AppendText("\r\n\r\n")));
                output.Invoke(new Action(() => output.AppendText("Downloading job completed! All downloaded files will be located in your chosen path.")));
                EnableBoxes();
            }
            catch (Exception ex)
            {
                string error = ex.ToString();
                output.Invoke(new Action(() => output.AppendText("Failed to download (First Phase). Error information below.\r\n\r\n")));
                output.Invoke(new Action(() => output.AppendText(error)));
                EnableBoxes();
            }
        }

        // For downloading "artist" links [MOSTLY WORKING]
        private void DownloadDiscogBG_DoWork(object sender, DoWorkEventArgs e)
        {
            // Set "basePath" as the selected path.
            String downloadBasePath = folderBrowserDialog.SelectedPath;

            // Empty output, then say Grabbing IDs.
            output.Invoke(new Action(() => output.Text = String.Empty));
            output.Invoke(new Action(() => output.AppendText("Grabbing Album IDs...\r\n\r\n")));

            try
            {
                // Get Artist model object
                Artist qobuzArtist = ExecuteApiCall(apiService => apiService.GetArtist(DowloadItemID, true, "albums", "release_desc", 999999));

                // If API call failed, abort
                if (qobuzArtist == null) { EnableBoxes(); return; }

                foreach (Album qobuzAlbum in qobuzArtist.Albums.Items)
                {
                    // Empty output, then say Starting Downloads.
                    output.Invoke(new Action(() => output.Text = string.Empty));
                    output.Invoke(new Action(() => output.AppendText("Starting Downloads...\r\n\r\n")));

                    bool albumDownloaded = DownloadAlbum(qobuzAlbum, downloadBasePath, true, $" [{qobuzAlbum.Id}]");

                    // If download failed, abort
                    if (!albumDownloaded) { EnableBoxes(); return; }
                }

                // Say that downloading is completed.
                output.Invoke(new Action(() => output.AppendText("\r\n\r\n")));
                output.Invoke(new Action(() => output.AppendText("Downloading job completed! All downloaded files will be located in your chosen path.")));
                EnableBoxes();
            }
            catch (Exception downloadError)
            {
                // If there is an issue trying to, or during the download, show error info.
                string error = downloadError.ToString();
                output.Invoke(new Action(() => output.Text = String.Empty));
                output.Invoke(new Action(() => output.AppendText("Artist Download ERROR. Information below.\r\n\r\n")));
                output.Invoke(new Action(() => output.AppendText(error)));
                EnableBoxes();
                return;
            }
        }

        // For downloading "label" links [IN DEV]
        private void DownloadLabelBG_DoWork(object sender, DoWorkEventArgs e)
        {
            // Set "basePath" as the selected path + "/- Labels".
            string labelBasePath = Path.Combine(folderBrowserDialog.SelectedPath, "- Labels");

            // Empty output, then say Grabbing IDs.
            output.Invoke(new Action(() => output.Text = String.Empty));
            output.Invoke(new Action(() => output.AppendText("Grabbing Album IDs...\r\n\r\n")));

            try
            {
                // Get Label model object
                QobuzApiSharp.Models.Content.Label qobuzLabel = ExecuteApiCall(apiService => apiService.GetLabel(DowloadItemID, true, "albums", 999999));

                // If API call failed, abort
                if (qobuzLabel == null) { EnableBoxes(); return; }

                // Add Label name to basePath
                string safeLabelName = StringTools.GetSafeFilename(StringTools.DecodeEncodedNonAsciiCharacters(qobuzLabel.Name));
                labelBasePath = Path.Combine(labelBasePath, safeLabelName);

                foreach (Album qobuzAlbum in qobuzLabel.Albums.Items)
                {
                    // Empty output, then say Starting Downloads.
                    output.Invoke(new Action(() => output.Text = String.Empty));
                    output.Invoke(new Action(() => output.AppendText("Starting Downloads...\r\n\r\n")));

                    bool albumDownloaded = DownloadAlbum(qobuzAlbum, labelBasePath, true, $" [{qobuzAlbum.Id}]");

                    // If download failed, abort
                    if (!albumDownloaded) { EnableBoxes(); return; }
                }

                // Say that downloading is completed.
                output.Invoke(new Action(() => output.AppendText("\r\n\r\n")));
                output.Invoke(new Action(() => output.AppendText("Downloading job completed! All downloaded files will be located in your chosen path.")));
                EnableBoxes();
            }
            catch (Exception downloadError)
            {
                // If there is an issue trying to, or during the download, show error info.
                string error = downloadError.ToString();
                output.Invoke(new Action(() => output.Text = String.Empty));
                output.Invoke(new Action(() => output.AppendText("Artist Download ERROR. Information below.\r\n\r\n")));
                output.Invoke(new Action(() => output.AppendText(error)));
                EnableBoxes();
                return;
            }
        }

        // For downloading "favorites" (Albums only at the moment) [IN DEV]

        // Favorite Albums
        private void DownloadFaveAlbumsBG_DoWork(object sender, DoWorkEventArgs e)
        {
            // Set "basePath" as the selected path + "/- Favorites".
            string labelBasePath = Path.Combine(folderBrowserDialog.SelectedPath, "- Favorites");

            // Empty output, then say Grabbing IDs.
            output.Invoke(new Action(() => output.Text = String.Empty));
            output.Invoke(new Action(() => output.AppendText("Grabbing Album IDs...\r\n\r\n")));

            try
            {
                // Get UserFavorites model object
                UserFavorites qobuzUserFavorites = ExecuteApiCall(apiService => apiService.GetUserFavorites(DowloadItemID, "albums", 999999));

                // If API call failed, abort
                if (qobuzUserFavorites == null) { EnableBoxes(); return; }

                foreach (Album qobuzAlbum in qobuzUserFavorites.Albums.Items)
                {
                    // Empty output, then say Starting Downloads.
                    output.Invoke(new Action(() => output.Text = String.Empty));
                    output.Invoke(new Action(() => output.AppendText("Starting Downloads...\r\n\r\n")));

                    bool albumDownloaded = DownloadAlbum(qobuzAlbum, labelBasePath, true, $" [{qobuzAlbum.Id}]");

                    // If download failed, abort
                    if (!albumDownloaded) { EnableBoxes(); return; }
                }

                // Say that downloading is completed.
                output.Invoke(new Action(() => output.AppendText("\r\n\r\n")));
                output.Invoke(new Action(() => output.AppendText("Downloading job completed! All downloaded files will be located in your chosen path.")));
                EnableBoxes();
            }
            catch (Exception downloadError)
            {
                // If there is an issue trying to, or during the download, show error info.
                string error = downloadError.ToString();
                output.Invoke(new Action(() => output.Text = String.Empty));
                output.Invoke(new Action(() => output.AppendText("Artist Download ERROR. Information below.\r\n\r\n")));
                output.Invoke(new Action(() => output.AppendText(error)));
                EnableBoxes();
                return;
            }
        }

        // Favorite Artists
        private void downloadFaveArtistsBG_DoWork(object sender, DoWorkEventArgs e)
        {
            /* This hasn't been worked on yet */
        }

        // For downloading "playlist" links
        private void DownloadPlaylistBG_DoWork(object sender, DoWorkEventArgs e)
        {
            // Set "basePath" as the selected path.
            String playlistBasePath = folderBrowserDialog.SelectedPath;

            // Empty output, then say Starting Downloads.
            output.Invoke(new Action(() => output.Text = String.Empty));
            output.Invoke(new Action(() => output.AppendText("Starting Downloads...\r\n\r\n")));

            try
            {
                // Get Playlist model object
                Playlist qobuzPlaylist = ExecuteApiCall(apiService => apiService.GetPlaylist(DowloadItemID, true, "tracks", 10000));

                // If API call failed, abort
                if (qobuzPlaylist == null) { EnableBoxes(); return; }

                // Create Playlist root directory.
                string playlistNamePath = StringTools.GetSafeFilename(StringTools.DecodeEncodedNonAsciiCharacters(qobuzPlaylist.Name));
                playlistNamePath = playlistNamePath.Substring(0, Math.Min(playlistNamePath.Length, MaxLength)).TrimEnd();
                playlistBasePath = Path.Combine(playlistBasePath, "- Playlists", playlistNamePath);
                System.IO.Directory.CreateDirectory(playlistBasePath);

                // Download Playlist cover art to "Playlist.jpg" in root directory (if not exists
                string coverArtFilePath = Path.Combine(playlistBasePath, "Playlist.jpg");

                if (!System.IO.File.Exists(coverArtFilePath))
                {
                    try
                    {
                        using (WebClient imgClient = new WebClient())
                        {
                            imgClient.DownloadFile(new Uri(qobuzPlaylist.ImageRectangle.FirstOrDefault<string>()), coverArtFilePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Qobuz servers throw a 404 as if the image doesn't exist.
                        string[] errorLines = { "Error downloading full size playlist cover image file.", ex.Message, "\r\n" };
                        System.IO.File.AppendAllLines(downloadErrorLog, errorLines);
                    }
                }

                // Download Playlist tracks
                foreach (Track playlistTrack in qobuzPlaylist.Tracks.Items)
                {
                    if (!IsStreamable(playlistTrack, true)) continue;

                    // Fetch full Track info
                    Track qobuzTrack = ExecuteApiCall(apiService => apiService.GetTrack(playlistTrack.Id.GetValueOrDefault().ToString(), true));

                    DownloadTrack(qobuzTrack, playlistBasePath, true, false, true);
                }

                // Say that downloading is completed.
                output.Invoke(new Action(() => output.AppendText("\r\n\r\n")));
                output.Invoke(new Action(() => output.AppendText("Downloading job completed! All downloaded files will be located in your chosen path.")));
                EnableBoxes();

            }
            catch (Exception downloadError)
            {
                // If there is an issue trying to, or during the download, show error info.
                string error = downloadError.ToString();
                output.Invoke(new Action(() => output.Text = String.Empty));
                output.Invoke(new Action(() => output.AppendText("Playlist  Download ERROR. Information below.\r\n\r\n")));
                output.Invoke(new Action(() => output.AppendText(error)));
                EnableBoxes();
                return;
            }
        }

        private void tagsLabel_Click(object sender, EventArgs e)
        {
            if (this.Height == 533)
            {
                //New Height
                this.Height = 632;
                tagsLabel.Text = "🠉 Choose which tags to save (click me) 🠉";
            }
            else if (this.Height == 632)
            {
                //New Height
                this.Height = 533;
                tagsLabel.Text = "🠋 Choose which tags to save (click me) 🠋";
            }
        }

        private void albumCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.albumTag = albumCheckbox.Checked;
            Settings.Default.Save();
        }

        private void albumArtistCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.albumArtistTag = albumArtistCheckbox.Checked;
            Settings.Default.Save();
        }

        private void trackTitleCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.trackTitleTag = trackTitleCheckbox.Checked;
            Settings.Default.Save();
        }

        private void artistCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.artistTag = artistCheckbox.Checked;
            Settings.Default.Save();
        }

        private void trackNumberCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.trackTag = trackTitleCheckbox.Checked;
            Settings.Default.Save();
        }

        private void trackTotalCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.totalTracksTag = trackTotalCheckbox.Checked;
            Settings.Default.Save();
        }

        private void discNumberCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.discTag = discNumberCheckbox.Checked;
            Settings.Default.Save();
        }

        private void discTotalCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.totalDiscsTag = discTotalCheckbox.Checked;
            Settings.Default.Save();
        }

        private void releaseCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.yearTag = releaseCheckbox.Checked;
            Settings.Default.Save();
        }

        private void genreCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.genreTag = genreCheckbox.Checked;
            Settings.Default.Save();
        }

        private void composerCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.composerTag = composerCheckbox.Checked;
            Settings.Default.Save();
        }

        private void copyrightCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.copyrightTag = copyrightCheckbox.Checked;
            Settings.Default.Save();
        }

        private void isrcCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.isrcTag = isrcCheckbox.Checked;
            Settings.Default.Save();
        }

        private void typeCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.typeTag = typeCheckbox.Checked;
            Settings.Default.Save();
        }

        private void upcCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.upcTag = upcCheckbox.Checked;
            Settings.Default.Save();
        }

        private void explicitCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.explicitTag = explicitCheckbox.Checked;
            Settings.Default.Save();
        }

        private void commentCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.commentTag = commentCheckbox.Checked;
            Settings.Default.Save();
        }

        private void imageCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.imageTag = imageCheckbox.Checked;
            Settings.Default.Save();
        }

        private void commentTextbox_TextChanged(object sender, EventArgs e)
        {
            Settings.Default.commentText = commentTextbox.Text;
            Settings.Default.Save();
        }

        private void artSizeSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Set artSize to selected value, and save selected option to settings.
            artSize = artSizeSelect.Text;
            Settings.Default.savedArtSize = artSizeSelect.SelectedIndex;
            Settings.Default.Save();
        }

        private void filenameTempSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Set filename template to selected value, and save selected option to settings.
            if (filenameTempSelect.SelectedIndex == 0)
            {
                fileNameTemplateString = " ";
            }
            else if (filenameTempSelect.SelectedIndex == 1)
            {
                fileNameTemplateString = " - ";
            }
            else
            {
                fileNameTemplateString = " ";
            }

            Settings.Default.savedFilenameTemplate = filenameTempSelect.SelectedIndex;
            Settings.Default.savedFilenameTemplateString = fileNameTemplateString;
            Settings.Default.Save();
        }

        private void maxLengthTextbox_TextChanged(object sender, EventArgs e)
        {
            if (maxLengthTextbox.Text != null)
            {
                try
                {
                    if (Convert.ToInt32(maxLengthTextbox.Text) > 110)
                    {
                        maxLengthTextbox.Text = "110";
                    }
                    Settings.Default.savedMaxLength = Convert.ToInt32(maxLengthTextbox.Text);
                    Settings.Default.Save();

                    MaxLength = Convert.ToInt32(maxLengthTextbox.Text);
                }
                catch (Exception)
                {
                    MaxLength = 36;
                }
            }
            else
            {
                MaxLength = 36;
            }
        }

        private void flacHighCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.quality4 = flacHighCheckbox.Checked;
            Settings.Default.Save();

            if (flacHighCheckbox.Checked)
            {
                Globals.FormatIdString = "27";
                customFormatIDTextbox.Text = "27";
                Globals.AudioFileType = ".flac";
                Settings.Default.qualityFormat = Globals.FormatIdString;
                Settings.Default.audioType = Globals.AudioFileType;
                downloadButton.Enabled = true;
                flacMidCheckbox.Checked = false;
                flacLowCheckbox.Checked = false;
                mp3Checkbox.Checked = false;
            }
            else
            {
                if (!flacMidCheckbox.Checked && !flacLowCheckbox.Checked && !mp3Checkbox.Checked)
                {
                    downloadButton.Enabled = false;
                }
            }
        }

        private void flacMidCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.quality3 = flacMidCheckbox.Checked;
            Settings.Default.Save();

            if (flacMidCheckbox.Checked)
            {
                Globals.FormatIdString = "7";
                customFormatIDTextbox.Text = "7";
                Globals.AudioFileType = ".flac";
                Settings.Default.qualityFormat = Globals.FormatIdString;
                Settings.Default.audioType = Globals.AudioFileType;
                downloadButton.Enabled = true;
                flacHighCheckbox.Checked = false;
                flacLowCheckbox.Checked = false;
                mp3Checkbox.Checked = false;
            }
            else
            {
                if (!flacHighCheckbox.Checked && !flacLowCheckbox.Checked && !mp3Checkbox.Checked)
                {
                    downloadButton.Enabled = false;
                }
            }
        }

        private void flacLowCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.quality2 = flacLowCheckbox.Checked;
            Settings.Default.Save();

            if (flacLowCheckbox.Checked)
            {
                Globals.FormatIdString = "6";
                customFormatIDTextbox.Text = "6";
                Globals.AudioFileType = ".flac";
                Settings.Default.qualityFormat = Globals.FormatIdString;
                Settings.Default.audioType = Globals.AudioFileType;
                downloadButton.Enabled = true;
                flacHighCheckbox.Checked = false;
                flacMidCheckbox.Checked = false;
                mp3Checkbox.Checked = false;
            }
            else
            {
                if (!flacHighCheckbox.Checked && !flacMidCheckbox.Checked && !mp3Checkbox.Checked)
                {
                    downloadButton.Enabled = false;
                }
            }
        }

        private void mp3Checkbox_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.quality1 = mp3Checkbox.Checked;
            Settings.Default.Save();

            if (mp3Checkbox.Checked)
            {
                Globals.FormatIdString = "5";
                customFormatIDTextbox.Text = "5";
                Globals.AudioFileType = ".mp3";
                Settings.Default.qualityFormat = Globals.FormatIdString;
                Settings.Default.audioType = Globals.AudioFileType;
                downloadButton.Enabled = true;
                flacHighCheckbox.Checked = false;
                flacMidCheckbox.Checked = false;
                flacLowCheckbox.Checked = false;
            }
            else
            {
                if (!flacHighCheckbox.Checked && !flacMidCheckbox.Checked && !flacLowCheckbox.Checked)
                {
                    downloadButton.Enabled = false;
                }
            }
        }

        private void customFormatIDTextbox_TextChanged(object sender, EventArgs e)
        {
            if (Globals.FormatIdString != "5" || Globals.FormatIdString != "6" || Globals.FormatIdString != "7" || Globals.FormatIdString != "27")
            {
                Globals.FormatIdString = customFormatIDTextbox.Text;
            }
        }

        private void exitLabel_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void minimizeLabel_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void minimizeLabel_MouseHover(object sender, EventArgs e)
        {
            minimizeLabel.ForeColor = Color.FromArgb(0, 112, 239);
        }

        private void minimizeLabel_MouseLeave(object sender, EventArgs e)
        {
            minimizeLabel.ForeColor = Color.White;
        }

        private void aboutLabel_Click(object sender, EventArgs e)
        {
            Globals.AboutForm.ShowDialog();
        }

        private void aboutLabel_MouseHover(object sender, EventArgs e)
        {
            aboutLabel.ForeColor = Color.FromArgb(0, 112, 239);
        }

        private void aboutLabel_MouseLeave(object sender, EventArgs e)
        {
            aboutLabel.ForeColor = Color.White;
        }

        private void exitLabel_MouseHover(object sender, EventArgs e)
        {
            exitLabel.ForeColor = Color.FromArgb(0, 112, 239);
        }

        private void exitLabel_MouseLeave(object sender, EventArgs e)
        {
            exitLabel.ForeColor = Color.White;
        }

        private void QobuzDownloaderX_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void QobuzDownloaderX_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void logoBox_Click(object sender, EventArgs e)
        {
            devClickEggThingValue = devClickEggThingValue + 1;

            if (devClickEggThingValue >= 3)
            {
                streamableCheckbox.Visible = true;
                enableBtnsButton.Visible = true;
                hideDebugButton.Visible = true;
                displaySecretButton.Visible = true;
                secretTextbox.Visible = true;
                hiddenTextPanel.Visible = true;
                customFormatIDTextbox.Visible = true;
                customFormatPanel.Visible = true;
                formatIDLabel.Visible = true;
            }
            else
            {
                streamableCheckbox.Visible = false;
                displaySecretButton.Visible = false;
                secretTextbox.Visible = false;
                hiddenTextPanel.Visible = false;
                enableBtnsButton.Visible = false;
                hideDebugButton.Visible = false;
                customFormatIDTextbox.Visible = false;
                customFormatPanel.Visible = false;
                formatIDLabel.Visible = false;
            }
        }

        private void hideDebugButton_Click(object sender, EventArgs e)
        {
            streamableCheckbox.Visible = false;
            displaySecretButton.Visible = false;
            secretTextbox.Visible = false;
            hiddenTextPanel.Visible = false;
            enableBtnsButton.Visible = false;
            hideDebugButton.Visible = false;
            customFormatIDTextbox.Visible = false;
            customFormatPanel.Visible = false;
            formatIDLabel.Visible = false;

            devClickEggThingValue = 0;
        }

        private void displaySecretButton_Click(object sender, EventArgs e)
        {
            secretTextbox.Text = QobuzApiServiceManager.GetApiService().AppSecret;
        }

        private void logoutLabel_MouseHover(object sender, EventArgs e)
        {
            logoutLabel.ForeColor = Color.FromArgb(0, 112, 239);
        }

        private void logoutLabel_MouseLeave(object sender, EventArgs e)
        {
            logoutLabel.ForeColor = Color.FromArgb(88, 92, 102);
        }

        private void logoutLabel_Click(object sender, EventArgs e)
        {
            // Could use some work, but this works.
            Process.Start("QobuzDownloaderX.exe");
            Application.Exit();
        }

        private void enableBtnsButton_Click(object sender, EventArgs e)
        {
            EnableBoxes();
        }
    }
}