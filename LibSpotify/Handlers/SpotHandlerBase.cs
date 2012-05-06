﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace LibSpot.Handlers
{
    public abstract class SpotHandlerBase
    {
        #region External Functions
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        protected static extern int _SendMessage(IntPtr hWnd, int Msg, int wParam, uint lParam);

        protected int SendMessage(IntPtr hWnd, int Msg, int wParam, uint lParam)
        {
            if (checkIfRunning())
            {
                return _SendMessage(hWnd, Msg, wParam, lParam);
            }
            else
            {
                throw new PlayerNotRunningException(ProcessNotRunningMessage);
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        protected static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        protected static extern int GetWindowText(IntPtr hWnd, [Out, MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpString, int nMaxCount);
        #endregion

        protected Timer TrackTimer;
        protected TimerCallback TrackTimerDelegate;

        protected Timer PlayerStatusTimer;
        protected TimerCallback PlayerStatusTimerDelegate;

        protected IntPtr handle { get; set; }
        protected Process process { get; set; }
        protected string processname { get; set; }

        protected abstract string ProcessNotRunningMessage { get; set; }

        protected bool isAvailable { get; set; }
        protected bool isFirstRun { get; set; }

        protected string lastTitle { get; set; }

        #region Events
        // onTrackChanged Event
        public delegate void TrackChangedEventHandler(object sender, TrackChangedEventArgs e);
        public event TrackChangedEventHandler TrackChanged;

        // onPlayerStatusChanged Event
        public delegate void PlayerStatusChangedEventHandler(object sender, PlayerStatusChangedEventArgs e);
        public event PlayerStatusChangedEventHandler PlayerStatusChanged;
        #endregion

        #region Eventfunctions
        // onTrackChanged Event
        protected void onTrackChanged(object sender, TrackChangedEventArgs e)
        {
            if (TrackChanged != null)
                TrackChanged(sender, e);
        }

        // onPlayerStatusChangged Event
        protected void onPlayerStatusChanged(object sender, PlayerStatusChangedEventArgs e)
        {
            if (PlayerStatusChanged != null)
                PlayerStatusChanged(sender, e);
        }
        #endregion

        #region AppCOMMAND
        private const int WM_APPCOMMAND = 0x0319;

        public const int APPCOMMAND_MEDIA_STOP = 13;
        public const int APPCOMMAND_MEDIA_PLAY_PAUSE = 14;
        public const int APPCOMMAND_MEDIA_PLAY = 46;
        public const int APPCOMMAND_MEDIA_PAUSE = 47;
        public const int APPCOMMAND_MEDIA_RECORD = 48;
        public const int APPCOMMAND_MEDIA_FAST_FORWARD = 49;
        public const int APPCOMMAND_MEDIA_REWIND = 50;
        public const int APPCOMMAND_MEDIA_CHANNEL_UP = 51;
        public const int APPCOMMAND_MEDIA_CHANNEL_DOWN = 52;
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public SpotHandlerBase(string processname)
        {
            this.processname = processname;
            this.PlayerStatusChanged += new PlayerStatusChangedEventHandler(PlayerBase_PlayerStatusChanged);
            this.isAvailable = true;
            this.isFirstRun = true;

            PlayerStatusTimerDelegate = new TimerCallback(CheckIfProcessIsRunning);
            PlayerStatusTimer = new Timer(PlayerStatusTimerDelegate, null, 1000, 1000);
        }

        #region Abstract methods
        protected abstract string getCurrentTrack();
        #endregion

        /// <summary>
        /// Fired when player status has changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void PlayerBase_PlayerStatusChanged(object sender, PlayerStatusChangedEventArgs e)
        {
            if (e.state == PlayerStates.Online)
            {
                SetupCheckForNewTitle();
            }
            else if (e.state == PlayerStates.Offline)
            {
                StopCheckForNewTrack();
            }
        }

        /// <summary>
        /// Sets up the timer and starts it
        /// </summary>
        public void SetupCheckForNewTitle()
        {
            TrackTimerDelegate = new System.Threading.TimerCallback(CheckForNewTitle);
            TrackTimer = new System.Threading.Timer(TrackTimerDelegate, null, 0, 100);
        }

        /// <summary>
        /// Stops the timer
        /// </summary>
        public void StopCheckForNewTrack()
        {
            TrackTimer = null;
        }

        /// <summary>
        /// Checks if vlc is playing a new title. Throws an event if true
        /// </summary>
        /// <param name="s">null</param>
        protected void CheckForNewTitle(object s)
        {
            try
            {
                string currentTitle = getCurrentTrack();
                if (lastTitle == null || !lastTitle.Equals(currentTitle))
                {
                    lastTitle = currentTitle;
                    onTrackChanged(this, new TrackChangedEventArgs(currentTitle));
                }
            }
            catch (PlayerNotRunningException) { }
        }

        /// <summary>
        /// Checks if the Process is running
        /// </summary>
        /// <param name="processname">processname</param>
        /// <returns>true if running, false if not running</returns>
        protected bool checkIfRunning()
        {
            Process[] processes = Process.GetProcessesByName(processname);

            if (processes.Length == 0)
                return false;
            return true;
        }

        /// <summary>
        /// Checks if the player is running
        /// </summary>
        /// <param name="s"></param>
        protected void CheckIfProcessIsRunning(object s)
        {
            try
            {
                Process[] p = Process.GetProcessesByName(this.processname);
                if (p.Length == 0)
                {
                    if (isAvailable || isFirstRun)
                    {
                        onPlayerStatusChanged(this, new PlayerStatusChangedEventArgs(PlayerStates.Offline));
                        this.isAvailable = false;
                        this.isFirstRun = false;
                    }
                }
                else
                {
                    if (!isAvailable || isFirstRun)
                    {
                        this.process = p[0];
                        this.handle = process.MainWindowHandle;
                        onPlayerStatusChanged(this, new PlayerStatusChangedEventArgs(PlayerStates.Online));
                        this.isAvailable = true;
                        this.isFirstRun = false;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Updated the processinstance
        /// </summary>
        protected void updateProcessInstance()
        {
            Process[] p = Process.GetProcessesByName(this.processname);

            if (p.Length != 0)
            {
                this.process = p[0];
                this.handle = p[0].MainWindowHandle;
            }
            else
            {
                onPlayerStatusChanged(this, new PlayerStatusChangedEventArgs(PlayerStates.Offline));
            }

        }
    }

    /// <summary>
    /// EventArgs for TrackChangedEvent
    /// </summary>
    public class TrackChangedEventArgs : EventArgs
    {
        public string track { get; set; }

        public TrackChangedEventArgs(string track)
        {
            this.track = track;
        }
    }

    /// <summary>
    /// EventArgs for PlayerStatusChangedEvent
    /// </summary>
    public class PlayerStatusChangedEventArgs : EventArgs
    {
        public PlayerStates state { get; set; }

        public PlayerStatusChangedEventArgs(PlayerStates state)
        {
            this.state = state;
        }
    }

    /// <summary>
    /// PlayerNotRunningException
    /// </summary>
    public class PlayerNotRunningException : ApplicationException
    {
        public PlayerNotRunningException(string message) : base(message) { }
    }

    /// <summary>
    /// Playerstates
    /// </summary>
    public enum PlayerStates
    {
        Online,
        Offline
    }
}
