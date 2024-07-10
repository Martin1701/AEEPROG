using System.IO.Ports;
using System.Management;
using System.Runtime.Versioning;

namespace AEEProg
{
    public class PortFinder
    {
        public string FindPort(string deviceName)
        {
            Console.Write($"AEEProg.exe: ");
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    Console.Write($"looking for {deviceName}...");
                    return FindDeviceByName(deviceName);

                }
                catch (PortNotFoundException)
                {
                    Console.Write("device not found in device manager");
                    Console.Write("")
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                Console.Write("looking for programmer");
                if (FindByResponse(port))
                {
                    return true;
                }
                Console.WriteLine($" found: {port.PortName}");
            }
            if (portName != null || portName != "")
            {
                Console.WriteLine($"found on: {portName}");
                return portName;
            }
            else
            {
                Console.WriteLine("not found");
            }
        }

        [SupportedOSPlatform("windows")]
        private static string FindDeviceByName(string deviceName)
        {
            ManagementObjectSearcher comPortSearcher = new(@"\\localhost\root\CIMV2", "SELECT * FROM Win32_PnPEntity WHERE ConfigManagerErrorCode = 0");

            using (comPortSearcher)
            {
                foreach (var obj in comPortSearcher.Get())
                {
                    if (obj != null)
                    {
                        object captionObj = obj["Caption"];
                        string? caption = captionObj.ToString();
                        if (caption != null && caption.Contains(deviceName))
                        {
                            return caption[caption.LastIndexOf("(COM")..^0].Replace("(", string.Empty).Replace(")", string.Empty);
                        }
                    }
                }
            }
            throw new PortNotFoundException();
        }

        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        private static string FindByResponse()
        {
            SerialPort port = new SerialPort();
            port.DtrEnable = false;
            port.ReadTimeout = 10;
            port.WriteTimeout = 10;

            string[] availablePorts = SerialPort.GetPortNames();
            int passes = 5;
            for (; passes > 0; passes--)
            {
                foreach (string COMPort in availablePorts)
                {
                    Console.Write('.');
                    try
                    {
                        port.PortName = COMPort;
                        port.Open();
                        port.WriteLine("start");
                        // wait for correct response
                        if (port.ReadLine().Contains("start"))
                        {
                            port.Close();
                            return port.PortName;
                        }
                        else port.Close();
                    }
                    catch
                    {
                        /* read or write timeout */
                        port.DiscardOutBuffer();
                        port.DiscardInBuffer();
                        port.Close();
                        // next one
                    }
                }
            }
            throw new PortNotFoundException();
        }
        public class PortNotFoundException : Exception
        {
            public PortNotFoundException() : base("Port not found")
            {

            }
            public PortNotFoundException(string message) : base(message)
            {
            }
        }
    }
}