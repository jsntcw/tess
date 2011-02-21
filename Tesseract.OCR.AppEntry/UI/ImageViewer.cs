/**
Copyright 2011, Cong Nguyen

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

   http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
**/

using System;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using IPoVn.Engine.IPMath;

namespace IPoVn.UI
{
    internal class ImageViewer : Control
    {
        #region Member fields
        private Image _image = null;

        private Point[] _pts = null;

        private Polygon[] _regions = null;

        private float[][] _ptsArray ={ 
                            new float[] {1, 0, 0, 0, 0},
                            new float[] {0, 1, 0, 0, 0},
                            new float[] {0, 0, 1, 0, 0},
                            new float[] {0, 0, 0, 0.5f, 0}, 
                            new float[] {0, 0, 0, 0, 1}};
        private ImageAttributes _imgAttributes = null;
        #endregion Member fields

        #region Constructors and destructors
        public ImageViewer()
        {
            this.SetStyle(
                ControlStyles.DoubleBuffer |
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint,
                true);

            this.UpdateStyles();

            ColorMatrix clrMatrix = new ColorMatrix(_ptsArray);
            _imgAttributes = new ImageAttributes();
            _imgAttributes.SetColorMatrix(clrMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
        }
        #endregion Constructors and destructors

        #region Properties
        public Image Image
        {
            get { return _image; }
            set
            {
                if (_image != value)
                {
                    if (_image != null)
                    {
                        _image.Dispose();
                        _image = null;
                    }

                    _image = value;
                    OnImageChanged();
                }
            }
        }

        private void OnImageChanged()
        {
            this.Visible &= (_image != null);

            // correct size
            if (_image != null)
                this.Size = new Size(_image.Width, _image.Height);
            else
                this.Size = new Size(10, 10);

            // require to redraw
            this.Invalidate(true);
        }

        public Point[] Points
        {
            get { return _pts; }
            set
            {
                _pts = value;
                this.Invalidate(true);
            }
        }

        public Polygon[] Regions
        {
            get { return _regions; }
            set
            {
                _regions = value;
                this.Invalidate(true);
            }
        }
        #endregion Properties

        #region Overrides
        protected override void OnPaint(PaintEventArgs e)
        {
            if (_image == null)
                return;

            if (_pts != null && _pts.Length > 0)
            {
                Rectangle dstRect = new Rectangle(0, 0, _image.Width, _image.Height);
                e.Graphics.DrawImage(
                    _image, dstRect, 0, 0, _image.Width, _image.Height,
                    GraphicsUnit.Pixel, _imgAttributes);
            }
            else
            {
                e.Graphics.DrawImage(_image, 0, 0);
            }

            using (Pen pen = new Pen(Color.Blue, 1.0f))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            }

            if (_pts != null && _pts.Length > 0)
            {
                Graphics grph = e.Graphics;
                using (Pen pen = new Pen(Color.Red, 1.0f))
                {
                    foreach (Point pt in _pts)
                    {
                        //grph.DrawEllipse(pen, pt.X - 1, pt.Y - 1, 3, 3);
                        grph.DrawRectangle(pen, pt.X, pt.Y, 1, 1);
                    }
                }
            }

            if (_regions != null && _regions.Length > 0)
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                Color bgColor = Color.FromArgb(100, Color.DarkGreen);
                Color borderColor = Color.FromArgb(130, Color.Blue);
                using (Pen pen = new Pen(borderColor, 1.0f))
                using (Brush brush = new SolidBrush(bgColor))
                {
                    foreach (Polygon region in _regions)
                    {
                        DrawPolygon(e.Graphics, brush, pen, region);
                    }
                }
            }
        }

        private void DrawPolygon(Graphics grph, Brush brush, Pen pen, Polygon region)
        {
            if (grph == null || region == null)
                return;

            PointF[] points = new PointF[region.nPoints];
            for (int i = 0; i < region.nPoints; i++)
            {
                points[i] = new PointF((float)region.XPoints[i], (float)region.YPoints[i]);
            }

            using (Pen newp = new Pen(Color.Green, 1.0f))
            {
                Pen[] pens = new Pen[] { pen, newp };
                for (int i = 0; i < region.nPoints-1; i++)
                {
                    Pen p = pens[(i + 1) % 2];
                    grph.DrawLine(p, points[i], points[i+1]);
                }

                grph.DrawLine(pens[region.nPoints % 2], points[region.nPoints-1], points[0]);
            }
            return;

            

            //grph.FillPolygon(brush, points);
            grph.DrawPolygon(pen, points);
        }
        #endregion Overrides

        #region Events
        #endregion Events

        #region Methods
        #endregion Methods

        #region Helepers
        #endregion Helepers
    }
}
