using QobuzApiSharp.Exceptions;
using QobuzApiSharp.Models.Content;
using QobuzApiSharp.Service;
using QobuzDownloaderX.Models;
using QobuzDownloaderX.Models.Content;
using QobuzDownloaderX.Properties;
using QobuzDownloaderX.Shared;
using QobuzDownloaderX.View;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TagLib;

namespace QobuzDownloaderX
{
    public partial class QobuzDownloaderX : HeadlessForm
    {
        private readonly string downloadErrorLogPath = Path.Combine(Globals.LoggingDir, "Download_Errors.log");

        public QobuzDownloaderX()
        {
            InitializeComponent();

            // Remove previous download error log
            if (System.IO.File.Exists(downloadErrorLogPath))
            {
                System.IO.File.Delete(downloadErrorLogPath);
            }
        }

        private string DownloadLogPath { get; set; }

        public string ArtSize { get; set; }
        public string FileNameTemplateString { get; set; }
        public string FinalTrackNamePath { get; set; }
        public string FinalTrackNameVersionPath { get; set; }
        public string QualityPath { get; set; }
        public int MaxLength { get; set; }
        public int DevClickEggThingValue { get; set; }
        public int DebugMode { get; set; }

        // Important strings
        public string DowloadItemID { get; set; }

        public string Stream { get; set; }

        // Info strings for creating paths
        public string AlbumArtistPath { get; set; }
        public string PerformerNamePath { get; set; }
        public string AlbumNamePath { get; set; }
        public string TrackNamePath { get; set; }
        public string VersionNamePath { get; set; }
        public string Path1Full { get; set; }
        public string Path2Full { get; set; }
        public string Path3Full { get; set; }
        public string Path4Full { get; set; }

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
            FileNameTemplateString = Settings.Default.savedFilenameTemplateString;
            MaxLength = Settings.Default.savedMaxLength;

            customFormatIDTextbox.Text = Globals.FormatIdString;

            ArtSize = artSizeSelect.Text;
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

        /// <summary>
        /// Add string to download log file, screen or both.<br/>
        /// When a line is written to the log file, a timestamp is added as prefix for non blank lines.<br/>
        /// For writing to log file, blank lines are filtered exept if the given string starts with a blank line.<br/>
        /// Use AddEmptyDownloadLogLine to create empty devider lines in the download log.
        /// </summary>
        /// <param name="logEntry">String to be logged</param>
        /// <param name="logToFile">Should string be logged to file?</param>
        /// <param name="logToScreen">Should string be logged to screen?</param>
        private void AddDownloadLogLine(string logEntry, bool logToFile, bool logToScreen = false)
        {
            if (string.IsNullOrEmpty(logEntry)) return;

            if (logToScreen) output?.Invoke(new Action(() => output.AppendText(logEntry)));

            if (logToFile)
            {
                var logEntries = logEntry.Split(new[] { Environment.NewLine }, StringSplitOptions.None)
                    .Select(logLine => string.IsNullOrWhiteSpace(logLine) ? logLine : $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} : {logLine}");

                // Filter out all empty lines exept if the logEntry started with an empty line to avoid blank lines for each newline in UI
                var filteredLogEntries = logEntries.Aggregate(new List<string>(), (accumulator, current) =>
                {
                    if (accumulator.Count == 0 || !string.IsNullOrWhiteSpace(current))
                    {
                        accumulator.Add(current);
                    }

                    return accumulator;
                });

                System.IO.File.AppendAllLines(DownloadLogPath, filteredLogEntries);
            }
        }
        /// <summary>
        /// Convenience method to add [ERROR] prefix to logged string before calling AddDownloadLogLine.
        /// </summary>
        /// <param name="logEntry">String to be logged</param>
        /// <param name="logToFile">Should string be logged to file?</param>
        /// <param name="logToScreen">Should string be logged to screen?</param>
        private void AddDownloadLogErrorLine(string logEntry, bool logToFile, bool logToScreen = false)
        {
            AddDownloadLogLine($"[ERROR] {logEntry}", logToFile, logToScreen);
        }

        /// <summary>
        /// Convenience method to add empty spacing line to log.
        /// </summary>
        /// <param name="logToFile">Should string be logged to file?</param>
        /// <param name="logToScreen">Should string be logged to screen?</param>
        private void AddEmptyDownloadLogLine(bool logToFile, bool logToScreen = false)
        {
            AddDownloadLogLine($"{Environment.NewLine}{Environment.NewLine}", logToFile, logToScreen);
        }

        private void AddDownloadErrorLogLines(IEnumerable<string> logEntries)
        {
            if (logEntries == null && !logEntries.Any()) return;

            System.IO.File.AppendAllLines(downloadErrorLogPath, logEntries);
        }

        private void AddDownloadErrorLogLine(string logEntry)
        {
            AddDownloadErrorLogLines(new string[] { logEntry });
        }

        /// <summary>
        /// Standardized logging when global download task fails.
        /// After logging, disabled controls are re-enabled.
        /// </summary>
        /// <param name="downloadTaskType">Name of the failed download task</param>
        /// <param name="downloadEx">Exception thrown by task</param>
        private void LogDownloadTaskException(string downloadTaskType, Exception downloadEx)
        {
            // If there is an issue trying to, or during the download, show error info.
            output.Invoke(new Action(() => output.Text = String.Empty));
            AddDownloadLogErrorLine($"{downloadTaskType} Download Task ERROR. Details saved to error log.{Environment.NewLine}", true, true);

            AddDownloadErrorLogLine($"{downloadTaskType} Download Task ERROR.");
            AddDownloadErrorLogLine(downloadEx.ToString());
            AddDownloadErrorLogLine(Environment.NewLine);
            EnableControlsAfterDownload();
        }

