using System.Drawing;

namespace SmokeScreenEngine
{
    public static class Theme
    {
        public static Color Background = Color.FromArgb(11, 15, 20);
        public static Color CardBackground = Color.FromArgb(17, 22, 28);
        public static Color AccentBlue = Color.FromArgb(31, 111, 235);
        public static Color Success = Color.FromArgb(52, 199, 89);
        public static Color Error = Color.FromArgb(245, 101, 101); // Added this to fix the build error
        public static Color TextSecondary = Color.FromArgb(150, 150, 150);
    }
}