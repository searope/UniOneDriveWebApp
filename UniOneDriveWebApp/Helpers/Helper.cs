using System;
using System.IO;
using Microsoft.OneDrive.Sdk;

namespace UniOneDriveWebApp.Helpers
{
    public static class Helper
    {
        public static string SizeFormatter(long size)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            while (size >= 1024 && ++order < sizes.Length)
            {
                size = size / 1024;
            }

            // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
            // show a single decimal place, and no space.
            return $"{size:0.##} {sizes[order]}";
        }

        public static string QuickXorHash(this Hashes hashes)
        {
            object quickXorHash = null;
            hashes?.AdditionalData?.TryGetValue("quickXorHash", out quickXorHash);
            return quickXorHash?.ToString();
        }
    }
}