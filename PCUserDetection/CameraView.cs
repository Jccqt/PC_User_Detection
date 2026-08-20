using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PCUserDetection
{
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

        private VideoCaptureDevice device;
        private string activeMoniker;

        public CameraView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Canvas;
        }

        /// <summary>Shown in the middle of the view while there is no frame to draw.</summary>
        public string Placeholder { get; set; } = "The camera feed will appear here.";

        /// <summary>True while the view is held on the frame that was captured.</summary>
        public bool IsFrozen { get; private set; }

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
            device = new VideoCaptureDevice(monikerString);
            device.NewFrame += OnNewFrame;
            device.Start();
            IsFrozen = false;
        }

        public void Stop()
        {
            if (device != null)
            {
                if (device.IsRunning)
                {
                    device.SignalToStop();
                    device.WaitForStop();
                }
                device.NewFrame -= OnNewFrame;
                device = null;
            }

            activeMoniker = null;
            IsFrozen = false;
            SetFrame(null);
        }

        /// <summary>Holds the view on the frame that is on screen.</summary>
        public void Freeze()
        {
            IsFrozen = true;
        }

        /// <summary>Lets the live feed take the view back over.</summary>
        public void Resume()
        {
            IsFrozen = false;
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
            if (IsFrozen) return;

            SetFrame((Bitmap)e.Frame.Clone());

            // Invalidate is not safe to call from the capture thread, and
            // BeginInvoke does not block it the way Invoke would.
            if (IsHandleCreated) BeginInvoke((Action)Invalidate);
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
            e.Graphics.Clear(Theme.Canvas);

            lock (frameLock)
            {
                if (frame == null)
                {
                    TextRenderer.DrawText(e.Graphics, Placeholder, Theme.Body, ClientRectangle,
                        Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
                else
                {
                    e.Graphics.InterpolationMode = InterpolationMode.Bilinear;
                    e.Graphics.DrawImage(frame, FitRectangle(frame.Width, frame.Height));
                }
            }

            using (var pen = new Pen(Theme.Border))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
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
            if (disposing) Stop();
            base.Dispose(disposing);
        }
    }
}