        private void debuggingEvents(object sender, EventArgs e)
        {
            DevClickEggThingValue = 0;

            // Debug mode for things that are only for testing, or shouldn't be on public releases. At the moment, does nothing.
            if (!Debugger.IsAttached)
            {
                DebugMode = 0;
            }
            else
            {
                DebugMode = 1;
            }

            // Show app_secret value.
            //output.Invoke(new Action(() => output.AppendText("\r\n\r\napp_secret = " + Globals.AppSecret)));

            // Show format_id value.
            //output.Invoke(new Action(() => output.AppendText("\r\n\r\nformat_id = " + Globals.FormatIdString)));
        }

        private void OpenSearch_Click(object sender, EventArgs e)
        {
            Globals.SearchForm.ShowDialog(this);
        }

        private void DownloadButton_Click(object sender, EventArgs e)
        {
            getLinkTypeBG.RunWorkerAsync();
        }

        private void DownloadUrl_KeyDown(object sender, KeyEventArgs e)
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

                AddEmptyDownloadLogLine(false, true);
                AddDownloadLogErrorLine($"Communication problem with Qobuz API. Details saved to error log{Environment.NewLine}", true, true);

                switch (ex)
                {
                    case ApiErrorResponseException erEx:
                        errorLines.Add("Failed API request:");
                        errorLines.Add(erEx.RequestContent);
                        errorLines.Add($"Api response code: {erEx.ResponseStatusCode}");
                        errorLines.Add($"Api response status: {erEx.ResponseStatus}");
                        errorLines.Add($"Api response reason: {erEx.ResponseReason}");
                        break;
                    case ApiResponseParseErrorException pEx:
                        errorLines.Add("Error parsing API response");
                        errorLines.Add($"Api response content: {pEx.ResponseContent}");
                        break;
                    default:
                        errorLines.Add("Unknown error trying API request:");
                        errorLines.Add($"{ex}");
                        break;
                }

                // Write detailed info to error log
                AddDownloadErrorLogLines(errorLines);
            }

