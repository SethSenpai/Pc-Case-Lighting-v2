using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.Win32;

namespace CaseLightingv2
{
    /// <summary>
    /// class containing functions that have been outsourced to a different file
    /// </summary>
    
    public class f
    {
       /// <summary>
       /// write a console log to both the console and the console output in the program.
       /// </summary> 
        public static void ConsoleOut(string write)
        {
            Console.WriteLine(write);
            ((MainWindow)System.Windows.Application.Current.MainWindow).consoleOutput.Content = write;
        }

        /// <summary>
        /// Send a colour value to a single led.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="dataStream"></param>
        /// <param name="comConnect"></param>
        public static void sendOne(string command, SerialPort dataStream, bool comConnect)
        {
            string comTemplate = "0:{0}&1:{1}&2:{2}&3:{3}&4:{4}";
            string[] cpart = command.Split(',');
            if (comConnect)
            {
                dataStream.WriteLine(string.Format(comTemplate, 1, cpart[1], cpart[2], cpart[3], cpart[4]));
            }
        }

        ///<summary>
        ///Send a command to the mcu to set the colour of all the LED's
        /// </summary>   
        public static void sendAll(string command, SerialPort dataStream, bool comConnect)
        {
            string comTemplate = "0:{0}&1:{1}&2:{2}&3:{3}&4:{4}";
            string[] cpart = command.Split(',');
            if(comConnect)
            {
                dataStream.WriteLine(string.Format(comTemplate, 0, 0, cpart[1], cpart[2], cpart[3]));
            }
        }

        /// <summary>
        /// Reconnect to a different com port
        /// </summary>
        /// <param name="command">the full command that was entered</param>
        /// <param name="dataStream">the SerialPort used by the main program</param>
        /// <param name="comConnect">the boolean to check if we are connected</param>
        public static void reconnect(string command, out SerialPort dataStream, out bool comConnect)
        {
            string[] cpart = command.Split(',');
            ConsoleOut("setting new port to: " + cpart[1]);

            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\CaseLightingV2");
            key.SetValue("Port", cpart[1]);
            key.Close();

            dataStream = new SerialPort();
            //dataStream.Close();
            try
            {
                dataStream = new SerialPort(cpart[1], 250000);
                dataStream.ReadTimeout = 20;
                dataStream.DtrEnable = true;
                dataStream.Open();
                comConnect = true;
                ConsoleOut("connected to device");
            }
            catch
            {
                comConnect = false;
                ConsoleOut("failed to connect");
            }
        }

        /// <summary>
        /// Convert HSV to RGB
        /// h is from 0-360
        /// s,v values are 0-1
        /// r,g,b values are 0-255
        /// Based upon http://ilab.usc.edu/wiki/index.php/HSV_And_H2SV_Color_Space#HSV_Transformation_C_.2F_C.2B.2B_Code_2
        /// </summary>
       public static void HsvToRgb(double h, double S, double V, out int r, out int g, out int b)
        {
            // ######################################################################
            // T. Nathan Mundhenk
            // mundhenk@usc.edu
            // C/C++ Macro HSV to RGB

            double H = h;
            while (H < 0) { H += 360; };
            while (H >= 360) { H -= 360; };
            double R, G, B;
            if (V <= 0)
            { R = G = B = 0; }
            else if (S <= 0)
            {
                R = G = B = V;
            }
            else
            {
                double hf = H / 60.0;
                int i = (int)Math.Floor(hf);
                double f = hf - i;
                double pv = V * (1 - S);
                double qv = V * (1 - S * f);
                double tv = V * (1 - S * (1 - f));
                switch (i)
                {

                    // Red is the dominant color

                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;

                    // Green is the dominant color

                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;

                    // Blue is the dominant color

                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;

                    // Red is the dominant color

                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // The color is not defined, we should throw an error.

                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = V; // Just pretend its black/white
                        break;
                }
            }
            r = Clamp((int)(R * 255.0));
            g = Clamp((int)(G * 255.0));
            b = Clamp((int)(B * 255.0));
        }

        /// <summary>
        /// Clamp a value to 0-255
        /// </summary>
        private static int Clamp(int i)
        {
            if (i < 0) return 0;
            if (i > 255) return 255;
            return i;
        }
    }
}
