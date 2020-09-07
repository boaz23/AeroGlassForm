using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Utility.Win32;

namespace Utility.Windows.Forms {
    public partial class AeroGlassForm : Form {
        //Notes:
        //1. Drawing somethings on the form will appear in a odd way: any black text, will appear transparent and generally the controls will look wierd. It is solved by setting the form's 'TransparencyKey' property on and painting it's background with that color.
        //2. Some transparency key colors will also cause the controls and the form to look wierd.
        //3. In order for controls to appear correctly, their background color must (not necessarily the 'BackColor' property) be the 'TransparencyKey'. This is what we are doing while painting the form's background.
        //4. Checkboxe's, label's and radio button's background color must be painted before painting anything else. Simply filling their client area with 'TransparencyKey' within the 'OnPaint' method won't work because if it is filled after the windows forms adapter renders them, only the their background color will be seen since it covers what the windows forms adapter painted, and if it is filled before the windows forms adapter renders it, it will get covered by what the windows forms adapter painted. Also, check boxes and radio buttons do not raise 'OnPaintBackground' method (which I think defeats the purpose of inheritance), so it's impossible to paint their client area's background with 'TransparencyKey' within 'OnPaintBackground' method.
        //5. To solve the problem in note #4, we set the checkbox's, label's or radio button 'BackColor' to 'TransparencyKey' in order to let the windows forms adapter to paint them with that background color. However, we also want to keep track of the user's requested background color for checkboxes, labels and radio buttons. To achieve this, we use the 'IAeroGlassCompatibleControl' interface. The label also implement this interface just for consistency.
        //6. Checkboxe's, label's and radio button's text will wierd on the form regardless of their background color. In order to fix that, they must use the 'AntiAliasGridFit' or 'SingleBitPerPixelGridFit' TextRenderingHint.
        //7. Whenever the aero glass composition changes from enabled to disabled and only the transparency key is enabled, the non-client area turns black for some reason. To fix it, we can do multiple things:
        //  • Send a message to the form using 'WndProc' telling it that its non-client area needs to be changed to indicate an active or inactive state (obviously, sending the message with the current active state of the form).
        //  • Send a message to the default window procedure using 'DefWndProc' telling the form that its non-client area needs to be changed to indicate an active or inactive state (obviously, sending the message with the current active state of the form).
        //  • Tell Windows to send a message to the form that its non-client area needs to be changed to indicate an active or inactive state (obviously, sending the message with the current active state of the form).
        //  • Send Windows a message telling that the form's non-client area needs to be changed to indicate an active or inactive state (obviously, sending the message with the current active state of the form) using the default window procedure.
        //Also see http://stackoverflow.com/questions/5978086/windows-aero-glass-form-text-problem and http://stackoverflow.com/questions/4258295/aero-how-to-draw-solid-opaque-colors-on-glass for additional information.

        //Known Bugs:
        //1. Drawing anything on the form while the aero glass composition in enabled by using 'BlurBehindWindow' will look wierd and the transparency key does not solve that.

        #region Fields
        /// <summary>
        /// Determines the default color to use when enabling the transparency key.
        /// </summary>
        public static readonly Color DefaultTransparencyKeyColor;
        /// <summary>
        /// Indicates whether the DWM (Desktop Windows Manager) is supported for the current operating system (Windows Vista or later).
        /// </summary>
        public static readonly bool IsDwmSupported;
        private const bool DefualtAutoDetectAeroGlassCompositionAndTransparencyKey = true;
        private static readonly object EventHandlerKeyColorizationColor,
                                       EventHandlerKeyComposition,
                                       EventHandlerKeyNonClientAreaRendering;
        private static readonly PropertyInfo Type_Form_Active;
        private List<Control> _iAeroGlassCompatibleControls;
        #endregion

        #region Enums
        [Flags]
        protected enum Messages {
            All = -1,
            None = 0x0,
            CompositionChanged = 0x1,
            NonClientAreaRenderingChanged = 0x2,
            ControlAdded = 0x4,
            ControlRemoved = 0x8,
            HandleCreated = 0x10,
            PaintBackground = 0x20,
            StyleChanged = 0x40
        }
        #endregion

