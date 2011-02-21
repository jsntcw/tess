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
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace IPoVn.Engine.IPCommon
{
    unsafe internal class RGBImage : ImageBase
    {
        #region Member fields
        #endregion Member fields

        #region Constructors and destructors
        public RGBImage()
        {
        }

        public RGBImage(int width, int height)
        {
            InitializeAndAllocateMemory(width, height);
        }

        public RGBImage(byte[][] data, int width, int height, bool cloneData)
        {
            if (data == null)
                throw new System.ArgumentException("Invalid inputs!");

            if (cloneData)
            {
                InitializeAndAllocateMemory(width, height);
                Array.Copy(data[0], ((byte[][])_data)[0], _length);
                Array.Copy(data[1], ((byte[][])_data)[1], _length);
                Array.Copy(data[2], ((byte[][])_data)[2], _length);
            }
            else
            {
                InitializeWithoutAllocateMemory(width, height);
                _data = data;
            }
        }

        public RGBImage(GreyImage refImage, bool cloneData)
        {
            if (refImage == null)
                throw new System.ArgumentException("Input is invalid!");

            if (cloneData)
            {
                InitializeAndAllocateMemory(refImage.Width, refImage.Height);
                Array.Copy(((byte[][])refImage.Data)[0], ((byte[][])_data)[0], _length);
                Array.Copy(((byte[][])refImage.Data)[1], ((byte[][])_data)[1], _length);
                Array.Copy(((byte[][])refImage.Data)[2], ((byte[][])_data)[2], _length);
            }
            else
            {
                InitializeWithoutAllocateMemory(refImage.Width, refImage.Height);
                _data = refImage.Data;
            }
        }

        public RGBImage(RGBImage refImage, int l, int t, int w, int h)
        {
            if (refImage == null)
                throw new System.ArgumentException("Input is invalid!");

            int width = refImage.Width;
            int height = refImage.Height;

            if (l < 0) l = 0;
            if (l >= width) l = width - 1;
            if (t < 0) t = 0;
            if (t >= height) t = height - 1;

            if (l + w > width) w = width - l;
            if (t + h > height) h = height - t;
            if (w < 1) w = 1;
            if (h < 1) h = 1;

            // update image's info
            _width = w;
            _height = h;
            _length = _width * _height;

            // update image's data
            byte[][] src = (byte[][])refImage.Data;
            byte[][] data = new byte[3][];
            data[0] = Utilities.Crop(src[0], 1, width, height, l, t, w, h);
            data[1] = Utilities.Crop(src[1], 1, width, height, l, t, w, h);
            data[2] = Utilities.Crop(src[2], 1, width, height, l, t, w, h);

            _data = data;
        }

        public RGBImage(
            RGBImage refImage,
            int marginX, int marginY,
            bool trueIsExpanding_falseIsCropping,
            bool interpolationForExpandingOnly)
        {
            if (refImage == null)
                throw new System.ArgumentException("Input is invalid!");

            int new_width = 0;
            int new_height = 0;
            byte[][] new_data = new byte[3][];

            byte[][] src = (byte[][])refImage.Data;

            if (trueIsExpanding_falseIsCropping) /* expand */
            {
                if (interpolationForExpandingOnly)
                {
                    new_data[0] = Utilities.ExpandAndInterpolate(
                        src[0], refImage.Width, refImage.Height,
                        marginX, marginY, ref new_width, ref new_height);
                    new_data[1] = Utilities.ExpandAndInterpolate(
                        src[1], refImage.Width, refImage.Height,
                        marginX, marginY, ref new_width, ref new_height);
                    new_data[2] = Utilities.ExpandAndInterpolate(
                        src[2], refImage.Width, refImage.Height,
                        marginX, marginY, ref new_width, ref new_height);
                }
                else
                {
                    new_data[0] =
                        Utilities.Expand(src[0], refImage.Width, refImage.Height,
                        marginX, marginY, ref new_width, ref new_height);
                    new_data[1] =
                        Utilities.Expand(src[1], refImage.Width, refImage.Height,
                        marginX, marginY, ref new_width, ref new_height);
                    new_data[2] =
                        Utilities.Expand(src[2], refImage.Width, refImage.Height,
                        marginX, marginY, ref new_width, ref new_height);
                }
            }
            else /* crop */
            {
                new_data[0] =
                    Utilities.Crop(src[0], 1, refImage.Width, refImage.Height,
                    marginX, marginY, ref new_width, ref new_height);
                new_data[1] =
                    Utilities.Crop(src[1], 1, refImage.Width, refImage.Height,
                    marginX, marginY, ref new_width, ref new_height);
                new_data[2] =
                    Utilities.Crop(src[2], 1, refImage.Width, refImage.Height,
                    marginX, marginY, ref new_width, ref new_height);
            }

            // update image's info
            _width = new_width;
            _height = new_height;
            _length = _width * _height;

            // update image's data
            _data = new_data;
        }

        protected override void InitializeAndAllocateMemory(int width, int height)
        {
            _width = width;
            _height = height;
            _length = _width * _height;

            byte[][] data = new byte[3][];
            data[0] = new byte[_length]; // R-chanel
            data[1] = new byte[_length]; // G-chanel
            data[2] = new byte[_length]; // B-chanel
            _data = data;
        }

        protected override void InitializeWithoutAllocateMemory(int width, int height)
        {
            _width = width;
            _height = height;
            _length = _width * _height;

            _data = null;
        }
        #endregion Constructors and destructors

        #region Overrides
        public override object Clone()
        {
            throw new Exception("The method or operation is not implemented.");
        }
        #endregion Overrides

        #region Properties
        public override int LengthByBytes
        {
            get { return 3 * _length; }
        }
        #endregion Properties

        #region Methods
        public override bool DoCommand(
            string sCommand, object[] inputs, ref object[] outputs)
        {
            switch (sCommand)
            {
                case SupportedImageActions.Save:
                    if (inputs == null || inputs.Length != 2 ||
                        !(inputs[0] is string) || !(inputs[1] is eImageFormat))
                        throw new System.ArgumentException("At least one input is invalid!");

                    return Save((string)inputs[0], (eImageFormat)inputs[1]);

                case SupportedImageActions.Load:

                    if (inputs == null || inputs.Length != 1 || !(inputs[0] is string))
                        throw new System.ArgumentException("At least one input is invalid!");

                    return Load((string)inputs[0]);

                case SupportedImageActions.ToImage:

                    if (outputs == null || outputs.Length != 1 ||
                        (outputs[0] != null && !(outputs[0] is Image)))
                        throw new System.ArgumentException("At least one input is invalid!");

                    return ToImage(ref outputs[0]);

                case SupportedImageActions.QuickSave:
                    return QuickSave(inputs);

                case SupportedImageActions.QuickLoad:
                    return QuickLoad(inputs);

                case SupportedImageActions.InvertColor:
                    IImage invertedImage = InvertColor();
                    outputs = new object[] { invertedImage };
                    return true;

                default:
                    throw new System.Exception
                        (string.Format("Don't support command: {0}.", sCommand));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="imgFormat"></param>
        /// <returns></returns>
        public bool Save(
            string fileName, eImageFormat imgFormat)
        {
            object image = null;

            if (ToImage(ref image) && image != null)
            {
                (image as Image).Save(fileName, Common.GetImageFormat(imgFormat));

                return true;
            }

            return false;
        }

        /// <summary>
        /// Load image from file, after that convert it to grey scale
        /// </summary>
        /// <param name="fileName"> Image's file name </param>
        /// <returns> 'true' if succeed, else is 'false' </returns>
        public bool Load(string fileName)
        {
            Image image = null;
            try
            {
                // load image from file using .net lib
                using (image = Image.FromFile(fileName))
                {
                    if (image == null)
                        throw new System.ArgumentException(
                            string.Format("Cannot load image from file: {0}", fileName));

                    return Load(image);
                }
            }
            catch
            {
                if (image != null)
                {
                    image.Dispose();
                    image = null;
                }

                throw;
            }

            return true;
        }

        public bool Load(Image image)
        {
            PixelFormat pxFormat = image.PixelFormat;

            GraphicsUnit unit = GraphicsUnit.Pixel;
            RectangleF boundsF = image.GetBounds(ref unit);

            Rectangle bounds = new Rectangle(
                (int)boundsF.X, (int)boundsF.Y, (int)boundsF.Width, (int)boundsF.Height);

            BitmapData bitmapData =
                (image as Bitmap).LockBits(bounds, ImageLockMode.ReadWrite, pxFormat);

            try
            {                
                int width = image.Width;
                int height = image.Height;
                int length = width * height;

                InitializeAndAllocateMemory(width, height);
                byte[][] dst = (byte[][])_data;
                byte[] dst_r = dst[0];
                byte[] dst_g = dst[1];
                byte[] dst_b = dst[2];

                int realWidth = (int)Math.Abs(bitmapData.Stride);
                int bytenum = realWidth / width;
                int reserved = realWidth - bytenum * width;

                byte* src = (byte*)bitmapData.Scan0.ToPointer();
                {
                    int index = 0;
                    switch (bytenum)
                    {
                        case 1:
                            for (int y = 0; y < height; y++)
                            {
                                for (int x = 0; x < width; x++, index++, src++)
                                {
                                    dst_r[index] = (byte)(*(src));
                                    dst_g[index] = (byte)(*(src));
                                    dst_b[index] = (byte)(*(src));
                                }
                                src += reserved;
                            }
                            break;
                        case 3:
                            for (int y = 0; y < height; y++)
                            {
                                for (int x = 0; x < width; x++, index++, src += 3)
                                {
                                    dst_r[index] = (byte)(*(src));
                                    dst_g[index] = (byte)(*(src + 1));
                                    dst_b[index] = (byte)(*(src + 2));
                                }
                                src += reserved;
                            }
                            break;
                        case 4:
                            for (int y = 0; y < height; y++)
                            {
                                for (int x = 0; x < width; x++, index++, src += 4)
                                {
                                    dst_r[index] = (byte)(*(src));
                                    dst_g[index] = (byte)(*(src + 1));
                                    dst_b[index] = (byte)(*(src + 2));
                                }
                                src += reserved;
                            }
                            break;
                    }
                }
            }
            catch
            {
            }
            finally
            {
                if (image != null && bitmapData != null)
                {
                    (image as Bitmap).UnlockBits(bitmapData);
                }
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public bool ToImage(ref object image)
        {
            Image tmpImg = null;
            try
            {
                tmpImg = new Bitmap(_width, _height, PixelFormat.Format24bppRgb);

                GraphicsUnit unit = GraphicsUnit.Pixel;

                RectangleF boundsF = tmpImg.GetBounds(ref unit);
                Rectangle bounds = new Rectangle(
                    (int)boundsF.X, (int)boundsF.Y, (int)boundsF.Width, (int)boundsF.Height);

                BitmapData bitmapData = (tmpImg as Bitmap).LockBits(
                    bounds, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

                int reserved = (int)Math.Abs(bitmapData.Stride) - _width * 3;

                byte[][] src = (byte[][])_data;
                byte[] src_r = src[0];
                byte[] src_g = src[1];
                byte[] src_b = src[2];

                byte* dst = (byte*)bitmapData.Scan0.ToPointer();

                int x, y, index = 0;

                //fixed (byte* src = (byte[])_data)
                {
                    for (y = 0; y < _height; y++)
                    {
                        for (x = 0; x < _width; x++, index++)
                        {
                            *(dst++) = src_r[index];
                            *(dst++) = src_g[index];
                            *(dst++) = src_b[index];
                        }
                        dst += reserved;
                    }
                }

                (tmpImg as Bitmap).UnlockBits(bitmapData);

                image = tmpImg;
            }
            catch
            {
                if (tmpImg != null)
                {
                    tmpImg.Dispose();
                    tmpImg = null;
                }

                image = null;

                throw;
            }

            return true;
        }

        public bool ConvertToGreyImage(ref byte[] greyImgData)
        {
            greyImgData = new byte[_width * _height];
            byte[][] src = (byte[][])this.Data;
            byte[] src_r = src[0];
            byte[] src_g = src[1];
            byte[] src_b = src[2];

            //fixed (byte* pR = src_r, pG = src_g, pB = src_b)            
            {
                for (int index = 0; index < _length; index++)
                {
                    greyImgData[index] = (byte)(
                        (src_r[index] * 29 + src_g[index] * 150 + src_b[index] * 77 + 128) / 256);
                }
            }

            return true;
        }

        public IImage ConvertToGreyImage()
        {
            byte[] greyImgData = new byte[_width * _height];
            byte[][] src = (byte[][])this.Data;
            byte[] src_r = src[0];
            byte[] src_g = src[1];
            byte[] src_b = src[2];

            //fixed (byte* pR = src_r, pG = src_g, pB = src_b)            
            {
                for (int index = 0; index < _length; index++)
                {
                    greyImgData[index] = (byte)(
                        (src_r[index] * 29 + src_g[index] * 150 + src_b[index] * 77 + 128) / 256);
                }
            }

            return new GreyImage(greyImgData, _width, _height, false);
        }

        #region Quick Save
        private bool QuickSave(object[] inputs)
        {
            if (inputs == null || inputs.Length < 1 || inputs[0] == null)
                throw new System.ArgumentException("Input is invalid.");

            if (inputs[0] is string)
            {
                return QuickSave((string)inputs[0]);
            }

            if (inputs[0] is FileStream)
            {
                return QuickSave(inputs[0] as FileStream);
            }

            if (inputs[0] is BinaryReader)
            {
                return QuickSave(inputs[0] as BinaryWriter);
            }


            throw new System.ArgumentException(
                                string.Format(
                                "Don't support input's type is {0}.",
                                inputs[0].GetType().ToString()));
        }

        private bool QuickSave(string fileName)
        {
            using (FileStream fs =
                new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                return QuickSave(fs);
            }
        }

        private bool QuickSave(FileStream fs)
        {
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                return QuickSave(writer);
            }
        }

        private bool QuickSave(BinaryWriter writer)
        {
            try
            {
                writer.Write((int)eImageType.Color24bits);

                writer.Write(_width);
                writer.Write(_height);

                writer.Write(LengthByBytes);

                byte[][] src = (byte[][])_data;
                writer.Write(src[0]);
                writer.Write(src[1]);
                writer.Write(src[2]);

                return true;
            }
            catch
            {
                throw;
            }
        }
        #endregion Quick Save

        #region Quick Load
        private bool QuickLoad(object[] inputs)
        {
            if (inputs == null || inputs.Length < 1 || inputs[0] == null)
                throw new System.ArgumentException("Input is invalid.");

            if (inputs[0] is string)
            {
                return QuickLoad((string)inputs[0]);
            }

            if (inputs[0] is FileStream)
            {
                return QuickLoad(inputs[0] as FileStream);
            }

            if (inputs[0] is BinaryReader)
            {
                return QuickLoad(inputs[0] as BinaryReader);
            }

            throw new System.ArgumentException(
                    string.Format(
                    "Don't support input's type is {0}.",
                    inputs[0].GetType().ToString()));
        }

        private bool QuickLoad(string fileName)
        {
            using (FileStream fs =
                new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite))
            {
                return QuickLoad(fs);
            }
        }

        private bool QuickLoad(FileStream fs)
        {
            using (BinaryReader reader = new BinaryReader(fs))
            {
                eImageType imageType = (eImageType)reader.ReadInt32();

                if (imageType != eImageType.Color24bits)
                    throw new System.Exception("Invalid format!");

                return QuickLoad(reader);
            }
        }

        private bool QuickLoad(BinaryReader reader)
        {
            try
            {
                _width = reader.ReadInt32();
                _height = reader.ReadInt32();

                int lengthByBytes = reader.ReadInt32();

                int length = _width * _height;

                byte[][] data = new byte[3][];

                data[0] = reader.ReadBytes(lengthByBytes);
                data[1] = reader.ReadBytes(lengthByBytes);
                data[2] = reader.ReadBytes(lengthByBytes);

                _data = data;

                return true;
            }
            catch
            {
                throw;
            }
        }
        #endregion Quick Load

        public IImage InvertColor()
        {
            //double inv_255 = 1.0 / 255;

            byte[][] srcData = (byte[][])_data;
            byte[] src_r = srcData[0];
            byte[] src_g = srcData[1];
            byte[] src_b = srcData[2];

            byte[][] dstData = new byte[3][];
            byte[] dst_r = new byte[_length];
            byte[] dst_g = new byte[_length];
            byte[] dst_b = new byte[_length];

            for (int index = 0; index < _length; index++)
            {
                dst_r[index] = (byte)(~src_r[index]);
                dst_g[index] = (byte)(~src_g[index]);
                dst_b[index] = (byte)(~src_b[index]);

                //double r = 0, g = 0, b = 0, h = 0, s = 0, i = 0;
                //r = src_r[index] * inv_255;
                //g = src_g[index] * inv_255;
                //b = src_b[index] * inv_255;

                //ColorModel.RGB2HSI(r, g, b, ref h, ref s, ref i);

                //ColorModel.HSI2RGB(h, s, i, ref r, ref g, ref b);

                //dst_r[index] = (byte)(r * 255 + 0.5);
                //dst_g[index] = (byte)(g * 255 + 0.5);
                //dst_b[index] = (byte)(b * 255 + 0.5);
            }

            dstData[0] = dst_r;
            dstData[1] = dst_g;
            dstData[2] = dst_b;

            return new RGBImage(dstData, _width, _height, false);
        }
        #endregion Methods
    }
}
