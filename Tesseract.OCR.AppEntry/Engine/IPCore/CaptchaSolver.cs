#if DEBUG
#define TEST
#endif

using System;
using System.Collections.Generic;
using System.Text;
using CaptchaOCR.AppEntry.Engine.IPCommon;
using System.Drawing;
using CaptchaOCR.AppEntry.Engine.IPMath;

namespace CaptchaOCR.AppEntry.Engine.IPCore
{
    internal class CaptchaSolver
    {
        public static GreyImage[] GoldenImages = null;
        public static Cluster[] GoldenClusters = null;

        public static GreyImage PerformPreprocessing(IImage image)
        {
            if (image == null) return null;

            IImage greyImage = null;
            if (image is RGBImage)
                greyImage = (image as RGBImage).ConvertToGreyImage();
            else
                greyImage = image;

            greyImage = (greyImage as GreyImage).InvertColor();

            Histogram histogram = HistogramProcessor.Calc(greyImage);

            double backgroundColor = histogram.MaxBin;
            double subjectVal = backgroundColor + 20;
            //double subjectVal = backgroundColor + 19;

            Calculator calculator = new Calculator();
            greyImage = calculator.Apply(greyImage, eOperator.Subtraction, subjectVal);
            double mulFactor = 4.2;
            greyImage = calculator.Apply(greyImage, eOperator.Multiplication, mulFactor);

            return greyImage as GreyImage;

        }

        public static Cluster[] DetectObjects(
            IImage greyImage, ref Point[] objCenters, ref Polygon[] polyBounds)
        {
            IImage result = new GreyImage(greyImage as GreyImage, true);

            IFilter maxFilter = new RankFilter(3, 3, 3 * 3 - 1);
            maxFilter.Apply(ref greyImage);

            // get objects
            CaptchaOpticalDetector detector = new CaptchaOpticalDetector(
                (byte[])greyImage.Data, greyImage.Width, greyImage.Height);

            Cluster[] clusters = null;
            polyBounds = null;
            objCenters = detector.GetObjects(
                (byte[])result.Data, ref clusters, ref polyBounds);

            return clusters;
        }

        public static string Match(Cluster[] clusters)
        {
            string result = "";

            for (int iCluster = 0; iCluster < clusters.Length; iCluster++)
            //foreach (Cluster cluster in clusters)
            {
                Cluster cluster = clusters[iCluster];

                double minDiff = double.MaxValue;
                int classId = 0;
                string className = "";
                double bestOrientation = 0;

                for (int i = 0; i < GoldenImages.Length; i++)
                {
                    double orientation = 0;
                    double diff = cluster.Match(GoldenImages[i],
                        GoldenClusters[i].CenterX, GoldenClusters[i].CenterY,
                        ref orientation);

                    if (diff < minDiff)
                    {
                        minDiff = diff;
                        className = GoldenClusters[i].ClassName;
                        bestOrientation = orientation;
#if TEST
                        classId = i;
#endif
                    }
                }

                result += className;

#if TEST
                clusters[iCluster] = cluster.Rotate(bestOrientation);
                clusters[iCluster].ClassID = classId;
#endif
            }

            return result;
        }
    }
}
