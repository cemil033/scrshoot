using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerUdp
{
    internal class Program
    {
        static TcpListener listener;
        static UdpClient client=new UdpClient();
        static string ipadr;
        static IPEndPoint remoteEndPoint;
        static void Main(string[] args)
        {
            Task.Run(
               () =>
               {
                   while (true) { 
                   var ip = IPAddress.Parse("192.168.0.109");
                   var ep = new IPEndPoint(ip, 63291);
                   listener = new TcpListener(ep);
                   object a = new object();

                       while (true)
                       {
                           listener.Start();
                           var client1 = listener.AcceptTcpClient();
                           Console.WriteLine($"{client1.Client.RemoteEndPoint} Connected...");
                           var stream = client1.GetStream();
                           var br = new BinaryReader(stream);
                           while (client1.Connected)
                           {
                               ipadr = br.ReadString();
                               remoteEndPoint = new IPEndPoint(IPAddress.Parse(ipadr), 63291);
                               Console.WriteLine(ipadr);
                               client1.Close();
                               listener.Stop();                               
                           }
                           while (true)
                           {
                               SendIMG();
                               Thread.Sleep(10000);
                           }
                       }
                   }

               });            
            Console.ReadKey();
        }
        static byte[] ConvertToByte(Bitmap bmp)
        {
            MemoryStream memoryStream = new MemoryStream();
            bmp.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);
            return memoryStream.ToArray();
        }

        static List<byte[]> CutMsg(byte[] bt)
        {
            int Lenght = bt.Length;
            byte[] temp;
            List<byte[]> msg = new List<byte[]>();

            MemoryStream memoryStream = new MemoryStream();
            // Записываем в первые 2 байта количество пакетов
            memoryStream.Write(
                      BitConverter.GetBytes((short)((Lenght / 65500) + 1)), 0, 2);
            // Далее записываем первый пакет
            memoryStream.Write(bt, 0, bt.Length);

            memoryStream.Position = 0;
            // Пока все пакеты не разделили - делим КЭП
            while (Lenght > 0)
            {
                temp = new byte[65500];
                memoryStream.Read(temp, 0, 65500);
                msg.Add(temp);
                Lenght -= 65500;
            }

            return msg;
        }
        
       
        static void SendIMG()
        {
            Bitmap BackGround = new Bitmap(1920, 1080);
            Graphics graphics = Graphics.FromImage(BackGround);

            while (true)
            {                
                graphics.CopyFromScreen(0, 0, 0, 0, new Size(1920, 1080));                
                byte[] bytes = ConvertToByte(BackGround);
                List<byte[]> lst = CutMsg(bytes);
                for (int i = 0; i < lst.Count; i++)
                {                    
                    client.Send(lst[i], lst[i].Length, remoteEndPoint);
                }
            }
        }
        

      
    }
}
