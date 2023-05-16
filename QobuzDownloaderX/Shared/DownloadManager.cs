using QobuzApiSharp.Exceptions;
using QobuzApiSharp.Models.Content;
using QobuzApiSharp.Service;
using QobuzDownloaderX.Models;
using QobuzDownloaderX.Models.Content;
using QobuzDownloaderX.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace QobuzDownloaderX.Shared
{
    public class DownloadManager
    {
        private readonly DownloadLogger logger;
        private CancellationTokenSource cancellationTokenSource;

        public delegate void DownloadTaskStatusChanged();
        public delegate void UpdateAlbumTagsUi(DownloadItemInfo downloadInfo);
        private readonly UpdateAlbumTagsUi UpdateAlbumUiTags;
        public delegate void UpdateDownloadSpeed(string speed);
        private readonly UpdateDownloadSpeed UpdateUiDownloadSpeed;

        public DownloadItemInfo DownloadInfo { get; private set; }

        public DownloadItemPaths DownloadPaths { get; private set; }

        public bool Buzy { get; private set; }

        public bool CheckIfStreamable { get; set; }

        public DownloadManager (DownloadLogger logger, UpdateAlbumTagsUi updateAlbumTagsUi, UpdateDownloadSpeed updateUiDownloadSpeed)
        {
            Buzy = false;
            CheckIfStreamable = true;
            this.logger = logger;
            UpdateUiDownloadSpeed = updateUiDownloadSpeed;
            UpdateAlbumUiTags = updateAlbumTagsUi;
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

                logger.AddEmptyDownloadLogLine(false, true);
                logger.AddDownloadLogErrorLine($"Communication problem with Qobuz API. Details saved to error log{Environment.NewLine}", true, true);

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
                logger.AddDownloadErrorLogLines(errorLines);
            }

            return default;
        }

        public void StopDownloadTask()
        {
            this.cancellationTokenSource?.Cancel();
        }

        public bool IsStreamable(Track qobuzTrack, bool inPlaylist = false)
        {
            bool tryToStream = true;

            if (qobuzTrack.Streamable == false)
            {
                switch (CheckIfStreamable)
                {
                    case true:
                        string trackReference;

                        if (inPlaylist)
                        {
                            trackReference = $"{qobuzTrack.Performer?.Name} - {qobuzTrack.Title}";
                        }
                        else
                        {
                            trackReference = $"{qobuzTrack.TrackNumber.GetValueOrDefault()} {StringTools.DecodeEncodedNonAsciiCharacters(qobuzTrack.Title.Trim())}";
                        }

                        logger.AddDownloadLogLine($"Track {trackReference} is not available for streaming. Unable to download.\r\n", true, true);
                        tryToStream = false;
                        break;

                    default:
                        logger.AddDownloadLogLine("Track is not available for streaming. But streamable check is being ignored for debugging, or messed up releases. Attempting to download...\r\n", tryToStream, tryToStream);
                        break;
                }
            }

            return tryToStream;
        }

        public async Task DownloadFileAsync(HttpClient httpClient, string downloadUrl, string filePath)
        {
            using (Stream streamToReadFrom = await httpClient.GetStreamAsync(downloadUrl))
            {
                using (FileStream streamToWriteTo = System.IO.File.Create(filePath))
                {
                    long totalBytesRead = 0;
                    DateTime startTime = DateTime.Now;
                    DateTime lastUpdateTime = DateTime.Now;
                    byte[] buffer = new byte[8192]; // Use an 8KB buffer size for copying data
                    bool firstBufferRead = false;

                    int bytesRead;
                    while ((bytesRead = await streamToReadFrom.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        // Write only the minimum of buffer.Length and bytesRead bytes to the file
                        await streamToWriteTo.WriteAsync(buffer, 0, Math.Min(buffer.Length, bytesRead));

                        // Calculate download speed
                        totalBytesRead += bytesRead;
                        double speed = (totalBytesRead / 1024d / 1024d) / DateTime.Now.Subtract(startTime).TotalSeconds;

                        // Update the downloadSpeedLabel with the current speed at download start and then max. every 100 ms, with 3 decimal places
                        if (!firstBufferRead || DateTime.Now.Subtract(lastUpdateTime).TotalMilliseconds >= 100)
                        {
                            UpdateUiDownloadSpeed.Invoke($"Downloading... {speed:F3} MB/s");
                            lastUpdateTime = DateTime.Now;
                        }

                        firstBufferRead = true;
                    }
                }
            }

            // After download completes successfully
            UpdateUiDownloadSpeed.Invoke("Idle");
        }

        private async Task<bool> DownloadTrackAsync(CancellationToken cancellationToken, Track qobuzTrack, string basePath, bool isPartOfTracklist, bool isPartOfAlbum, bool removeTagArtFileAfterDownload = false, string albumPathSuffix = "")
        {
            // Just for good measure...
            // User requested task cancellation!
            cancellationToken.ThrowIfCancellationRequested();

            string trackIdString = qobuzTrack.Id.GetValueOrDefault().ToString();

            DownloadInfo.SetTrackTaggingInfo(qobuzTrack);

            // If track is downloaded as part of Album, Album related processings should already be done.
            // Only handle Album related processing when downloading a single track.
            if (!isPartOfAlbum)
            {
                // Get all album information and update UI fields via callback
                DownloadInfo.SetAlbumDownloadInfo(qobuzTrack.Album);
                UpdateAlbumUiTags.Invoke(DownloadInfo);
            }

            // Check if available for streaming.
            if (!IsStreamable(qobuzTrack)) return false;

            // Create directories if they don't exist yet
            // Add Album ID to Album Path if requested (to avoid conflicts for similar albums with trimmed long names)
            CreateTrackDirectories(basePath, DownloadPaths.QualityPath, albumPathSuffix, isPartOfTracklist);

            // Set trackPath to the created directories
            string trackPath = DownloadInfo.CurrentDownloadPaths.Path4Full;

            // Create padded track number string with minimum of 2 integer positions based on number of total tracks
            string paddedTrackNumber = DownloadInfo.TrackNumber.ToString().PadLeft(Math.Max(2, (int)Math.Floor(Math.Log10(DownloadInfo.TrackTotal) + 1)), '0');

            // Create full track filename
            if (isPartOfTracklist)
            {
                DownloadPaths.FinalTrackNamePath = string.Concat(DownloadPaths.PerformerNamePath, Globals.FileNameTemplateString, DownloadPaths.TrackNamePath).TrimEnd();
            }
            else
            {
                DownloadPaths.FinalTrackNamePath = string.Concat(paddedTrackNumber, Globals.FileNameTemplateString, DownloadPaths.TrackNamePath).TrimEnd();
            }

            // Shorten full filename if over MaxLength to avoid errors with file names being too long
            DownloadPaths.FinalTrackNamePath = StringTools.TrimToMaxLength(DownloadPaths.FinalTrackNamePath, Globals.MaxLength);

            // Check if the file already exists
            string checkFile = Path.Combine(trackPath, DownloadPaths.FinalTrackNamePath + Globals.AudioFileType);

            if (System.IO.File.Exists(checkFile))
            {
                string message = $"File for \"{DownloadPaths.FinalTrackNamePath}\" already exists. Skipping.\r\n";
                logger.AddDownloadLogLine(message, true, true);
                return false;
            }

            // Notify UI of starting track download.
            logger.AddDownloadLogLine($"Downloading - {DownloadPaths.FinalTrackNamePath} ...... ", true, true);

            // Get track streaming URL, abort if failed.
            string streamUrl = ExecuteApiCall(apiService => apiService.GetTrackFileUrl(trackIdString, Globals.FormatIdString))?.Url;

            if (string.IsNullOrEmpty(streamUrl))
            {
                // Can happen with free accounts trying to download non-previewable tracks (or if API call failed). 
                logger.AddDownloadLogLine($"Couldn't get streaming URL for Track \"{DownloadPaths.FinalTrackNamePath}\". Skipping.\r\n", true, true);
                return false;
            }

            try
            {
                // Create file path strings
                string filePath = Path.Combine(trackPath, DownloadPaths.FinalTrackNamePath + Globals.AudioFileType);
                string coverArtFilePath = Path.Combine(DownloadPaths.Path3Full, "Cover.jpg");
                string coverArtTagFilePath = Path.Combine(DownloadPaths.Path3Full, Globals.TaggingOptions.ArtSize + ".jpg");

                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", Globals.USER_AGENT);

                    // Save streamed file from link
                    await DownloadFileAsync(httpClient, streamUrl, filePath);

                    // Download selected cover art size for tagging files (if not exists)
                    if (!System.IO.File.Exists(coverArtTagFilePath))
                    {
                        try
                        {
                            await DownloadFileAsync(httpClient, DownloadInfo.FrontCoverImgTagUrl, coverArtTagFilePath);
                        }
                        catch (Exception ex)
                        {
                            // Qobuz servers throw a 404 as if the image doesn't exist.
                            logger.AddDownloadErrorLogLines(new string[] { "Error downloading image file for tagging.", ex.Message, Environment.NewLine });
                        }
                    }

                    // Download max quality Cover Art to "Cover.jpg" file in chosen path (if not exists & not part of playlist).
                    if (!isPartOfTracklist && !System.IO.File.Exists(coverArtFilePath))
                    {
                        try
                        {
                            await DownloadFileAsync(httpClient, DownloadInfo.FrontCoverImgUrl, coverArtFilePath);
                        }
                        catch (Exception ex)
                        {
                            // Qobuz servers throw a 404 as if the image doesn't exist.
                            logger.AddDownloadErrorLogLines(new string[] { "Error downloading full size cover image file.", ex.Message, Environment.NewLine });
                        }
                    }
                }

                // Tag metadata to downloaded track.
                AudioFileTagger.AddMetaDataTags(DownloadInfo, filePath, coverArtTagFilePath, logger);

                // Remove temp tagging art file if requested and exists.
                if (removeTagArtFileAfterDownload && System.IO.File.Exists(coverArtTagFilePath))
                {
                    System.IO.File.Delete(coverArtTagFilePath);
                }

                logger.AddDownloadLogLine("Track Download Done!\r\n", true, true);
                System.Threading.Thread.Sleep(100);
            }
            catch (AggregateException ae)
            {
                // When a Task fails, an AggregateException is thrown. Could be a HttpClient timeout or network error.
                logger.AddDownloadLogErrorLine($"Track Download canceled, probably due to network error or request timeout. Details saved to error log.{Environment.NewLine}", true, true);

                logger.AddDownloadErrorLogLine("Track Download canceled, probably due to network error or request timeout.");
                logger.AddDownloadErrorLogLine(ae.ToString());
                logger.AddDownloadErrorLogLine(Environment.NewLine);
                return false;
            }
            catch (Exception downloadEx)
            {
                // If there is an unknown issue trying to, or during the download, show and log error info.
                logger.AddDownloadLogErrorLine($"Unknown error during Track Download. Details saved to error log.{Environment.NewLine}", true, true);

                logger.AddDownloadErrorLogLine("Unknown error during Track Download.");
                logger.AddDownloadErrorLogLine(downloadEx.ToString());
                logger.AddDownloadErrorLogLine(Environment.NewLine);
                return false;
            }

            return true;
        }

        private async Task<bool> DownloadAlbumAsync(CancellationToken cancellationToken, Album qobuzAlbum, string basePath, bool loadTracks = false, string albumPathSuffix = "")
        {
            bool noErrorsOccured = true;

            if (loadTracks)
            {
                // Get Album model object with tracks for when tracks aren't already loaded in Album model
                qobuzAlbum = ExecuteApiCall(apiService => apiService.GetAlbum(qobuzAlbum.Id, true));
            }

            // If API call failed or empty Album was provided, abort Album Download
            if (qobuzAlbum == null || string.IsNullOrEmpty(qobuzAlbum.Id)) { return false; }

            // Get all album information and update UI fields via callback
            DownloadInfo.SetAlbumDownloadInfo(qobuzAlbum);
            UpdateAlbumUiTags.Invoke(DownloadInfo);

            // Download all tracks of the Album, clean albumArt tag file after last track
            int trackCount = qobuzAlbum.TracksCount ?? 0;

            for (int i = 0; i < trackCount; i++)
            {
                // User requested task cancellation!
                cancellationToken.ThrowIfCancellationRequested();

                Track qobuzTrack = qobuzAlbum.Tracks.Items[i];

                // Nested Album objects in Tracks are not always fully populated, inject current qobuzAlbum in Track to be downloaded
                qobuzTrack.Album = qobuzAlbum;

                if (!await DownloadTrackAsync(cancellationToken, qobuzTrack, basePath, false, true, i == trackCount - 1, albumPathSuffix)) noErrorsOccured = false;
            }

            // Look for digital booklet(s) in "Goodies"
            // Don't fail on failed "Goodies" downloads, just log...
            if (!await DownloadBookletsAsync(qobuzAlbum, DownloadPaths.Path3Full)) noErrorsOccured = false;

            return noErrorsOccured;
        }

        private async Task<bool> DownloadBookletsAsync(Album qobuzAlbum, string basePath)
        {
            bool noErrorsOccured = true;

            List<Goody> booklets = qobuzAlbum.Goodies?.Where(g => g.FileFormatId == (int)GoodiesFileType.BOOKLET).ToList();

            if (booklets?.Any() == false)
            {
                // No booklets found, just return
                return noErrorsOccured;
            }

            logger.AddDownloadLogLine($"Goodies found, downloading...{Environment.NewLine}", true, true);

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
                        logger.AddDownloadLogLine($"Booklet file for \"{bookletFileName}\" already exists. Skipping.{Environment.NewLine}", true, true);
                    }
                    else
                    {
                        // When a booklet download fails, mark error occured but continue downloading others if they exist 
                        if (!await DownloadBookletAsync(booklet, httpClient, bookletFileName, bookletFilePath)) noErrorsOccured = false;
                    }

                    counter++;
                }
            }

            return noErrorsOccured;
        }

        private async Task<bool> DownloadBookletAsync(Goody booklet, HttpClient httpClient, string fileName, string filePath)
        {
            bool noErrorsOccured = true;

            try
            {
                // Download booklet
                await DownloadFileAsync(httpClient, booklet.Url, filePath);

                logger.AddDownloadLogLine($"Booklet \"{fileName}\" download complete!{Environment.NewLine}", true, true);
            }
            catch (AggregateException ae)
            {
                // When a Task fails, an AggregateException is thrown. Could be a HttpClient timeout or network error.
                logger.AddDownloadLogErrorLine($"Goodies Download canceled, probably due to network error or request timeout. Details saved to error log.{Environment.NewLine}", true, true);

                logger.AddDownloadErrorLogLine("Goodies Download canceled, probably due to network error or request timeout.");
                logger.AddDownloadErrorLogLine(ae.ToString());
                logger.AddDownloadErrorLogLine(Environment.NewLine);
                noErrorsOccured = false;
            }
            catch (Exception downloadEx)
            {
                // If there is an unknown issue trying to, or during the download, show and log error info.
                logger.AddDownloadLogErrorLine($"Unknown error during Goodies Download. Details saved to error log.{Environment.NewLine}", true, true);

                logger.AddDownloadErrorLogLine("Unknown error during Goodies Download.");
                logger.AddDownloadErrorLogLine(downloadEx.ToString());
                logger.AddDownloadErrorLogLine(Environment.NewLine);
                noErrorsOccured = false;
            }

            return noErrorsOccured;
        }

        private async Task<bool> DownloadAlbumsAsync(CancellationToken cancellationToken, string basePath, List<Album> albums, bool isEndOfDownloadJob)
        {
            bool noAlbumErrorsOccured = true;

            foreach (Album qobuzAlbum in albums)
            {
                // User requested task cancellation!
                cancellationToken.ThrowIfCancellationRequested();

                // Empty output, then say Starting Downloads.
                logger.ClearUiLogComponent();
                logger.AddEmptyDownloadLogLine(true, false);
                logger.AddDownloadLogLine($"Starting Downloads for album \"{qobuzAlbum.Title}\" with ID: <{qobuzAlbum.Id}>...", true, true);
                logger.AddEmptyDownloadLogLine(true, true);

                bool albumDownloadOK = await DownloadAlbumAsync(cancellationToken, qobuzAlbum, basePath, true, $" [{qobuzAlbum.Id}]");

                // If album download failed, mark error occured and continue
                if (!albumDownloadOK) noAlbumErrorsOccured = false;
            }

            if (isEndOfDownloadJob)
            {
                logger.LogFinishedDownloadJob(noAlbumErrorsOccured);
            }

            return noAlbumErrorsOccured;
        }

        public async Task StartDownloadItemTaskAsync(DownloadItem downloadItem, DownloadTaskStatusChanged downloadStartedCallback, DownloadTaskStatusChanged downloadStoppedCallback)
        {
            // Create new cancellation token source.
            using (this.cancellationTokenSource = new CancellationTokenSource())
            {
                Buzy = true;

                try
                {
                    downloadStartedCallback?.Invoke();

                    // Link should be valid here, start new download log
                    logger.DownloadLogPath = Path.Combine(Globals.LoggingDir, $"Download_Log_{DateTime.Now:yyyy-MM-dd_HH.mm.ss.fff}.log");

                    string logLine = $"Downloading <{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(downloadItem.Type)}> from {downloadItem.Url}";
                    logger.AddDownloadLogLine(new string('=', logLine.Length).PadRight(logLine.Length), true);
                    logger.AddDownloadLogLine(logLine, true);
                    logger.AddDownloadLogLine(new string('=', logLine.Length).PadRight(logLine.Length), true);
                    logger.AddEmptyDownloadLogLine(true);

                    DownloadInfo = new DownloadItemInfo
                    {
                        DowloadItemID = downloadItem.Id
                    };
                    DownloadPaths = DownloadInfo.CurrentDownloadPaths;

                    switch (downloadItem.Type)
                    {
                        case "track":
                            await StartDownloadTrackTaskAsync(cancellationTokenSource.Token);
                            break;
                        case "album":
                            await StartDownloadAlbumTaskAsync(cancellationTokenSource.Token);
                            break;

                        case "artist":
                            await StartDownloadArtistDiscogTaskAsync(cancellationTokenSource.Token);
                            break;

                        case "label":
                            await StartDownloadLabelTaskAsync(cancellationTokenSource.Token);
                            break;

                        case "user":
                            if (DownloadInfo.DowloadItemID == @"library/favorites/albums")
                            {
                                await StartDownloadFaveAlbumsTaskAsync(cancellationTokenSource.Token);
                            }
                            else if (DownloadInfo.DowloadItemID == @"library/favorites/artists")
                            {
                                await StartDownloadFaveArtistsTaskAsync(cancellationTokenSource.Token);
                            }
                            else if (DownloadInfo.DowloadItemID == @"library/favorites/tracks")
                            {
                                await StartDownloadFaveTracksTaskAsync(cancellationTokenSource.Token);
                            }
                            else
                            {
                                logger.ClearUiLogComponent();
                                logger.AddDownloadLogLine($"You entered an invalid user favorites link.{Environment.NewLine}", true, true);
                                logger.AddDownloadLogLine($"Favorite Tracks, Albums & Artists are supported with the following links:{Environment.NewLine}", true, true);
                                logger.AddDownloadLogLine($"Tracks - https://play.qobuz.com/user/library/favorites/tracks{Environment.NewLine}", true, true);
                                logger.AddDownloadLogLine($"Albums - https://play.qobuz.com/user/library/favorites/albums{Environment.NewLine}", true, true);
                                logger.AddDownloadLogLine($"Artists - https://play.qobuz.com/user/library/favorites/artists{Environment.NewLine}", true, true);
                            }
                            break;

                        case "playlist":
                            await StartDownloadPlaylistTaskAsync(cancellationTokenSource.Token);
                            break;
                        default:
                            // We shouldn't get here?!? I'll leave this here just in case...
                            logger.ClearUiLogComponent();
                            logger.AddDownloadLogLine("URL not understood. Is there a typo?", true, true);
                            break;
                    }
                }
                catch (OperationCanceledException)
                {
                    // Handle cancellation.
                    logger.AddEmptyDownloadLogLine(true, true);
                    logger.AddDownloadLogLine("Download stopped by user!", true, true);
                }
                finally
                {
                    downloadStoppedCallback?.Invoke();
                    Buzy = false;
                }
            }
        }

        // For downloading "track" links
        private async Task StartDownloadTrackTaskAsync(CancellationToken cancellationToken)
        {
            // Empty screen output, then say Grabbing info.
            logger.ClearUiLogComponent();
            logger.AddDownloadLogLine($"Grabbing Track info...{Environment.NewLine}", true, true);

            // Set "basePath" as the selected path.
            string downloadBasePath = Settings.Default.savedFolder;

            try
            {
                Track qobuzTrack = ExecuteApiCall(apiService => apiService.GetTrack(DownloadInfo.DowloadItemID, true));

                // If API call failed, abort
                if (qobuzTrack == null) { return; }

                logger.AddDownloadLogLine($"Track \"{qobuzTrack.Title}\" found. Starting Download...", true, true);
                logger.AddEmptyDownloadLogLine(true, true);

                bool fileDownloaded = await DownloadTrackAsync(cancellationToken, qobuzTrack, downloadBasePath, true, false, true);

                // If download failed, abort
                if (!fileDownloaded) { return; }

                // Say that downloading is completed.
                logger.AddEmptyDownloadLogLine(true, true);
                logger.AddDownloadLogLine("Download job completed! All downloaded files will be located in your chosen path.", true, true);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Rethrow when cancellation requested by user.
                throw;
            }
            catch (Exception downloadEx)
            {
                logger.LogDownloadTaskException("Track", downloadEx);
            }
        }

        // For downloading "album" links
        private async Task StartDownloadAlbumTaskAsync(CancellationToken cancellationToken)
        {
            // Empty screen output, then say Grabbing info.
            logger.ClearUiLogComponent();
            logger.AddDownloadLogLine($"Grabbing Album info...{Environment.NewLine}", true, true);

            // Set "basePath" as the selected path.
            String downloadBasePath = Settings.Default.savedFolder;

            try
            {
                // Get Album model object
                Album qobuzAlbum = ExecuteApiCall(apiService => apiService.GetAlbum(DownloadInfo.DowloadItemID, true));

                // If API call failed, abort
                if (qobuzAlbum == null) { return; }

                logger.AddDownloadLogLine($"Album \"{qobuzAlbum.Title}\" found. Starting Downloads...", true, true);
                logger.AddEmptyDownloadLogLine(true, true);

                logger.LogFinishedDownloadJob(await DownloadAlbumAsync(cancellationToken, qobuzAlbum, downloadBasePath));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Rethrow when cancellation requested by user.
                throw;
            }
            catch (Exception downloadEx)
            {
                logger.LogDownloadTaskException("Album", downloadEx);
            }
        }

        // For downloading "artist" links
        private async Task StartDownloadArtistDiscogTaskAsync(CancellationToken cancellationToken)
        {
            // Set "basePath" as the selected path.
            String artistBasePath = Settings.Default.savedFolder;

            // Empty output, then say Grabbing IDs.
            logger.ClearUiLogComponent();
            logger.AddDownloadLogLine("Grabbing Artist info...", true, true);

            try
            {
                // Get Artist model object
                Artist qobuzArtist = ExecuteApiCall(apiService => apiService.GetArtist(DownloadInfo.DowloadItemID, true, "albums", "release_desc", 999999));

                // If API call failed, abort
                if (qobuzArtist == null) { return; }

                logger.AddDownloadLogLine($"Starting Downloads for artist \"{qobuzArtist.Name}\" with ID: <{qobuzArtist.Id}>...", true, true);

                await DownloadAlbumsAsync(cancellationToken, artistBasePath, qobuzArtist.Albums.Items, true);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Rethrow when cancellation requested by user.
                throw;
            }
            catch (Exception downloadEx)
            {
                logger.ClearUiLogComponent();
                logger.LogDownloadTaskException("Artist", downloadEx);
            }
        }

        // For downloading "label" links
        private async Task StartDownloadLabelTaskAsync(CancellationToken cancellationToken)
        {
            // Set "basePath" as the selected path + "/- Labels".
            string labelBasePath = Path.Combine(Settings.Default.savedFolder, "- Labels");

            // Empty output, then say Grabbing IDs.
            logger.ClearUiLogComponent();
            logger.AddDownloadLogLine("Grabbing Label info...", true, true);

            try
            {
                // Get Label model object
                QobuzApiSharp.Models.Content.Label qobuzLabel = ExecuteApiCall(apiService => apiService.GetLabel(DownloadInfo.DowloadItemID, true, "albums", 999999));

                // If API call failed, abort
                if (qobuzLabel == null) { return; }

                logger.AddDownloadLogLine($"Starting Downloads for label \"{qobuzLabel.Name}\" with ID: <{qobuzLabel.Id}>...", true, true);

                // Add Label name to basePath
                string safeLabelName = StringTools.GetSafeFilename(StringTools.DecodeEncodedNonAsciiCharacters(qobuzLabel.Name));
                labelBasePath = Path.Combine(labelBasePath, safeLabelName);

                await DownloadAlbumsAsync(cancellationToken, labelBasePath, qobuzLabel.Albums.Items, true);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Rethrow when cancellation requested by user.
                throw;
            }
            catch (Exception downloadEx)
            {
                logger.ClearUiLogComponent();
                logger.LogDownloadTaskException("Label", downloadEx);
            }
        }

        // For downloading "favorites"

        // Favorite Albums
        private async Task StartDownloadFaveAlbumsTaskAsync(CancellationToken cancellationToken)
        {
            // Set "basePath" as the selected path + "/- Favorites".
            string favoritesBasePath = Path.Combine(Settings.Default.savedFolder, "- Favorites");

            // Empty output, then say Grabbing IDs.
            logger.ClearUiLogComponent();
            logger.AddDownloadLogLine("Grabbing Favorite Album IDs...", true, true);

            try
            {
                // Get UserFavorites model object
                UserFavorites qobuzUserFavorites = ExecuteApiCall(apiService => apiService.GetUserFavorites(DownloadInfo.DowloadItemID, "albums", 999999));

                // If API call failed, abort
                if (qobuzUserFavorites == null) { return; }

                await DownloadAlbumsAsync(cancellationToken, favoritesBasePath, qobuzUserFavorites.Albums.Items, true);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Rethrow when cancellation requested by user.
                throw;
            }
            catch (Exception downloadEx)
            {
                logger.ClearUiLogComponent();
                logger.LogDownloadTaskException("Favorite Albums", downloadEx);
            }
        }

        // Favorite Artists
        private async Task StartDownloadFaveArtistsTaskAsync(CancellationToken cancellationToken)
        {
            // Set "basePath" as the selected path + "/- Favorites".
            string favoritesBasePath = Path.Combine(Settings.Default.savedFolder, "- Favorites");

            // Empty output, then say Grabbing IDs.
            logger.ClearUiLogComponent();
            logger.AddDownloadLogLine("Grabbing Favorite Artists...", true, true);

            try
            {
                bool noArtistErrorsOccured = true;

                // Get UserFavorites model object
                UserFavorites qobuzUserFavorites = ExecuteApiCall(apiService => apiService.GetUserFavorites(DownloadInfo.DowloadItemID, "artists", 999999));

                // If API call failed, abort
                if (qobuzUserFavorites == null) { return; }

                // If user has no favorite artists, log and abort
                if (qobuzUserFavorites.Artists?.Total == 0)
                {
                    logger.AddEmptyDownloadLogLine(true, true);
                    logger.AddDownloadLogLine("No favorite artists found, nothing to download.", true, true);
                    return;
                }

                foreach (Artist favoriteArtist in qobuzUserFavorites.Artists.Items)
                {
                    // User requested task cancellation!
                    cancellationToken.ThrowIfCancellationRequested();

                    // Get Artist model object
                    Artist qobuzArtist = ExecuteApiCall(apiService => apiService.GetArtist(favoriteArtist.Id.ToString(), true, "albums", "release_desc", 999999));

                    // If API call failed, mark artist error occured and continue with next artist
                    if (qobuzArtist == null) { noArtistErrorsOccured = false; continue; }

                    logger.AddEmptyDownloadLogLine(true, true);
                    logger.AddDownloadLogLine($"Starting Downloads for artist \"{qobuzArtist.Name}\" with ID: <{qobuzArtist.Id}>...", true, true);

                    // If albums download failed, mark artist error occured and continue with next artist
                    if (!await DownloadAlbumsAsync(cancellationToken, favoritesBasePath, qobuzArtist.Albums.Items, false)) noArtistErrorsOccured = false;
                }

                logger.LogFinishedDownloadJob(noArtistErrorsOccured);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Rethrow when cancellation requested by user.
                throw;
            }
            catch (Exception downloadEx)
            {
                logger.ClearUiLogComponent();
                logger.LogDownloadTaskException("Favorite Albums", downloadEx);
            }
        }

        // Favorite Tracks
        private async Task StartDownloadFaveTracksTaskAsync(CancellationToken cancellationToken)
        {
            // Set "basePath" as the selected path + "/- Favorites".
            string favoriteTracksBasePath = Path.Combine(Settings.Default.savedFolder, "- Favorites");

            // Empty screen output, then say Grabbing info.
            logger.ClearUiLogComponent();
            logger.AddDownloadLogLine("Grabbing Favorite Tracks...", true, true);
            logger.AddEmptyDownloadLogLine(true, true);

            try
            {
                bool noTrackErrorsOccured = true;

                // Get UserFavoritesIds model object, getting Id's allows more results.
                UserFavoritesIds qobuzUserFavoritesIds = ExecuteApiCall(apiService => apiService.GetUserFavoriteIds(DownloadInfo.DowloadItemID, "tracks", 999999));

                // If API call failed, abort
                if (qobuzUserFavoritesIds == null) { return; }

                // If user has no favorite tracks, log and abort
                if (qobuzUserFavoritesIds.Tracks?.Count == 0)
                {
                    logger.AddDownloadLogLine("No favorite tracks found, nothing to download.", true, true);
                    return;
                }

                logger.AddDownloadLogLine("Favorite tracks found. Starting Downloads...", true, true);
                logger.AddEmptyDownloadLogLine(true, true);

                // Download favorite tracks
                foreach (int favoriteTrackId in qobuzUserFavoritesIds.Tracks)
                {
                    // User requested task cancellation!
                    cancellationToken.ThrowIfCancellationRequested();

                    Track qobuzTrack = ExecuteApiCall(apiService => apiService.GetTrack(favoriteTrackId.ToString(), true));

                    // If API call failed, log and continue with next track
                    if (qobuzTrack == null) { noTrackErrorsOccured = false; continue; }

                    if (! await DownloadTrackAsync(cancellationToken, qobuzTrack, favoriteTracksBasePath, true, false, true)) noTrackErrorsOccured = false;
                }

                logger.LogFinishedDownloadJob(noTrackErrorsOccured);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Rethrow when cancellation requested by user.
                throw;
            }
            catch (Exception downloadEx)
            {
                logger.ClearUiLogComponent();
                logger.LogDownloadTaskException("Playlist", downloadEx);
            }
        }

        // For downloading "playlist" links
        private async Task StartDownloadPlaylistTaskAsync(CancellationToken cancellationToken)
        {
            // Set "basePath" as the selected path.
            String playlistBasePath = Settings.Default.savedFolder;

            // Empty screen output, then say Grabbing info.
            logger.ClearUiLogComponent();
            logger.AddDownloadLogLine("Grabbing Playlist info...", true, true);
            logger.AddEmptyDownloadLogLine(true, true);

            try
            {
                // Get Playlist model object
                Playlist qobuzPlaylist = ExecuteApiCall(apiService => apiService.GetPlaylist(DownloadInfo.DowloadItemID, true, "tracks", 10000));

                // If API call failed, abort
                if (qobuzPlaylist == null) { return; }

                logger.AddDownloadLogLine($"Playlist \"{qobuzPlaylist.Name}\" found. Starting Downloads...", true, true);
                logger.AddEmptyDownloadLogLine(true, true);

                // Create Playlist root directory.
                string playlistNamePath = StringTools.GetSafeFilename(StringTools.DecodeEncodedNonAsciiCharacters(qobuzPlaylist.Name));
                playlistNamePath = StringTools.TrimToMaxLength(playlistNamePath, Globals.MaxLength);
                playlistBasePath = Path.Combine(playlistBasePath, "- Playlists", playlistNamePath);
                System.IO.Directory.CreateDirectory(playlistBasePath);

                // Download Playlist cover art to "Playlist.jpg" in root directory (if not exists)
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
                        logger.AddDownloadErrorLogLines(new string[] { "Error downloading full size playlist cover image file.", ex.Message, "\r\n" });
                    }
                }

                bool noTrackErrorsOccured = true;

                // Download Playlist tracks
                foreach (Track playlistTrack in qobuzPlaylist.Tracks.Items)
                {
                    // User requested task cancellation!
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!IsStreamable(playlistTrack, true)) continue;

                    // Fetch full Track info
                    Track qobuzTrack = ExecuteApiCall(apiService => apiService.GetTrack(playlistTrack.Id.GetValueOrDefault().ToString(), true));

                    // If API call failed, log and continue with next track
                    if (qobuzTrack == null) { noTrackErrorsOccured = false; continue; }

                    if (! await DownloadTrackAsync(cancellationToken, qobuzTrack, playlistBasePath, true, false, true)) noTrackErrorsOccured = false;
                }

                logger.LogFinishedDownloadJob(noTrackErrorsOccured);

            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Rethrow when cancellation requested by user.
                throw;
            }
            catch (Exception downloadEx)
            {
                logger.ClearUiLogComponent();
                logger.LogDownloadTaskException("Playlist", downloadEx);
            }
        }

        public void CreateTrackDirectories(string basePath, string qualityPath, string albumPathSuffix = "", bool forTracklist = false)
        {
            if (forTracklist)
            {
                DownloadPaths.Path1Full = basePath;
                DownloadPaths.Path2Full = DownloadPaths.Path1Full;
                DownloadPaths.Path3Full = Path.Combine(basePath, qualityPath);
                DownloadPaths.Path4Full = DownloadPaths.Path3Full;
            }
            else
            {
                DownloadPaths.Path1Full = Path.Combine(basePath, DownloadPaths.AlbumArtistPath);
                DownloadPaths.Path2Full = Path.Combine(basePath, DownloadPaths.AlbumArtistPath, DownloadPaths.AlbumNamePath + albumPathSuffix);
                DownloadPaths.Path3Full = Path.Combine(basePath, DownloadPaths.AlbumArtistPath, DownloadPaths.AlbumNamePath + albumPathSuffix, qualityPath);

                // If more than 1 disc, create folders for discs. Otherwise, strings will remain null
                // Pad discnumber with minimum of 2 integer positions based on total number of disks
                if (DownloadInfo.DiscTotal > 1)
                {
                    // Create strings for disc folders
                    string discFolder = "CD " + DownloadInfo.DiscNumber.ToString().PadLeft(Math.Max(2, (int)Math.Floor(Math.Log10(DownloadInfo.DiscTotal) + 1)), '0');
                    DownloadPaths.Path4Full = Path.Combine(basePath, DownloadPaths.AlbumArtistPath, DownloadPaths.AlbumNamePath + albumPathSuffix, qualityPath, discFolder);
                }
                else
                {
                    DownloadPaths.Path4Full = DownloadPaths.Path3Full;
                }
            }

            System.IO.Directory.CreateDirectory(DownloadPaths.Path4Full);
        }
    }
}