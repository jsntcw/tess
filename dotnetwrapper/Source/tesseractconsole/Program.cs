using System;
using System.Collections.Generic;
using System.Text;

using System.Reflection;
using System.IO;
using System.Drawing;
using OCR.TesseractWrapper;

namespace tesseractconsole
{
    class Program
    {
        static void Main(string[] args)
        {
            //Recognize();

            AnalyseLayout();

            Console.Write("\n\n\nPress any key to exit...");
            Console.ReadKey();
        }

        private static void Recognize()
        {
            string imageFile = @"D:\Self-Study\OpenSources\Tesseract\original\phototest.tif";
            imageFile = @"D:\Self-Study\OpenSources\Tesseract\original\eurotext.tif";


            string tessdata = @"D:\Self-Study\OpenSources\Tesseract\original\tessdata\";
            string language = "eng";
            int oem = 3;
            
            TesseractProcessor processor = new TesseractProcessor();
            processor.Init(tessdata, language, oem);            

            using (Bitmap bmp = Bitmap.FromFile(imageFile) as Bitmap)
            {                
                string text = processor.Recognize(bmp);
                Console.WriteLine(
                    string.Format("Text:\n{0}\n", text));
            }
        }

        private static void AnalyseLayout()
        {
            string imageFile = @"D:\Self-Study\OpenSources\Tesseract\original\phototest.tif";
            //imageFile = @"D:\Self-Study\OpenSources\Tesseract\original\eurotext.tif";

            TesseractProcessor processor = new TesseractProcessor();
            processor.InitForAnalysePage();
            processor.SetPageSegMode(ePageSegMode.PSM_AUTO);
           
            using (Bitmap bmp = Bitmap.FromFile(imageFile) as Bitmap)
            {                
                DocumentLayout doc = processor.AnalyseLayout(bmp);
                Console.WriteLine(doc.ToString());

                using (Image tmp = new Bitmap(bmp.Width, bmp.Height)) // prevents one-byte index format
                {
                    using (Graphics grph = Graphics.FromImage(tmp))
                    {
                        Rectangle rect = new Rectangle(0, 0, tmp.Width, tmp.Height);

                        grph.DrawImage(bmp, rect, rect, GraphicsUnit.Pixel);

                        grph.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                        foreach (Block block in doc.Blocks)
                        {
                            DrawBlock(grph, block);
                        }
                    }

                    tmp.Save(@"D:\temp\page_layout_test2.bmp");
                }
            }
        }












        private static void DrawBlock(Graphics grph, Block block)
        {
            foreach (Paragraph para in block.Paragraphs)
                DrawParagraph(grph, para);

            block.Draw(grph);
        }

        private static void DrawParagraph(Graphics grph, Paragraph para)
        {
            foreach (TextLine line in para.Lines)
                DrawTextLine(grph, line);

            para.Draw(grph);
        }

        private static void DrawTextLine(Graphics grph, TextLine line)
        {
            foreach (Word word in line.Words)
                DrawWord(grph, word);

            line.Draw(grph);
        }

        private static void DrawWord(Graphics grph, Word word)
        {
            foreach (Character ch in word.CharList)
                DrawChar(grph, ch);

            word.Draw(grph);
        }

        private static void DrawChar(Graphics grph, Character ch)
        {
            ch.Draw(grph);
        }
    }
}