        #region Constructors
        static AeroGlassForm() {
            {
                OperatingSystem osVersion;

                osVersion = Environment.OSVersion;
                IsDwmSupported = osVersion != null && osVersion.Platform == PlatformID.Win32NT && osVersion.Version.Major > 5;
            }
            DefaultTransparencyKeyColor = Color.FromArgb(255, 241, 240, 240);
            Type_Form_Active = typeof(Form).GetProperty("Active", BindingFlags.NonPublic | BindingFlags.Instance, null, typeof(bool), Type.EmptyTypes, null);
            EventHandlerKeyColorizationColor = new object();
            EventHandlerKeyComposition = new object();
            EventHandlerKeyNonClientAreaRendering = new object();
        }
        /// <summary>
        /// Initializes a new Instance of AeroGlassForm class with automatic detection whether to enable or disable the windows aero glass composition and transparency key for this form.
        /// </summary>
        public AeroGlassForm()
            : this(DefualtAutoDetectAeroGlassCompositionAndTransparencyKey) {
        }
        /// <summary>
        /// Initializes a new Instance of AeroGlassForm class indicating wheter it should use of the windows aero glass and transparency key.
        /// </summary>
        /// <param name="autoDetectAeroGlassCompositionAndTransparencyKey">
        /// If true, it checks to see wheter it is possible to the apply the use of the windows aero glass and transparency key. If so, it applies them.
        /// Otherwise, it they cannot be applied or this value is false, it keeps them disabled.
        /// </param>
        public AeroGlassForm(bool autoDetectAeroGlassCompositionAndTransparencyKey)
        : this(autoDetectAeroGlassCompositionAndTransparencyKey, false, false) {
        }
        /// <summary>
        /// Initializes a new Instance of AeroGlassForm class indicating wheter it should use of the windows aero glass and transparency key.
        /// </summary>
        /// <param name="useAeroGlassComposition">
        /// A value indicating wheter this form should use the windows aero glass composition (if possible).
        /// </param>
        /// <param name="useTransparencyKey">
        /// A value indicating wheter this form should use the transparency key.
        /// </param>
        public AeroGlassForm(bool useAeroGlassComposition, bool useTransparencyKey)
        : this(false, useAeroGlassComposition, useTransparencyKey) {
        }
        /// <summary>
        /// Initializes a new Instance of AeroGlassForm class indicating wheter it should use of the windows aero glass and transparency key.
        /// </summary>
        /// <param name="autoDetectAeroGlassCompositionAndTransparencyKey">
        /// If true, it checks to see wheter it is possible to the apply the use of the windows aero glass and transparency key. If so, it applies them.
        /// Otherwise, if it they cannot be applied or this value is false, it uses the 'useAeroGlassComposition' to apply or remove the windows aero glass for this form (if possible at all) and uses 'useTransparencyKey' to apply or remove the transparency key for this form.
        /// </param>
        /// <param name="useAeroGlassComposition">
        /// A value indicating wheter this form should use the windows aero glass composition (if possible).
        /// This value is ignored if 'autoDetectAeroGlassCompositionAndTransparencyKey' is true.
        /// </param>
        /// <param name="useTransparencyKey">
        /// A value indicating wheter this form should use the transparency key.
        /// This value is ignored if 'autoDetectAeroGlassCompositionAndTransparencyKey' is true.
        /// </param>
        public AeroGlassForm(bool autoDetectAeroGlassCompositionAndTransparencyKey, bool useAeroGlassComposition, bool useTransparencyKey)
        : this(Messages.All, autoDetectAeroGlassCompositionAndTransparencyKey, useAeroGlassComposition, useTransparencyKey) {
        }
        /// <summary>
        /// Initializes a new Instance of AeroGlassForm class with automatic detection whether to enable or disable the windows aero glass composition and transparency key for this form.
        /// </summary>
        /// <param name="autoHandleMessages">
        /// A value determening what messages this form handles automatically.
        /// </param>
        protected AeroGlassForm(Messages autoHandleMessages)
            : this(autoHandleMessages, DefualtAutoDetectAeroGlassCompositionAndTransparencyKey) {
        }
        /// <summary>
        /// Initializes a new Instance of AeroGlassForm class with the specified AutoHandelMessagesAndEvents value and indicating wheter it should use of the windows aero glass and transparency key.
        /// </summary>
        /// <param name="autoHandleMessages">
        /// A value determening what messages this form handles automatically.
        /// </param>
        /// <param name="autoDetectAeroGlassCompositionAndTransparencyKey">
        /// If true, it checks to see wheter it is possible to the apply the use of the windows aero glass and transparency key. If so, it applies them.
        /// Otherwise, it they cannot be applied or this value is false, it keeps them disabled.
        /// </param>
        protected AeroGlassForm(Messages autoHandleMessages, bool autoDetectAeroGlassCompositionAndTransparencyKey)
            : this(autoHandleMessages, autoDetectAeroGlassCompositionAndTransparencyKey, false, false) {
        }
        /// <summary>
        /// Initializes a new Instance of AeroGlassForm class with the specified AutoHandelMessagesAndEvents value and indicating wheter it should use of the windows aero glass and transparency key.
        /// </summary>
        /// <param name="autoHandleMessages">
        /// A value determening what messages this form handles automatically.
        /// </param>
        /// <param name="useAeroGlassComposition">
        /// A value indicating wheter this form should use the windows aero glass composition (if possible).
        /// </param>
        /// <param name="useTransparencyKey">
        /// A value indicating wheter this form should use the transparency key.
        /// </param>
        protected AeroGlassForm(Messages autoHandleMessages, bool useAeroGlassComposition, bool useTransparencyKey)
            : this(autoHandleMessages, false, useAeroGlassComposition, useTransparencyKey) {
        }
        /// <summary>
        /// Initializes a new Instance of AeroGlassForm class with the specified AutoHandelMessagesAndEvents value and indicating wheter it should use the windows aero glass and transparency key.
        /// </summary>
        /// <param name="autoHandleMessages">
        /// A value determening what messages this form handles automatically.
        /// </param>
        /// <param name="autoDetectAeroGlassCompositionAndTransparencyKey">
        /// If true, it checks to see wheter it is possible to the apply the use of the windows aero glass and transparency key. If so it applies them.
        /// Otherwise, if it they cannot be applied or this value is false, it uses the 'useAeroGlassComposition' to apply or remove the windows aero glass for this form (if possible at all) and uses 'useTransparencyKey' to apply or remove the transparency key for this form.
        /// </param>
        /// <param name="useAeroGlassComposition">
        /// A value indicating wheter this form should use the windows aero glass composition (if possible).
        /// This value is ignored if 'autoDetectAeroGlassCompositionAndTransparencyKey' is true.
        /// </param>
        /// <param name="useTransparencyKey">
        /// A value indicating wheter this form should use the transparency key.
        /// This value is ignored if 'autoDetectAeroGlassCompositionAndTransparencyKey' is true.
        /// </param>
        protected AeroGlassForm(Messages autoHandleMessages, bool autoDetectAeroGlassCompositionAndTransparencyKey, bool useAeroGlassComposition, bool useTransparencyKey) {
            this.AutoHandleMessagesStyle = autoHandleMessages;
            this.SetAeroGlassCompositionAndTransparencyKey(autoDetectAeroGlassCompositionAndTransparencyKey, useAeroGlassComposition, useTransparencyKey);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Automatically detect whether to enable or disable the windows aero glass composition and transparency key for this form and apply them if changes are needed.
        /// </summary>
        public void SetAeroGlassCompositionAndTransparencyKey() {
            this.SetAeroGlassCompositionAndTransparencyKey(DefualtAutoDetectAeroGlassCompositionAndTransparencyKey);
        }
        /// <summary>
        /// Specifies wheter this form should use the windows aero glass and transparency key.
        /// </summary>
        /// <param name="autoDetectAeroGlassCompositionAndTransparencyKey">
        /// If true, it checks to see wheter it is possible to the apply the use of the windows aero glass and transparency key. If so, it applies them.
        /// Otherwise, it they cannot be applied or this value is false, it keeps them disabled.
        /// </param>
        public void SetAeroGlassCompositionAndTransparencyKey(bool autoDetectAeroGlassCompositionAndTransparencyKey) {
            this.SetAeroGlassCompositionAndTransparencyKey(true, false, false);
        }
        /// <summary>
        /// Specifies wheter this form should use the windows aero glass and transparency key.
        /// </summary>
        /// <param name="useAeroGlassComposition">
        /// A value indicating wheter this form should use the windows aero glass composition (if possible).
        /// </param>
        /// <param name="useTransparencyKey">
        /// A value indicating wheter this form should use the transparency key.
        /// </param>
        public void SetAeroGlassCompositionAndTransparencyKey(bool useAeroGlassComposition, bool useTransparencyKey) {
            this.SetAeroGlassCompositionAndTransparencyKey(false, useAeroGlassComposition, useTransparencyKey);
        }
        /// <summary>
        /// Specifies wheter this form should use the windows aero glass and transparency key.
        /// </summary>
        /// <param name="autoDetectAeroGlassCompositionAndTransparencyKey">
        /// If true, it checks to see wheter it is possible to the apply the use of the windows aero glass and transparency key. If so it applies them.
        /// Otherwise, if it they cannot be applied or this value is false, it uses the 'useAeroGlassComposition' to apply or remove the windows aero glass for this form (if possible at all) and uses 'useTransparencyKey' to apply or remove the transparency key for this form.
        /// </param>
        /// <param name="useAeroGlassComposition">
        /// A value indicating wheter this form should use the windows aero glass composition (if possible).
        /// This value is ignored if 'autoDetectAeroGlassCompositionAndTransparencyKey' is true.
        /// </param>
        /// <param name="useTransparencyKey">
        /// A value indicating wheter this form should use the transparency key.
        /// This value is ignored if 'autoDetectAeroGlassCompositionAndTransparencyKey' is true.
        /// </param>
        public void SetAeroGlassCompositionAndTransparencyKey(bool autoDetectAeroGlassCompositionAndTransparencyKey, bool useAeroGlassComposition, bool useTransparencyKey) {
            if (autoDetectAeroGlassCompositionAndTransparencyKey) {
                useAeroGlassComposition = IsWindowsAeroGlassCompositionSupportedAndEnabled;
                useTransparencyKey = useAeroGlassComposition;
            }
            if (this.IsHandleCreatedRunTime) {
                if (!autoDetectAeroGlassCompositionAndTransparencyKey || IsDwmSupported) {
                    this.UseAeroGlassComposition = useAeroGlassComposition;
                }
                this.UseTransparencyKey = useTransparencyKey;
            }
            else {
                this.UseAeroGlassCompositionProtected = useAeroGlassComposition;
                this.UseTransparencyKeyProtected = useTransparencyKey;
            }
        }
        /// <summary>
        /// Throws a 'NotSupportedException' if the current running operation does not support the DWM (Desktop Windows Manager) functions.
        /// </summary>
        protected static void ThrowIfDwmIsNotSupported() {
            if (IsDwmSupported) {
                return;
            }
            throw new NotSupportedException("DWM (Desktop Windows Manager) is not supported by the operation system.");
        }
        protected virtual void OnColorizationColorChanged(ColorizationColorChangedEventArgs e) {
            this.RaiseColorizationColorChanged(e);
        }
        protected virtual void OnCompositionChanged(CompositionChangedEventArgs e) {
            if (this.AutoHandleMessagesStyle.HasFlags(Messages.CompositionChanged)) {
                if (this.UseAeroGlassComposition && IsDwmSupported && e.Enabled) {
                    this.UseAeroGlassCompositionCore = true;
                }
                if (this.UseTransparencyKey) {
                    if (this.UseAeroGlassComposition) {
                        this.UseTransparencyKeyCore = e.Enabled;
                    }
                    else if (!e.Enabled) {
                        Message m;

                        m = Message.Create(this.Handle, (int)Win32Constants.WM.NCACTIVATE, this.IsActiveWParam, IntPtr.Zero);
                        this.WndProc(ref m);
                        //this.DefWndProc(ref m);
                        //Win32Methods.CallWindowProc(Win32Methods.GetWindowLong(new HandleRef(this, this.Handle), -4),
                        //                            this.Handle,
                        //                            (int)Win32Constants.WM.NCACTIVATE,
                        //                            this.IsActiveWParam,
                        //                            IntPtr.Zero);
                        //Win32Methods.DefWindowProc(this.Handle, (int)Win32Constants.WM.NCACTIVATE, this.IsActiveWParam, IntPtr.Zero);
                    }
                }
            }
            this.RaiseCompositionChanged(e);
        }
        protected virtual void OnNonClientAreaRenderingChanged(NonClientAreaRenderingChangedEventArgs e) {
            if (this.ShouldAutoHandleMessages(Messages.NonClientAreaRenderingChanged) && this.FormBorderStylePrevAeroGlassCompositionEnable.HasValue &&
                this.FormBorderStyle != this.FormBorderStylePrevAeroGlassCompositionEnable &&
                (this.FormBorderStyle == FormBorderStyle.None || this.FormBorderStylePrevAeroGlassCompositionEnable == FormBorderStyle.None) &&
                this.IsAeroGlassCompositionSupportedAndEnabled) {
                this.UseAeroGlassCompositionCore = true;
            }
            this.RaiseNonClientAreaRenderingChanged(e);
        }
        protected override void OnControlAdded(ControlEventArgs e) {
            if (this.AutoHandleMessagesStyle.HasFlags(Messages.ControlAdded) && this.IsTransparencyKeyEnabled) {
                this.SetAeroGlassCompatibleControlBackColor(e.Control, true);
            }
            if (e.Control is IAeroGlassCompatibleControl) {
                this.IAeroGlassCompatibleControls.Add(e.Control);
            }
            base.OnControlAdded(e);
        }
        protected override void OnControlRemoved(ControlEventArgs e) {
            base.OnControlRemoved(e);
            if (this.AutoHandleMessagesStyle.HasFlags(Messages.ControlRemoved)) {
                this.SetAeroGlassCompatibleControlBackColor(e.Control, false);
            }
            if (e.Control is IAeroGlassCompatibleControl) {
                this.IAeroGlassCompatibleControls.Remove(e.Control);
            }
        }
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            this.CatchStyleChanged = true;
            if (!this.AutoHandleMessagesStyle.HasFlags(Messages.HandleCreated)) {
                return;
            }
            if (IsDwmSupported && this.UseAeroGlassComposition) {
                this.UseAeroGlassCompositionCore = true;
            }
            if (this.UseTransparencyKey) {
                this.UseTransparencyKeyCore = true;
            }
        }
        protected override void OnPaintBackground(PaintEventArgs e) {
            if (this.AutoHandleMessagesStyle.HasFlags(Messages.PaintBackground)) {
                if (this.IsTransparencyKeyEnabled) {
                    e.Graphics.Clear(this.TransparencyKey);
                }
                else if (this.IsAeroGlassCompositionSupportedAndEnabled) {
                    e.Graphics.Clear(Color.Black);
                }
                else {
                    base.OnPaintBackground(e);
                }
            }
            else {
                base.OnPaintBackground(e);
            }
        }
        //I think it would be more neat to handle the allow transparency changed in the 'WndProc' method, but for that we need the exact message windows sends when it is changed, so for now, it's here.
        protected override void OnStyleChanged(EventArgs e) {
            base.OnStyleChanged(e);
            if (this.ShouldAutoHandleMessages(Messages.StyleChanged) && this.CatchStyleChanged && this.UseTransparencyKey) {
                bool useTransparencyKey;

                useTransparencyKey = this.AllowTransparency;
                this.UseTransparencyKeyProtected = useTransparencyKey;
                this.UseTransparencyKeyCore = this.AllowTransparency;
            }
        }
        protected override void WndProc(ref Message m) {
            switch ((DwmApi.WM_DWM_CHANGED)m.Msg) {
                case DwmApi.WM_DWM_CHANGED.COMPOSITION:
                    this.OnCompositionChanged(new CompositionChangedEventArgs(IsWindowsAeroGlassCompositionSupportedAndEnabled));
                    break;
                case DwmApi.WM_DWM_CHANGED.NCRENDERING:
                    this.OnNonClientAreaRenderingChanged(new NonClientAreaRenderingChangedEventArgs(m.WParam.ToInt32() != 0));
                    break;
                case DwmApi.WM_DWM_CHANGED.COLORIZATIONCOLOR:
                    this.OnColorizationColorChanged(new ColorizationColorChangedEventArgs(Color.FromArgb(m.WParam.ToInt32()), m.LParam.ToInt32() != 0));
                    break;
            }
            base.WndProc(ref m);
        }
        /// <summary>
        /// Applies aero glass composition for this form (according to this form's border style: if it is 'None' it uses 'BlurBehind' method, otherwise it uses 'ExtendFrameIntoClientArea' method).
        /// No checks and validation are made, so use it carefully.
        /// </summary>
        /// <returns>
        /// If it succeeded, it returns 0 (DwmApi.HRESULT.S_OK), otherwise it returns the windows error code.
        /// </returns>
        protected DwmApi.HRESULT ApplyAeroGlassCompositionCore() {
            DwmApi.HRESULT hResult;

            if (this.FormBorderStyle == FormBorderStyle.None) {
                DwmApi.DWM_BLURBEHIND dwm_blurBehind;

                dwm_blurBehind = new DwmApi.DWM_BLURBEHIND(true);
                hResult = this.DwmEnableBlurBehindWindow(ref dwm_blurBehind);
            }
            else {
                DwmApi.MARGINS margins;

                margins = new DwmApi.MARGINS(true);
                hResult = this.DwmExtendFrameIntoClientArea(ref margins);
            }
            return hResult;
        }
        /// <summary>
        /// Applies the aero glass composition using the 'BlurBehind' method (see https://msdn.microsoft.com/en-us/library/windows/desktop/aa969508%28v=vs.85%29.aspx).
        /// No checks and validation are made, so use it carefully.
        /// </summary>
        /// <param name="pBlurBehind">
        /// A 'DwmApi.DWM_BLURBEHIND' structure that provides blur behind data
        /// </param>
        /// <returns>
        /// If it succeeded, it returns 0 (DwmApi.HRESULT.S_OK), otherwise it returns the windows error code.
        /// </returns>
        protected DwmApi.HRESULT DwmEnableBlurBehindWindow([In] ref DwmApi.DWM_BLURBEHIND pBlurBehind) {
            return DwmApi.DwmEnableBlurBehindWindow(this.Handle, ref pBlurBehind);
        }
        /// <summary>
        /// Applies the aero glass composition using the 'ExtendFrameIntoClientArea' method (see https://msdn.microsoft.com/en-us/library/windows/desktop/aa969512%28v=vs.85%29.aspx).
        /// No checks and validation are made, so use it carefully.
        /// </summary>
        /// <param name="pMarInset">
        /// A 'DwmApi.MARGINS' structure that describes the margins to use when extending the frame into the client area
        /// </param>
        /// <returns>
        /// If it succeeded, it returns 0 (DwmApi.HRESULT.S_OK), otherwise it returns the windows error code.
        /// </returns>
        protected DwmApi.HRESULT DwmExtendFrameIntoClientArea([In] ref DwmApi.MARGINS pMarInset) {
            return DwmApi.DwmExtendFrameIntoClientArea(this.Handle, ref pMarInset);
        }
        /// <summary>
        /// Raises the 'ColorizationColorChanged' event.
        /// </summary>
        /// <param name="e">
        /// The event args to use when raising the event
        /// </param>
        protected void RaiseColorizationColorChanged(ColorizationColorChangedEventArgs e) {
            ColorizationColorEventHandler eh;

            eh = (ColorizationColorEventHandler)this.Events[EventHandlerKeyColorizationColor];
            if (eh != null) {
                eh(this, e);
            }
        }
        /// <summary>
        /// Raises the 'CompositionChanged' event.
        /// </summary>
        /// <param name="e">
        /// The event args to use when raising the event
        /// </param>
        protected void RaiseCompositionChanged(CompositionChangedEventArgs e) {
            CompositionChangedEventHandler eh;

            eh = (CompositionChangedEventHandler)this.Events[EventHandlerKeyComposition];
            if (eh != null) {
                eh(this, e);
            }
        }
        /// <summary>
        /// Raises the 'NonClientAreaRenderingChanged' event.
        /// </summary>
        /// <param name="e">
        /// The event args to use when raising the event
        /// </param>
        protected void RaiseNonClientAreaRenderingChanged(NonClientAreaRenderingChangedEventArgs e) {
            NonClientAreaRenderingEventHandler eh;

            eh = (NonClientAreaRenderingEventHandler)this.Events[EventHandlerKeyNonClientAreaRendering];
            if (eh != null) {
                eh(this, e);
            }
        }
        /// <summary>
        /// Removes the aero glass composition for this form depending on the form border style that the form had the last time the aero glass composition was enabled for it.
        /// No checks and validation are made, so use it carefully.
        /// </summary>
        /// <returns>
        /// If it succeeded, it returns 0 (DwmApi.HRESULT.S_OK), otherwise it returns the windows error code.
        /// </returns>
        protected DwmApi.HRESULT RemoveAeroGlassCompositionCore() {
            DwmApi.HRESULT hResult;

            if (this.FormBorderStylePrevAeroGlassCompositionEnable.Value == FormBorderStyle.None) {
                DwmApi.DWM_BLURBEHIND dwm_blurBehind;

                dwm_blurBehind = new DwmApi.DWM_BLURBEHIND(false);
                hResult = this.DwmEnableBlurBehindWindow(ref dwm_blurBehind);
            }
            else {
                DwmApi.MARGINS margins;

                margins = new DwmApi.MARGINS();
                hResult = this.DwmExtendFrameIntoClientArea(ref margins);
            }
            return hResult;
        }
        private void SetAeroGlassCompatibleControlBackColor(Control control, bool useTransparencyKey) {
            IAeroGlassCompatibleControl aeroGlassCompatibleControl;

            aeroGlassCompatibleControl = control as IAeroGlassCompatibleControl;
            if (aeroGlassCompatibleControl != null) {
                Color backColor;

                backColor = useTransparencyKey ? this.TransparencyKey == Color.Empty ? DefaultTransparencyKeyColor : this.TransparencyKey : aeroGlassCompatibleControl.PrevBackColor;
                if (control.BackColor != backColor) {
                    bool aeroGlassCompatibleControl_atchBackColorChanged;

                    aeroGlassCompatibleControl_atchBackColorChanged = aeroGlassCompatibleControl.CatchBackColorChanged;
                    aeroGlassCompatibleControl.CatchBackColorChanged = false;
                    control.BackColor = backColor;
                    aeroGlassCompatibleControl.CatchBackColorChanged = aeroGlassCompatibleControl_atchBackColorChanged;
                }
            }
        }
        private bool ShouldAutoHandleMessages(Messages messages) {
            return this.IsHandleCreatedRunTime && this.AutoHandleMessagesStyle.HasFlags(messages);
        }
        private bool ShouldSerializeUseAeroGlassComposition() {
            return this.UseAeroGlassComposition;
        }
        private bool ShouldSerializeUseTransparencyKey() {
            return this.UseTransparencyKey;
        }
        #endregion

