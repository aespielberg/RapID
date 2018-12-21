using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using System.Net;
using System.Net.Sockets;


// This is helpful!
// https://msdn.microsoft.com/en-us/library/system.net.sockets.socket(v=vs.110).aspx

namespace WiFi
{
    // This class acts like a TCP client, sending packets
    // to the Arduino Wifi Shield over a socket.
    public class WifiClient
    {
    private bool connected;
    private Socket socket;

        public WifiClient() {
            this.connected = false;
        }

        public bool Connected() {
            return this.connected;
        }

        // Returns true on success.
        public bool Connect(string address, int port)
        {
            this.connected = false;
            IPAddress ipAddress = IPAddress.Parse(address);
            IPEndPoint ipe = new IPEndPoint(ipAddress, port);
            Socket tmp = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try {
                tmp.Connect(ipe);
            } catch (SocketException e) {
                Console.WriteLine(e);
                return false;
            }
            if (tmp.Connected) {
                this.socket = tmp;
                this.connected = true;
                Console.WriteLine("connected!");
                return true;
            } else {
                Console.WriteLine("failed to connect!");
                return false;
            }
        }

        public string SendServoCommand(int[] servoCommand)
        {
            byte[] m = new byte[servoCommand.Length + 2];
            m[0] = (byte)'[';
            int i = 1;
            foreach (int cmd in servoCommand) {
                m[i] = (byte)(cmd & 0xff);
                i++;
            }
            m[i] = (byte)']';
            Console.WriteLine("sending: " + m.ToString());
            string response = this.Send(m);
            Console.WriteLine("received: " + response);
            return response;
        }

        // Sends a message and returns the reply.
        private string Send(byte[] message) {
            if (!this.connected) {
                    Console.WriteLine("cannot send, no connection");
            return "";
            }
            byte[] received = new byte[8]; // Arduino will simply acknowledge.
            this.socket.Send(message, message.Length, 0);
            Console.WriteLine("sent, waiting for response");
                int byteCount;
                try
                {
                    byteCount = this.socket.Receive(received, received.Length, 0);
                } catch(SocketException e)
                {
                    Console.WriteLine(e);
                    return "";
                }
            string response = Encoding.ASCII.GetString(received, 0, byteCount);
            Console.WriteLine("got response");
            return response;
        }

    }
}
