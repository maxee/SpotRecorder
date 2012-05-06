using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LibSpot.HelperClasses;

namespace LibSpot.Handlers
{
    public class SpotHandler : SpotHandlerBase
    {
        protected override string ProcessNotRunningMessage { get; set; }
        protected string replaceRegex = @"^((Spotify)( - )?)";

        public SpotHandler(string processname) : base ( processname)
        {
            ProcessNotRunningMessage = "Spotify is not running.";
        }

        /// <summary>
        /// Returns the Current Track which is currently played
        /// </summary>
        /// <returns></returns>
        protected override string getCurrentTrack()
        {
            if (checkIfRunning())
            {
                updateProcessInstance();

                string fullTitle = this.process.MainWindowTitle;

                string title = Regex.Replace(fullTitle, this.replaceRegex, "").Trim();

                if (string.IsNullOrEmpty(title) || string.IsNullOrWhiteSpace(title))
                    return string.Empty;
                else return title;
            }
            else
            {
                throw new PlayerNotRunningException(ProcessNotRunningMessage);
            }
        }

        /// <summary>
        /// Converts a Spotify Window title to a SpotTrack-Object
        /// </summary>
        /// <param name="Track"></param>
        /// <returns></returns>
        public static SpotTrack getSpotTrackObject(string Track)
        {
            string[] splitchars = new string[] { "–" };

            string[] stringSplitted = Track.Split(splitchars, StringSplitOptions.RemoveEmptyEntries);

            if (stringSplitted.Length == 2)
                return new SpotTrack(stringSplitted[0], stringSplitted[1]);
            else
                return new SpotTrack(stringSplitted[0], string.Empty);
        }   
    }
}
