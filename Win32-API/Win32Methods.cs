using System;
using System.Runtime.InteropServices;

namespace Utility.Win32 {
    internal class Win32Methods {
        [DllImport(Win32Dll.User32, CharSet = CharSet.Auto)]
        public static extern IntPtr CallWindowProc(IntPtr wndProc, IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        [DllImport(Win32Dll.User32, CharSet = CharSet.Auto)]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        [DllImport(Win32Dll.User32, CharSet = CharSet.Auto, EntryPoint = "GetWindowLong")]
        public static extern IntPtr GetWindowLong32(HandleRef hWnd, int nIndex);
        [DllImport(Win32Dll.User32, CharSet = CharSet.Auto, EntryPoint = "GetWindowLongPtr")]
        public static extern IntPtr GetWindowLongPtr64(HandleRef hWnd, int nIndex);

        public static IntPtr GetWindowLong(HandleRef hWnd, int nIndex) {
            return IntPtr.Size == 4 ? GetWindowLong32(hWnd, nIndex) : GetWindowLongPtr64(hWnd, nIndex);
        }
    }
}