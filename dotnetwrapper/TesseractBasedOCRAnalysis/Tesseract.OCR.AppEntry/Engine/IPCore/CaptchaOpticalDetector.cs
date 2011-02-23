using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using CaptchaOCR.AppEntry.Engine.IPMath;
using System.IO;
using CaptchaOCR.AppEntry.Engine.IPCommon;

namespace CaptchaOCR.AppEntry.Engine.IPCore
{   
    internal class CaptchaOpticalDetector
    {   
        private byte[] _data = null;
        private int _width = 0;
        private int _height = 0;
        private List<Cluster> _cluster = null;

        public CaptchaOpticalDetector(byte[] data, int width, int height)
        {
            _data = data;
            _width = width;
            _height = height;
        }

        public Point[] GetObjects(
            byte[] realData, ref Cluster[] clusters, ref Polygon[] polys)
        {
            Point[] centerOfClusters = null;

            try
            {
                double threshold = 10;

                int length = _width * _height;
                bool[] boolData = new bool[length];
                for (int i = 0; i < length; i++)
                {
                    if (_data[i] >= threshold)
                        boolData[i] = true;
                }

                int nCluster = 0;
                int[] ptX = null;
                int[] ptY = null;
                int[] labels = null;

                float maxConnectedDistance = (float)(Math.Sqrt(2) + 10 * double.Epsilon);

                ConnectedComponentProcessor.GetConnectedComponent(
                    boolData, _width, maxConnectedDistance, ref nCluster, ref ptX, ref ptY, ref labels);

                int n = labels.Length;

                _cluster = new List<Cluster>(nCluster);
                for (int i = 0; i < nCluster; i++)
                {
                    _cluster.Add(new Cluster());
                }

                int iCluster = 0, x, y;
                for (int i = 0; i < n; i++)
                {
                    if (labels[i] <= 0 || labels[i] > nCluster)
                        continue;

                    iCluster = labels[i] - 1;

                    x = ptX[i];
                    y = ptY[i];

                    if (x < 0 || x >= _width || y < 0 || y >= _height)
                        continue;

                    if (realData[y * _width + x] < threshold)
                        continue;

                    // update elements
                    _cluster[iCluster].PointsCount++;
                    _cluster[iCluster].XPoints.Add(x);
                    _cluster[iCluster].YPoints.Add(y);

                    // update center
                    _cluster[iCluster].CenterX += x;
                    _cluster[iCluster].CenterY += y;
                }                

                int minPointCount = 50;
                for (int i = nCluster - 1; i >= 0; i--)
                {
                    if (_cluster[i].PointsCount < minPointCount)
                    {
                        _cluster.RemoveAt(i);
                        continue;
                    }

                    _cluster[i].CalcDataRange();
                }

                nCluster = _cluster.Count;

                // process big cluster here
                for (int i = nCluster - 1; i >= 0; i--)
                {
                    Polygon rect = _cluster[i].PolyBounds;

                    double w = Math.Abs(rect.XPoints[0] - rect.XPoints[1]);
                    double h = Math.Abs(rect.YPoints[0] - rect.YPoints[rect.YPoints.Length-1]);

                    double sz = Math.Max(w, h);
                    if (sz > 32)
                    {
                        Cluster big = _cluster[i];
                        _cluster.RemoveAt(i);

                        Cluster c1 = null;
                        Cluster c2 = null;
                        big.Split(out c1, out c2);

                        if (c1 != null && c1.PointsCount >= minPointCount)
                        {
                            _cluster.Add(c1);
                        }

                        if (c2 != null && c2.PointsCount >= minPointCount)
                        {
                            _cluster.Add(c2);
                        }
                    }
                }

                _cluster.Sort();

                clusters = _cluster.ToArray();
                polys = new Polygon[_cluster.Count];

                centerOfClusters = new Point[_cluster.Count];
                for (int i = 0; i < _cluster.Count; i++)
                {
                    centerOfClusters[i] = clusters[i].Center;
                    polys[i] = clusters[i].PolyBounds;
                }
            }
            catch (System.Exception exp)
            {
                centerOfClusters = null;

                throw exp;
            }
            finally
            {
            }

            return centerOfClusters;
        }        
    }
}
