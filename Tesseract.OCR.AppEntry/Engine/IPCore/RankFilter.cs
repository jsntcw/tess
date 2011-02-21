using System;
using System.Text;
using CaptchaOCR.AppEntry.Engine.IPCommon;


namespace CaptchaOCR.AppEntry.Engine.IPCore
{
    internal class RankFilter : IFilter
    {
        #region Member fields
        protected int _kWidth = 3;
        protected int _kHeight = 3;
        protected int _kOrder = 4;
        protected int _kLength = 9;
        protected byte[] _kVals = null;

        protected int[] _offsets = null;
        #endregion Member fields

        #region Constructors and destructors
        public RankFilter(int kWidth, int kHeight, int kOrder)
        {
            if (kWidth % 2 == 0 || kHeight % 2 == 0)
                throw new ArgumentException("Kernel size should be odd.");

            _kWidth = kWidth;
            _kHeight = kHeight;
            _kLength = _kWidth * _kHeight;
            _kVals = new byte[_kLength];

            _kOrder = kOrder;
            if (_kOrder < 0)
                _kOrder = 0;
            if (_kOrder >= _kLength)
                _kOrder = _kLength - 1;
        }

        protected virtual void Initialize(int imgWidth)
        {
            if (_offsets == null)
                _offsets = new int[_kWidth * _kHeight];

            int kHalfWidth = _kWidth / 2;
            int kHalfHeight = _kHeight / 2;
            int index = 0, offset;

            for (int y = 0; y < _kHeight; y++)
            {
                offset = (y - kHalfHeight) * imgWidth;
                for (int x = 0; x < _kWidth; x++, index++)
                {
                    _offsets[index] = offset + (x - kHalfWidth);
                }
            }
        }
        #endregion Constructors and destructors

        #region IFilter Members

        public virtual int kWidth
        {
            get { return _kWidth; }
        }

        public virtual int kHeight
        {
            get { return _kHeight; }
        }

        public bool Apply(ref IPCommon.IImage input)
        {
            if (input == null)
                return false;

            int imgWidth = input.Width;
            int imgHeight = input.Height;

            this.Initialize(input.Width);

            if (input is GreyImage)
            {
                byte[] dst = null;

                Apply((byte[])input.Data, imgWidth, imgHeight, ref dst);

                input.Data = dst;

                return true;
            }

            if (input is RGBImage)
            {
                byte[][] src = (byte[][])input.Data;

                byte[] dst_r = null;
                Apply(src[0], imgWidth, imgHeight, ref dst_r);
                byte[] dst_g = null;
                Apply(src[1], imgWidth, imgHeight, ref dst_g);
                byte[] dst_b = null;
                Apply(src[2], imgWidth, imgHeight, ref dst_b);

                src[0] = dst_r;
                src[1] = dst_g;
                src[2] = dst_b;

                return true;
            }

            return false;
        }

        public bool Apply(IPCommon.IImage input, ref IPCommon.IImage output)
        {
            if (input == null)
                return false;

            int imgWidth = input.Width;
            int imgHeight = input.Height;

            this.Initialize(input.Width);

            if (input is GreyImage)
            {
                byte[] dst = null;

                Apply((byte[])input.Data, imgWidth, imgHeight, ref dst);

                output = new GreyImage(dst, imgWidth, imgHeight, false);

                return true;
            }

            if (input is RGBImage)
            {
                byte[][] src = (byte[][])input.Data;

                byte[] dst_r = null;
                Apply(src[0], imgWidth, imgHeight, ref dst_r);
                byte[] dst_g = null;
                Apply(src[1], imgWidth, imgHeight, ref dst_g);
                byte[] dst_b = null;
                Apply(src[2], imgWidth, imgHeight, ref dst_b);

                byte[][] dst = new byte[3][];
                dst[0] = dst_r;
                dst[1] = dst_g;
                dst[2] = dst_b;

                output = new RGBImage(dst, imgWidth, imgHeight, false);

                return true;
            }

            return false;
        }

        private bool Apply(byte[] src, int imgWidth, int imgHeight, ref byte[] dst)
        {
            if (dst == null || dst.Length != src.Length)
                dst = new byte[src.Length];

            Array.Copy(src, dst, src.Length);

            int kHalfWidth = _kWidth / 2;
            int kHalfHeight = _kHeight / 2;

            int xEnd = imgWidth - kHalfWidth;
            int yEnd = imgHeight - kHalfHeight;

            int i, x, y, index;
            byte val;

            if (_kOrder == 0) /*min filter*/
            {
                for (y = _kHeight; y < yEnd; y++)
                {
                    index = y * imgWidth + _kWidth;
                    for (x = _kWidth; x < xEnd; x++, index++)
                    {
                        val = src[index + _offsets[0]];
                        for (i = 1; i < _kLength; i++)
                        {
                            if (val > src[index + _offsets[i]])
                                val = src[index + _offsets[i]];
                        }
                        dst[index] = val;
                    }
                }

                return true;
            }

            if (_kOrder == _kLength - 1) /*max filter*/
            {
                for (y = _kHeight; y < yEnd; y++)
                {
                    index = y * imgWidth + _kWidth;
                    for (x = _kWidth; x < xEnd; x++, index++)
                    {
                        val = src[index + _offsets[0]];
                        for (i = 1; i < _kLength; i++)
                        {
                            if (val < src[index + _offsets[i]])
                                val = src[index + _offsets[i]];
                        }
                        dst[index] = val;
                    }
                }

                return true;
            }

            for (y = _kHeight; y < yEnd; y++) /*rank filter, and median filter is the special case*/
            {
                index = y * imgWidth + _kWidth;
                for (x = _kWidth; x < xEnd; x++, index++)
                {
                    for (i = 0; i < _kLength; i++)
                    {
                        _kVals[i] = src[index + _offsets[i]];
                    }

                    dst[index] = FindkOrderValue();
                    //dst[index] = FindAverage();
                }
            }

            return true;
        }
        #endregion

        #region Helpers
        protected byte FindMinValue()
        {
            byte minVal = _kVals[0];

            for (int i = 1; i < _kLength; i++)
            {
                if (minVal > _kVals[i])
                    minVal = _kVals[i];
            }

            return minVal;
        }

        protected byte FindMaxValue()
        {
            byte maxVal = _kVals[0];

            for (int i = 1; i < _kLength; i++)
            {
                if (maxVal < _kVals[i])
                    maxVal = _kVals[i];
            }

            return maxVal;
        }

        protected byte FindAverage()
        {
            double v = 0;

            for (int i = 1; i < _kLength; i++)
            {
                v += _kVals[i];
            }

            return (byte)Math.Round(v / _kLength);
        }

        protected byte FindkOrderValue()
        {
            byte temp;
            int i, j;
            int indexLelf = 0;
            int indexRight = _kLength - 1;
            byte kOrderValue = _kVals[_kOrder];

            while (indexLelf < indexRight)
            {
                i = indexLelf;
                j = indexRight;
                do
                {
                    while (_kVals[i] < kOrderValue)
                    {
                        i++;
                    }
                    while (_kVals[j] > kOrderValue)
                    {
                        j--;
                    }

                    // swap
                    temp = _kVals[i];
                    _kVals[i] = _kVals[j];
                    _kVals[j] = temp;

                    // update index
                    i++;
                    j--;
                } while (j >= _kOrder && i <= _kOrder);

                // update index
                if (j < _kOrder)
                    indexLelf = i;
                if (_kOrder < i)
                    indexRight = j;

                kOrderValue = _kVals[_kOrder];
            }

            return kOrderValue;
        }
        #endregion Helpers
    }
}
