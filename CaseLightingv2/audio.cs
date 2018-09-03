using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace CaseLightingv2
{
    /// <summary>
    /// Class containing the audio piping for the audio in preset
    /// </summary>
    class AudioLoopback
    {
        int samplenumber = 0;
        double maxval = 0;
        double minval = 0;
        int SAMPLE_RATEi = 0;
        int DISPLAY_UPDATE_RATE = 32;

        WaveIn waveIn;

        int currentR = 0;
        int currentG = 0;
        int currentB = 0;

        /// <summary>
        /// constructor for the object
        /// </summary>
        /// <param name="SAMPLE_RATE"></param>
        /// <param name="DeviceNum"></param>
        public AudioLoopback(int SAMPLE_RATE, int DeviceNum)
        {

            SAMPLE_RATEi = SAMPLE_RATE;
            int waveInDevices = WaveIn.DeviceCount;
            for (int waveInDevice = 0; waveInDevice < waveInDevices; waveInDevice++)
            {
                WaveInCapabilities deviceInfo = WaveIn.GetCapabilities(waveInDevice);
                Console.WriteLine("Device {0}: {1}, {2} channels",
                    waveInDevice, deviceInfo.ProductName, deviceInfo.Channels);
            }
                waveIn = new WaveIn();
                waveIn.DeviceNumber = DeviceNum; //TODO: Let the user choose which device, this comes from the device numbers above
                waveIn.DataAvailable += waveIn_DataAvailable;
                int sampleRate = SAMPLE_RATE; // 8 kHz
                int channels = 1; // mono
                waveIn.WaveFormat = new WaveFormat(sampleRate, channels);
                waveIn.StartRecording();
            
        }

        /// <summary>
        /// event that triggers when there is new data on the device port which is like fucking always
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            for (int index = 0; index < e.BytesRecorded; index += 2)
            {
                short sample = (short)((e.Buffer[index + 1] << 8) |
                                        e.Buffer[index + 0]);
                float sample32 = sample / 32768f;

                ProcessSample(sample32);
                //Console.WriteLine(sample32);
            }

        }

        /// <summary>
        /// function that runs the show and calcs the highest and lowest value
        /// </summary>
        /// <param name="sample1"></param>
        void ProcessSample(float sample1)
        {
            samplenumber += 1;

            if (sample1 > maxval)
            {
                maxval = sample1;
            }

            if (sample1 < minval)
            {
                minval = sample1;
            }


            //Run updateView every few loops
            //only update every few ms because it locks up the program otherwise
            if (samplenumber > (double)SAMPLE_RATEi / DISPLAY_UPDATE_RATE)
            {
                samplenumber = 0;
                updateView(); //needs to be fast!
            }
        }

        /// <summary>
        /// function that outputs the data and logs it to console
        /// </summary>
        void updateView()
        {
            Console.WriteLine(maxval);
            //Console.WriteLine(minval);

            maxval = 0;
            //minval = 0;
        }

        /// <summary>
        /// public function to calculate the rgb values and take in the maxval of the audio as the HSV value
        /// </summary>
        /// <param name="currentH"></param>
        /// <param name="currentS"></param>
        /// <param name="currentR"></param>
        /// <param name="currentG"></param>
        /// <param name="currentB"></param>
        public void getRGB(int currentH, double currentS, out int currentR, out int currentG, out int currentB)
        {
            int r, g, b;
            f.HsvToRgb(currentH, currentS, maxval, out r, out g, out b);
            Console.WriteLine(r + "," + g + "," + b);
            currentR = r;
            currentG = g;
            currentB = b;
        }


        /// <summary>
        /// function that kills the intenal processes and unregisters the event. dont forget to also set the called object to null in the main script.
        /// </summary>
        public void destroy()
        {
            waveIn.StopRecording();
            waveIn.DataAvailable -= waveIn_DataAvailable;
            
            Console.WriteLine("killed event");
            waveIn = null;
        }
    }
}
