using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;

namespace PCUserDetection
{
    /// <summary>Carries what the camera said when the feed failed.</summary>
    internal class CameraFailedEventArgs : EventArgs
    {
        public CameraFailedEventArgs(string detail, bool wasDelivering)
        {
            Detail = detail;
            WasDelivering = wasDelivering;
        }

        /// <summary>The device's own description of the failure, or the view's when it timed out.</summary>
        public string Detail { get; }

        /// <summary>
        /// True when the feed had been delivering frames and stopped, as opposed
        /// to never having produced one. The two have different causes and are
        /// worth telling apart on screen.
        /// </summary>
        public bool WasDelivering { get; }
    }

    /// <summary>
    /// Shows the live webcam feed and hands out the frame that is currently on
    /// screen. The Detect and Add user screens share one instance, so the camera
    /// is opened, stopped and disposed in a single place.
    /// </summary>
    internal class CameraView : Control
    {
        // the feed runs on a worker thread, so the frame it writes and the frame
        // OnPaint reads have to be guarded
        private readonly object frameLock = new object();
        private Bitmap frame;
        private bool frozen;

        private VideoCaptureDevice device;
        private string activeMoniker;

        /// <summary>
        /// How long the feed may go without a frame before it is called failed.
        /// A camera another app is holding opens and runs without ever
        /// delivering, and it never raises an error, so the silence is the only
        /// sign there is. Long enough that a slow camera starting up is not
        /// mistaken for one.
        /// </summary>
        private const int StallTimeoutMs = 8000;

        /// <summary>The corner radius every control in the window shares.</summary>
        private const int Radius = 3;

        private readonly System.Windows.Forms.Timer watchdog;

        // written from the capture thread on every frame and read by the
        // watchdog on the UI thread
        private long lastFrameTicks;
        private int frameSeen;

        // only ever touched on the UI thread, in ReportFailure
        private bool failureReported;

        public CameraView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Canvas;

            watchdog = new System.Windows.Forms.Timer { Interval = 1000 };
            watchdog.Tick += OnWatchdogTick;
        }

        /// <summary>Shown in the middle of the view while there is no frame to draw.</summary>
        public string Placeholder { get; set; } = "The camera feed will appear here.";

        /// <summary>
        /// Raised when the feed fails, which is what a camera another app is
        /// holding does. Raised on the UI thread, once per Start, and the view
        /// has already dropped the device by the time it arrives.
        /// </summary>
        public event EventHandler<CameraFailedEventArgs> Failed;

        /// <summary>
        /// Raised on the UI thread when the feed's first frame arrives. Until it
        /// does there is nothing to capture, however willing the device looked.
        /// </summary>
        public event EventHandler Ready;

        /// <summary>True once the feed has delivered a frame, and until it is stopped.</summary>
        public bool HasFrame
        {
            get { lock (frameLock) { return frame != null; } }
        }

        /// <summary>True while the view is held on the frame that was captured.</summary>
        public bool IsFrozen
        {
            get { lock (frameLock) { return frozen; } }
        }

        public bool IsRunning
        {
            get { return device != null && device.IsRunning; }
        }

        /// <summary>
        /// Starts the given capture device. Calling this again with the device
        /// that is already running does nothing, so screens can ask for the feed
        /// without restarting it every time.
        /// </summary>
        public void Start(string monikerString)
        {
            if (IsRunning && monikerString == activeMoniker)
            {
                Resume();
                return;
            }

            Stop();

            activeMoniker = monikerString;
            failureReported = false;
            frameSeen = 0;
            Interlocked.Exchange(ref lastFrameTicks, Environment.TickCount64);

            device = new VideoCaptureDevice(monikerString);
            device.NewFrame += OnNewFrame;
            device.VideoSourceError += OnVideoSourceError;
            device.Start();

            // the device is up, but a device that is up is not the same as one
            // that is delivering, and only the watchdog can tell the difference
            watchdog.Start();
            Resume();
        }

        public void Stop()
        {
            watchdog.Stop();

            if (device != null)
            {
                if (device.IsRunning)
                {
                    device.SignalToStop();
                    device.WaitForStop();
                }
                device.NewFrame -= OnNewFrame;
                device.VideoSourceError -= OnVideoSourceError;
                device = null;
            }

            activeMoniker = null;
            Resume();
            SetFrame(null);
        }

        /// <summary>
        /// Holds the view on the frame that is on screen. Taken under the frame
        /// lock, so a frame the feed is already delivering cannot replace that
        /// one once this has returned.
        /// </summary>
        public void Freeze()
        {
            lock (frameLock) { frozen = true; }
        }

        /// <summary>Lets the live feed take the view back over.</summary>
        public void Resume()
        {
            lock (frameLock) { frozen = false; }
        }

        /// <summary>
        /// Returns a copy of the frame on screen, or null when the feed has not
        /// produced one yet. The caller owns the copy and has to dispose it.
        /// </summary>
        public Bitmap CaptureFrame()
        {
            lock (frameLock)
            {
                return frame == null ? null : (Bitmap)frame.Clone();
            }
        }

