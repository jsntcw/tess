using System;
using System.Collections.Generic;
using System.Text;
using CaptchaOCR.AppEntry.Engine.IPMath;
using System.Drawing;
using CaptchaOCR.AppEntry.Engine.IPCommon;
using System.IO;

namespace CaptchaOCR.AppEntry.Engine.IPCore
{
    internal class Cluster : IComparable
    {
        private const double dAngle = 30;
        private const double minAngle = -dAngle;
        private const double maxAngle = dAngle;
        private const double stepAngle = 1;

        private const int MaxWidth = 24;
        private const int MaxHeight = 24;

        public int ClassID = 0;
        public string ClassName = "";
        public int PointsCount = 0;
        public List<double> XPoints = new List<double>(100);
        public List<double> YPoints = new List<double>(100);
        public double CenterX = 0;
        public double CenterY = 0;

        public double XMin = double.MaxValue;
        public double XMax = double.MinValue;
        public double YMin = double.MaxValue;
        public double YMax = double.MinValue;

        public double SzX = 0;
        public double SzY = 0;

        private Polygon _polyBounds = null;

        public Cluster()
        {
        }

        public Point Center
        {
            get { return new Point((int)CenterX, (int)CenterY); }
        }

        public Polygon PolyBounds
        {
            get
            {
                if (_polyBounds == null)
                {
                    FitPolyBounds();
                }

                return _polyBounds;
            }
        }

        public Point[] ToPoints
        {
            get
            {
                Point[] pts = new Point[PointsCount];

                for (int i = 0; i < PointsCount; i++)
                {
                    pts[i] = new Point((int)XPoints[i], (int)YPoints[i]);
                }

                return pts;
            }
        }

        public void CalcDataRange()
        {
            double centerX = 0;
            double centerY = 0;
            for (int i = 0; i < PointsCount; i++)
            {
                if (XMin > XPoints[i]) XMin = XPoints[i];
                if (XMax < XPoints[i]) XMax = XPoints[i];
                if (YMin > YPoints[i]) YMin = YPoints[i];
                if (YMax < YPoints[i]) YMax = YPoints[i];

                centerX += XPoints[i];
                centerY += YPoints[i];
            }

            CenterX = centerX / PointsCount;
            CenterY = centerY / PointsCount;
        }

        public void FitPolyBounds()
        {
            double minArea = double.MaxValue;
            double orientation = 0;

            for (double angle = minAngle; angle <= maxAngle; angle += stepAngle)
            {
                double area = CalcArea(angle);
                if (area < minArea)
                {
                    minArea = area;
                    orientation = angle;
                }
            }

            _polyBounds = CreatePolygonBounds(orientation);
        }

        public double GetAlignmentAngle()
        {
            double dAngle = 30;            

            double minArea = double.MaxValue;
            double orientation = 0;

            for (double angle = minAngle; angle <= maxAngle; angle += stepAngle)
            {
                double area = CalcArea(angle);
                if (area < minArea)
                {
                    minArea = area;
                    orientation = angle;
                }
            }

            return orientation;
        }

        private double CalcArea(double angle)
        {
            return AAA(angle);

            double area = 0;

            double orientation = angle * Math.PI / 180;
            double sin = Math.Sin(orientation);
            double cos = Math.Cos(orientation);

            double xMin = double.MaxValue;
            double yMin = double.MaxValue;
            double xMax = double.MinValue;
            double yMax = double.MinValue;

            double x, y, rx, ry;
            for (int i = 0; i < PointsCount; i++)
            {
                x = (double)XPoints[i] - CenterX;
                y = CenterY - (double)YPoints[i];

                rx = cos * x - sin * y;
                ry = sin * x + cos * y;

                if (rx < xMin) xMin = rx;
                if (rx > xMax) xMax = rx;
                if (ry < yMin) yMin = ry;
                if (ry > yMax) yMax = ry;
            }

            area = (xMax - xMin) * (yMax - yMin);

            return area;
        }

        private double AAA(double angle)
        {
            Cluster c = this.Rotate(angle);
            c.CalcDataRange();

            double countMin = 0;
            double countMax = 0;

            int yMin = (int)c.YMin;
            int yMax = (int)c.YMax;
            int y;

            for (int i = c.PointsCount - 1; i >= 0; i--)
            {
                y = (int)c.YPoints[i];
                if (yMin == y)
                    countMin += 1.0;
                else if (yMax == y)
                    countMax += 1.0;
            }

            return -(Math.Max(countMin, countMax));
        }

