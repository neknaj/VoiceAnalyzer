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
        public MainWindow()
        {
            InitializeComponent();

            sout = new WaveOutEvent(); // プレイヤーの作成
        }
        async private void tick()
        {
            do
            {
                current_playback_position.Content = ((double)audStream.Position) / bPs;
                time_slider.Value = (double)audStream.Position;
                Wave_Image_Scroll.ScrollToHorizontalOffset((((double)audStream.Position) / bPs + 1) * (double)100 - (double)100);
                Spectrogram_Image_Scroll.ScrollToHorizontalOffset((((double)audStream.Position) / bPs + 1) * (double)100 - (double)100);
                await Task.Delay(10);
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
            Wave_Image_Scroll.ScrollToHorizontalOffset((((double)audStream.Position) / bPs+1) * (double)100 - (double)100);
            Spectrogram_Image_Scroll.ScrollToHorizontalOffset((((double)audStream.Position) / bPs + 1) * (double)100 - (double)100);
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
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        private void Create_WaveImage()
        {
            int width = samples.Length/20;
            int height = 500;
            int dpi = 100;
            WriteableBitmap bitmap = new WriteableBitmap(width, height, dpi, dpi, PixelFormats.Pbgra32, null);

            // 計算用のバイト列の準備
            int pixelsSize = (int)(width * height * 4);
            byte[] pixels = new byte[pixelsSize];

            for (int t = 0; t < samples.Length; t++)
            {
                int val = (int)(samples[t] * 200) + (int)(height / 2);
                int index = (t/20 + (val * width)) * 4;
                pixels[index] = 0;
                pixels[index + 1] = 0;
                pixels[index + 2] = 0;
                pixels[index + 3] = 255;
            }

            // バイト列をBitmapImageに変換する
            int stride = width * 4; // 一行あたりのバイトサイズ
            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0, 0);

            // ウィンドウに表示
            Wave_Image.Source = bitmap;
            Wave_Image.Height = height*2;
            Wave_Image.Width = width*0.04;
            Spectrogram_Image.Source = bitmap;
            Spectrogram_Image.Height = height * 2;
            Spectrogram_Image.Width = width * 0.04;
        }
    }
}
