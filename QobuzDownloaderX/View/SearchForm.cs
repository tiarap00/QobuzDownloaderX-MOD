using QobuzApiSharp.Exceptions;
using QobuzApiSharp.Models.Content;
using QobuzDownloaderX.Models.UI;
using QobuzDownloaderX.Shared;
using QobuzDownloaderX.View;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QobuzDownloaderX
{
    public partial class SearchForm : HeadlessForm
    {
        private readonly string errorLog = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Search_Errors.log");
        TableLayoutPanel resultsTableLayoutPanel;

        public SearchForm()
        {
            InitializeComponent();

            // Remove previous search error log
            if (System.IO.File.Exists(errorLog))
            {
                System.IO.File.Delete(errorLog);
            }

            searchTypeSelect.SelectedItem = "Album";
        }

        private void RemoveControls(Control userControl)
        {
            while (userControl.Controls.Count > 0)
            {
                Control control = userControl.Controls[0];
                if (control.HasChildren)
                {
                    RemoveControls(control); // Recursively remove and dispose all children
                }
                userControl.Controls.Remove(control);
                control.Dispose(); // Remove Control to clear from memory
            }
        }

        private void ResetResultsTableLayoutPanel()
        {
            if (resultsTableLayoutPanel != null)
            {
                containerScrollPanel.Controls.Clear();
                RemoveControls(resultsTableLayoutPanel);
                resultsTableLayoutPanel.Dispose();
            }

            resultsTableLayoutPanel = new TableLayoutPanel
            {
                Name = "resultsTableLayoutPanel",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.FromArgb(33, 33, 33),
                Location = new Point(0, 0),
                Size = new Size(894, 70),
                TabIndex = 4
            };
            resultsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            resultsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 613F));
            resultsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
            resultsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 101F));
            resultsTableLayoutPanel.CellPaint += ResultsTableLayoutPanel_CellPaint;

            containerScrollPanel.Controls.Add(resultsTableLayoutPanel);
        }

        private void ShowAndLogSearchResultError(Exception ex)
        {
            ResetResultsTableLayoutPanel();

            resultsTableLayoutPanel.RowCount++;
            resultsTableLayoutPanel.ColumnStyles.Clear();
            resultsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 914F));
            resultsTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            TextBox ErrorMesage = CreateTextBox($"{ex.Message}", true, GetResultRowColor(0), Color.OrangeRed, FontManager.CreateFont("Hanken Grotesk Medium", 10, FontStyle.Bold | FontStyle.Italic), BorderStyle.None);
            ResizeControlForText(ErrorMesage, 5);
            resultsTableLayoutPanel.Controls.Add(ErrorMesage, 0, 0);

            TextBox ErrorSavedMesage = CreateTextBox($"Error log saved to {errorLog}.", true, GetResultRowColor(0), Color.OrangeRed, FontManager.CreateFont("Hanken Grotesk Medium", 10, FontStyle.Bold | FontStyle.Italic), BorderStyle.None);
            ResizeControlForText(ErrorSavedMesage, 5);
            resultsTableLayoutPanel.Controls.Add(ErrorSavedMesage, 0, 1);

            List<string> errorLines = new List<string> { "", ex.Message };

            switch (ex)
            {
                case ApiErrorResponseException erEx:
                    errorLines.Add($"Failed API request: {erEx.RequestContent}");
                    errorLines.Add($"Api response code: {erEx.ResponseStatusCode}");
                    errorLines.Add($"Api response status: {erEx.ResponseStatus}");
                    errorLines.Add($"Api response reason: {erEx.ResponseReason} \r\n");
                    break;
                case ApiResponseParseErrorException pEx:
                    errorLines.Add($"Api response content: {pEx.ResponseContent} \r\n");
                    break;
            }

            errorLines.Add(ex.ToString());

            // Write detailed info to log
            System.IO.File.AppendAllLines(errorLog, errorLines);
        }

        private void ExitLabel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ExitLabel_MouseHover(object sender, EventArgs e)
        {
            exitLabel.ForeColor = Color.FromArgb(0, 112, 239);
        }

        private void ExitLabel_MouseLeave(object sender, EventArgs e)
        {
            exitLabel.ForeColor = Color.White;
        }

        private void Fill_AlbumResultsTablePanel(SearchResult searchResult)
        {
            FillResultsTablePanel(searchResult?.Albums?.Items, album => new SearchResultRow
            {
                ThumbnailUrl = album.Image.Thumbnail,
                Artist = album.Artist.Name,
                Title = StringTools.DecodeEncodedNonAsciiCharacters(album.Version != null ? $"{album.Title.TrimEnd()} ({album.Version})" : album.Title.TrimEnd()),
                FormattedDuration = StringTools.FormatDurationInSeconds(album.Duration.GetValueOrDefault()),
                FormattedQuality = $"{album.MaximumBitDepth}-Bit / {album.MaximumSamplingRate} kHz",
                WebPlayerUrl = $"{Globals.WEBPLAYER_BASE_URL}/album/{album.Id}",
                StoreUrl = album.Url,
                ReleaseYear = album.ReleaseDateOriginal.GetValueOrDefault().Year.ToString(),
                HiRes = album.Hires.GetValueOrDefault(),
                TrackCount = album.TracksCount.GetValueOrDefault()
            });
        }

        private void Fill_TrackResultsTablePanel(SearchResult searchResult)
        {
            FillResultsTablePanel(searchResult?.Tracks?.Items, track => new SearchResultRow
            {
                ThumbnailUrl = track.Album.Image.Thumbnail,
                Artist = track.Performer.Name,
                Title = StringTools.DecodeEncodedNonAsciiCharacters(track.Version != null ? $"{track.Title.TrimEnd()} ({track.Version})" : track.Title.TrimEnd()),
                FormattedDuration = StringTools.FormatDurationInSeconds(track.Duration.GetValueOrDefault()),
                FormattedQuality = $"{track.MaximumBitDepth}-Bit / {track.MaximumSamplingRate} kHz",
                WebPlayerUrl = $"{Globals.WEBPLAYER_BASE_URL}/track/{track.Id}",
                StoreUrl = track.Album.Url,
                ReleaseYear = track.Album.ReleaseDateOriginal.GetValueOrDefault().Year.ToString(),
                HiRes = track.Hires.GetValueOrDefault()
            });
        }

        private void FillResultsTablePanel<T>(IEnumerable<T> items, Func<T, SearchResultRow> createSearchResultRow)
        {
            ResetResultsTableLayoutPanel();

            if (items != null)
            {
                resultsTableLayoutPanel.SuspendLayout();
                foreach (var item in items)
                {
                    CreateResultRow(createSearchResultRow(item));
                }
                resultsTableLayoutPanel.ResumeLayout();
            }
        }

        public void CreateResultRow(SearchResultRow result)
        {
            resultsTableLayoutPanel.RowCount++;
            resultsTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            int currentRow = resultsTableLayoutPanel.RowCount - 1;

            Color rowColor = GetResultRowColor(currentRow);

            PictureBox thumbnail = CreateThumbnail(result.ThumbnailUrl, rowColor);
            resultsTableLayoutPanel.Controls.Add(thumbnail, 0, currentRow);

            TableLayoutPanel secondColumnPanel = CreateReleaseInfoColumn(result, rowColor);
            resultsTableLayoutPanel.Controls.Add(secondColumnPanel, 1, currentRow);

            TableLayoutPanel thirdColumnPanel = CreateQualityColumn(result, rowColor);
            resultsTableLayoutPanel.Controls.Add(thirdColumnPanel, 2, currentRow);

            Button selectButton = CreateDownloadButton(result.WebPlayerUrl);
            resultsTableLayoutPanel.Controls.Add(selectButton, 3, currentRow);
        }

        private Color GetResultRowColor(int currentRow)
        {
            return currentRow % 2 == 1 ? Color.FromArgb(33, 33, 33) : Color.FromArgb(45, 45, 45);
        }

        private PictureBox CreateThumbnail(string thumbnailUrl, Color rowColor)
        {
            return new PictureBox
            {
                ImageLocation = thumbnailUrl,
                SizeMode = PictureBoxSizeMode.CenterImage,
                Size = new Size(50, 50),
                Anchor = AnchorStyles.None,
                Dock = DockStyle.Fill,
                BackColor = rowColor
            };
        }

        private TableLayoutPanel CreateReleaseInfoColumn(SearchResultRow result, Color rowColor)
        {
            Font detailsFont = FontManager.CreateFont("Hanken Grotesk Medium", 10);
            TextBox title = CreateTextBox(result.Title, true, rowColor, Color.White, FontManager.CreateFont("Hanken Grotesk ExtraBold", 13, FontStyle.Bold), BorderStyle.None);
            TextBox duration = CreateTextBox(result.FormattedDuration, true, rowColor, Color.WhiteSmoke, detailsFont, BorderStyle.None, HorizontalAlignment.Right);
            TextBox artist = CreateTextBox(result.Artist, true, rowColor, Color.WhiteSmoke, FontManager.CreateFont("Hanken Grotesk", 11, FontStyle.Bold | FontStyle.Italic), BorderStyle.None);
            TextBox year = CreateTextBox(result.ReleaseYear, true, rowColor, Color.WhiteSmoke, detailsFont, BorderStyle.None, HorizontalAlignment.Right);

            ResizeControlForText(title, 5);
            ResizeControlForText(artist, 5);

            TextBox tracks = null;
            if (result.TrackCount > 0)
            {
                string trackText = result.TrackCount != 1 ? $"{result.TrackCount} Tracks" : $"{result.TrackCount} Track";
                tracks = CreateTextBox(trackText, true, rowColor, Color.WhiteSmoke, detailsFont, BorderStyle.None, HorizontalAlignment.Right);
            }

            LinkLabel storeLink = CreateLinkLabel("Preview in Store", result.StoreUrl, Color.FromArgb(0, 112, 239), Color.Blue, rowColor);
            AddLinkLabelFunctionality(storeLink);

            LinkLabel webLink = CreateLinkLabel("Preview in Web Player", result.WebPlayerUrl, Color.FromArgb(0, 112, 239), Color.Blue, rowColor);
            AddLinkLabelFunctionality(webLink);

            TableLayoutPanel releaseInfoColumnPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = rowColor,
                ColumnStyles = { new ColumnStyle(SizeType.Absolute, 490), new ColumnStyle(SizeType.Absolute, 90) }
            };
            releaseInfoColumnPanel.Controls.Add(title, 0, 0);
            releaseInfoColumnPanel.Controls.Add(duration, 1, 0);
            releaseInfoColumnPanel.Controls.Add(artist, 0, 1);
            releaseInfoColumnPanel.Controls.Add(year, 1, 1);
            if (tracks != null) releaseInfoColumnPanel.Controls.Add(tracks, 1, 2);

            if (webLink != null || storeLink != null)
            {
                FlowLayoutPanel linksPanel = new FlowLayoutPanel
                {
                    BackColor = releaseInfoColumnPanel.BackColor,
                    AutoSize = true
                };

                if (webLink != null) linksPanel.Controls.Add(webLink);
                if (storeLink != null) linksPanel.Controls.Add(storeLink);

                releaseInfoColumnPanel.Controls.Add(linksPanel, 0, 2);
            }

            return releaseInfoColumnPanel;
        }

        private TableLayoutPanel CreateQualityColumn(SearchResultRow result, Color rowColor)
        {
            TextBox quality = CreateTextBox(result.FormattedQuality, true, rowColor, Color.White, FontManager.CreateFont("Hanken Grotesk Medium", 10), BorderStyle.None);
            PictureBox hiResIcon = null;

            if (result.HiRes)
            {
                hiResIcon = new PictureBox
                {
                    Image = Properties.Resources.smr_audio,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Size = new Size(30, 30),
                    Margin = new Padding(0, 0, 0, 0),
                    Anchor = AnchorStyles.Top
                };
            }

            TableLayoutPanel thirdColumnPanel = new TableLayoutPanel
            {
                ColumnCount = 1,
                RowCount = hiResIcon == null ? 1 : 2,
                BackColor = rowColor,
                ColumnStyles = { new ColumnStyle(SizeType.Percent, 100) },
                RowStyles = { new RowStyle(SizeType.AutoSize), new RowStyle(SizeType.AutoSize) },
            };

            thirdColumnPanel.Controls.Add(quality, 0, 0);
            if (hiResIcon != null) thirdColumnPanel.Controls.Add(hiResIcon, 0, 1);

            return thirdColumnPanel;
        }

        private Button CreateDownloadButton(string webPlayerUrl)
        {
            Button downloadButton = new Button
            {
                Text = "Download",
                BackColor = Color.FromArgb(0, 112, 239),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                Height = 23,
                Anchor = AnchorStyles.None,
                TextAlign = ContentAlignment.MiddleCenter
            };
            downloadButton.FlatAppearance.BorderSize = 0;
            downloadButton.Click += (sender, e) =>
            {
                Globals.SelectedDownloadUrl = webPlayerUrl;
                Globals.QbdlxForm.getLinkTypeBG.RunWorkerAsync();
                this.Close();
            };

            return downloadButton;
        }

        private TextBox CreateTextBox(string text, bool readOnly, Color backColor, Color foreColor, Font font, BorderStyle borderStyle, HorizontalAlignment textAlign = HorizontalAlignment.Left)
        {
            return new TextBox
            {
                Text = text,
                ReadOnly = readOnly,
                BackColor = backColor,
                ForeColor = foreColor,
                Font = font,
                BorderStyle = borderStyle,
                Margin = new Padding(0, 0, 0, 0),
                AutoSize = false,
                TextAlign = textAlign
            };
        }

        private LinkLabel CreateLinkLabel(string text, string url, Color linkColor, Color activeLinkColor, Color backColor)
        {
            if (string.IsNullOrEmpty(url)) return null;

            return new LinkLabel
            {
                Text = text,
                Tag = url,
                Font = FontManager.CreateFont("Hanken Grotesk Medium", 9),
                AutoSize = true,
                LinkBehavior = LinkBehavior.HoverUnderline,
                LinkColor = linkColor,
                ActiveLinkColor = activeLinkColor,
                BackColor = backColor
            };
        }

        private void AddLinkLabelFunctionality(LinkLabel linkLabel)
        {
            if (linkLabel == null || linkLabel.Tag == null) return;

            // Get URL from Tag attribute
            string url = linkLabel.Tag.ToString();

            // Open the URL in the default browser when clicked
            linkLabel.LinkClicked += (sender, e) => Process.Start(url);

            // Add tooltip with the URL
            ToolTip toolTip = new ToolTip();
            toolTip.SetToolTip(linkLabel, url);

            // Add context menu with "Copy URL" option
            ContextMenu contextMenu = new ContextMenu();
            MenuItem copyUrlItem = new MenuItem("Copy URL");
            copyUrlItem.Click += (sender, e) => CopyToClipboard(url);
            contextMenu.MenuItems.Add(copyUrlItem);
            linkLabel.ContextMenu = contextMenu;
        }

        private void ResizeControlForText(Control control, int extraMargin = 0)
        {
            Size preferredSize = TextRenderer.MeasureText(control.Text, control.Font);
            control.Size = new Size(preferredSize.Width + extraMargin, preferredSize.Height);
        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            // In case of an exception, a newline is added to the searchQuery string for some reason.
            // So we clone the object to prevent newline in TextBox on error.
            string searchQuery = searchInput.Text.Clone().ToString();

            if (string.IsNullOrEmpty(searchQuery))
            {
                return;
            }

            try
            {
                if (searchTypeSelect.Text == "Album")
                {
                    SearchResult albumsResult = QobuzApiServiceManager.GetApiService().SearchAlbums(searchQuery, 15, 0, true);
                    Fill_AlbumResultsTablePanel(albumsResult);
                }
                else if (searchTypeSelect.Text == "Track")
                {
                    SearchResult tracksResult = QobuzApiServiceManager.GetApiService().SearchTracks(searchQuery, 15, 0, true);
                    Fill_TrackResultsTablePanel(tracksResult);
                }
            }
            catch (Exception ex)
            {
                ShowAndLogSearchResultError(ex);
            }
        }

        private void SearchForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Nothing yet
        }

        private void SearchForm_Load(object sender, EventArgs e)
        {
            // Nothing yet
        }

        // Enable moving Form with mouse in absence of titlebar
        private void SearchForm_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void SearchInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SearchButton_Click(sender, e);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        // Alternating row colors for search results
        private void ResultsTableLayoutPanel_CellPaint(object sender, TableLayoutCellPaintEventArgs e)
        {
            e.Graphics.FillRectangle(new SolidBrush(GetResultRowColor(e.Row)), e.CellBounds);
        }

        private void CopyToClipboard(string text)
        {
            Task.Run(() =>
            {
                Thread thread = new Thread(() => Clipboard.SetText(text));
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
            });
        }
    }
}