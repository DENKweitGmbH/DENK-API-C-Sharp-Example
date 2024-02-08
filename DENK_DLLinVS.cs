// Beispiel für C# Integration der DENK DLL/API in Visual Studio 2019
// Created: Arvid Moritz, 05/2023

// Vorbereitung
// in dem Ordner der EXE müssen die Datein: denk.dll oder denk_dml.dll, DirectML.dll, onnxruntime.dll und ein Ordner mit dem "models" mit dem Netzwerk drine liegen
// und die Results.cs muss im Ordner mit liegen
// Google.Protobuf muss installiert werden in VS




// Einbinden von Bibliothken
using System;							// wird bei mir als unnötig angezeigt
using System.Drawing;					// wird bei mir als unnötig angezeigt
using System.Runtime.InteropServices;
using System.Text;
using System.Drawing;					// wird bei mir als unnötig angezeigt
using System.Drawing.Imaging;
using System.Windows.Forms;     		// wird bei mir als unnötig angezeigt
using AForge.Video;                     //Verwendung von USB Kameras
using AForge.Video.DirectShow;          //Verwendung von USB Kameras
using Google.Protobuf;					// wird bei mir als unnötig angezeigt
using static Results;					// Einbinden der Results.cs Datei



// Aufruf der DLL und Import der Funktionen
// für die Verwendung von GPUs unter Windows 10 ab Version 1904 --> Austaschen der "denk.dll" gegen "denk_dml.dll"
 
private FilterInfoCollection videoDevices;
private VideoCaptureDevice videoSource;
private Bitmap snapshotImage;

[DllImport("denk.dll")] static extern Int32 FindDongle();
[DllImport("denk.dll")] public static extern int TokenLogin(string token, string new_label);
[DllImport("denk.dll")] static extern Int32 GetOnnxVersion(out Int32 major, out Int32 minor);
[DllImport("denk.dll")] static extern Int32 GetDeviceInformation([Out] Byte[] protoChars, ref Int32 protoSize);
[DllImport("denk.dll")] static extern void UnsetInfoFile(Byte[] infoData, Int32 infoDataSize);
[DllImport("denk.dll")] static extern void EndSession();
[DllImport("denk.dll")] static extern Int32 SwitchBR(Byte[] data, Int32 slength);
[DllImport("denk.dll")] public static extern int EvaluateImage(Int32 index);

[DllImport("denk.dll")]
static extern Int32 ReadAllModels(Byte[] path, [Out] Byte[] protoChars, ref Int32 protoSize, Int32 device);


[DllImport("denk.dll")]
static extern Int32 LoadImageData(ref Int32 datasetIndex, Byte[] imageData, Int32 imageDataLength);

[DllImport("denk.dll")]
private static extern int GetResults( Int32 index, [Out] Byte[] output_chars, ref Int32 output_chars_size); // int


[DllImport("denk.dll")]
public static extern int DrawBoxes(Int32 index, double overlap_threshold, double alpha_boxes, double alpha_segmentations, [Out] Byte[] output_data, ref Int32 output_data_size);



