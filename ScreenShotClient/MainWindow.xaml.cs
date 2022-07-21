
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ScreenShotClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private BitmapSource src;
        public BitmapSource Src
        {
            get => src; set
            {
                src = value;
                OnPropertyChanged();
            }
        }

        UdpClient client;
        TcpClient tcpClient = new TcpClient();
        IPEndPoint remoteEndPoint;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }
        private void ImageRecive()
        {
            string imageName = "Image-" + System.DateTime.Now.Ticks + ".JPG";
            try
            {
                UdpClient client = new UdpClient(63291);
                byte[] c = client.Receive(ref remoteEndPoint);
                int t = int.Parse(Encoding.ASCII.GetString(c));
                byte[] data = new byte[t];
                List<byte[]> bytes = new List<byte[]>();
                for (int i = 0; i < t / 65000; i++)
                {
                    bytes.Add(client.Receive(ref remoteEndPoint));
                }
                foreach (var item in bytes)
                {
                    for (int i = 0; i < item.Length; i++)
                    {
                        data.Append(item[i]);
                    }
                }
                MessageBox.Show(data.Length.ToString());
                MemoryStream ms = new MemoryStream(data);
                var img = (Bitmap)Image.FromStream(ms);
                img.Save(imageName, ImageFormat.Jpeg);
                Src = BitmapToBitmapSource(img);
                Image1.Source = Src;
                ms.Close();
            }
            catch (Exception e)
            {

                MessageBox.Show(e.Message);
            }
            MessageBox.Show(Src.Width.ToString());
        }
        public BitmapSource BitmapToBitmapSource(Bitmap source)
        {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                          source.GetHbitmap(),
                          IntPtr.Zero,
                          Int32Rect.Empty,
                          BitmapSizeOptions.FromEmptyOptions());
        }
        private Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        private void Receiver()
        {
            int countErorr = 0;
            UdpClient client = new UdpClient(63291);
            object a = new object();

            while (true)
            {
                lock (a)
                {

                    try
                    {
                        MemoryStream memoryStream = new MemoryStream();
                        byte[] bytes = client.Receive(ref remoteEndPoint);
                        memoryStream.Write(bytes, 2, bytes.Length - 2);

                        int countMsg = bytes[0] - 1;
                        if (countMsg > 10)
                            throw new Exception();
                        for (int i = 0; i < countMsg; i++)
                        {
                            byte[] bt = client.Receive(ref remoteEndPoint);
                            memoryStream.Write(bt, 0, bt.Length);
                        }

                        GetData(memoryStream.ToArray());
                        memoryStream.Close();

                    }
                    catch
                    {
                        dispatcher.Invoke(() =>
                        {
                            countErorr++;
                        });
                    }
                    Thread.Sleep(10000);

                }
            }
        }
        private void GetData(byte[] Date)
        {
            dispatcher.Invoke(() =>
            {
                Src = BitmapToBitmapSource(ConvertToBitmap(Date));
            });
        }        
        private Bitmap ConvertToBitmap(byte[] bytes)
        {
            MemoryStream memoryStream = new MemoryStream(bytes);

            Bitmap bmp =
                (Bitmap)Bitmap.FromStream(memoryStream);
            
            memoryStream = new MemoryStream();
            bmp.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);  
            return bmp;
        }

        public void ReceiveScreenshot()
        {

            try
            {
                UdpClient client = new UdpClient(63291);
                byte[] receivedBytes = client.Receive(ref remoteEndPoint);
                var fragmentCount = int.Parse(Encoding.ASCII.GetString(receivedBytes));
                MessageBox.Show(fragmentCount.ToString(),"FRG count");
                int fragmentSize = 65500;
                var screenshotSize = long.Parse(Encoding.ASCII.GetString(client.Receive(ref remoteEndPoint)));
                MessageBox.Show(screenshotSize.ToString(),"Sc Size");
                byte[] screenshot = new byte[screenshotSize];
                byte[] buffer;
                for (int fragNum = 0; fragNum < fragmentCount; fragNum++)
                {
                    buffer = client.Receive(ref remoteEndPoint);                    
                    for (int i = 0; i < buffer.Length; i++)
                        screenshot[i + fragNum * fragmentSize] = buffer[i];
                }
                MessageBox.Show(screenshot.Length.ToString(),"SC yekun");
                MemoryStream ms = new MemoryStream(screenshot);
                string imageName = "Image-" + System.DateTime.Now.Ticks + ".JPG";
                var img = (Bitmap)Image.FromStream(ms);
                img.Save(imageName, ImageFormat.Jpeg);
                Src = BitmapToBitmapSource(img);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void btn_cnnct_Click(object sender, RoutedEventArgs e)
        {            
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(ipadr.Text), 0);
            var EndPoint = new IPEndPoint(IPAddress.Parse("192.168.0.109"), 63291);
            try
            {

                tcpClient.Connect(EndPoint);
                if (tcpClient.Connected)
                {
                    var stream = tcpClient.GetStream();
                    var bw = new BinaryWriter(stream);
                    bw.Write(ipadr.Text);
                    tcpClient.Close();
                    Task.Run(() =>
                    {
                        Receiver();
                    });
                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            tcpClient.Dispose();
            
        }
    }
}
