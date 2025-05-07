using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms.Integration;

namespace WpfAppArm64
{
    public partial class WebView : System.Windows.Controls.UserControl
    {
        private IntPtr m_hWndChild = IntPtr.Zero;
        private Process _electronProcess;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        [DllImport("user32.dll")]
        private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        private const int GWL_STYLE = -16;
        private const uint WS_CHILD = 0x40000000;
        private const uint WS_BORDER = 0x00800000;

        public WebView()
        {
            InitializeComponent();
            Loaded += Web2Container_Loaded;
            SizeChanged += Web2Container_SizeChanged;
        }

        private void Web2Container_Loaded(object sender, RoutedEventArgs e)
        {
            StartElectronApp();
        }

        private void StartElectronApp()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory + @"Exteral\MyElectronApp.exe";
            _electronProcess = new Process
            {
                StartInfo =
                {
                    FileName = path,
                    UseShellExecute = false
                }
            };
            _electronProcess.Start();

            Task.Run(() =>
            {
                while (_electronProcess.MainWindowHandle == IntPtr.Zero && !_electronProcess.HasExited)
                {
                    Thread.Sleep(100);
                    _electronProcess.Refresh();
                }
                if (!_electronProcess.HasExited) ToSetWndParent(_electronProcess.MainWindowHandle);
            });
        }

        private async void ToSetWndParent(IntPtr wnd)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                if (this.FindName("MyWnd") is MyWndHost wfh && wfh.Handle != IntPtr.Zero)
                {
                    m_hWndChild = wnd;
                    SetParent(m_hWndChild, wfh.Handle);

                    uint style = GetWindowLong(m_hWndChild, GWL_STYLE);
                    style = (style & ~WS_BORDER) | WS_CHILD;
                    SetWindowLong(m_hWndChild, GWL_STYLE, style);

                    AdjustChildWindowSize();
                }
            });
        }

        private void Web2Container_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdjustChildWindowSize();
        }

        private void AdjustChildWindowSize()
        {
            if (m_hWndChild != IntPtr.Zero)
            {
                var dpiScale = GetDpiFromVisual(this);
                MoveWindow(
                    m_hWndChild,
                    0,
                    0,
                    (int)(ActualWidth * dpiScale),
                    (int)(ActualHeight * dpiScale),
                    true
                );
            }
        }

        private double GetDpiFromVisual(System.Windows.Media.Visual visual)
        {
            var source = PresentationSource.FromVisual(visual);
            double dpiX = 96.0;
            if (source?.CompositionTarget != null)
            {
                dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
            }
            return dpiX / 96.0;
        }
    }
}