// Auswertung auf Button Click Aktion
 private void button_connect_Click(object sender, EventArgs e)
        {
            StringWriter stringWriter = new StringWriter();
            Console.SetOut(stringWriter);

            //------------------------------------------------------------------
            // Check for Licence
            // Lade Token
            string token = "";
            string new_label = "YourPC";
            int result = TokenLogin(token, new_label);

            //Console.WriteLine(result.ToString());
            //textBox1.Text = result.ToString();
            //Console.WriteLine(result.ToString());

            //------------------------------------------------------------------
            //Read all Models funktion

            string networkPathString = "models";
            Byte[] networkPathBytes = Encoding.UTF8.GetBytes(networkPathString);

            int proto_size = 10000;
            byte[] proto_chars = new byte[proto_size];
            int device1 = -1;
            int result1 = ReadAllModels(networkPathBytes, proto_chars, ref proto_size, device1);

            Console.WriteLine(result1.ToString());

            
            Results resultObj;
            Byte[] slicedArray = proto_chars.Take(proto_size).ToArray();
            resultObj = Results.Parser.ParseFrom(slicedArray);


            // Print out results
            Console.WriteLine("Models:");
            Console.WriteLine(resultObj.ToString());

            //------------------------------------------------------------------

            string consoleOutput = stringWriter.ToString();
            textBox1.Text = consoleOutput;


            //------------------------------------------------------------------
            // Load Image

            //Image from Snapshot
            Bitmap image22 = (Bitmap)pictureBoxSnapshot.Image;

            byte[] imageBytes;
            using (MemoryStream ms = new MemoryStream())
            {
                image22.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg); // oder ein anderes Format, das du benötigst
                imageBytes = ms.ToArray();
            }

            Int32 dataset_index = 0; // initialize your variables
            byte[] image_in_data = imageBytes;
            int image_in_data_size = image_in_data.Length;
            IntPtr image_in_data_ptr = Marshal.AllocHGlobal(image_in_data_size);
            Marshal.Copy(image_in_data, 0, image_in_data_ptr, image_in_data_size);

            int result2 = LoadImageData(ref dataset_index, image_in_data, image_in_data_size);

            textBox1.Text = result2.ToString();

            //------------------------------------------------------------------
            // Evaluate Image
            int result3 = EvaluateImage(dataset_index); // Aufruf der Funktion EvaluateImage
            textBox1.Text = result3.ToString();

            //------------------------------------------------------------------
            //GET RESULTS

            int outputSize = 100000;
            byte[] output = new byte[outputSize];

            int result4 = GetResults(dataset_index, output, ref outputSize);
            if (result4 != 0xde000000)
            {
                if (result4 == 0xde000010)
                {
                    output = new byte[outputSize];
                    result4 = GetResults(dataset_index, output, ref outputSize);
                }
            }
            
            Results resultObjout;
            Byte[] slicedArray2 = output.Take(outputSize).ToArray();
            resultObjout = Results.Parser.ParseFrom(slicedArray2);


            // Print out results
            Console.WriteLine("Out:");
            Console.WriteLine(resultObjout.ToString());
            //Console.WriteLine(resultObj.ToString());

            textBox1.Text = result3.ToString();
            string consoleOutput1 = stringWriter.ToString();

            textBox1.Text = consoleOutput1;
            //------------------------------------------------------------------
            //DRAW 

            // Rufe die DrawBoxes-Funktion auf
            double overlapThreshold = 1.0;
            double alphaBoxes = 0.5;
            double alphaSegmentations = 0.5;

            int resultSize = 0x002a3000;
            byte[] resultImageRGB = new byte[resultSize]; ;
            
            int result5 = DrawBoxes(dataset_index, overlapThreshold, alphaBoxes, alphaSegmentations, resultImageRGB, ref resultSize);
                if (result5 != 0xde000000)
                {
                    if (result5 == 0xde000010)
                    {
                    resultImageRGB = new byte[resultSize];
                        result5 = DrawBoxes(dataset_index, overlapThreshold, alphaBoxes, alphaSegmentations, resultImageRGB, ref resultSize);
                    }

                }



            byte[] rgbByteArray = resultImageRGB; // your RGB byte array
            int width = image22.Width;
            int height = image22.Height;


            SwitchBR(rgbByteArray, resultSize);

            // Create a Bitmap object with the specified width and height
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb); //Format24bppBgr Format24bppRgb

            // Lock the bitmap's bits
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                                              ImageLockMode.WriteOnly,
                                              bmp.PixelFormat);

            // Copy the RGB byte array to the bitmap's bits
            Marshal.Copy(rgbByteArray, 0, bmpData.Scan0, rgbByteArray.Length);

            // Unlock the bitmap's bits
            bmp.UnlockBits(bmpData);

            // Your image is now in the bmp object
            pictureBoxSnapshot.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.Image = bmp; //resultImage

            //------------------------------------------------------------------
            //Data-Output

            //pictureBoxSnapshot.SizeMode = PictureBoxSizeMode.Zoom;
            //pictureBox1.Image = bitmap; //resultImage
            //string consoleOutput3 = stringWriter.ToString();
            double evaluationTime = 0.0;
            double postProcessingTime = 0.0;
            foreach (ResultField otp in resultObjout.Output)
            {
                evaluationTime += otp.EvaluationTimeMs;
                postProcessingTime += otp.PostProcessingTimeMs;
                Console.WriteLine(evaluationTime.ToString());
            }
            //double evatime = 0;
            //ResultField otp = resultObjout.Output;
            //evatime = otp.EvaluationTimeMs;
            textBox1.Text = result5.ToString();
            textBox1.Text = evaluationTime.ToString();
            string consoleOutput2 = stringWriter.ToString();
            //Label label = new Label();


            textBox1.Text = consoleOutput2;
        }