            return default;
        }

        private void DisableControlsDuringDownload()
        {
            mp3Checkbox.Invoke(new Action(() => mp3Checkbox.AutoCheck = false));
            flacLowCheckbox.Invoke(new Action(() => flacLowCheckbox.AutoCheck = false));
            flacMidCheckbox.Invoke(new Action(() => flacMidCheckbox.AutoCheck = false));
            flacHighCheckbox.Invoke(new Action(() => flacHighCheckbox.AutoCheck = false));

            selectFolderButton.Invoke(new Action(() => selectFolderButton.Enabled = false));
            openSearchButton.Invoke(new Action(() => openSearchButton.Enabled = false));
            downloadButton.Invoke(new Action(() => downloadButton.Enabled = false));
        }

        private void EnableControlsAfterDownload()
        {
            mp3Checkbox.Invoke(new Action(() => mp3Checkbox.AutoCheck = true));
            flacLowCheckbox.Invoke(new Action(() => flacLowCheckbox.AutoCheck = true));
            flacMidCheckbox.Invoke(new Action(() => flacMidCheckbox.AutoCheck = true));
            flacHighCheckbox.Invoke(new Action(() => flacHighCheckbox.AutoCheck = true));

            selectFolderButton.Invoke(new Action(() => selectFolderButton.Enabled = true));
            openSearchButton.Invoke(new Action(() => openSearchButton.Enabled = true));
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
            // Open selected folder
            if (string.IsNullOrEmpty(folderBrowserDialog.SelectedPath))
            {
                // If there's no selected path.
                MessageBox.Show("No path selected!", "ERROR",
                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                EnableControlsAfterDownload();
            }
            else
            {
                // If selected path doesn't exist, create it. (Will be ignored if it does)
                System.IO.Directory.CreateDirectory(folderBrowserDialog.SelectedPath);
                // Open selected folder
                Process.Start(@folderBrowserDialog.SelectedPath);
            }
        }

        private void OpenLogFolderButton_Click(object sender, EventArgs e)
        {
            // Open log folder. Folder should exist here so no extra check
            Process.Start(@Globals.LoggingDir);
        }

        private void GetLinkTypeBG_DoWork(object sender, DoWorkEventArgs e)
        {
            DisableControlsDuringDownload();

            // Check if there's no selected path.
            if (string.IsNullOrEmpty(folderBrowserDialog.SelectedPath))
            {
                // If there is NOT a saved path.
                output.Invoke(new Action(() => output.Text = String.Empty));
                output.Invoke(new Action(() => output.AppendText("No path has been set! Remember to Choose a Folder!\r\n")));
                EnableControlsAfterDownload();
                return;
            }

            // Get download item type and ID from url
            DownloadItem downloadItem = DownloadUrlParser.ParseDownloadUrl(this.downloadUrl.Text);

            // If download item could not be parsed, abort
            if (downloadItem.IsEmpty())
            {
                output.Invoke(new Action(() => output.Text = String.Empty));
                output.Invoke(new Action(() => output.AppendText("URL not understood. Is there a typo?")));
                EnableControlsAfterDownload();
                return;
            }

            // Link should be valid here, start new download log
            DownloadLogPath = Path.Combine(Globals.LoggingDir, $"Download_Log_{DateTime.Now:yyyy-MM-dd_HH.mm.ss.fff}.log");

            string logLine = $"Downloading <{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(downloadItem.Type)}> from {this.downloadUrl.Text}";
            AddDownloadLogLine(new string('=', logLine.Length).PadRight(logLine.Length), true);
            AddDownloadLogLine(logLine, true);
            AddDownloadLogLine(new string('=', logLine.Length).PadRight(logLine.Length), true);
            AddEmptyDownloadLogLine(true);

            DowloadItemID = downloadItem.Id;

            if (downloadItem.Type == "track")
            {
                downloadTrackBG.RunWorkerAsync();
            }
            else if (downloadItem.Type == "album")
            {
                downloadAlbumBG.RunWorkerAsync();
            }
            else if (downloadItem.Type == "artist")
            {
                downloadDiscogBG.RunWorkerAsync();
            }
            else if (downloadItem.Type == "label")
            {
                downloadLabelBG.RunWorkerAsync();
            }
            else if (downloadItem.Type == "user")
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
                    AddDownloadLogLine($"Downloading favorites only works on favorite albums at the moment. More options will be added in the future.{Environment.NewLine}", true, true);
                    AddDownloadLogLine("If you'd like to go ahead and grab your favorite albums, paste this link in the URL section - https://play.qobuz.com/user/library/favorites/albums", true, true);
                    EnableControlsAfterDownload();
                }
            }
            else if (downloadItem.Type == "playlist")
            {
                downloadPlaylistBG.RunWorkerAsync();
            }
            else
            {
                // We shouldn't get here?!? I'll leave this here just in case...
                output.Invoke(new Action(() => output.Text = String.Empty));
                AddDownloadLogLine("URL not understood. Is there a typo?", true, true);
                EnableControlsAfterDownload();
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
                    TagLib.Id3v2.Tag customId3v2 = (TagLib.Id3v2.Tag)tfile.GetTag(TagTypes.Id3v2, true);

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
                            AddDownloadLogErrorLine($"Cover art tag failed, .jpg still exists?...{Environment.NewLine}", true, true);
                        }
                    }

                    // Track Title tag, version is already added to name if available
                    if (trackTitleCheckbox.Checked) { tfile.Tag.Title = TrackName; }

                    // Album Title tag, version is already added to name if available
                    if (albumCheckbox.Checked) { tfile.Tag.Album = AlbumName; }

                    // Album Artits tag
                    if (albumArtistCheckbox.Checked) { tfile.Tag.AlbumArtists = new string[] { AlbumArtist }; }

                    // Track Artist tag
                    if (artistCheckbox.Checked) { tfile.Tag.Performers = new string[] { PerformerName }; }

                    // Composer tag
                    if (composerCheckbox.Checked) { tfile.Tag.Composers = new string[] { ComposerName }; }

                    // Release Date tag
                    if (releaseCheckbox.Checked) { ReleaseDate = ReleaseDate.Substring(0, 4); tfile.Tag.Year = UInt32.Parse(ReleaseDate); }

                    // Genre tag
                    if (genreCheckbox.Checked) { tfile.Tag.Genres = new string[] { Genre }; }

                    // Disc Number tag
                    if (discNumberCheckbox.Checked) { tfile.Tag.Disc = Convert.ToUInt32(DiscNumber); }

                    // Total Discs tag
                    if (discTotalCheckbox.Checked) { tfile.Tag.DiscCount = Convert.ToUInt32(DiscTotal); }

                    // Total Tracks tag
                    if (trackTotalCheckbox.Checked) { tfile.Tag.TrackCount = Convert.ToUInt32(TrackTotal); }

                    // Track Number tag
                    // !! Set Track Number after Total Tracks to prevent taglib-sharp from re-formatting the field to a "two-digit zero-filled value" !!
                    if (trackNumberCheckbox.Checked)
                    {
                        // Set TRCK tag manually to prevent using "two-digit zero-filled value"
                        // See https://github.com/mono/taglib-sharp/pull/240 where this change was introduced in taglib-sharp v2.3
                        // Original command: tfile.Tag.Track = Convert.ToUInt32(TrackNumber);
                        customId3v2.SetNumberFrame("TRCK", Convert.ToUInt32(TrackNumber), tfile.Tag.TrackCount);
                    }

                    // Comment tag
                    if (commentCheckbox.Checked) { tfile.Tag.Comment = commentTextbox.Text; }

                    // Copyright tag
                    if (copyrightCheckbox.Checked) { tfile.Tag.Copyright = Copyright; }

                    // ISRC tag
                    if (isrcCheckbox.Checked) { customId3v2.SetTextFrame("TSRC", Isrc); }

                    // Release Type tag
                    if (MediaType != null && typeCheckbox.Checked) { customId3v2.SetTextFrame("TMED", MediaType); }

                    // Save all selected tags to file
                    tfile.Save();

                    break;

                case ".flac":

                    // For custom / troublesome tags.
                    TagLib.Ogg.XiphComment custom = (TagLib.Ogg.XiphComment)tfile.GetTag(TagLib.TagTypes.Xiph);

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
                            AddDownloadLogErrorLine($"Cover art tag failed, .jpg still exists?...{Environment.NewLine}", true, true);
                        }
                    }

                    // Track Title tag, version is already added to name if available
                    if (trackTitleCheckbox.Checked) { tfile.Tag.Title = TrackName; }

                    // Album Title tag, version is already added to name if available
                    if (albumCheckbox.Checked) { tfile.Tag.Album = AlbumName; }

                    // Album Artist tag
                    if (albumArtistCheckbox.Checked) { custom.SetField("ALBUMARTIST", AlbumArtist); }

                    // Track Artist tag
                    if (artistCheckbox.Checked) { custom.SetField("ARTIST", PerformerName); }

                    // Composer tag
                    if (composerCheckbox.Checked) { custom.SetField("COMPOSER", ComposerName); }

                    // Release Date tag
                    if (releaseCheckbox.Checked) { custom.SetField("YEAR", ReleaseDate); }

                    // Genre tag
                    if (genreCheckbox.Checked) { tfile.Tag.Genres = new string[] { Genre }; }

                    // Track Number tag
                    if (trackNumberCheckbox.Checked)
                    {
                        tfile.Tag.Track = Convert.ToUInt32(TrackNumber);
                        // Override TRACKNUMBER tag again to prevent using "two-digit zero-filled value"
                        // See https://github.com/mono/taglib-sharp/pull/240 where this change was introduced in taglib-sharp v2.3
                        custom.SetField("TRACKNUMBER", Convert.ToUInt32(TrackNumber));
                    }

                    // Disc Number tag
                    if (discNumberCheckbox.Checked) { tfile.Tag.Disc = Convert.ToUInt32(DiscNumber); }

                    // Total Discs tag
                    if (discTotalCheckbox.Checked) { tfile.Tag.DiscCount = Convert.ToUInt32(DiscTotal); }

                    // Total Tracks tag
                    if (trackTotalCheckbox.Checked) { tfile.Tag.TrackCount = Convert.ToUInt32(TrackTotal); }

                    // Comment tag
                    if (commentCheckbox.Checked) { custom.SetField("COMMENT", commentTextbox.Text); }

                    // Copyright tag
                    if (copyrightCheckbox.Checked) { custom.SetField("COPYRIGHT", Copyright); }
                    // UPC tag
                    if (upcCheckbox.Checked) { custom.SetField("UPC", Upc); }

                    // ISRC tag
                    if (isrcCheckbox.Checked) { custom.SetField("ISRC", Isrc); }

                    // Release Type tag
                    if (MediaType != null && typeCheckbox.Checked)
                    {
                        custom.SetField("MEDIATYPE", MediaType);
                    }

                    // Explicit tag
                    if (explicitCheckbox.Checked)
                    {
                        if (Advisory == true) { custom.SetField("ITUNESADVISORY", "1"); } else { custom.SetField("ITUNESADVISORY", "0"); }
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
                Path1Full = basePath;
                Path2Full = Path1Full;
                Path3Full = Path.Combine(basePath, qualityPath);
                Path4Full = Path3Full;
            }
            else
            {
                Path1Full = Path.Combine(basePath, AlbumArtistPath);
                Path2Full = Path.Combine(basePath, AlbumArtistPath, AlbumNamePath + albumPathSuffix);
                Path3Full = Path.Combine(basePath, AlbumArtistPath, AlbumNamePath + albumPathSuffix, qualityPath);

                // If more than 1 disc, create folders for discs. Otherwise, strings will remain null
                // Pad discnumber with minimum of 2 integer positions based on total number of disks
                if (DiscTotal > 1)
                {
                    // Create strings for disc folders
                    string discFolder = "CD " + DiscNumber.ToString().PadLeft(Math.Max(2, (int)Math.Floor(Math.Log10(DiscTotal) + 1)), '0');
                    Path4Full = Path.Combine(basePath, AlbumArtistPath, AlbumNamePath + albumPathSuffix, qualityPath, discFolder);
                }
                else
                {
                    Path4Full = Path3Full;
                }
            }

            System.IO.Directory.CreateDirectory(Path4Full);
        }

        // Set Track tagging info, tagbased Paths
        private void GetTrackTaggingInfo(Track qobuzTrack)
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
            PerformerNamePath = StringTools.GetSafeFilename(PerformerName);
            TrackNamePath = StringTools.GetSafeFilename(TrackName);
        }

        // Set Album tagging info, tagbased Paths
        private void GetAlbumTaggingInfo(Album qobuzAlbum)
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

            // Paths
            AlbumArtistPath = StringTools.GetSafeFilename(AlbumArtist);
            AlbumNamePath = StringTools.GetSafeFilename(AlbumName);
        }

        private void UpdateAlbumTagsUI()
        {
            // Update UI
            albumArtistTextBox.Invoke(new Action(() => albumArtistTextBox.Text = AlbumArtist));
            albumTextBox.Invoke(new Action(() => albumTextBox.Text = AlbumName));
            releaseDateTextBox.Invoke(new Action(() => releaseDateTextBox.Text = ReleaseDate));
            upcTextBox.Invoke(new Action(() => upcTextBox.Text = Upc));
            totalTracksTextbox.Invoke(new Action(() => totalTracksTextbox.Text = TrackTotal.ToString()));
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
            TrackNamePath = null;
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
            AlbumArtistPath = null;
            AlbumNamePath = null;
        }

        private void GetAlbumCoverArtUrls(Album qobuzAlbum)
        {
            // Grab cover art link
            FrontCoverImgUrl = qobuzAlbum.Image.Large;
            // Get 150x150 artwork for cover art box
            FrontCoverImgBoxUrl = FrontCoverImgUrl.Replace("_600.jpg", "_150.jpg");
            // Get selected artwork size for tagging
            FrontCoverImgTagUrl = FrontCoverImgUrl.Replace("_600.jpg", $"_{ArtSize}.jpg");
            // Get max sized artwork ("_org.jpg" is compressed version of the original "_org.jpg")
            FrontCoverImgUrl = FrontCoverImgUrl.Replace("_600.jpg", "_org.jpg");

            albumArtPicBox.Invoke(new Action(() => albumArtPicBox.ImageLocation = FrontCoverImgBoxUrl));
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
                        TrackName = StringTools.DecodeEncodedNonAsciiCharacters(qobuzTrack.Title.Trim());
                        AddDownloadLogLine($"Track {trackReference} is not available for streaming. Unable to download.\r\n", tryToStream, tryToStream);
                        tryToStream = false;
                        break;

                    default:
                        AddDownloadLogLine("Track is not available for streaming. But streamable check is being ignored for debugging, or messed up releases. Attempting to download...\r\n", tryToStream, tryToStream);
                        break;
                }
            }

            return tryToStream;
        }

        private async Task DownloadFileAsync(HttpClient httpClient, string downloadUrl, string filePath)
        {
            // See https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient?view=netframework-4.8
            // "HttpClient.GetStreamAsync(uri)" should internally use "HttpCompletionOption.ResponseHeadersRead", 
            // which seems apropriate to download potentially large files.
            // If this should cause problems, alternative is using "HttpCompletionOption.ResponseContentRead" which might use more memory.
            // See https://stackoverflow.com/questions/69849953/net-httpclient-getstreamasync-behaves-differently-to-getasync
            using (Stream streamToReadFrom = await httpClient.GetStreamAsync(downloadUrl))
            {
                using (FileStream streamToWriteTo = System.IO.File.Create(filePath))
                {
                    await streamToReadFrom.CopyToAsync(streamToWriteTo);
                }
            }
        }

        private bool DownloadTrack(Track qobuzTrack, string basePath, bool isPartOfPlaylist, bool isPartOfAlbum, bool removeTagArtFileAfterDownload = false, string albumPathSuffix = "")
        {
            string trackIdString = qobuzTrack.Id.GetValueOrDefault().ToString();

            GetTrackTaggingInfo(qobuzTrack);

            // If track is downloaded as part of Album, Album related processings should already be done.
            // Only handle Album related processing when downloading a single track.
            if (!isPartOfAlbum) PrepareAlbumDownload(qobuzTrack.Album);

            // Check if available for streaming.
            if (!IsStreamable(qobuzTrack)) return false;

            // If AlbumArtist, PerformerName or AlbumName goes over set MaxLength number of characters, limit them to the MaxLength
            AlbumArtistPath = StringTools.TrimToMaxLength(AlbumArtistPath, MaxLength);
            PerformerNamePath = StringTools.TrimToMaxLength(PerformerNamePath, MaxLength);
            AlbumNamePath = StringTools.TrimToMaxLength(AlbumNamePath, MaxLength);

            // Create directories if they don't exist yet
            // Add Album ID to Album Path if requested (to avoid conflicts for similar albums with trimmed long names)
            CreateTrackDirectories(basePath, QualityPath, albumPathSuffix, isPartOfPlaylist);

            // Set trackPath to the created directories
            string trackPath = Path4Full;

            // Create padded track number string with minimum of 2 integer positions based on number of total tracks
            string paddedTrackNumber = TrackNumber.ToString().PadLeft(Math.Max(2, (int)Math.Floor(Math.Log10(TrackTotal) + 1)), '0');

            // Create full track filename
            if (isPartOfPlaylist)
            {
                FinalTrackNamePath = string.Concat(PerformerNamePath, FileNameTemplateString, TrackNamePath).TrimEnd();
            }
            else
            {
                FinalTrackNamePath = string.Concat(paddedTrackNumber, FileNameTemplateString, TrackNamePath).TrimEnd();
            }

            // Shorten full filename if over MaxLength to avoid errors with file names being too long
            FinalTrackNamePath = StringTools.TrimToMaxLength(FinalTrackNamePath, MaxLength);

            // Check if the file already exists
            string checkFile = Path.Combine(trackPath, FinalTrackNamePath + Globals.AudioFileType);

            if (System.IO.File.Exists(checkFile))
            {
                string message = $"File for \"{FinalTrackNamePath}\" already exists. Skipping.\r\n";
                AddDownloadLogLine(message, true, true);
                return false;
            }

            // Notify UI of starting track download.
            AddDownloadLogLine($"Downloading - {FinalTrackNamePath} ...... ", true, true);

            // Get track streaming URL, abort if failed.
            string streamUrl = ExecuteApiCall(apiService => apiService.GetTrackFileUrl(trackIdString, Globals.FormatIdString))?.Url;

            if (string.IsNullOrEmpty(streamUrl))
            {
                // Can happen with free accounts trying to download non-previewable tracks (or if API call failed). 
                AddDownloadLogLine($"Couldn't get streaming URL for Track \"{FinalTrackNamePath}\". Skipping.\r\n", true, true);
                return false;
            }

            try
            {
                // Create file path strings
                string filePath = Path.Combine(trackPath, FinalTrackNamePath + Globals.AudioFileType);
                string coverArtFilePath = Path.Combine(Path3Full, "Cover.jpg");
                string coverArtTagFilePath = Path.Combine(Path3Full, ArtSize + ".jpg");

                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", Globals.USER_AGENT);

                    // Save streamed file from link
                    // GetAwaiter().GetResult(); is best of bad options, entire sync / async should be refactored.
                    DownloadFileAsync(httpClient, streamUrl, filePath).GetAwaiter().GetResult();

                    // Download selected cover art size for tagging files (if not exists)
                    if (!System.IO.File.Exists(coverArtTagFilePath))
                    {
                        try
                        {
                            DownloadFileAsync(httpClient, FrontCoverImgTagUrl, coverArtTagFilePath).GetAwaiter().GetResult();
                        }
                        catch (Exception ex)
                        {
                            // Qobuz servers throw a 404 as if the image doesn't exist.
                            AddDownloadErrorLogLines(new string[] { "Error downloading image file for tagging.", ex.Message, Environment.NewLine });
                        }
                    }

                    // Download max quality Cover Art to "Cover.jpg" file in chosen path (if not exists & not part of playlist).
                    if (!isPartOfPlaylist && !System.IO.File.Exists(coverArtFilePath))
                    {
                        try
                        {
                            DownloadFileAsync(httpClient, FrontCoverImgUrl, coverArtFilePath).GetAwaiter().GetResult();
                        }
                        catch (Exception ex)
                        {
                            // Qobuz servers throw a 404 as if the image doesn't exist.
                            AddDownloadErrorLogLines(new string[] { "Error downloading full size cover image file.", ex.Message, Environment.NewLine });
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

                AddDownloadLogLine("Track Download Done!\r\n", true, true);
                System.Threading.Thread.Sleep(100);
            }
            catch (AggregateException ae)
            {
                // When a Task fails, an AggregateException is thrown. Could be a HttpClient timeout or network error.
                AddDownloadLogErrorLine($"Track Download canceled, probably due to network error or request timeout. Details saved to error log.{Environment.NewLine}", true, true);

                AddDownloadErrorLogLine("Track Download canceled, probably due to network error or request timeout.");
                AddDownloadErrorLogLine(ae.ToString());
                AddDownloadErrorLogLine(Environment.NewLine);
                return false;
            }
            catch (Exception downloadEx)
            {
                // If there is an unknown issue trying to, or during the download, show and log error info.
                AddDownloadLogErrorLine($"Unknown error during Track Download. Details saved to error log.{Environment.NewLine}", true, true);

                AddDownloadErrorLogLine("Unknown error during Track Download.");
                AddDownloadErrorLogLine(downloadEx.ToString());
                AddDownloadErrorLogLine(Environment.NewLine);
                return false;
            }

            return true;
        }

        private bool DownloadAlbum(Album qobuzAlbum, string basePath, bool loadTracks = false, string albumPathSuffix = "")
        {
            bool noErrorsOccured = true;

            if (loadTracks)
            {
                // Get Album model object with tracks for when tracks aren't already loaded in Album model
                qobuzAlbum = ExecuteApiCall(apiService => apiService.GetAlbum(qobuzAlbum.Id, true));
            }

            // If API call failed or empty Album was provided, abort
            if (qobuzAlbum == null || string.IsNullOrEmpty(qobuzAlbum.Id)) { EnableControlsAfterDownload(); return false; }

            PrepareAlbumDownload(qobuzAlbum);

            // Download all tracks of the Album, clean albumArt tag file after last track
            int trackCount = qobuzAlbum.TracksCount ?? 0;

            for (int i = 0; i < trackCount; i++)
            {
                Track qobuzTrack = qobuzAlbum.Tracks.Items[i];

                // Nested Album objects in Tracks are not always fully populated, inject current qobuzAlbum in Track to be downloaded
                qobuzTrack.Album = qobuzAlbum;

                if (!DownloadTrack(qobuzTrack, basePath, false, true, i == trackCount - 1, albumPathSuffix)) noErrorsOccured = false;
            }

            // Look for digital booklet(s) in "Goodies"
            // Don't fail on failed "Goodies" downloads, just log...
            if (!DownloadBooklets(qobuzAlbum, Path3Full)) noErrorsOccured = false;

            return noErrorsOccured;
        }

        private bool DownloadBooklets(Album qobuzAlbum, string basePath)
        {
            bool noErrorsOccured = true;

            List<Goody> booklets = qobuzAlbum.Goodies?.Where(g => g.FileFormatId == (int)GoodiesFileType.BOOKLET).ToList();

            if (booklets?.Any() == false)
            {
                // No booklets found, just return
                return noErrorsOccured;
            }

            AddDownloadLogLine($"Goodies found, downloading...{Environment.NewLine}", true, true);

            using (HttpClient httpClient = new HttpClient())
            {
                int counter = 1;

                foreach (Goody booklet in booklets)
                {
                    string bookletFileName = counter == 1 ? "Digital Booklet.pdf" : $"Digital Booklet {counter}.pdf";
                    string bookletFilePath = Path.Combine(basePath, bookletFileName);

                    // Download booklet if file doesn't exist yet
                    if (System.IO.File.Exists(bookletFilePath))
                    {
                        AddDownloadLogLine($"Booklet file for \"{bookletFileName}\" already exists. Skipping.{Environment.NewLine}", true, true);
                    } else
                    {
                        // When a booklet download fails, mark error occured but continue downloading others if they exist 
                        if (!DownloadBooklet(booklet, httpClient, bookletFileName, bookletFilePath)) noErrorsOccured = false;
                    }

                    counter++;
                }
            }

            return noErrorsOccured;
        }

        private bool DownloadBooklet(Goody booklet, HttpClient httpClient, string fileName, string filePath)
        {
            bool noErrorsOccured = true;

            try
            {
                // Download booklet
                DownloadFileAsync(httpClient, booklet.Url, filePath).GetAwaiter().GetResult();

                AddDownloadLogLine($"Booklet \"{fileName}\" download complete!{Environment.NewLine}", true, true);
            }
            catch (AggregateException ae)
            {
                // When a Task fails, an AggregateException is thrown. Could be a HttpClient timeout or network error.
                AddDownloadLogErrorLine($"Goodies Download canceled, probably due to network error or request timeout. Details saved to error log.{Environment.NewLine}", true, true);

                AddDownloadErrorLogLine("Goodies Download canceled, probably due to network error or request timeout.");
                AddDownloadErrorLogLine(ae.ToString());
                AddDownloadErrorLogLine(Environment.NewLine);
                noErrorsOccured = false;
            }
            catch (Exception downloadEx)
            {
                // If there is an unknown issue trying to, or during the download, show and log error info.
                AddDownloadLogErrorLine($"Unknown error during Goodies Download. Details saved to error log.{Environment.NewLine}", true, true);

                AddDownloadErrorLogLine("Unknown error during Goodies Download.");
                AddDownloadErrorLogLine(downloadEx.ToString());
                AddDownloadErrorLogLine(Environment.NewLine);
                noErrorsOccured = false;
            }

            return noErrorsOccured;
        }

        private void DownloadAlbums(string basePath, List<Album> albums)
        {
            bool noAlbumErrorsOccured = true;

            foreach (Album qobuzAlbum in albums)
            {
                // Empty output, then say Starting Downloads.
                output.Invoke(new Action(() => output.Text = String.Empty));
                AddEmptyDownloadLogLine(true, false);
                AddDownloadLogLine($"Starting Downloads for album \"{qobuzAlbum.Title}\" with ID: <{qobuzAlbum.Id}>...", true, true);
                AddEmptyDownloadLogLine(true, true);

                bool albumDownloadOK = DownloadAlbum(qobuzAlbum, basePath, true, $" [{qobuzAlbum.Id}]");

                // If album download failed, mark error occured and continue
                if (!albumDownloadOK) noAlbumErrorsOccured = false;
            }

            AddEmptyDownloadLogLine(true, true);

            // Say that downloading is completed.
            if (noAlbumErrorsOccured)
            {
                AddDownloadLogLine("Download job completed! All downloaded files will be located in your chosen path.", true, true);
            }
            else
            {
                AddDownloadLogLine("Download job completed with warnings and/or errors! Some or all files could be missing!", true, true);
            }

            EnableControlsAfterDownload();
        }

        private void PrepareAlbumDownload(Album qobuzAlbum)
        {
            // Grab sample rate and bit depth for album track is from.
            (string quality, string qualityPathLocal) = QualityStringMappings.GetQualityStrings(Globals.FormatIdString, qobuzAlbum);
            QualityPath = qualityPathLocal;

            // Display album quality in quality textbox.
            qualityTextbox.Invoke(new Action(() => qualityTextbox.Text = quality));

            GetAlbumCoverArtUrls(qobuzAlbum);
            GetAlbumTaggingInfo(qobuzAlbum);
            UpdateAlbumTagsUI();
        }

        // For downloading "track" links
        private void DownloadTrackBG_DoWork(object sender, DoWorkEventArgs e)
        {
            // Empty screen output, then say Grabbing info.
            output.Invoke(new Action(() => output.Text = String.Empty));
            AddDownloadLogLine($"Grabbing Track info...{Environment.NewLine}", true, true);

            // Set "basePath" as the selected path.
            string downloadBasePath = folderBrowserDialog.SelectedPath;

            try
            {
                Track qobuzTrack = ExecuteApiCall(apiService => apiService.GetTrack(DowloadItemID, true));

                // If API call failed, abort
                if (qobuzTrack == null) { EnableControlsAfterDownload(); return; }

                AddDownloadLogLine($"Track \"{qobuzTrack.Title}\" found. Starting Download...", true, true);
                AddEmptyDownloadLogLine(true, true);

                bool fileDownloaded = DownloadTrack(qobuzTrack, downloadBasePath, false, false, true);

                // If download failed, abort
                if (!fileDownloaded) { EnableControlsAfterDownload(); return; }

                // Say that downloading is completed.
                AddEmptyDownloadLogLine(true, true);
                AddDownloadLogLine("Download job completed! All downloaded files will be located in your chosen path.", true, true);
                EnableControlsAfterDownload();
            }
            catch (Exception downloadEx)
            {
                LogDownloadTaskException("Track", downloadEx);
            }
        }

        // For downloading "album" links
        private void DownloadAlbumBG_DoWork(object sender, DoWorkEventArgs e)
        {
            // Empty screen output, then say Grabbing info.
            output.Invoke(new Action(() => output.Text = String.Empty));
            AddDownloadLogLine($"Grabbing Album info...{Environment.NewLine}", true, true);

            // Set "basePath" as the selected path.
            String downloadBasePath = folderBrowserDialog.SelectedPath;

            try
            {
                // Get Album model object
                Album qobuzAlbum = ExecuteApiCall(apiService => apiService.GetAlbum(DowloadItemID, true));

                // If API call failed, abort
                if (qobuzAlbum == null) { EnableControlsAfterDownload(); return; }

                AddDownloadLogLine($"Album \"{qobuzAlbum.Title}\" found. Starting Downloads...", true, true);
                AddEmptyDownloadLogLine(true, true);

                bool albumDownloaded = DownloadAlbum(qobuzAlbum, downloadBasePath);

                AddEmptyDownloadLogLine(true, true);

                if (albumDownloaded) {
                    // Say that downloading is completed.
                    AddDownloadLogLine("Download job completed! All downloaded files will be located in your chosen path.", true, true);
                } else
                {
                    // Say that downloading job is completed with errors.
                    AddDownloadLogLine("Download job completed with warnings and/or errors! Some or all files could be missing!", true, true);
                }

                EnableControlsAfterDownload();
            }
            catch (Exception downloadEx)
            {
                LogDownloadTaskException("Album", downloadEx);
            }
        }

        // For downloading "artist" links [MOSTLY WORKING]
        private void DownloadDiscogBG_DoWork(object sender, DoWorkEventArgs e)
        {
            // Set "basePath" as the selected path.
            String artistBasePath = folderBrowserDialog.SelectedPath;

            // Empty output, then say Grabbing IDs.
            output.Invoke(new Action(() => output.Text = String.Empty));
            AddDownloadLogLine("Grabbing Artist Album IDs...", true, true);

            try
            {
                // Get Artist model object
                Artist qobuzArtist = ExecuteApiCall(apiService => apiService.GetArtist(DowloadItemID, true, "albums", "release_desc", 999999));

                // If API call failed, abort
                if (qobuzArtist == null) { EnableControlsAfterDownload(); return; }

                DownloadAlbums(artistBasePath, qobuzArtist.Albums.Items);
            }
            catch (Exception downloadEx)
            {
                output.Invoke(new Action(() => output.Text = String.Empty));
                LogDownloadTaskException("Artist", downloadEx);
            }
        }

        // For downloading "label" links [IN DEV]
        private void DownloadLabelBG_DoWork(object sender, DoWorkEventArgs e)
        {
            // Set "basePath" as the selected path + "/- Labels".
            string labelBasePath = Path.Combine(folderBrowserDialog.SelectedPath, "- Labels");

            // Empty output, then say Grabbing IDs.
            output.Invoke(new Action(() => output.Text = String.Empty));
            AddDownloadLogLine("Grabbing Label Album IDs...", true, true);

            try
            {
                // Get Label model object
                QobuzApiSharp.Models.Content.Label qobuzLabel = ExecuteApiCall(apiService => apiService.GetLabel(DowloadItemID, true, "albums", 999999));

                // If API call failed, abort
                if (qobuzLabel == null) { EnableControlsAfterDownload(); return; }

                // Add Label name to basePath
                string safeLabelName = StringTools.GetSafeFilename(StringTools.DecodeEncodedNonAsciiCharacters(qobuzLabel.Name));
                labelBasePath = Path.Combine(labelBasePath, safeLabelName);

                DownloadAlbums(labelBasePath, qobuzLabel.Albums.Items);
            }
            catch (Exception downloadEx)
            {
                output.Invoke(new Action(() => output.Text = String.Empty));
                LogDownloadTaskException("Label", downloadEx);
            }
        }

        // For downloading "favorites" (Albums only at the moment) [IN DEV]

        // Favorite Albums
        private void DownloadFaveAlbumsBG_DoWork(object sender, DoWorkEventArgs e)
        {
            // Set "basePath" as the selected path + "/- Favorites".
            string favoritesBasePath = Path.Combine(folderBrowserDialog.SelectedPath, "- Favorites");

            // Empty output, then say Grabbing IDs.
            output.Invoke(new Action(() => output.Text = String.Empty));
            AddDownloadLogLine("Grabbing Favorite Album IDs...", true, true);

            try
            {
                // Get UserFavorites model object
                UserFavorites qobuzUserFavorites = ExecuteApiCall(apiService => apiService.GetUserFavorites(DowloadItemID, "albums", 999999));

                // If API call failed, abort
                if (qobuzUserFavorites == null) { EnableControlsAfterDownload(); return; }

                DownloadAlbums(favoritesBasePath, qobuzUserFavorites.Albums.Items);
            }
            catch (Exception downloadEx)
            {
                output.Invoke(new Action(() => output.Text = String.Empty));
                LogDownloadTaskException("Favorite Albums", downloadEx);
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

            // Empty screen output, then say Grabbing info.
            output.Invoke(new Action(() => output.Text = String.Empty));
            AddDownloadLogLine("Grabbing Playlist info...", true, true);
            AddEmptyDownloadLogLine(true, true);

            try
            {
                // Get Playlist model object
                Playlist qobuzPlaylist = ExecuteApiCall(apiService => apiService.GetPlaylist(DowloadItemID, true, "tracks", 10000));

                // If API call failed, abort
                if (qobuzPlaylist == null) { EnableControlsAfterDownload(); return; }

                AddDownloadLogLine($"Playlist \"{qobuzPlaylist.Name}\" found. Starting Downloads...", true, true);
                AddEmptyDownloadLogLine(true, true);

                // Create Playlist root directory.
                string playlistNamePath = StringTools.GetSafeFilename(StringTools.DecodeEncodedNonAsciiCharacters(qobuzPlaylist.Name));
                playlistNamePath = StringTools.TrimToMaxLength(playlistNamePath, MaxLength);
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
                        AddDownloadErrorLogLines(new string[] { "Error downloading full size playlist cover image file.", ex.Message, "\r\n" });
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
                AddEmptyDownloadLogLine(true, true);
                AddDownloadLogLine("Download job completed! All downloaded files will be located in your chosen path.", true, true);
                EnableControlsAfterDownload();

            }
            catch (Exception downloadEx)
            {
                output.Invoke(new Action(() => output.Text = String.Empty)); 
                LogDownloadTaskException("Playlist", downloadEx);
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
            // Set ArtSize to selected value, and save selected option to settings.
            ArtSize = artSizeSelect.Text;
            Settings.Default.savedArtSize = artSizeSelect.SelectedIndex;
            Settings.Default.Save();
        }

        private void filenameTempSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Set filename template to selected value, and save selected option to settings.
            if (filenameTempSelect.SelectedIndex == 0)
            {
                FileNameTemplateString = " ";
            }
            else if (filenameTempSelect.SelectedIndex == 1)
            {
                FileNameTemplateString = " - ";
            }
            else
            {
                FileNameTemplateString = " ";
            }

            Settings.Default.savedFilenameTemplate = filenameTempSelect.SelectedIndex;
            Settings.Default.savedFilenameTemplateString = FileNameTemplateString;
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
            DevClickEggThingValue = DevClickEggThingValue + 1;

            if (DevClickEggThingValue >= 3)
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

            DevClickEggThingValue = 0;
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
            EnableControlsAfterDownload();
        }
    }
}