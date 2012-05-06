using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibSpot.HelperClasses
{
    public class TrackRecordedEventArgs : EventArgs
    {
        public SpotTrack Track { get; set; }

        public TrackRecordedEventArgs(SpotTrack track)
        {
            this.Track = track;
        }
    }
}
