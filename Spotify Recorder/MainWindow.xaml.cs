using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Threading;
using System.IO;
using LibSpot;
using LibSpot.HelperClasses;
using DirectX.Capture;

namespace Spotify_Recorder
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        protected SpotRecorder rec;
        protected Filters filters;
        protected string Folder { get; set; }
        protected ObservableCollection<RecordedTrack> _recordedTracks = new ObservableCollection<RecordedTrack>();
        public ObservableCollection<RecordedTrack> recordedTracks { get { return _recordedTracks; } }


        public MainWindow()
        {
            InitializeComponent();
            loadRecordingDevices();
        }

        /// <summary>
        /// Loads Recording devices
        /// </summary>
        private void loadRecordingDevices()
        {
            try
            {
                filters = new Filters();
                cb_recordingDevices.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(
                    delegate()
                    {
                        foreach (Filter f in filters.AudioInputDevices)
                        {
                            cb_recordingDevices.Items.Add(f.Name);
                        }
                    }
                    ));
            }
            catch (Exception ex)
            {
                MessageBox.Show("No recording devices found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Select-Folder Button:
        /// Display an open-folder-dialog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_selectFolder_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog ofd = new System.Windows.Forms.FolderBrowserDialog();
            ofd.Description = "Please choose a folder where recorded music should be saved.";
            ofd.ShowDialog();

            if (!string.IsNullOrEmpty(ofd.SelectedPath))
            {
                Folder = ofd.SelectedPath;
                tb_folderLocation.Text = ofd.SelectedPath;
            }
            else
            {
                MessageBox.Show("No folder selected!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Button, which starts recording
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_recordingStart_Click(object sender, RoutedEventArgs e)
        {
            // Check if no folder is selected
            if (String.IsNullOrEmpty(Folder))
            {
                MessageBox.Show("No Output-Folder selected. Please select one.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check of folder exists
            if (!Directory.Exists(Folder))
            {
                MessageBox.Show("The Output-Folder you selected does not exist.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check if recording device is selected
            if (cb_recordingDevices.SelectedIndex.Equals(-1))
            {
                MessageBox.Show("No recording device selected!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                rec = new SpotRecorder(Folder, filters.AudioInputDevices[cb_recordingDevices.SelectedIndex]);
                rec.TrackRecorded += new SpotRecorder.TrackRecordedEventHandler(rec_TrackRecorded);
                rec.spotHandler.TrackChanged += new LibSpot.Handlers.SpotHandlerBase.TrackChangedEventHandler(spotHandler_TrackChanged);
                lb_recordingStatus.Content = "Recording...";
                
                // Disable and enable buttons
                btn_stopRecording.IsEnabled = true;
                btn_recordingStart.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Button, which stops recording
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_stopRecording_Click(object sender, RoutedEventArgs e)
        {
            if (rec != null)
            {
                rec.DisposeAll();
                rec = null;
            }

            // Disable and enable button
            btn_recordingStart.IsEnabled = true;
            btn_stopRecording.IsEnabled = false;

            lb_recordingStatus.Content = "Not Recording";
        }

        /// <summary>
        /// Shows info window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_info_Click(object sender, RoutedEventArgs e)
        {
            Info i = new Info();
            i.ShowDialog();
        }

        /// <summary>
        /// Fired if the Spotify Track changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void spotHandler_TrackChanged(object sender, LibSpot.Handlers.TrackChangedEventArgs e)
        {
            try
            {
                if (!String.IsNullOrEmpty(e.track))
                {

                    SpotTrack track = LibSpot.Handlers.SpotHandler.getSpotTrackObject(e.track);

                    SpotRecorderWindow.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, TimeSpan.FromSeconds(3), new Action(
                        delegate()
                        {
                            lb_currentArtist.Content = track.Artist;
                            lb_currentTitle.Content = track.Title;
                        }
                        ));
                }
                else
                {
                    SpotRecorderWindow.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, TimeSpan.FromSeconds(3), new Action(
                        delegate()
                        {
                            lb_currentArtist.Content = "";
                            lb_currentTitle.Content = "";
                        }
                        ));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }


        /// <summary>
        /// Fired if the SpotifyRecorder has recorded a new track
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void rec_TrackRecorded(object sender, LibSpot.HelperClasses.TrackRecordedEventArgs e)
        {
            SpotRecorderWindow.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, TimeSpan.FromSeconds(3), new Action(
                delegate()
                {
                    _recordedTracks.Add(RecordedTrack.convertSpotTrack(e.Track));
                    new System.Threading.Thread(() => convertTrack(RecordedTrack.convertSpotTrack(e.Track))).Start();
                }
                ));
        }

        /// <summary>
        /// Converts a track to MP3 and sets the Status in the list view
        /// </summary>
        /// <param name="track">Track Object which should be converted</param>
        protected void convertTrack(RecordedTrack track)
        {
            string newPath = track.Path.Replace(".wav", ".mp3");
            int itemIndex = 0;

            SpotRecorderWindow.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, TimeSpan.FromSeconds(3), new Action(
                delegate()
                {
                    int counter = 0;
                    foreach (RecordedTrack r in _recordedTracks)
                    {
                        if (r.Path.Equals(track.Path))
                        {
                            itemIndex = counter;
                            _recordedTracks[counter].Status = "Converting...";
                            lv_recordedTracks.Items.Refresh();
                            break;
                        }
                        counter++;
                    }
                }
                ));

            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
            psi.FileName = "ffmpeg.exe";
            psi.CreateNoWindow = true;
            psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            psi.Arguments = string.Format("-i \"{0}\" -acodec libmp3lame -ab 192k -ac 2 -y \"{1}\"", track.Path, newPath);
            System.Diagnostics.Process p = System.Diagnostics.Process.Start(psi);
            p.WaitForExit();

            SpotRecorderWindow.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, TimeSpan.FromSeconds(3), new Action(
                delegate()
                {
                    if (File.Exists(newPath))
                    {
                        File.Delete(track.Path);
                        track.Status = "Converted";
                        track.Path = track.Path.Replace(".wav", ".mp3");
                        _recordedTracks[itemIndex] = track;
                    }
                    else
                    {
                        track.Status = "Converting Error";
                        _recordedTracks[itemIndex] = track;
                    }
                    lv_recordedTracks.Items.Refresh();
                }
                ));

            tagTrack(track, itemIndex);

        }

        /// <summary>
        /// Tags a File and sets status in the listview
        /// </summary>
        /// <param name="track">SpotTrack-Object which should be tagged</param>
        /// <param name="listViewIndex">Index of the listview</param>
        protected void tagTrack(RecordedTrack track, int listViewIndex)
        {
            if (File.Exists(track.Path))
            {
                TagLib.File f = TagLib.File.Create(track.Path);

                if (!string.IsNullOrEmpty(track.Title))
                {
                    f.Tag.Title = track.Title;
                }

                if (!string.IsNullOrEmpty(track.Artist))
                {
                    f.Tag.Performers = new string[] { track.Artist };
                }

                try
                {
                    f.Save();

                    SpotRecorderWindow.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(
                        delegate()
                        {
                            _recordedTracks[listViewIndex].Status = "Finished";
                            lv_recordedTracks.Items.Refresh();
                        }
                        ));

                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }
    }
}
