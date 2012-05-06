using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibSpot.HelperClasses;

namespace Spotify_Recorder
{
    public class RecordedTrack : SpotTrack
    {
        public string Status { get; set; }

        public RecordedTrack(string _artist, string _title) : base (_artist, _title)
        {
            Status = string.Empty;
        }

        public static RecordedTrack convertSpotTrack(SpotTrack track)
        {
            RecordedTrack r = new RecordedTrack(track.Artist, track.Title);
            r.Path = track.Path;
            return r;
        }
    }
}
