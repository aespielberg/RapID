using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;


namespace WiFi
{
    // This class is just for testing our WifiClient.
    static class WifiTest
    {
        private const string arduinoIP = "128.31.37.152";
        private const int arduinoPort = 1234;

        public static int WifiMain(string[] args)
        {
            // Send a bunch of messages and print out the responses.
            WifiClient wifi = new WifiClient();
            Console.WriteLine("beginnning test...");
            bool success = wifi.Connect(arduinoIP, arduinoPort);
            while (!success) {
                success = wifi.Connect(arduinoIP, arduinoPort);
            }
            // Send one set of servo commands, meaning "[...]" where ... is 8 chars
            // representing 8 numbers between 0 and 255 for the desired servo position.
            int i = 20;
            while (i <= 165) {
                int[] servoCommand = new int[8];
                for (int j = 0; j < 8; j++)
                {
                    servoCommand[j] = i;
                }
                string response = wifi.SendServoCommand(servoCommand);
                i += 10;
                System.Threading.Thread.Sleep(2);
            }
            while (true)
            {
                // Waiting...
            }
        }
    }
}