        #region Events
        /// <summary>
        /// Occurs when color of the DWM (Desktop Windows Manager) composition changes. This event is only raised if this form is a top-level window.
        /// </summary>
        [Category("Desktop Window Manager"),
         Description("Occurs when color of the DWM (Desktop Windows Manager) composition changes. This event is only raised if the form is a top-level window.")]
        public event ColorizationColorEventHandler ColorizationColorChanged {
            add {
                this.Events.AddHandler(EventHandlerKeyColorizationColor, value);
            }
            remove {
                this.Events.RemoveHandler(EventHandlerKeyColorizationColor, value);
            }
        }
        /// <summary>
        /// Occurs when the DWM (Desktop Window Manager) composition is enabled or disabled. This event is only raised if this form is a top-level window.
        /// </summary>
        [Category("Desktop Window Manager"),
         Description("Occurs when the DWM (Desktop Window Manager) composition is enabled or disabled. This event is only raised if the form is a top-level window.")]
        public event CompositionChangedEventHandler CompositionChanged {
            add {
                this.Events.AddHandler(EventHandlerKeyComposition, value);
            }
            remove {
                this.Events.RemoveHandler(EventHandlerKeyComposition, value);
            }
        }
        /// <summary>
        /// Occurs when the non-client area rendering policy changes.
        /// </summary>
        [Category("Desktop Window Manager"), Description("Occurs when the non-client area rendering policy changes.")]
        public event NonClientAreaRenderingEventHandler NonClientAreaRenderingChanged {
            add {
                this.Events.AddHandler(EventHandlerKeyNonClientAreaRendering, value);
            }
            remove {
                this.Events.RemoveHandler(EventHandlerKeyNonClientAreaRendering, value);
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get or sets whether the windows aero glass composition is enabled (generally, not just for this form).
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// DWM (Desktop Windows Manager) is not supported by the operation system.
        /// </exception>
        public static bool IsWindowsAeroGlassCompositionEnabled {
            get {
                ThrowIfDwmIsNotSupported();
                return DwmApi.DwmIsCompositionEnabled();
            }
            set {
                ThrowIfDwmIsNotSupported();
                DwmApi.DwmEnableComposition(value ? DwmApi.DWM_EC_COMPOSITION.ENABLE : DwmApi.DWM_EC_COMPOSITION.DISABLE);
            }
        }
        /// <summary>
        /// Get or sets whether the windows aero glass composition is supported and enabled (generally, not just for this form).
        /// </summary>
        public static bool IsWindowsAeroGlassCompositionSupportedAndEnabled {
            get {
                return IsDwmSupported && IsWindowsAeroGlassCompositionEnabled;
            }
        }
        /// <summary>
        /// Gets or sets a value indicating whether this form should use the windows aero glass composition.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// DWM (Desktop Windows Manager) is not supported by the operation system.
        /// </exception>
        [Bindable(true), Category("Desktop Window Manager"), DefaultValue(false),
         Description("Determines whether the form uses window's aero glass composition which is provided by the DWM (Desktop Windows Manager)."), SettingsBindable(true)]
        public virtual bool UseAeroGlassComposition {
            get {
                return this.UseAeroGlassCompositionProtected;
            }
            set {
                this.UseAeroGlassCompositionProtected = value;
                if (!this.IsHandleCreatedRunTime) {
                    this.IsAeroGlassCompositionEnabledProtected = false;
                    return;
                }
                ThrowIfDwmIsNotSupported();
                if (value == this.IsAeroGlassCompositionEnabled) {
                    return;
                }
                this.UseAeroGlassCompositionCore = value;
            }
        }
        /// <summary>
        /// Gets or sets a value indicating whether this form should use the transparency key
        /// (to solve rendering problems that occuring when the windows aero glass composition is enabled for this form).
        /// </summary>
        [Bindable(true), Category("Desktop Window Manager"), DefaultValue(false),
         Description(
         "Determines whether the form should paint it's background color with it's transparency key and sets the background color of it's controls which render text directly on it to it's transparency key. This corrects many rendering behavior problems which are caused by enabling the use of window's aero glass composition for the form."
         ), SettingsBindable(true)]
        public virtual bool UseTransparencyKey {
            get {
                return this.UseTransparencyKeyProtected;
            }
            set {
                this.UseTransparencyKeyProtected = value;
                if (!this.IsHandleCreatedRunTime) {
                    this.IsTransparencyKeyEnabledProtected = false;
                    return;
                }
                if (value == this.IsTransparencyKeyEnabled) {
                    return;
                }
                this.UseTransparencyKeyCore = value;
            }
        }
        /// <summary>
        /// Gets a value indicating whether the windows aero glass composition is enabled for this form.
        /// </summary>
        [Browsable(false)]
        public bool IsAeroGlassCompositionEnabled {
            get {
                return this.IsAeroGlassCompositionEnabledProtected && this.IsHandleCreatedRunTime && IsWindowsAeroGlassCompositionEnabled;
            }
        }
        /// <summary>
        /// Gets a value indicating whether the windows aero glass composition is supported and enabled for this form.
        /// </summary>
        [Browsable(false)]
        public bool IsAeroGlassCompositionSupportedAndEnabled {
            get {
                return this.IsAeroGlassCompositionEnabledProtected && this.IsHandleCreatedRunTime && IsWindowsAeroGlassCompositionSupportedAndEnabled;
            }
        }
        /// <summary>
        /// Gets a value indicating whether the transparency key is enabled for this form.
        /// </summary>
        [Browsable(false)]
        public bool IsTransparencyKeyEnabled {
            get {
                return this.IsTransparencyKeyEnabledProtected && this.IsHandleCreatedRunTime && this.AllowTransparency;
            }
        }
        /// <summary>
        /// Gets or sets a value indicating whether this form should use the windows aero glass composition.
        /// No further checks and validation are made, so use it carefully.
        /// </summary>
        protected virtual bool UseAeroGlassCompositionCore {
            set {
                if (this.IsAeroGlassCompositionEnabledProtected && this.FormBorderStylePrevAeroGlassCompositionEnable.HasValue) {
                    this.IsAeroGlassCompositionEnabledProtected = this.RemoveAeroGlassCompositionCore() != DwmApi.HRESULT.S_OK;
                }
                if (value) {
                    this.IsAeroGlassCompositionEnabledProtected = this.ApplyAeroGlassCompositionCore() == DwmApi.HRESULT.S_OK;
                    if (this.IsAeroGlassCompositionEnabledProtected) {
                        this.FormBorderStylePrevAeroGlassCompositionEnable = this.FormBorderStyle;
                    }
                }
                this.Invalidate();
            }
        }
        /// <summary>
        /// Gets or sets a value indicating whether this form should use the transparency key
        /// (to solve rendering problems that occuring when the windows aero glass composition is enabled for this form).
        /// No further checks and validation are made, so use it carefully.
        /// </summary>
        protected virtual bool UseTransparencyKeyCore {
            set {
                {
                    Color transparencyKeyColor,
                          this_transparencyKey;

                    this_transparencyKey = this.TransparencyKey;
                    transparencyKeyColor = value ? this_transparencyKey == Color.Empty ? DefaultTransparencyKeyColor : this_transparencyKey : Color.Empty;
                    if (this_transparencyKey != transparencyKeyColor) {
                        this.CatchStyleChanged = false;
                        this.TransparencyKey = transparencyKeyColor;
                        this.CatchStyleChanged = true;
                    }
                }
                foreach (Control control in this.IAeroGlassCompatibleControls) {
                    this.SetAeroGlassCompatibleControlBackColor(control, value);
                }
                this.IsTransparencyKeyEnabledProtected = value;
            }
        }
        /// <summary>
        /// Gets or sets a value determening what messages this form handles automatically.
        /// </summary>
        protected Messages AutoHandleMessagesStyle {
            get;
            set;
        }
        /// <summary>
        /// Determines whether this form should handle the 'OnStyleChanged' method.
        /// </summary>
        protected bool CatchStyleChanged {
            get;
            set;
        }
        /// <summary>
        /// Gets a list of this form's controls which implement the interface 'IAeroGlassCompatibleControl'
        /// </summary>
        protected List<Control> IAeroGlassCompatibleControls {
            get {
                return this._iAeroGlassCompatibleControls ?? (this._iAeroGlassCompatibleControls = new List<Control>());
            }
        }
        /// <summary>
        /// A value indicating the whether the windows aero glass composition is enabled for this form
        /// from the result of the last 'UseAeroGlassCompositionCore' call, set this only when you know what you're doing)
        /// </summary>
        protected bool IsAeroGlassCompositionEnabledProtected {
            get;
            set;
        }
        /// <summary>
        /// Gets a value indicating whether this form's handle was created and is not in design mode.
        /// </summary>
        protected bool IsHandleCreatedRunTime {
            get {
                return this.IsHandleCreated && !this.DesignMode;
            }
        }
        /// <summary>
        /// A value indicating the whether the transparency key is enabled for this form
        /// (from the result of the last 'UseTransparencyKeyCore' call, set this only when you know what you're doing)
        /// </summary>
        protected bool IsTransparencyKeyEnabledProtected {
            get;
            set;
        }
        /// <summary>
        /// A value indicating wheter this form should use the aero glass composition
        /// </summary>
        protected bool UseAeroGlassCompositionProtected {
            get;
            set;
        }
        /// <summary>
        /// A value indicating wheter this form should use the transparency key.
        /// </summary>
        protected bool UseTransparencyKeyProtected {
            get;
            set;
        }
        private FormBorderStyle? FormBorderStylePrevAeroGlassCompositionEnable {
            get;
            set;
        }
        private IntPtr IsActiveWParam {
            get {
                return (bool)Type_Form_Active.GetValue(this,
                                                       new object[] {
                                                       })
                       ? new IntPtr(1)
                       : IntPtr.Zero;
            }
        }
        #endregion
    }
}