using System.Numerics;

namespace QuestoGraph.Data.Settings
{
    public class ColorSettings
    {
        public Vector4 SidebarDefaultColor { get; set; } = new Vector4(1f, 1f, 1f, 1f);

        public Vector4 SidebarCompletedColor { get; set; } = new Vector4(1f, 1f, 1f, 0.49f);

        public Vector4 SidebarMSQColor { get; set; } = new Vector4(1f, 0.875f, 0.5f, 1f);

        public Vector4 SidebarMSQCompletedColor { get; set; } = new Vector4(1f, 0.875f, 0.5f, 0.58f);

        public Vector4 SidebarBlueColor { get; set; } = new Vector4(0.58f, 0.69f, 0.98f, 1f);

        public Vector4 SidebarBlueCompletedColor { get; set; } = new Vector4(0.58f, 0.69f, 0.98f, 0.49f);
    }
}
