using System.Collections.Generic;
using System.Drawing;

namespace VirtualDesktopManager
{
    class Configuration : IConfiguration
    {
        public IConfiguration.ITrayIcon TrayIcon(int desktopIndex)
        {
            // Lucida Sans Typewritter: 99  >> 170px,regular,0+0
            // Lucida Sans Typewritter: 999 >> 125px,regular,0+0
            // DejaVu Sans Mono
            // OCR A
            if (desktopIndex < 10)
            {

                return new TrayIconConfig()
                {
                    FontName = "DejaVu Sans Mono",
                    FontSize = 275,
                    FontStyle = FontStyle.Bold,
                    FontColor = Brushes.White,
                    OffsetX = 0,
                    OffsetY = 0,
                };
            }
            if (desktopIndex < 100)
            {
                return new TrayIconConfig()
                {
                    FontName = "Lucida Sans Typewritter",
                    FontSize = 170,
                    FontStyle = FontStyle.Regular,
                    FontColor = Brushes.White,
                    OffsetX = 0,
                    OffsetY = 0,
                };
            }
            return new TrayIconConfig()
            {
                FontName = "Lucida Sans Typewritter",
                FontSize = 125,
                FontStyle = FontStyle.Regular,
                FontColor = Brushes.White,
                OffsetX = 0,
                OffsetY = 0,
            };
        }
        
        private class TrayIconConfig : IConfiguration.ITrayIcon
        {
            public string FontName { get; protected internal set; }
            public int FontSize { get; protected internal set; }
            public FontStyle FontStyle { get; protected internal set; }
            public Brush FontColor { get; protected internal set; }
            public GraphicsUnit FontGraphicsUnit => GraphicsUnit.Pixel;
            public int OffsetX { get; protected internal set; }
            public int OffsetY { get; protected internal set; }
            public string Format(int desktopIndex)
            {
                var num = desktopIndex.ToString();
                return (num.Length <= 2
                    ? num
                    : num.Substring(0, 2) + "\n" + num.Substring(2)
                );
            }
        }
    }

    public interface IConfiguration
    {
        IConfiguration.ITrayIcon TrayIcon(int desktopIndex);
        
        public interface ITrayIcon
        {
            string FontName { get; }
            int FontSize { get; }
            FontStyle FontStyle { get; }
            Brush FontColor { get; }
            GraphicsUnit FontGraphicsUnit { get; }
            int OffsetX { get; }
            int OffsetY { get; }
            string Format(int desktopIndex);
        }
    }
}
