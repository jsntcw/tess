using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using CaptchaOCR.AppEntry.Engine.IPCommon;

namespace CaptchaOCR.AppEntry.Engine.IPCore
{
    internal class HistogramProcessor
    {
        public static Histogram Calc(IImage image)
        {
            if (image == null)
                return null;

            Histogram hist = null;
            if (image is GreyImage)
            {
                hist = new Histogram(image as GreyImage);
                hist.Normalize();
                return hist;
            }

            if (image is RGBImage)
            {
                hist = new Histogram((image as RGBImage).ConvertToGreyImage() as GreyImage);
                hist.Normalize();
                return hist;
            }

            return null;
        }

        public static Histogram[] Calc(IImage[] images)
        {
            if (images == null || images.Length == 0)
                return null;

            Histogram[] hists = new Histogram[images.Length];
            for (int i = images.Length - 1; i >= 0; i--)
            {
                hists[i] = HistogramProcessor.Calc(images[i]);
            }

            return hists;
        }

        public static Histogram LoadFromFile(string filePath)
        {
            if (filePath == null || filePath == string.Empty)
                return null;

            Histogram hist = null;
            try
            {
                hist = Histogram.Load(filePath);
            }
            catch
            {
                hist = null;
            }

            return hist;
        }

        public static Histogram[] Calc(string folder)
        {
            if (folder == null || folder == string.Empty)
                return null;

            List<Histogram> hists = new List<Histogram>();

            try
            {
                string[] files = Directory.GetFiles(folder);
                foreach (string file in files)
                {
                    try
                    {
                        GreyImage greyImage = new GreyImage();
                        if (!greyImage.Load(file))
                        {
                            continue;
                        }

                        Histogram hist = HistogramProcessor.Calc(greyImage);
                        if (hist != null)
                        {
                            hists.Add(hist);
                        }
                    }
                    catch
                    {
                        // nothing to do
                    }
                }
            }
            catch
            {
                hists = null;
            }

            if (hists == null || hists.Count == 0)
                return null;

            return hists.ToArray();
        }

        public static Histogram[] LoadFromFolder(string folder)
        {
            if (folder == null || folder == string.Empty)
                return null;

            List<Histogram> hists = new List<Histogram>();

            try
            {
                string[] files = Directory.GetFiles(folder);
                foreach (string file in files)
                {
                    try
                    {

                        Histogram hist = HistogramProcessor.LoadFromFile(file);
                        if (hist != null)
                        {
                            hists.Add(hist);
                        }
                    }
                    catch
                    {
                        // nothing to do
                    }
                }
            }
            catch
            {
                hists = null;
            }

            if (hists == null || hists.Count == 0)
                return null;

            return hists.ToArray();
        }

        public static Histogram Avg(Histogram[] hists)
        {
            if (hists == null)
                return null;

            Histogram avgHist = new Histogram();
            avgHist.NumBins = 256;
            avgHist.Bins = new float[avgHist.NumBins];

            int nBins = avgHist.NumBins;
            float[] avgData = avgHist.Bins;

            int nValidHistograms = 0;
            foreach (Histogram hist in hists)
            {
                if (hist == null)
                    continue;

                nValidHistograms++;

                float[] currentHistData = hist.Bins;
                for (int i = 0; i < nBins; i++)
                {
                    avgData[i] += currentHistData[i];
                }
            }

            if (nValidHistograms == 0)
                return null;

            for (int i = 0; i < nBins; i++)
            {
                avgData[i] = avgData[i] / nValidHistograms;
            }

            return avgHist;
        }

        public static double Diff(Histogram h1, Histogram h2)
        {
            if (h1 == null || h2 == null)
                return double.MaxValue;

            int nBins = h1.NumBins;
            float[] h1Data = h1.Bins;
            float[] h2Data = h2.Bins;

            double diff = 0;
            for (int i = 0; i < nBins; i++)
            {
                //diff += (h1Data[i] - h2Data[i]) * (h1Data[i] - h2Data[i]);

                diff += Math.Abs(h1Data[i] - h2Data[i]);
            }

            //return Math.Sqrt(diff);
            return diff;
        }
    }
}
