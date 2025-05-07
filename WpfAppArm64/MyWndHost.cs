using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WpfAppArm64
{
    public class MyWndHost : HwndHost
    {
        public new IntPtr Handle
        {
            get => (IntPtr)GetValue(HandleProperty);
            set => SetValue(HandleProperty, value);
        }
        // Using a DependencyProperty as the backing store for Hwnd.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HandleProperty =
            DependencyProperty.Register("Handle", typeof(IntPtr), typeof(MyWndHost), new PropertyMetadata(IntPtr.Zero));
        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            Handle = CreateWindowEx(
                0, "static", "",
                WS_CHILD | WS_VISIBLE | WS_CLIPCHILDREN | WS_CLIPSIBLINGS,
                0, 0,
                (int)Width, (int)Height,
                hwndParent.Handle,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);
            return new HandleRef(this, Handle);
        }
        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            DestroyWindow(hwnd.Handle);
        }
        const int WS_CHILD = 0x40000000;
        const int WS_VISIBLE = 0x10000000;
        const int WS_CLIPCHILDREN = 0x02000000;
        const int WS_CLIPSIBLINGS = 0x04000000;

        [DllImport("user32.dll")]
        internal static extern IntPtr CreateWindowEx(int exStyle, string className, string windowName, int style, int x, int y, int width, int height, IntPtr hwndParent, IntPtr hMenu, IntPtr hInstance, IntPtr pvParam);
        [DllImport("user32.dll")]
        internal static extern bool DestroyWindow(IntPtr hwnd);
    }
}
