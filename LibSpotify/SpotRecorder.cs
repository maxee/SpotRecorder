using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibSpot.Handlers;
using LibSpot.HelperClasses;
using DirectX.Capture;

namespace LibSpot
{
    public class SpotRecorder
    {
        public string FileDirectory { get; set; }
        public Filter RecordingDevice { get; set; }

        public SpotHandler spotHandler;

        protected SpotTrack lastRecordedTrack;

        protected Capture recorder;
        protected const string processName = "spotify";
        protected const string replaceRegex = "[@/\\?%*:|\"<>.]";

        public SpotRecorder(string fileDirectory, Filter recordingDevice)
        {
            if (!fileDirectory.Substring(fileDirectory.Length - 1, 1).Equals(@"\"))
            {
                this.FileDirectory = fileDirectory + @"\";
            }
            else
            {
                this.FileDirectory = fileDirectory;
            }

            this.RecordingDevice = recordingDevice;
            this.recorder = null;

            spotHandler = new SpotHandler(processName);
            spotHandler.TrackChanged += new SpotHandlerBase.TrackChangedEventHandler(spotHandler_TrackChanged);
        }

        /// <summary>
        /// Fired when played track changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void spotHandler_TrackChanged(object sender, TrackChangedEventArgs e)
        {
            try
            {
                if (String.IsNullOrEmpty(e.track) || String.IsNullOrWhiteSpace(e.track))
                {
                    stopRecording();
                }
                else
                {
                    // Replace not allowed chars
                    e.track = System.Text.RegularExpressions.Regex.Replace(e.track, replaceRegex, "");

                    recordTrack(SpotHandler.getSpotTrackObject(e.track));
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
        }

        /// <summary>
        /// Records a track to the specified directory
        /// </summary>
        /// <param name="track">Track which should be saved</param>
        protected void recordTrack(SpotTrack track)
        {
            stopRecording();

            if (recorder == null)
            {
                Filters f = new Filters();
                recorder = new Capture(null, RecordingDevice);           
                recorder.CaptureComplete += new EventHandler(recorder_CaptureComplete);
            }

            recorder.Filename = FileDirectory + string.Format("{0} - {1}", track.Artist, track.Title) + ".wav";

            track.Path = recorder.Filename;

            lastRecordedTrack = track;
            recorder.Start();
        }

        /// <summary>
        /// Stops the recorder and destroys it
        /// </summary>
        protected void stopRecording()
        {
            if (recorder != null)
            {
                if (!recorder.Stopped)
                    recorder.Stop();
                recorder.Dispose();
                recorder = null;
            }
        }

        /// <summary>
        /// Disposes important objects
        /// </summary>
        public void DisposeAll()
        {
            this.stopRecording();

            if (spotHandler != null)
            {
                spotHandler = null;
            }
        }

        /// <summary>
        /// Fired when Recorder recorded a track successfully
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void recorder_CaptureComplete(object sender, EventArgs e)
        {
            onTrackRecorded(this, new TrackRecordedEventArgs(lastRecordedTrack));
        }

        public delegate void TrackRecordedEventHandler(object sender, TrackRecordedEventArgs e);
        public event TrackRecordedEventHandler TrackRecorded;

        protected void onTrackRecorded (object sender, TrackRecordedEventArgs e)
        {
            if (TrackRecorded != null)
                TrackRecorded(sender, e);
        }
    }
}
