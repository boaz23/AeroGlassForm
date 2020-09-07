using System.Drawing;

namespace Utility.Windows.Forms {
    public interface IAeroGlassCompatibleControl {
        Color PrevBackColor {
            get;
        }
        bool CatchBackColorChanged {
            get;
            set;
        }
    }
}
