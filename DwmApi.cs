using System;
using System.Runtime.InteropServices;

namespace Utility.Windows.Forms {
    public partial class AeroGlassForm {
        #region Classes
        protected static class DwmApi {
            #region Constants
            private const string DwmApiDll = "DwmApi.dll";
            #endregion

            #region Extern Methods
            [DllImport(DwmApiDll)]
            public static extern HRESULT DwmEnableBlurBehindWindow(IntPtr hWnd, [In] ref DWM_BLURBEHIND pBlurBehind);
            [DllImport(DwmApiDll)]
            public static extern HRESULT DwmEnableComposition(DWM_EC_COMPOSITION compositionAction);
            [DllImport(DwmApiDll)]
            public static extern HRESULT DwmExtendFrameIntoClientArea(IntPtr hWnd, [In] ref MARGINS pMarInset);
            [DllImport(DwmApiDll)]
            public static extern HRESULT DwmGetColorizationColor([In, Out] ref int pcrColorization, [In, Out, MarshalAs(UnmanagedType.Bool)] ref bool pfOpaqueBlend);
            [DllImport(DwmApiDll, PreserveSig = false)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DwmIsCompositionEnabled();
            #endregion

            #region Enums
            [Flags]
            public enum DWM_BB {
                ENABLE = 0x1,
                BLURREGION = 0x2,
                TRANSITIONONMAXIMIZED = 0x4
            }
            public enum DWM_EC_COMPOSITION : uint {
                DISABLE = 0x0,
                ENABLE = 0x1
            }
            public enum HRESULT {
                S_OK = 0x0
            }
            public enum WM_DWM_CHANGED {
                COMPOSITION = 0x031E,
                NCRENDERING = 0x031F,
                COLORIZATIONCOLOR = 0x0320
            }
            #endregion

            #region Structures
            [StructLayout(LayoutKind.Sequential)]
            public struct DWM_BLURBEHIND {
                #region Fields
                public DWM_BB dwFlags;
                [MarshalAs(UnmanagedType.Bool)]
                public bool fEnable;
                [MarshalAs(UnmanagedType.Bool)]
                public bool fTransitionOnMaximized;
                public IntPtr hRgnBlur;
                #endregion

                #region Constructors
                public DWM_BLURBEHIND(bool enable)
                : this(DWM_BB.ENABLE, enable, IntPtr.Zero, false) {
                }
                public DWM_BLURBEHIND(bool enable, bool transitionOnMaximized)
                : this(DWM_BB.ENABLE | DWM_BB.TRANSITIONONMAXIMIZED, enable, IntPtr.Zero, transitionOnMaximized) {
                }
                public DWM_BLURBEHIND(bool enable, IntPtr blurRegion)
                : this(DWM_BB.ENABLE | DWM_BB.BLURREGION, enable, blurRegion, false) {
                }
                public DWM_BLURBEHIND(bool enable, IntPtr blurRegion, bool transitionOnMaximized)
                : this(DWM_BB.ENABLE | DWM_BB.BLURREGION | DWM_BB.TRANSITIONONMAXIMIZED, enable, blurRegion, transitionOnMaximized) {
                }
                public DWM_BLURBEHIND(DWM_BB flags, bool enable, IntPtr blurRegion, bool transitionOnMaximized)
                : this() {
                    this.dwFlags = flags;
                    this.fEnable = enable;
                    this.hRgnBlur = blurRegion;
                    this.fTransitionOnMaximized = transitionOnMaximized;
                }
                #endregion
            }
            [StructLayout(LayoutKind.Sequential)]
            public struct MARGINS {
                #region Fields
                public int cxLeftWidth;
                public int cxRightWidth;
                public int cyBottomHeight;
                public int cyTopHeight;
                #endregion

                #region Constructors
                public MARGINS(bool fullWindow)
                : this() {
                    if (fullWindow) {
                        this.cxLeftWidth = -1;
                    }
                }
                public MARGINS(int left, int top, int right, int bottom)
                : this() {
                    cxLeftWidth = left;
                    cyTopHeight = top;
                    cxRightWidth = right;
                    cyBottomHeight = bottom;
                }
                public MARGINS(bool fullScreen, int left, int top, int right, int bottom)
                : this() {
                    if (fullScreen) {
                        this.cxLeftWidth = -1;
                    }
                    else {
                        cxLeftWidth = left;
                        cyTopHeight = top;
                        cxRightWidth = right;
                        cyBottomHeight = bottom;
                    }
                }
                #endregion
            }
            #endregion
        }
        #endregion
    }
}