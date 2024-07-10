using System.Diagnostics;
using System.IO.Ports;
using System.Net;
using static AEEProg.Program;
using static AEEProg.ProgrammerData;
using System.Drawing;

namespace AEEProg
{
    internal class Prog
    {
        public static void WriteIntelHex(SerialPort port, string source)
        {
            byte[][] bytes = ASCIIToProgrammer(source);
            ConsoleMessage($"writing ({ConversionDataSize()} bytes):");
            Console.Write($"\nWriting | ");

            port.WriteLine("write_I");
            if (port.ReadLine() != "ready") throw new Exception("Incorrect programmer response, aborting action.");

            float progressBarStep = (float)bytes.Length / 51.0f;
            int progressCounter = 1;
            int totalBytesWritten = 0;

            Stopwatch _stopWatch = new();
            _stopWatch.Start();

            for(int i = 0; i < bytes.Length; i++)
            {
                int byteCount = bytes[i][0];
                port.Write(bytes[i], 0, bytes[i].Length);
                int bytesWritten = port.ReadByte();
                totalBytesWritten += bytesWritten;
                if(bytesWritten < byteCount)
                {
                    _stopWatch.Stop();
                    for (int j = progressCounter; j < 50; j++)
                    {
                        Console.Write(" ");
                    }
                    Console.WriteLine($" | {(Math.Round((float)i / bytes.Length * 10000) / 100).ToString().Replace(",", ".")}% {((float)_stopWatch.ElapsedMilliseconds / 1000).ToString().Replace(",", ".")}s\n");
                    ConsoleMessage($"{totalBytesWritten - 1} bytes written");
                    throw new Exception($"Unable to write to memory address: {totalBytesWritten}");
                }
                if (i > (progressBarStep * progressCounter))
                {
                    Console.Write("#");
                    progressCounter++;
                }
            }
            _stopWatch.Stop();
            Console.WriteLine($" | 100% {((float)_stopWatch.ElapsedMilliseconds / 1000).ToString().Replace(",", ".")}s\n");
            ConsoleMessage($"{totalBytesWritten} bytes written");
        }
        public static void Write(SerialPort port, byte[] bytes, byte size)
        {
            ConsoleMessage($"writing ({bytes.Length} bytes):");
            Console.Write($"\nWriting | ");

            port.WriteLine("write");
            if (port.ReadLine() != "ready") throw new Exception("Programmer responded incorrectly");

            byte[][] chunkedBytes = bytes.Chunk(size).ToArray();

            float progressBarStep = (float)chunkedBytes.Length / 51.0f;
            int progressCounter = 1;

            Stopwatch _stopWatch = new();
            _stopWatch.Start();

            for (int i = 0; i < chunkedBytes.Length; i++)
            {
                int address = i * size;
                byte[] buff = new byte[] { (byte)chunkedBytes[i].Length,  (byte)(address >> 8), (byte)address};

                port.Write(buff, 0, buff.Length);
                port.Write(chunkedBytes[i], 0, chunkedBytes[i].Length);

                if (i > (progressBarStep * progressCounter))
                {
                    Console.Write("#");
                    progressCounter++;
                }

                int bytesWritten = port.ReadByte();
                if(bytesWritten != chunkedBytes[i].Length)
                {
                    _stopWatch.Stop();
                    for (int j = progressCounter; j < 50; j++)
                    {
                        Console.Write(" ");
                    }
                    Console.WriteLine($" | {(Math.Round((float)i / chunkedBytes.Length * 10000) / 100).ToString().Replace(",", ".")}% {((float)_stopWatch.ElapsedMilliseconds / 1000).ToString().Replace(",", ".")}s\n");
                    ConsoleMessage($"{(i * size) + bytesWritten} bytes written");
                    throw new Exception($"Unable to write to memory address: {address + bytesWritten}");
                }
            }
            // send programmer we have zero bytes to write, signalising end of writing
            byte[] zero = new byte[] { 0 };
            port.Write(zero, 0, 1);

            for (int i = progressCounter; i <= 50; i++)
            {
                Console.Write("#");
            }


            _stopWatch.Stop();
            Console.WriteLine($" | 100% {((float)_stopWatch.ElapsedMilliseconds / 1000).ToString().Replace(",", ".")}s\n");
            ConsoleMessage($"{bytes.Length} bytes written");
        }
        public static byte[] Read(SerialPort port, int count)
        {
            ConsoleMessage($"Reading data from chip:");
            Console.Write("\nReading | ");
            port.WriteLine("read");
            if (port.ReadLine() != "ready") throw new Exception("Programmer responded incorrectly");

            byte[] bytes = new byte[count];

            float progressBarStep = (float)count / 51.0f;
            int progressCounter = 1;

            byte[] buf = new byte[] { (byte)(count >> 8), (byte)count };
            port.Write(buf, 0, buf.Length);


            Stopwatch _stopWatch = new();
            _stopWatch.Start();

            int bytesRead = 0;
            while (bytesRead < count)
            {
                bytesRead += port.Read(bytes, bytesRead, count - bytesRead);
                if (bytesRead > (progressBarStep * progressCounter))
                {
                    Console.Write("#");
                    progressCounter++;
                }
            }

            for (int i = progressCounter; i <= 50; i++)
            {
                Console.Write("#");
            }

            _stopWatch.Stop();
            Console.WriteLine($" | 100% {((float)_stopWatch.ElapsedMilliseconds / 1000).ToString().Replace(",", ".")}s\n");
            ConsoleMessage($"{bytesRead} bytes read");

            return bytes;
        }
    }
}
