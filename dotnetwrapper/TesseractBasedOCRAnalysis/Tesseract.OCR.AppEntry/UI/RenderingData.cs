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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace Tesseract.OCR.AppEntry.UI
{
    internal abstract class RenderingData : IRenderingData
    {
        protected Image _bitmap = null;
        protected bool _isDataChanged = false;
        protected double _scaleFactor = 1.0;

        protected float[][] _ptsArray ={ 
                            new float[] {1, 0, 0, 0, 0},
                            new float[] {0, 1, 0, 0, 0},
                            new float[] {0, 0, 1, 0, 0},
                            new float[] {0, 0, 0, 0.5f, 0}, 
                            new float[] {0, 0, 0, 0, 1}};
        protected ImageAttributes _imgAttributes = null;

        protected virtual void Initialize()
        {
            ColorMatrix clrMatrix = new ColorMatrix(_ptsArray);
            _imgAttributes = new ImageAttributes();
            _imgAttributes.SetColorMatrix(
                clrMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
        }

        #region IRenderingData Members

        public Image Image
        {
            get
            {
                return _bitmap;
            }
            set
            {
                if (_bitmap != value)
                {
                    if (_bitmap != null)
                    {
                        _bitmap.Dispose();
                    }

                    _bitmap = value;
                    _isDataChanged = true;
                }                
            }
        }

        #endregion

        #region IRenderingData Members


        public bool IsDataChanged
        {
            get { return _isDataChanged; }
            set { _isDataChanged = value; }
        }

        #endregion

        #region IRenderingData Members


        public virtual void ClearData()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
