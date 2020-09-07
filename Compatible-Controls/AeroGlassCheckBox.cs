using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace Utility.Windows.Forms {
    [ToolboxBitmap(typeof(CheckBox))]
    public class AeroGlassCheckBox : CheckBox, IAeroGlassCompatibleControl {
        public AeroGlassCheckBox() {
            this.PrevBackColor = DefaultBackColor;
        }

        protected override void OnBackColorChanged(EventArgs e) {
            base.OnBackColorChanged(e);
            if (this.CatchBackColorChanged) {
                this.PrevBackColor = this.BackColor;
            }
        }
        protected override void OnPaint(PaintEventArgs e) {
            {
                Graphics g;

                g = e.Graphics;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            }
            base.OnPaint(e);
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color PrevBackColor {
            get;
            private set;
        }
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool CatchBackColorChanged {
            get;
            set;
        }
    }
}
