using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibSpot.HelperClasses
{
    public class SpotTrack
    {
        public string Artist { get; set; }
        public string Title { get; set; }
        public string Path { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="artist">Artist</param>
        /// <param name="title">Songtitle</param>
        public SpotTrack (string artist, string title)
        {
            this.Artist = artist.Trim();
            this.Title = title.Trim();
        }
    
    }
}