        private Polygon CreatePolygonBounds(double angle)
        {
            double orientation = angle * Math.PI / 180;
            double sin = Math.Sin(orientation);
            double cos = Math.Cos(orientation);

            double xMin = double.MaxValue;
            double yMin = double.MaxValue;
            double xMax = double.MinValue;
            double yMax = double.MinValue;

            double x, y, rx, ry;
            for (int i = 0; i < PointsCount; i++)
            {
                x = (double)XPoints[i] - CenterX;
                y = CenterY - (double)YPoints[i];

                rx = cos * x - sin * y;
                ry = sin * x + cos * y;

                if (rx < xMin)
                    xMin = rx;

                if (rx > xMax)
                    xMax = rx;

                if (ry < yMin)
                    yMin = ry;

                if (ry > yMax)
                    yMax = ry;
            }

            SzX = xMax - xMin;
            SzY = yMax - yMin;

            double[] xBounds = new double[4];
            double[] yBounds = new double[4];

            xBounds[0] = xMin;
            yBounds[0] = yMin;
            xBounds[1] = xMax;
            yBounds[1] = yMin;
            xBounds[2] = xMax;
            yBounds[2] = yMax;
            xBounds[3] = xMin;
            yBounds[3] = yMax;

            orientation = -angle * Math.PI / 180;
            sin = Math.Sin(orientation);
            cos = Math.Cos(orientation);

            for (int i = 0; i < 4; i++)
            {
                x = xBounds[i];
                y = yBounds[i];

                xBounds[i] = (CenterX + (cos * x - sin * y));
                yBounds[i] = (CenterY - (sin * x + cos * y));
            }

            Polygon polygon = new Polygon(xBounds, yBounds, 4, false);

            return polygon;
        }

        public Cluster Rotate(double angle)
        {
            double orientation = Math.PI * angle / 180.0;
            double sin = Math.Sin(orientation);
            double cos = Math.Cos(orientation);

            Cluster c = new Cluster();
            c.PointsCount = PointsCount;
            c.XPoints = new List<double>(PointsCount);
            c.YPoints = new List<double>(PointsCount);
            c.SzX = SzX;
            c.SzY = SzY;

            double x, y, rx, ry;
            for (int i = 0; i < PointsCount; i++)
            {
                x = (double)XPoints[i] - CenterX;
                y = CenterY - (double)YPoints[i];

                rx = cos * x - sin * y;
                ry = sin * x + cos * y;

                c.XPoints.Add(rx + CenterX);
                c.YPoints.Add(CenterY - ry);
            }

            return c;
        }

        public void AlignTo(
            ref GreyImage greyImage, double referX, double referY,
            double angle)
        {
            greyImage.Reset();

            int w = greyImage.Width;
            int h = greyImage.Height;
            byte[] data = (byte[])greyImage.Data;

            double orientation = Math.PI * angle / 180.0;
            double sin = Math.Sin(orientation);
            double cos = Math.Cos(orientation);

            double x, y, rx, ry;
            int xp, yp;
            for (int i = 0; i < PointsCount; i++)
            {
                x = (double)XPoints[i] - CenterX;
                y = CenterY - (double)YPoints[i];

                rx = cos * x - sin * y;
                ry = sin * x + cos * y;

                //x = rx + CenterX;
                //y = CenterY - ry;

                //c.XPoints.Add(rx + CenterX);
                //c.YPoints.Add(CenterY - ry);
                xp = (int)Math.Round(rx + referX);
                if (xp < 0 || xp >= w)
                    continue;

                yp = (int)Math.Round(referY - ry);
                if (yp < 0 || yp >= h)
                    continue;

                data[yp * w + xp] = 1;
            }
        }

        public void SaveAsImage(string filePath)
        {
            double xMin = double.MaxValue;
            double xMax = double.MinValue;
            double yMin = double.MaxValue;
            double yMax = double.MinValue;

            for (int i = 0; i < PointsCount; i++)
            {
                if (xMin > XPoints[i]) xMin = XPoints[i];
                if (xMax < XPoints[i]) xMax = XPoints[i];
                if (yMin > YPoints[i]) yMin = YPoints[i];
                if (yMax < YPoints[i]) yMax = YPoints[i];
            }

            int w = (int)(xMax - xMin);
            int h = (int)(yMax - yMin);

            using (Image image = new Bitmap(w, h))
            {
                using (Graphics grph = Graphics.FromImage(image))
                {
                    grph.Clear(Color.Black);
                    using (Pen pen = new Pen(Color.White, 1.0f))
                        for (int i = 0; i < PointsCount; i++)
                        {
                            int x = (int)(XPoints[i] - xMin);
                            int y = (int)(YPoints[i] - yMin);
                            grph.DrawRectangle(pen, x, y, 1, 1);
                        }
                }

                image.Save(filePath);
            }
        }

