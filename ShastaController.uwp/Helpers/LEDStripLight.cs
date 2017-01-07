using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Networking;
using Windows.Networking.Sockets;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

namespace ShastaController.uwp.Helpers
{
    class LEDStripLight
    {
        private StreamSocket socket { get; set; }
        private bool isConnected = false;

        public async Task ConnectAsync(string ip, string port)
        {
            socket = new StreamSocket();
            HostName hostName = new HostName(ip);
            // Set NoDelay to false so that the Nagle algorithm is not disabled
            socket.Control.NoDelay = false;

            try
            {
                // Connect to the server
                await socket.ConnectAsync(hostName, port);
                isConnected = true;
                Debug.WriteLine("LED is connected");
            }
            catch (Exception exception)
            {
                switch (SocketError.GetStatus(exception.HResult))
                {
                    case SocketErrorStatus.HostNotFound:
                        // Handle HostNotFound Error
                        Debug.WriteLine("LED Host/Port not found");
                        break;
                    default:
                        // If this is an unknown status it means that the error is fatal and retry will likely fail.
                        Debug.WriteLine(exception.Message);
                        break;
                }
            }
        }

        public async void WriteToLight(double red, double green, double blue, double brightness)
        {
            if (isConnected)
            {
                /**********************************************************
                 * This is a port from https://www.npmjs.com/package/rgb-led 
                 * Credits to:
                    Secesh aka Matthew R Chase (matt@chasefox.net)
                * This is the core of the protocol we discovered.  Commands
                * look like this:
                *     [STX], [RED], [GREEN], [BLUE], [ETX]
                * Each field is a hex byte with possible values 00-FF, which
                * are expressed in decimal as 0-255.
                *
                * Each command starts (the STX value) with a value of 86.
                * Each command ends (the ETX value) with a value of 170.
                *
                * The red, green, blue values combine to determine the color
                * and brightness level.  For example:
                *
                *     Bright red would be: 255,0,0
                *     reduce the value to dim; a dim red could be: 40,0,0
                *     Bright green would be: 0,255,0
                *     Bright purple would be: 255,0,255
                *     a dim purple could be 40,0,40
                *     a less dim purple could be 160,0,160
                *
                *     White is: 255,255,255
                *     Off is: 0,0,0
                ************************************************************/
                red = Math.Round(red * brightness / 100);
                green = Math.Round(green * brightness / 100);
                blue = Math.Round(blue * brightness / 100);

                DataWriter writer = new DataWriter(socket.OutputStream);

                // Create the data writer object backed by the in-memory stream. 
                using (writer = new DataWriter(socket.OutputStream))
                {
                    var message = new int[] { 86, Convert.ToInt32(red), Convert.ToInt32(green), Convert.ToInt32(blue), 170 };
                    byte[] messageBytes = message.SelectMany(value => BitConverter.GetBytes(value)).ToArray();
                    writer.WriteBytes(messageBytes);


                    // Send the contents of the writer to the backing stream.
                    try
                    {
                        await writer.StoreAsync();

                    }
                    catch (Exception exception)
                    {
                        switch (SocketError.GetStatus(exception.HResult))
                        {
                            case SocketErrorStatus.HostNotFound:
                                // Handle HostNotFound Error
                                Debug.WriteLine("LED Host/Port not found");
                                break;
                            default:
                                // If this is an unknown status it means that the error is fatal and retry will likely fail.
                                Debug.WriteLine(exception.Message);
                                break;
                        }
                    }

                    await writer.FlushAsync();
                    // In order to prolong the lifetime of the stream, detach it from the DataWriter
                    writer.DetachStream();

                }
            }
            else Debug.WriteLine("LED not connected");

        }

    }
}
