# AeroGlassForm
DO NOT USE! 
Was once intended to render an entire windows form with the windows 7 transparent glass color.
Now just serves for my job applications.
The rest text below was from a long time ago, kept for history reasons.

---------------------------------------------------------------------------
Provides a form that can uses the windows aero glass composition. Comes with extra things required like compatible controls.

Notes:

    1. Drawing somethings on the form will appear in a odd way: any black text, will appear transparent and generally the controls will look wierd. It is solved by setting the form's 'TransparencyKey' property on and painting it's background with that color.
    2. Some transparency key colors will also cause the controls and the form to look wierd.
    3. In order for controls to appear correctly, their background color must (not necessarily the 'BackColor' property) be the 'TransparencyKey'. This is what we are doing while painting the form's background.
    4. Checkboxe's, label's and radio button's background color must be painted before painting anything else. Simply filling their client area with 'TransparencyKey' within the 'OnPaint' method won't work because if it is filled after the windows forms adapter renders them, only the their background color will be seen since it covers what the windows forms adapter painted, and if it is filled before the windows forms adapter renders it, it will get covered by what the windows forms adapter painted. Also, check boxes and radio buttons do not raise 'OnPaintBackground' method (which I think defeats the purpose of inheritance), so it's impossible to paint their client area's background with 'TransparencyKey' within 'OnPaintBackground' method.
    5. To solve the problem in note #4, we set the checkbox's, label's or radio button 'BackColor' to 'TransparencyKey' in order to let the windows forms adapter to paint them with that background color. However, we also want to keep track of the user's requested background color for checkboxes, labels and radio buttons. To achieve this, we use the 'IAeroGlassCompatibleControl' interface. The label also implement this interface just for consistency.
    6. Checkboxe's, label's and radio button's text will wierd on the form regardless of their background color. In order to fix that, they must use the 'AntiAliasGridFit' or 'SingleBitPerPixelGridFit' TextRenderingHint.
    7. Whenever the aero glass composition changes from enabled to disabled and only the transparency key is enabled, the non-client area turns black for some reason. To fix it, we can do multiple things:
        • Send a message to the form using 'WndProc' telling it that its non-client area needs to be changed to indicate an active or inactive state (obviously, sending the message with the current active state of the form).
        • Send a message to the default window procedure using 'DefWndProc' telling the form that its non-client area needs to be changed to indicate an active or inactive state (obviously, sending the message with the current active state of the form).
        • Tell Windows to send a message to the form that its non-client area needs to be changed to indicate an active or inactive state (obviously, sending the message with the current active state of the form).
        • Send Windows a message telling that the form's non-client area needs to be changed to indicate an active or inactive state (obviously, sending the message with the current active state of the form) using the default window procedure.


Also see http://stackoverflow.com/questions/5978086/windows-aero-glass-form-text-problem and http://stackoverflow.com/questions/4258295/aero-how-to-draw-solid-opaque-colors-on-glass for additional information.



Known Bugs:

    1. Drawing anything on the form while the aero glass composition in enabled by using 'BlurBehindWindow' will look wierd and the transparency key does not solve that.
