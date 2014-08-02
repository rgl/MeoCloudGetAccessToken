using System.Globalization;

namespace MeoCloudGetAccessToken
{
    static class HumanReadableBytes
    {
        // Returns the human-readable file size for an arbitrary, 64-bit file size
        // The default format is "0.### XB", e.g. "4.2 KB" or "1.434 GB"
        //
        // NB this is a slightly modified version of the code at
        //    C# Human Readable File Size Optimized Function
        //    http://www.somacon.com/p576.php
        public static string ToHumanReadableBytes(this long i)
        {
            string suffix;
            double readable;

            if (i >= 0x1000000000000000) // Exabyte
            {
                suffix = "EB";
                readable = i >> 50;
            }
            else if (i >= 0x4000000000000) // Petabyte
            {
                suffix = "PB";
                readable = i >> 40;
            }
            else if (i >= 0x10000000000) // Terabyte
            {
                suffix = "TB";
                readable = i >> 30;
            }
            else if (i >= 0x40000000) // Gigabyte
            {
                suffix = "GB";
                readable = i >> 20;
            }
            else if (i >= 0x100000) // Megabyte
            {
                suffix = "MB";
                readable = i >> 10;
            }
            else if (i >= 0x400) // Kilobyte
            {
                suffix = "KB";
                readable = i;
            }
            else // Byte
            {
                return i + " B";
            }

            readable = readable / 1024;

            return (i < 0 ? "-" : "") + readable.ToString("0.### ", CultureInfo.InvariantCulture) + suffix;
        }
    }
}
