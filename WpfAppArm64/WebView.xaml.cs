using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

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

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        private const int GWL_STYLE = -16;
        private const uint WS_CHILD = 0x40000000;
        private const uint WS_BORDER = 0x00800000;
        public const uint WS_POPUP = 0x80000000;
        public const uint WS_OVERLAPPED = 0x00000000;
        public const uint WS_THICKFRAME = 0x00040000;
        public const uint WS_MAXIMIZEBOX = 0x00010000;
        public const uint WS_MINIMIZEBOX = 0x00020000;
        public const uint WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;
        public const uint WS_CAPTION = 0x00C00000;     // WS_BORDER | WS_DLGFRAME
        public const uint WS_SYSMENU = 0x00080000;

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
            var path = AppDomain.CurrentDomain.BaseDirectory + @"Exteral\\MyElectronApp.exe";
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
                    //wnd = wfh.Handle; // 这里设置了 MyWndHost 的 Handle 属性 - 不成为子窗口
                    SetParent(m_hWndChild, wfh.Handle);

                    uint style = GetWindowLong(m_hWndChild, GWL_STYLE);
                    //style &= ~(WS_POPUP | WS_OVERLAPPEDWINDOW | WS_THICKFRAME | WS_MAXIMIZEBOX | WS_MINIMIZEBOX);
                    //style |= WS_CHILD;


                    style &= ~(WS_POPUP);
                    style &= ~(WS_OVERLAPPEDWINDOW);
                    //20221201因设置子窗口属性会导致主面板无法响应输入事件，去掉该属性
                    //style |= WS_CHILD;
                    style &= ~WS_OVERLAPPED;
                    style &= ~WS_THICKFRAME;
                    style &= ~WS_MAXIMIZEBOX;
                    style &= ~WS_MINIMIZEBOX;

                    //uint style = GetWindowLong(m_hWndChild, GWL_STYLE);
                    //style = (style & ~WS_BORDER) | WS_CHILD;
                    SetWindowLong(m_hWndChild, GWL_STYLE, style);

                    AdjustChildWindowSize();

                    //// 找到 Electron 渲染窗口并设置焦点
                    //var renderHwnd = FindRenderWindow(m_hWndChild);
                    //SetForegroundWindow(renderHwnd);
                    //SetFocus(renderHwnd);

                    //FocusElectronWindow();
                }
            });
        }

        private void FocusElectronWindow()
        {
            if (m_hWndChild != IntPtr.Zero)
            {
                // 尝试设置子窗口中的实际 WebView 焦点
                EnumChildWindows(m_hWndChild, (child, lParam) =>
                {
                    Console.WriteLine("Found child window: " + child);
                    SetForegroundWindow(child);
                    SetFocus(child);
                    return false; // 找到第一个子窗口就停止
                }, IntPtr.Zero);
            }
        }

        private IntPtr FindRenderWindow(IntPtr parent)
        {
            IntPtr renderWindow = IntPtr.Zero;

            EnumWindowsProc callback = (hwnd, lParam) =>
            {
                renderWindow = hwnd; // 最深子窗口通常是 Chromium 渲染窗口
                return true;
            };

            EnumChildWindows(parent, callback, IntPtr.Zero);
            return renderWindow != IntPtr.Zero ? renderWindow : parent;
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

                FocusElectronWindow();
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