using System.Text;
using System.Text.RegularExpressions;
using static AEEProg.Constants;
using static AEEProg.Program;

namespace AEEProg
{
    public class ProgrammerData
    {
        private int dataSizeCount = 0; // storing data size of last conversion in bytes
        private int IntelHEXMaxAddress = 0;
        private byte[][] data;

        private static readonly Regex allowedInputFiles = new Regex(@"\.(hex|ihex|rom|bin)$", RegexOptions.IgnoreCase);


        ProgrammerData(string @path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException($"Could not find file: ${path}");
            string fileSuffix = path[(path.LastIndexOf('.') + 1)..^0];

            if (!allowedInputFiles.IsMatch(fileSuffix))
            {
                throw new InvalidDataException($"{fileSuffix} files are not supported");
            }


            data = ConvertFile(path);
        }

        public int ConversionDataSize()
        {
            return dataSizeCount;
        }
        public int HEXMaxAddress()
        {
            return IntelHEXMaxAddress;
        }

        public int ConvertFile(string path)
        {
            if (!File.Exists(path)) throw new Exception($"Could not find file: ${path}");
            string fileSuffix = path[(path.LastIndexOf('.') + 1)..^0];

            if (fileSuffix == "hex")
            {
                if (IsIntelHEX(path))
                {
                    // .hex to .rom
                    string records = File.ReadAllText(path);
                    ConsoleMessage($"Reading input file: {path}");

                    ConsoleMessage("Converting file...");
                    byte[] bytes = ASCIIToBinary(records);

                    path = path[0..path.LastIndexOf('.')] + ".rom";
                    ConsoleMessage($"Writing output file: {path}");
                    File.WriteAllBytes(path, bytes);
                }
                else
                {
                    // .hex to Intel HEX (using .ihex)
                    byte[] bytes = File.ReadAllBytes(path);
                    ConsoleMessage($"Reading input file: {path}");

                    ConsoleMessage("Converting file...");
                    string records = BinaryToASCII(bytes, outputRecordByteCount);

                    path = path[0..path.LastIndexOf('.')] + ".ihex";
                    ConsoleMessage($"Writing output file: {path}");
                    File.WriteAllText(path, records);
                }
            }
            else if (fileSuffix == "rom" || fileSuffix == "bin")
            {
                // .rom or .bin to Intel HEX
                byte[] bytes = File.ReadAllBytes(path);
                ConsoleMessage($"Reading input file: {path}");

                ConsoleMessage("Converting file...");
                string records = BinaryToASCII(bytes, outputRecordByteCount);

                path = path[0..path.LastIndexOf('.')] + ".hex";
                ConsoleMessage($"Writing output file: {path}");
                File.WriteAllText(path, records);
            }
            else
            {
                throw new Exception($"{fileSuffix} files are not supported");
            }
            return this.dataSizeCount;
        }
        /// <summary>
        /// Convert binary ROM data to programmer accepted data format with specified data <paramref name="byteCount"/>.
        /// </summary>
        /// <remarks>
        /// Every element except the last two have <paramref name="byteCount"/> number of data bytes.
        /// The last but one element will contain the remaining bytes and may be of a smaller size.
        /// The last element indicated end of file, and will always be 0000001FFh
        /// </remarks>
        /// <param name="source">
        /// An array of 8-bit unsigned integers.
        /// </param>
        /// <param name="byteCount">
        /// Maximum number of data bytes per element.
        /// </param>
        /// <returns>
        /// An jagged byte array, optimized array of Intel HEX records in binary form
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="binaryRecords"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="byteCount"/> is below 1.
        /// </exception>
        public byte[][] BinaryToProgrammer(byte[] source, int byteCount)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (byteCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(byteCount));
            }
            return ASCIIToProgrammer(BinaryToASCII(source, byteCount));
        }
        /// <summary>
        /// Converts Intel HEX format data to programmer accepted data.
        /// </summary>
        /// <remarks>
        /// Every element has exact same length as Intel HEX record.
        /// Programmer format is basically Intel HEX without ':','\r' and '\n' characters in binary format stored in array of byte arrays.
        /// </remarks>
        /// <param name="source">
        /// String in Intel HEX format
        /// </param>
        /// <returns>
        /// An jagged byte array, optimized array of Intel HEX records in binary form
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="binaryRecords"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="byteCount"/> is below 1.
        /// </exception>
        private byte[][] ASCIIToProgrammer(string source)
        {
            dataSizeCount = 0;
            IntelHEXMaxAddress = 0;
            source = source.Replace(":", string.Empty);
            source = source.Replace("\r", string.Empty);
            source = source.Remove(source.Length - 1); // remove last \n

            string[] recordLines = source.Split('\n');

            byte[][] lineBytes = new byte[recordLines.Length][];
            for (int i = 0; i < lineBytes.Length; i++)
            {
                lineBytes[i] = Convert.FromHexString(recordLines[i]);
                dataSizeCount += lineBytes[i][0];
                int address = (lineBytes[i][1] << 8) + lineBytes[i][2] + (lineBytes[i][0] - 1);
                if(address > IntelHEXMaxAddress)
                {
                    IntelHEXMaxAddress = address;
                }
            }
            return lineBytes;
        }
        /// <summary>
        /// Converts ASCII Intel HEX file to binary format */
        /// </summary>
        /// <remarks>
        /// If input file is in binary format, conversion is omited.
        /// </remarks>
        /// <param name="path">
        /// Input file path, can be both relative and absolute.
        /// </param>
        /// <returns>
        /// An array of bytes.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="filepath"/> is null.
        /// </exception>
        private byte[] FileToBinary(string path)
        {
            // TODO check ? is this needed ?
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (!File.Exists(path)) throw new Exception($"Could not find file: ${path}");
            string fileSuffix = path[(path.LastIndexOf('.') + 1)..^0];

            if (IsIntelHEX(path))
            {
                string records = File.ReadAllText(path);
                return ASCIIToBinary(records);
            }
            else if (fileSuffix == "rom" || fileSuffix == "bin")
            {
                byte[] bytes = File.ReadAllBytes(path);
                dataSizeCount = bytes.Length;
                return(bytes);
            }
            else
            {
                throw new Exception($"{fileSuffix} files are not supported");
            }
        }



        /// <summary>
        /// Convert ASCII Intel HEX format to binary rom format.
        /// </summary>
        /// <param name="source">
        /// A string containing data in Intel HEX format.
        /// </param>
        /// <returns>
        /// An array of bytes.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source"/> is null.
        /// </exception>
        public byte[] ASCIIToBinary(string source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            //dataSizeCount = 0;
            source = source.Replace(":", string.Empty);
            source = source.Replace("\r", string.Empty);
            source = source.Remove(source.Length - 1); // remove last \n

            string[] recordLines = source.Split('\n');
            StringBuilder sb = new();

            for (int i = 0; i < recordLines.Length; i++)
            {
                //dataSizeCount += Convert.FromHexString(recordLines[i])[0];
                sb.Append(recordLines[i][8..^2]);
            }

            byte[] bytes = Convert.FromHexString(sb.ToString());
            dataSizeCount = bytes.Length;
            return bytes;
        }
        /// <summary>
        /// Convert raw binary ROM data to Intel HEX format with specified <paramref name="byteCount"/>.
        /// </summary>
        /// <remarks>
        /// Every record except the last two have <paramref name="byteCount"/> number of data bytes.
        /// The last but one record will contain the remaining bytes and may be of a smaller size.
        /// The last record is end of file record, and will always be ":00000001FF"
        /// </remarks>
        /// <param name="source">
        /// An array of 8-bit unsigned integers.
        /// </param>
        /// <param name="byteCount">
        /// Maximum number of data bytes per record.
        /// </param>
        /// <returns>
        /// <see cref="string"/> that contains the created records.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="byteCount"/> is below 1.
        /// </exception>
        public string BinaryToASCII(byte[] source, int byteCount)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (byteCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(byteCount));
            }
            if (byteCount > 255)
            {
                throw new ArgumentOutOfRangeException(nameof(byteCount), "maximum value is 255");
            }

            dataSizeCount = source.Length;

            byte[][] dataRead = source.Chunk(byteCount).ToArray();

            string records = "";

            for (int i = 0; i < dataRead.Length; i++)
            {
                char[] record = new char[13 + (byteCount * 2)];
                record[0] = ':';
                record[1] = dataRead[i].Length.ToString("X2")[0];
                record[2] = dataRead[i].Length.ToString("X2")[1];

                //dataSizeCount += record[1] + record[2];

                byte checksum = (byte)(((i * byteCount) >> 8) + (byte)(i * byteCount) + dataRead[i].Length);
                string address = (i * byteCount).ToString("X4");

                record[3] = address[0];
                record[4] = address[1];
                record[5] = address[2];
                record[6] = address[3];
                record[7] = '0';
                record[8] = '0';

                int j = 0;
                for (; j < dataRead[i].Length; j++)
                {
                    checksum += dataRead[i][j];
                    string dataByte = dataRead[i][j].ToString("X2");

                    record[9 + (j * 2)] = dataByte[0];
                    record[10 + (j * 2)] = dataByte[1];
                }
                j = j * 2 + 9;
                checksum = (byte)((byte)~checksum + 1); // Two's complement

                record[j] = checksum.ToString("X2")[0];
                record[j + 1] = checksum.ToString("X2")[1];
                record[j + 2] = '\r';
                record[j + 3] = '\n';

                records += new string(record).Replace("\0", string.Empty);
            }
            records += ":00000001FF\r\n";

            return records;
        }
        public bool IsIntelHEX(string filepath)
        {
            string[] fileContents = File.ReadAllLines(filepath);

            foreach (string line in fileContents)
            {
                if (line[0] != ':') return false;
            }
            if (fileContents[^1] != ":00000001FF") return false; // ReadAllLines removes both \r\n
            return true;
        }
    }
}