        public void Split(out Cluster c1, out Cluster c2)
        {
            double orientation = this.GetAlignmentAngle();
            Cluster c = this.Rotate(orientation);
            c.CalcDataRange();

            double w = c.XMax - c.XMin;
            double h = c.YMax - c.YMin;

            c1 = new Cluster();
            c2 = new Cluster();

            List<double> listPoint = null;
            double splitVal = 0;

            if (w > h)
            {
                listPoint = c.XPoints;
                splitVal = (c.XMin + c.XMax) * 0.5;

                splitVal = splitVal + 4.5;
            }
            else
            {
                listPoint = c.YPoints;
                splitVal = (c.YMin + c.YMax) * 0.5;
            }            

            Cluster current = null;
            for (int i = 0; i < c.PointsCount; i++)
            {
                if (listPoint[i] <= splitVal)
                    current = c1;
                else
                    current = c2;

                //current.XPoints.Add(c.XPoints[i]);
                //current.YPoints.Add(c.YPoints[i]);
                current.XPoints.Add(XPoints[i]);
                current.YPoints.Add(YPoints[i]);
                current.PointsCount++;
            }

            c1.CalcDataRange();
            c2.CalcDataRange();
        }

        public void Offset(double dx, double dy)
        {
            for (int i = 0; i < PointsCount; i++)
            {
                XPoints[i] += dx;
                YPoints[i] += dy;
            }

            this.CalcDataRange();
        }

        #region IComparable Members

        public int CompareTo(object obj)
        {
            return (this.XMin.CompareTo((obj as Cluster).XMin));
        }
        #endregion

        public void Save(string fileName)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                Save(fs);
            }
        }

        public void Save(FileStream fs)
        {
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                Save(fs);
            }
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(ClassID);
            writer.Write(ClassName);
            writer.Write(PointsCount);
            for (int i = 0; i < PointsCount; i++)
            {
                writer.Write(XPoints[i]);
                writer.Write(YPoints[i]);
            }
        }

        public static Cluster FromFile(string fileName)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                return FromStream(fs);
            }
        }

        public static Cluster FromStream(FileStream fs)
        {
            using (BinaryReader reader = new BinaryReader(fs))
            {
                return FromStream(reader);
            }
        }

        public static Cluster FromStream(BinaryReader reader)
        {
            int classId = reader.ReadInt32();
            string className = reader.ReadString();
            int pointCount = reader.ReadInt32();

            double[] xPoints = new double[pointCount];
            double[] yPoints = new double[pointCount];

            for (int i = 0; i < pointCount; i++)
            {
                xPoints[i] = reader.ReadDouble();
                yPoints[i] = reader.ReadDouble();
            }

            Cluster c = new Cluster();
            c.ClassID = classId;
            c.ClassName = className;
            c.PointsCount = pointCount;
            c.XPoints.AddRange(xPoints);
            c.YPoints.AddRange(yPoints);

            c.CalcDataRange();

            return c;
        }

        public static Cluster FromGoldenImageFile(string fileName)
        {
            IImage image = new GreyImage();
            (image as GreyImage).Load(fileName);

            //RankFilter minFilter = new RankFilter(3, 3, 0);
            //minFilter.Apply(ref image);

            GreyImage greyImage = image as GreyImage;

            int w = greyImage.Width;
            int h = greyImage.Height;

            byte[] data = (byte[])greyImage.Data;

            Cluster c = new Cluster();

            char ch = Path.GetFileNameWithoutExtension(fileName)[0];
            c.ClassName = ch.ToString();
            if (ch >= 'A' && ch <= 'Z')
            {
                c.ClassID = ch - 'A';
            }
            else if (ch >= '0' && ch <= '9')
            {
                c.ClassID = 'Z' - 'A' + ch - '0';
            }

            double threshold = 255 - 30;
            int index = 0;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++, index++)
                {
                    if (data[index] >= threshold)
                        continue;

                    c.XPoints.Add(x);
                    c.YPoints.Add(y);
                }
            }
            c.PointsCount = c.XPoints.Count;
            c.CalcDataRange();

            return c;
        }

        public double Match(
            GreyImage greyImage, double centerX, double centerY,
            ref double bestMatchedAngle)
        {
            byte[] imgData = (byte[])greyImage.Data;

            double minDiff = double.MaxValue;
            bestMatchedAngle = 0;

            double da = 5;

            GreyImage matchingImage = new GreyImage(greyImage.Width, greyImage.Height);

            for (double angle = minAngle; angle <= maxAngle; angle += da)
            {
                this.AlignTo(ref matchingImage, centerX, centerY, angle);
                double diff = greyImage.Diff(matchingImage);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    bestMatchedAngle = angle;
                }
            }

            return minDiff;
        }
    }
}
