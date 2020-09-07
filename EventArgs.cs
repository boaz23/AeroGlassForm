using System;
using System.Drawing;

namespace Utility.Windows.Forms {
    public delegate void ColorizationColorEventHandler(object sender, ColorizationColorChangedEventArgs e);
    public delegate void CompositionChangedEventHandler(object sender, CompositionChangedEventArgs e);
    public delegate void NonClientAreaRenderingEventHandler(object sender, NonClientAreaRenderingChangedEventArgs e);

    public sealed class ColorizationColorChangedEventArgs : EventArgs {
        private Color m_colorizationColor;
        private bool m_isColorizationColorBlendedWithOpacity;

        public ColorizationColorChangedEventArgs(Color colorizationColor, bool isColorizationColorBlendedWithOpacity)
            : base() {
            this.m_colorizationColor = colorizationColor;
            this.m_isColorizationColorBlendedWithOpacity = isColorizationColorBlendedWithOpacity;
        }

        /// <summary>
        /// Get a value representing the color of the DWM (Desktop Windows Manager) aero glass composition.
        /// </summary>
        public Color ColorizationColor {
            get {
                return this.m_colorizationColor;
            }
        }
        /// <summary>
        /// Get a value indicating whether the color of the DWM (Desktop Windows Manager) aero glass composition is blended with opacity.
        /// </summary>
        public bool IsColorizationColorBlendedWithOpacity {
            get {
                return this.m_isColorizationColorBlendedWithOpacity;
            }
        }
    }
    public sealed class CompositionChangedEventArgs : EventArgs {
        private readonly bool m_enabled;

        public CompositionChangedEventArgs(bool enabled)
            : base() {
            this.m_enabled = enabled;
        }

        /// <summary>
        /// Get a value indicating whether the DWM (Desktop Windows Manager) aero glass composition is enabled or disabled.
        /// </summary>
        public bool Enabled {
            get {
                return this.m_enabled;
            }
        }
    }
    public sealed class NonClientAreaRenderingChangedEventArgs : EventArgs {
        private readonly bool m_enabled;

        public NonClientAreaRenderingChangedEventArgs(bool enabled)
            : base() {
            this.m_enabled = enabled;
        }

        /// <summary>
        /// Get a value indicating whether the DWM (Desktop Windows Manager) aero glass composition is enabled or disabled for the non-client area.
        /// </summary>
        public bool Enabled {
            get {
                return this.m_enabled;
            }
        }
    }
}
