using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using NAudio.Wave;
using NAudio.Dsp;
using Microsoft.Win32;
using static System.Net.Mime.MediaTypeNames;

namespace VoiceAnalyzer
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private WaveOutEvent sout;
        private WaveFileReader audStream;
        private double bPs;
        private float[] samples;
        private int spectrogram_span;
        public MainWindow()
        {
            InitializeComponent();

            sout = new WaveOutEvent(); // プレイヤーの作成
        }
        async private void tick()
        {
            do
            {
                time_slider.Value = (double)audStream.Position;
                await Task.Delay(1);
            }
            while (sout.PlaybackState == PlaybackState.Playing);
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                sout.Play();
                tick();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            sout.Stop();
        }

        private void time_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            audStream.Position = (long)time_slider.Value;
            current_playback_position.Content = ((double)audStream.Position) / bPs;
            //Wave_Image_Scroll.ScrollToHorizontalOffset((((double)audStream.Position) / bPs+1) * (double)100 - (double)100);
            Spectrogram_Image_Scroll.ScrollToHorizontalOffset((((double)audStream.Position) / bPs + 1) * (double)100 - (double)100);
            Create_WaveImage();
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                FilePath_Label.Content = dialog.FileName;
                try
                {
                    audStream = new WaveFileReader(dialog.FileName); // ファイルの読み込み

                    //byte[] samples = new byte[audStream.Length / audStream.BlockAlign * audStream.WaveFormat.Channels];
                    //audStream.Read(samples, 0, samples.Length);
                    samples = new float[audStream.Length / audStream.BlockAlign];
                    for (int i = 0; i < samples.Length; i++)
                    {
                        float[] sample = audStream.ReadNextSampleFrame();
                        for (int c= 0; c < audStream.WaveFormat.Channels; c++)
                        {
                            samples[i] += sample[c]/audStream.WaveFormat.Channels;
                        }
                    }
                    Console.WriteLine("Data Loaded");
                    Console.WriteLine("Length: "+samples.Length.ToString());
                    //Console.WriteLine(String.Join(" ", samples));

                    bPs = (audStream.WaveFormat.BitsPerSample / 8) * audStream.WaveFormat.SampleRate * audStream.WaveFormat.Channels;
                    audio_length.Content = (int)audStream.Length / bPs; // 音声の長さ
                    time_slider.Maximum = (int)audStream.Length;
                    audStream.Position = 0;
                    sout.Init(audStream);
                    Create_WaveImage();
                    Create_SpectrogramImage();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        private void Create_WaveImage()
        {
            int width = (int)Wave_Image.ActualWidth;
            int height = (int)Wave_Image.ActualHeight;
            Console.WriteLine("WaveImage: "+width.ToString() + " " + height.ToString());
            int dpi = 100;
            WriteableBitmap bitmap = new WriteableBitmap(width, height, dpi, dpi, PixelFormats.Pbgra32, null);

            int pixelsSize = (int)(width * height * 4);
            byte[] pixels = new byte[pixelsSize];

            for (int t = 0; t < width; t++)
            {
                int sindex = (int)(t * 30 + ((double)audStream.Position)*0.5);
                if (sindex>=samples.Length)
                {
                    break;
                }
                int val = (int)(samples[sindex] * (height*0.3)) + (int)(height / 2);
                int index = (t + (val * width)) * 4;
                pixels[index] = 0;
                pixels[index + 1] = 0;
                pixels[index + 2] = 0;
                pixels[index + 3] = 255;
            }

            int stride = width * 4;
            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0, 0);

            Wave_Image.Source = bitmap;
        }
        private void Create_SpectrogramImage()
        {
            spectrogram_span = 256;
            int width = samples.Length / spectrogram_span;
            int height = spectrogram_span;
            Console.WriteLine("SpectrogramImage: " + width.ToString() + " " + height.ToString());
            int dpi = 100;
            WriteableBitmap bitmap = new WriteableBitmap(width, height, dpi, dpi, PixelFormats.Pbgra32, null);

            int pixelsSize = (int)(width * height * 4);
            byte[] pixels = new byte[pixelsSize];

            for (int x = 0; x < width; x++)
            {
                float[] values = dct4(samples, x * spectrogram_span, (x + 1) * spectrogram_span);
                for (int y = 0; y < height; y++)
                {
                    int index = (x + (y * width)) * 4;
                    pixels[index] = (byte)(values[height-y-1]*100);
                    pixels[index + 1] = (byte)(values[height - y - 1] *10);
                    pixels[index + 2] = (byte)(values[height - y - 1] /10);
                    pixels[index + 3] = 255;
                }
            }

            int stride = width * 4;
            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0, 0);

            Spectrogram_Image.Source = bitmap;
            Spectrogram_Image.Height = height * 2;
            Spectrogram_Image.Width = width * 2;
        }
        private float[] dct4(float[] data,int start,int end)
        {
            int N = end-start;
            float[] ret = new float[N];
            for (int k=0;k<N;k++)
            {
                for (int n=0;n<N;n++) {
                    ret[k] += (float)Math.Cos( (Math.PI/N)*(n+0.5)*(k+0.5) )*data[start+n];
                }
            }
            return ret;
        }
    }
}
