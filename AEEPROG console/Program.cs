/****************************
  AEEProg - An
            EPROM
            EEPROM
            Programmer

****************************/

using System.Diagnostics;
using System.IO.Ports;
using System.Timers;
using static AEEProg.ProgrammerData;
using static AEEProg.PortFinder;
using static AEEProg.Constants;
using static AEEProg.Prog;
using static AEEProg.Programmer;
using System.Reflection;

namespace AEEProg
{
    internal class Program
    {
        public static void ConsoleMessage(string message)
        {
            Console.WriteLine($"AEEProg.exe: {message}");
        }
        private static void Main(string[] args)
        {
            var buildTime = GetLinkerTime(Assembly.GetEntryAssembly());

            Console.WriteLine($"AEEProg.exe: Version {version} (Build time: {buildTime.ToString().Replace(". ", ".")})\n");

            /* parse arguments */
            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    /* Convert between Intel HEX and binary and vice versa */
                    if (args[i].Contains("-C"))
                    {
                        /* handles regardless if there is space between argument or not */
                        if (args[i].Length > 2) path = args[i][2..^0];
                        //else path = args[i + 1];

                        int j = i + 1;
                        while (j < args.Length && !args[j].Contains('-'))
                        {
                            path += " " + args[j]; // arguments are separated by spaces, which are removed
                            j++;
                        }
                        ConsoleMessage($"Converting input file: {path}");
                        int dataSize = ConvertFile(path);
                        ConsoleMessage($"Sucessfully converted {dataSize} data bytes");
                        Console.Write("\n");
                        ConsoleMessage("done");
                        Environment.Exit(0);
                    }
                }
                if (args.Length > 0)
                {
                    /* reset programer by enabling DTR */
                    foreach (var arg in args)
                    {
                        if (arg == "-R")
                        {
                            _serialPort.DtrEnable = true;
                        }
                    }
                    /* connection port */
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (args[i].Contains("-P"))
                        {
                            /* handles regardless if there is space between argument or not */
                            if (args[i].Length > 2) _serialPort.PortName = args[i][2..^0];
                            else _serialPort.PortName = args[i + 1];

                            if (_serialPort.PortName.ToLower() == "auto")
                            {
                                if (!FindPort(_serialPort))
                                {
                                    throw new Exception(" unable to find serial port, try specifying it manually");
                                }
                                Console.WriteLine($"\n             Using Port                   : {_serialPort.PortName}");
                            }
                            else
                            {
                                string[] availablePorts = SerialPort.GetPortNames();

                                Console.WriteLine($"             Using Port                   : {_serialPort.PortName}");

                                bool portFound = false;
                                foreach (string port in availablePorts)
                                {
                                    if (port == _serialPort.PortName)
                                    {
                                        portFound = true;
                                        break;
                                    }
                                }
                                if (!portFound)
                                {
                                    throw new Exception($"Port {_serialPort.PortName} was not found");
                                }
                                _serialPort.Open();
                            }
                        }
                    }
                    if (!_serialPort.IsOpen) throw new Exception("Connection port not specified");
                    /* memory type, operation and path */
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (args[i].Contains("-U"))
                        {
                            string[] arg_split;
                            /* handles regardless if there is space between argument or not */
                            if (args[i].Length > 2) arg_split = args[i][2..^0].Split(':');
                            else arg_split = args[i + 1].Split(':');

                            if (arg_split.Length < 3)
                            {
                                throw new Exception("Not enough arguments for memory operation");
                            }
                            memtype = arg_split[0];
                            if (memtype != "EPROM" && memtype != "EEPROM")
                            {
                                throw new Exception("Invalid memtype specification");
                            }
                            Console.WriteLine($"             Memory type                  : {memtype}");

                            operation = arg_split[1];
                            switch (operation)
                            {
                                case "r":
                                    {
                                        Console.WriteLine($"             Perform operation            : read");
                                        break;
                                    }
                                case "w":
                                    {
                                        Console.WriteLine($"             Perform operation            : write");
                                        break;
                                    }
                                case "v":
                                    {
                                        Console.WriteLine($"             Perform operation            : verify");
                                        break;
                                    }
                                default: throw new Exception("Invalid memory operation specification");
                            }
                            if (arg_split.Length > 3)
                            {
                                path = arg_split[2] + ':' + arg_split[3]; // if using absoulte path, eg. C:\Users\User\Downloads... etc.
                            }
                            else
                            {
                                path = arg_split[2];
                            }

                            int j = i + 2;
                            while (j < args.Length && !args[j].Contains('-'))
                            {
                                path += " " + args[j]; // arguments are separated by spaces, which are removed
                                j++;
                            }
                            fileType = path[path.LastIndexOf('.')..^0];
                        }
                    }
                    /* memory size */
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (args[i].Contains("-S"))
                        {
                            int kbSize;
                            /* handles regardless if there is space between argument or not */
                            if (args[i].Length > 2) kbSize = int.Parse(args[i + 1][2..^0]);
                            else kbSize = int.Parse(args[i + 1]);

                            if ((kbSize % 64) != 0) throw new Exception("Invalid memory size specification");
                            maxAddress = (kbSize / 8 * 1024) - 1;
                            Console.WriteLine($"             Max address                  : {maxAddress}");
                        }
                    }
                    if (maxAddress == 0) throw new Exception("Memory size not specified");
                    /* erase chip */
                    foreach (var arg in args)
                    {
                        if (arg == "-e")
                        {
                            chipErase = true;
                            Console.WriteLine($"             Erase chip                   : {chipErase}\n");
                        }
                    }
                    /* do not write */
                    foreach (var arg in args)
                    {
                        if (arg == "-n")
                        {
                            not_write = true;
                            Console.WriteLine($"             Do not write                 : {not_write}");
                        }
                    }
                    /* do not verify */
                    foreach (var arg in args)
                    {
                        if (arg == "-v")
                        {
                            not_verify = true;
                            Console.WriteLine($"             Do not werify                : {not_verify}");
                        }
                    }
                    Console.Write("\n");
                    /**/
                    if (_serialPort.DtrEnable)
                    {
                        System.Timers.Timer dotProgress = new()
                        {
                            Interval = 100,
                            AutoReset = true,
                            Enabled = true
                        };
                        Console.Write($"AEEProg.exe: resetting programmer");
                        dotProgress.Elapsed += addDot;
                        void addDot(object? source, ElapsedEventArgs e)
                        {
                            Console.Write(".");
                        }
                        int ReadTimeout = _serialPort.ReadTimeout;
                        _serialPort.ReadTimeout = 2000;
                        _serialPort.WriteLine("start");
                        while (_serialPort.ReadLine() != "start") ; // wait until programmer resets
                        dotProgress.Dispose();
                        _serialPort.ReadTimeout = ReadTimeout;
                    }
                    else
                    {
                        ConsoleMessage("connecting with programmer");
                        _serialPort.WriteLine("start");
                        while (_serialPort.ReadLine() != "start") ;
                    }
                    // firstly, send the programmer what type of memory are we dealing with
                    do
                    {
                        _serialPort.WriteLine(memtype);
                    } while (_serialPort.ReadLine() != memtype);
                    // then, send max address value
                    do
                    {
                        _serialPort.WriteLine("maxAddr");
                        _serialPort.WriteLine(maxAddress.ToString());
                    } while (_serialPort.ReadLine() != maxAddress.ToString());
                    // report connection success only after setup things are sent
                    ConsoleMessage("successfully connected");
                    /**/
                    if (chipErase)
                    {
                        if (memtype == "EPROM") throw new Exception("EPROM erasing not supported");
                        ConsoleMessage("erasing chip");
                        Stopwatch _stopWatch = new();
                        Console.Write($"\nErasing | ");
                        _stopWatch.Start();
                        _serialPort.WriteLine("erase");
                        string line = _serialPort.ReadLine();

                        int counter = 0;
                        while (line != "end")
                        {
                            if (line == "err")
                            {
                                _stopWatch.Stop();
                                for (int i = counter; i < 50; i++)
                                {
                                    Console.Write(" ");
                                }
                                Console.WriteLine($" | {(Math.Round((float)counter / 50 * 10000) / 100).ToString().Replace(",", ".")}% {((float)_stopWatch.ElapsedMilliseconds / 1000).ToString().Replace(",", ".")}s\n");
                                throw new Exception($"Error occured while Erasing memory");
                            }
                            Console.Write("#");
                            line = _serialPort.ReadLine();
                            counter++;
                        }
                        _stopWatch.Stop();

                        Console.WriteLine($" | 100% {((float)_stopWatch.ElapsedMilliseconds / 1000).ToString().Replace(",", ".")}s\n");
                        // Reading | ################################################## | 100% 1000ms
                        // ^^ example template ^^
                    }

                    /* Write */
                    if (operation == "w")
                    {
                        if (memtype == "EPROM") throw new Exception("writing to EPROM is not supported (yet)");

                        if (fileType == ".asm" || fileType == ".rom" || fileType == ".hex")
                        {
                            if (fileType == ".asm")
                            {
                                string pathWithoutExtension = path[0..path.LastIndexOf('.')];
                                if (File.Exists(pathWithoutExtension + ".rom"))
                                {
                                    path = pathWithoutExtension + ".rom";
                                    ConsoleMessage("provided file is .asm but .rom version exists");
                                }
                                else if (File.Exists(pathWithoutExtension + ".hex"))
                                {
                                    path = pathWithoutExtension + ".hex";
                                    ConsoleMessage("provided file is .asm but .hex version exists");
                                }
                            }
                            ConsoleMessage("reading input file " + '"' + path + '"');

                            if (IsIntelHEX(path))
                            {
                                WriteIntelHex(_serialPort, File.ReadAllText(path));
                            }
                            else
                            {
                                Write(_serialPort, File.ReadAllBytes(path), 32);
                            }
                        } else
                        {
                            throw new Exception("Invalid file type");
                        }
                    }
                    if ((!not_verify && operation == "w") || operation == "v")
                    {
                        ConsoleMessage($"verifying memory against {path}");
                        ConsoleMessage($"loading data from file {path}");


                        if (IsIntelHEX(path))
                        {
                            byte[][] fileData = ASCIIToProgrammer(File.ReadAllText(path));

                            ConsoleMessage($"input file {path} contains {ConversionDataSize()} bytes");
                            byte[] bytes = Read(_serialPort, HEXMaxAddress() + 1);

                            ConsoleMessage("verifying ...");
                            int bytesVerified = 0;
                            for (int i = 0; i < fileData.Length; i++)
                            {
                                int byteCount = fileData[i][0];
                                int startAddress = (fileData[i][1] << 8) + fileData[i][2];
                                for(int j = 0; j < byteCount; j++)
                                {
                                    if (bytes[startAddress + j] != fileData[i][4 + j]) // 4 bytes need to be skipped (byteCount, address (2 bytes) and recordType)
                                    {
                                        ConsoleMessage($"First mismatch at address 0x{startAddress + j:X4}");
                                        ConsoleMessage($"{bytesVerified} bytes verified");
                                        throw new Exception("Memory contents are not correct");
                                    }
                                    bytesVerified++;
                                }
                            }
                            ConsoleMessage($"{bytesVerified} bytes verified");
                        }
                        else
                        {
                            byte[]fileData = File.ReadAllBytes(path);

                            ConsoleMessage($"input file {path} contains {fileData.Length} bytes");

                            byte[] bytes = Read(_serialPort, fileData.Length);
                            /* verify */
                            ConsoleMessage("verifying ...");
                            int bytesVerified = 0;

                            for (int address = 0; address < fileData.Length; address++)
                            {
                                if (fileData[address] != bytes[address])
                                {
                                    ConsoleMessage($"First mismatch at address 0x{address:X4}");
                                    ConsoleMessage($"{bytesVerified} bytes verified");
                                    throw new Exception("Memory contents are not correct"); 
                                }
                                bytesVerified++;
                            }
                            ConsoleMessage($"{bytesVerified} bytes verified");
                        }
                    }
                    if (operation == "r")
                    {
                        byte[] bytes = Read(_serialPort, maxAddress + 1);
                        path = path[0..path.LastIndexOf('.')] + ".rom"; // force .rom suffix
                        ConsoleMessage($"Writing output file: {path}");
                        File.WriteAllBytes(path, bytes);

                        Console.WriteLine("Notice: Currently supporting output only in .rom format");
                    }
                    _serialPort.Close();
                    Console.Write("\n");
                    ConsoleMessage("done");
                }
                else
                {
                    showHelp(true);
                }
            }
            catch (Exception e)
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.DiscardOutBuffer();
                    _serialPort.DiscardInBuffer();
                    _serialPort.Close();
                }
#if DEBUG
                Console.WriteLine($"\nAEEProg.exe: {e}");
#else
            Console.WriteLine($"\nAEEProg.exe: {e.GetType()}: {e.Message}");
#endif
                Console.Write("\n\n\nAEEProg.exe: done.");
                Environment.Exit(1);
            }

            void showHelp(bool exit)
            {
                Console.WriteLine("Usage: AEEProg.exe [options]");
                Console.WriteLine("Options:");
                Console.WriteLine("  -P <port>                      Specify connection port.");
                Console.WriteLine("  -S <sizeKb>                    Specify memory size in Kb.");
                Console.WriteLine("  -e                             Perform a chip erase.");
                Console.WriteLine("  -U <memtype>:r | w | v:<filename>");
                Console.WriteLine("                                 Memory operation specification.");
                Console.WriteLine("  -n                             Do not write anything to the device.");
                Console.WriteLine("  -V                             Do not verify.");
                Console.WriteLine("  -R                             Reset programmer.");
                Console.WriteLine("\n  <memtype>                      EPROM | EEPROM");
                Console.Write($"\nAEEProg version {version}");
                if (exit) Environment.Exit(0);
            }
        }
    }
}