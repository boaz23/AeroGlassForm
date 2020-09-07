namespace Utility.Win32 {
    internal class Win32Dll {
        public const string DwmApi = "DwmApi" + DllExt,
                            User32 = "User32" + DllExt;
        private const string DllExt = ".dll";
    }
}