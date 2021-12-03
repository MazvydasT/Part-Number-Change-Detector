using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace PartNumberChangeDetector
{
    public static class Utils
    {
        private static readonly ConditionalWeakTable<object, Dictionary<string, string>> cache = new ConditionalWeakTable<object, Dictionary<string, string>>();
        private static readonly Regex driveLetterRegexp = new Regex(@"^([A-Za-z]:).*$", RegexOptions.Compiled);
        public static string PathToUNC(string path, object cacheKey = null)
        {
            if (string.IsNullOrWhiteSpace(path) || path.StartsWith(@"\\") || !Path.IsPathRooted(path)) return path;

            path = Path.GetFullPath(path);

            var driveLetterMatch = driveLetterRegexp.Match(path);

            if (driveLetterMatch.Success)
            {
                var drivePath = driveLetterMatch.Groups[1].Value;
                string driveAddress;

                if (cacheKey != null)
                {
                    if (!cache.TryGetValue(cacheKey, out var lookup))
                    {
                        lookup = new Dictionary<string, string>();
                        cache.Add(cacheKey, lookup);
                    }

                    if (!lookup.TryGetValue(drivePath, out driveAddress))
                    {
                        driveAddress = GetDriveAddress(drivePath);

                        lookup.Add(drivePath, driveAddress);
                    }
                }

                else driveAddress = GetDriveAddress(drivePath);

                if (driveAddress != null)
                {
                    path = path.Replace(drivePath, driveAddress);
                }
            }

            return path;
        }

        private static string GetDriveAddress(string drivePath)
        {
            using (var managementObject = new ManagementObject())
            {
                managementObject.Path = new ManagementPath(string.Format("Win32_LogicalDisk='{0}'", (object)drivePath));

                try
                {
                    return Convert.ToUInt32(managementObject["DriveType"]) == 4 ? Convert.ToString(managementObject["ProviderName"]) : drivePath;
                }

                catch (Exception)
                {
                    return null;
                }
            }
        }
    }
}
