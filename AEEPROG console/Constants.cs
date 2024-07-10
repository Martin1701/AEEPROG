using System.Globalization;
using System.Reflection;

namespace AEEProg
{
    internal class Constants
    {
        /* version of the program */
        public const string version = "1.0";
        /* number of data bytes per record (line) of outputting Intel HEX file */
        public const int outputRecordByteCount = 32;
        /* number of data bytes per element of programmer data*/
        public const int programmerDataByteCount = 32;
        /* size of programmer write chunk in bytes */
        public const byte writeChunkSize = 255;


        public static DateTime GetLinkerTime(Assembly? assembly)
        {
            if(assembly == null) throw new ArgumentNullException(nameof(assembly));
            const string BuildVersionMetadataPrefix = "+build";

            var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (attribute?.InformationalVersion != null)
            {
                var value = attribute.InformationalVersion;
                var index = value.IndexOf(BuildVersionMetadataPrefix);
                if (index > 0)
                {
                    value = value[(index + BuildVersionMetadataPrefix.Length)..];
                    return DateTime.ParseExact(value, "yyyy-MM-ddTHH:mm:ss:fffZ", CultureInfo.InvariantCulture);
                }
            }

            return default;
        }
    }
}