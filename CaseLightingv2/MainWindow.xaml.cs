using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using System.IO.Ports;
using System.Timers;
using System.IO;
using Microsoft.Win32;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Gma.System.MouseKeyHook;

namespace CaseLightingv2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        //template for the sending of the commands to the MCU
        string comTemplate = "0:{0}&1:{1}&2:{2}&3:{3}&4:{4}";

        //object to store the serial connection to the MCU
        public SerialPort dataStream;
        
        //the 3 toggles
        private bool isBreathing;
        private bool isSpecial;
        private bool isOn = true;

        //mouse dragging on colourwheel
        private bool isDragging;
        private Point clickPosition;
        
        //set if connected
        private bool comConnect = false;
        
        //selected patern storage
        private int selectedSpecial = 0;

        //breathing cycles
        private int breathTick = 0;
        private double breathUnderplier = 0.1;

        //hsv values
        private int currentH;
        private double currentV = 1;
        private double currentS;

        //rgb values
        private int currentR;
        private int currentG;
        private int currentB;

        //rainbows
        int hCount = 0;
        int ledCount = 0;

        //cpu performance
        static PerformanceCounter cpuCounter;
        List<double> buffer = new List<double>();

        //christmas
        bool christmasT = true;
        int arrCount = 0;

        //audio
        AudioLoopback sMix;

        private IKeyboardMouseEvents m_GlobalHook;
        private int typeStrenght = 0;


        public MainWindow()
        {
            InitializeComponent();

            //setup the CPU monitor
            cpuCounter = new PerformanceCounter();
            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";

            //try connecting to the MCU
            try
            {
                string p;
                RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\CaseLightingV2"); //readout the saved com port
                if (key.GetValue("port") != null)
                {
                    p = key.GetValue("Port").ToString();
                    f.ConsoleOut(p);
                    key.Close();
                }
                else
                {
                    p = "COM5";
                }

                dataStream = new SerialPort(p, 250000); //make new serial connection with MCU
                dataStream.ReadTimeout = 20;
                dataStream.DtrEnable = true;
                dataStream.Open();
                comConnect = true;
                f.ConsoleOut("connected to device");
            }
            catch
            {
                comConnect = false;
                f.ConsoleOut("failed to connect");
            }

            //loading the settings from the registry
            loadSettings();           

            //timers setup
            Timer aTimer = new Timer();
            aTimer.Elapsed += ATimer_Elapsed;
            aTimer.Interval = 50;
            aTimer.Enabled = true;

            Timer breathTimer = new Timer();
            breathTimer.Elapsed += BreathTimer_Elapsed;
            breathTimer.Interval = 50;
            breathTimer.Enabled = true;

            Timer specialTimer = new Timer();
            specialTimer.Elapsed += SpecialTimer_Elapsed;
            specialTimer.Interval = 50;
            specialTimer.Enabled = true;
        }

        /// <summary>
        /// Hooked event for global keyboard trigger
        /// </summary>
        private void M_GlobalHook_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            typeStrenght = typeStrenght + 50;
            if (typeStrenght > 255)
            {
                typeStrenght = 255;
            }
           
        }

        /// <summary>
        /// Update timer event for the special paterns.
        /// </summary>
        private void SpecialTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if(isSpecial == true && isOn == true && comConnect == true)
            {
                //switch case checks for currently selected patern.
                switch (selectedSpecial)
                {
                    case 0:
                        //rainbow Swirl
                        if(hCount < 360) // step through hue circle
                        {
                            if(ledCount < 9) //step through leds
                            {
                                int step = 360 / 9; 
                                int h = hCount + step * ledCount; //set leds to a colour on equal parts of the rainbow
                                if (h > 360)
                                {
                                    h = h - 360; //if over 360 then reset back to 0
                                }
                                int r, g, b;
                                f.HsvToRgb(h, 1, 1, out r, out g, out b);
                                dataStream.WriteLine(string.Format(comTemplate, 1, ledCount, r, g, b));
                                ledCount++;
                            }
                            else
                            {
                                ledCount = 0;
                                hCount = hCount+5;
                            }
                            
                        }
                        else
                        {
                            hCount = 0;
                        }
                    break;
                    
                    //rainbows
                    case 1:
                        if(hCount < 360)
                        {
                            int h = hCount;
                            int r, g, b;
                            f.HsvToRgb(h, 1, 1, out r, out g, out b);
                            dataStream.WriteLine(string.Format(comTemplate, 0, 0, r, g, b));
                            hCount++;
                        }
                        else
                        {
                            hCount = 0;
                        }
                    break;

                    //CPU use
                    case 2:
                        if(buffer.Count > 10) //update and fill a buffer to smooth out cpu useage visualisation
                        {
                            buffer.RemoveAt(0);
                            buffer.Add(double.Parse(CurrentCPUUsage));

                        }
                        else
                        {
                            buffer.Add(float.Parse(CurrentCPUUsage));
                        }

                        double avg = buffer.Average();
                        if(avg < 50) //set the 50% mark to be pure white if lower go into cyan
                        {
                            int r, g, b;
                            g = 255;
                            b = 255;
                            r = (int)Math.Floor(avg * 5.1);
                            dataStream.WriteLine(string.Format(comTemplate, 0, 0, r, g, b));
                        }
                        else //if above 50% go into red
                        {
                            int r, g, b;
                            g = (int)Math.Floor(255-(avg-50)*5.1);
                            b = (int)Math.Floor(255-(avg-50)*5.1);
                            r = 255;
                            dataStream.WriteLine(string.Format(comTemplate, 0, 0, r, g, b));
                        }

                    break;

                    //audio in
                    case 3:
                        int re, ge, be;
                        sMix.getRGB(currentH, currentS, out re, out ge, out be); //get rgb values based on audio input set by the current hue and saturation settings
                        dataStream.WriteLine(string.Format(comTemplate, 0, 0, re, ge, be));
                        break;

                    //christmas
                    case 4:
                        if(arrCount < 9) //cycle through leds and set them to red or green. It's the holidays!
                        {
                            if (christmasT)
                            {
                                dataStream.WriteLine(string.Format(comTemplate, 1, arrCount, 255, 0, 0));
                            }
                            else
                            {
                                dataStream.WriteLine(string.Format(comTemplate, 1, arrCount, 0, 255, 0));
                            }
                            arrCount++;
                        }
                        else
                        {
                            arrCount = 0;
                            if (christmasT)
                            {
                                christmasT = false;
                            }
                            else
                            {
                                christmasT = true;
                            }
                        }
                    break;

                    //keystrokes
                    case 5:
                        if(typeStrenght > 0)
                        {
                            typeStrenght = typeStrenght - 15;
                        }
                        else
                        {
                            typeStrenght = 0;
                        }
                        int r5, g5, b5;
                        double vKey = typeStrenght;
                        vKey = vKey / 255;
                        f.HsvToRgb(currentH, currentS, vKey, out r5, out g5, out b5);
                        dataStream.WriteLine(string.Format(comTemplate, 0, 0, r5, g5, b5));
                        //Console.WriteLine(typeStrenght + "," + vKey + "," + r5);
                        break;
                }
            }
        }

        /// <summary>
        /// Timer event for the breathing cycle.
        /// </summary>
        private void BreathTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if(isBreathing == true && isOn == true && comConnect == true)
            {
                currentV = 0.5 * (Math.Sin(breathUnderplier * breathTick) + 1); //calculates the current V values
                breathTick++;
                int r, g, b;
                f.HsvToRgb(currentH, currentS, currentV, out r, out g, out b);
                currentR = r;
                currentG = g;
                currentB = b;
                dataStream.WriteLine(string.Format(comTemplate, 0, 0, currentR, currentG, currentB));
            }
        }

        /// <summary>
        /// Timer event for the dragging of the colourpicker on the colourwheel, triggers every 50ms to prevent the serial data from overwriting eachother.
        /// </summary>
        private void ATimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if(isDragging == true && isOn == true && comConnect == true)
            {
                dataStream.WriteLine(string.Format(comTemplate, 0, 0, currentR, currentG, currentB));
            }
            
        }

        //Next section deals with the events to handle mouse input and dragging of the colour picker
        /// <summary>
        /// Move the colour picker object
        /// </summary>
        private void PickerPoint_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging == true)
            {             
                double top = Canvas.GetTop(pickerPoint) + Mouse.GetPosition(pickerPoint).Y - clickPosition.Y;
              
                double left = Canvas.GetLeft(pickerPoint) + Mouse.GetPosition(pickerPoint).X - clickPosition.X;

                double length = Math.Sqrt(Math.Pow(145 - left, 2) + Math.Pow(145 - top, 2));

                double angle = Math.Atan2(top - 145, left - 145);
                angle = angle * 360 / (2 * Math.PI);
                if(angle > 0)
                {
                    angle = angle - 360;
                }
                currentH = (int)Math.Floor(Math.Abs(angle));
                currentS = Math.Floor(0.68965517241 * length)/100;

                if (length > 140)
                {
                    double phi = Math.Atan2(top - 145, left - 145);
                    int x = (int)Math.Floor(145 + 145 * Math.Cos(phi));
                    int y = (int)Math.Floor(145 + 145 * Math.Sin(phi));
                    Canvas.SetTop(pickerPoint, y);
                    Canvas.SetLeft(pickerPoint, x);
                }
                else
                {
                    Canvas.SetTop(pickerPoint, top);
                    Canvas.SetLeft(pickerPoint, left);
                }
                

                int r, g, b;
                f.HsvToRgb(currentH, currentS, currentV, out r, out g, out b);
                currentR = r;
                currentG = g;
                currentB = b;
            }
        }

        private void PickerPoint_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            Mouse.Capture(null);
            if (comConnect)
            {
                saveSettings();
            }
        }

        private void PickerPoint_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
            clickPosition = Mouse.GetPosition(pickerPoint);
            Mouse.Capture(pickerPoint);
        }

       
        /// <summary>
        /// global toggle that sets the leds on or off
        /// </summary>
        private void ToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            if (enableToggle.IsChecked == true)
            {
                isOn = true;
                f.ConsoleOut("LED's On");
                dataStream.WriteLine(string.Format(comTemplate,0,0,255,255,255));
                Canvas.SetTop(pickerPoint, 150); //when enabling the leds reset the colour wheel to the center and set the leds to white
                Canvas.SetLeft(pickerPoint, 150);
                currentH = 0;
                currentS = 0;
                currentR = 255;
                currentG = 255;
                currentB = 255;
            }
            else
            {
                isOn = false;
                f.ConsoleOut("LED's Off");
                dataStream.WriteLine(string.Format(comTemplate, 0,0,0,0,0));
            }
            if (comConnect)
            {
                saveSettings();
            }
        }

        /// <summary>
        /// Event handling the commands being input in the console box
        /// </summary>
        private void textBox_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Return)
            {
                string command = consoleBox.Text.Trim();
                f.ConsoleOut("submitted console command: " + command);
                consoleBox.Text = "";

                if (command.StartsWith("port,")) //change ports
                {
                    try
                    {
                        dataStream.Close();
                    }
                    catch
                    {
                        f.ConsoleOut("cant close");
                    }
                    f.reconnect(command , out dataStream, out comConnect);
                }
                if (command.StartsWith("sendAll,")) //manual led access
                {                    
                    f.sendAll(command, dataStream,comConnect);
                }
                if (command.StartsWith("sendOne,")) //manual single led access
                {
                    f.sendOne(command, dataStream, comConnect);
                }
            }
        }


        /// <summary>
        /// toggle event to enable the breathing cycle
        /// </summary>
        private void enableBreathing_Click(object sender, RoutedEventArgs e)
        {
            if (enableBreathing.IsChecked == true)
            {
                isBreathing = true;
                isSpecial = false;
                enableSpecial.IsChecked = false;
            }
            else
            {
                isBreathing = false;
                currentV = 1;
                int r;
                int g;
                int b;
                f.HsvToRgb(currentH, currentS, currentV, out r, out g, out b); //reset the leds to be full brightness on the last colour in memory
                currentR = r;
                currentG = g;
                currentB = b;
                dataStream.WriteLine(string.Format(comTemplate, 0, 0, currentR, currentG, currentB));

            }
            if (comConnect)
            {
                saveSettings(); //check if we're connected the the remote MCU to prevent this event from saving the settings before the settings can be loaded
            }
        }

        /// <summary>
        /// Triggers when the slider value is changed.
        /// </summary>
        private void breathSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //some math to make sure the values are mapped properly
            breathUnderplier = Math.Pow(breathSlider.Value,Math.E) * 0.000001;
            if (comConnect)
            {
                saveSettings(); //check if we're connected the the remote MCU to prevent this event from saving the settings before the settings can be loaded
            }
        }

        /// <summary>
        /// Event that triggers when the toggle for the special is set.
        /// </summary>
        private void enableSpecial_Click(object sender, RoutedEventArgs e)
        {
            if(enableSpecial.IsChecked == true)
            {
                if(selectedSpecial == 3) //check if we're needing to take audio input, if so make a new AudioLoopback device
                {
                    sMix = new AudioLoopback(48000, 1);
                }
                else
                {
                    try
                    {
                        sMix.destroy();
                        sMix = null;
                    }
                    catch
                    {
                        Console.WriteLine("already closed audio device");
                    }
                }
                if(selectedSpecial == 5)
                {
                    m_GlobalHook = Hook.GlobalEvents();
                    m_GlobalHook.KeyPress += M_GlobalHook_KeyPress;
                }
            
                isSpecial = true;
                isBreathing = false;
                enableBreathing.IsChecked = false; //make sure the interface is updated accordingly 
               
            }
            else
            {
                try
                {
                    sMix.destroy();
                    sMix = null;
                    m_GlobalHook.KeyPress -= M_GlobalHook_KeyPress;
                    m_GlobalHook.Dispose();
                }
                catch
                {
                    Console.WriteLine("neh");
                }

                
                isSpecial = false;
            }
            if (comConnect) //check if we're connected the the remote MCU to prevent this event from saving the settings before the settings can be loaded
            {
                saveSettings();
            }
        }

        /// <summary>
        /// Toggle when dropdown menu selection is changed and save the change to the registry
        /// </summary>
        private void specialDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var box = sender as SplitButton;
            selectedSpecial = box.SelectedIndex;
            if(comConnect) //check if we're connected the the remote MCU to prevent this event from saving the settings before the settings can be loaded
            {
                saveSettings();
            }
        }

        /// <summary>
        /// store the current gobal variables in the registry for later retrieval
        /// see loadSettings() for array index's
        /// </summary>
       public void saveSettings()
       { 
            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\CaseLightingV2");
            key.SetValue("Settings",isOn+","+isBreathing + "," +breathSlider.Value + "," + isSpecial + "," + selectedSpecial + "," + currentH + "," + currentS + "," + currentV + "," + currentR + "," + currentG + "," + currentB );
            key.Close();
        }

        /// <summary>
        /// load the settings currently stored in the registry
        /// 0 = onOff
        /// 1 = breathing
        /// 2 = breathingspeed
        /// 3 = special
        /// 4 = specialSelected
        /// 5 = h
        /// 6 = s
        /// 7 = v
        /// 8 = r
        /// 9 = g
        /// 10 = b
        /// </summary>
        public void loadSettings()
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\CaseLightingV2");
            if (key.GetValue("Settings") != null)
            {
                string a = key.GetValue("Settings").ToString();
                string[] b = a.Split(',');
                isOn = bool.Parse(b[0]);
                enableToggle.IsChecked = bool.Parse(b[0]);
                isBreathing = bool.Parse(b[1]);
                enableBreathing.IsChecked = bool.Parse(b[1]);
                breathSlider.Value = double.Parse(b[2]);
                isSpecial = bool.Parse(b[3]);
                enableSpecial.IsChecked = bool.Parse(b[3]);
                selectedSpecial = int.Parse(b[4]);
                specialDropdown.SelectedIndex = int.Parse(b[4]);
                currentH = int.Parse(b[5]);
                currentS = double.Parse(b[6]);
                currentV = double.Parse(b[7]);
                currentR = int.Parse(b[8]);
                currentG = int.Parse(b[9]);
                currentB = int.Parse(b[10]);
            }
            dataStream.WriteLine(string.Format(comTemplate, 0, 0, currentR, currentG, currentB));

        }

        /// <summary>
        /// Function to grab the current cpu usage
        /// </summary>
        private static string CurrentCPUUsage
        {
            get
            {
                return cpuCounter.NextValue().ToString();
            }
        }
    }
}