        private void OnNewFrame(object sender, NewFrameEventArgs e)
        {
            // the feed is alive whether or not the view is taking its frames, so
            // the watchdog is fed before the freeze is honoured
            Interlocked.Exchange(ref lastFrameTicks, Environment.TickCount64);

            if (IsFrozen) return;

            var newFrame = (Bitmap)e.Frame.Clone();

            lock (frameLock)
            {
                // the view may have been frozen while this frame was being
                // cloned, and it is meant to hold on the frame that was on
                // screen at that point
                if (frozen)
                {
                    newFrame.Dispose();
                    return;
                }

                SetFrame(newFrame);
            }

            // Invalidate is not safe to call from the capture thread, and
            // BeginInvoke does not block it the way Invoke would.
            if (!IsHandleCreated) return;

            BeginInvoke((Action)Invalidate);

            if (Interlocked.Exchange(ref frameSeen, 1) == 0) BeginInvoke((Action)RaiseReady);
        }

        private void RaiseReady()
        {
            // Stop may have run while this was queued
            if (device == null) return;

            var handler = Ready;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        /// <summary>
        /// Catches the feed that opened without complaint and then delivered
        /// nothing, which is what a camera another app is holding does, and the
        /// one that was delivering and quietly stopped.
        /// </summary>
        private void OnWatchdogTick(object sender, EventArgs e)
        {
            if (device == null)
            {
                watchdog.Stop();
                return;
            }

            if (Environment.TickCount64 - Interlocked.Read(ref lastFrameTicks) < StallTimeoutMs) return;

            ReportFailure(device, HasFrame
                ? "The feed stopped delivering frames."
                : "The feed did not deliver a frame within " + (StallTimeoutMs / 1000) + " seconds.");
        }

        /// <summary>
        /// The feed could not be opened, or has stopped delivering. Raised on
        /// the capture thread, so the report is handed to the UI thread the way
        /// a new frame is.
        /// </summary>
        private void OnVideoSourceError(object sender, VideoSourceErrorEventArgs e)
        {
            if (!IsHandleCreated) return;

            var source = (VideoCaptureDevice)sender;
            string detail = e.Description;

            BeginInvoke((Action)(() => ReportFailure(source, detail)));
        }

        /// <summary>
        /// Reports a feed that is not coming back, from whichever of the two
        /// noticed first. Always on the UI thread, so the guard below is all
        /// that is needed to keep the error and the watchdog from both
        /// reporting the same failure.
        /// </summary>
        private void ReportFailure(VideoCaptureDevice source, string detail)
        {
            // Stop, or a start on another camera, may have run while this was
            // queued; a device that is no longer the one on screen has nothing
            // left to say about it
            if (device != source || failureReported) return;
            failureReported = true;

            // Stop clears the frame, and whether there was one is the difference
            // between a camera that never started and one that gave up
            bool wasDelivering = HasFrame;

            // the feed is not coming back on its own, so the device is dropped
            // rather than left running dead behind a frame that will never change
            Stop();

            var handler = Failed;
            if (handler != null) handler(this, new CameraFailedEventArgs(detail, wasDelivering));
        }

        private void SetFrame(Bitmap newFrame)
        {
            lock (frameLock)
            {
                if (frame != null) frame.Dispose();
                frame = newFrame;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // the canvas has 3 pixel corners, so whatever is behind the view has
            // to be laid down first for the cut corners to show it
            e.Graphics.Clear(Parent == null ? Theme.Background : Parent.BackColor);

            Rectangle bounds = ClientRectangle;
            bounds.Width -= 1;
            bounds.Height -= 1;

            using (GraphicsPath canvas = Rounded.Path(bounds, Radius))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                using (var fill = new SolidBrush(Theme.Canvas))
                {
                    e.Graphics.FillPath(fill, canvas);
                }

                lock (frameLock)
                {
                    if (frame == null)
                    {
                        TextRenderer.DrawText(e.Graphics, Placeholder, Theme.Body, ClientRectangle,
                            Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                    else
                    {
                        // the frame is held to the same corners, so a picture
                        // wider than the view cannot square them off again
                        e.Graphics.SetClip(canvas);
                        e.Graphics.InterpolationMode = InterpolationMode.Bilinear;
                        e.Graphics.DrawImage(frame, FitRectangle(frame.Width, frame.Height));
                        e.Graphics.ResetClip();
                    }
                }

                using (var pen = new Pen(Theme.Border))
                {
                    e.Graphics.DrawPath(pen, canvas);
                }
            }
        }

        /// <summary>The largest centred rectangle that keeps the frame's aspect ratio.</summary>
        private Rectangle FitRectangle(int frameWidth, int frameHeight)
        {
            float scale = Math.Min((float)Width / frameWidth, (float)Height / frameHeight);
            int width = (int)(frameWidth * scale);
            int height = (int)(frameHeight * scale);
            return new Rectangle((Width - width) / 2, (Height - height) / 2, width, height);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stop();
                watchdog.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
