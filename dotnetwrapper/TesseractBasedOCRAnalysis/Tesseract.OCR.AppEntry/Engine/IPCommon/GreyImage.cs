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
    unsafe internal class GreyImage : ImageBase
    {
        #region Member fields
        #endregion Member fields

        #region Constructors and destructors
        public GreyImage()
        {
        }

        public GreyImage(int width, int height)
        {
            InitializeAndAllocateMemory(width, height);
        }

        public GreyImage(byte[] data, int width, int height, bool cloneData)
        {
            if (cloneData)
            {
                InitializeAndAllocateMemory(width, height);
                Array.Copy(data, (byte[])_data, this.LengthByBytes);
            }
            else
            {
                InitializeWithoutAllocateMemory(width, height);
                _data = data;
            }
        }

        public GreyImage(GreyImage refImage, bool cloneData)
        {
            if (refImage == null)
                throw new System.ArgumentException("Input is invalid!");

            if (cloneData)
            {
                InitializeAndAllocateMemory(refImage.Width, refImage.Height);
                Array.Copy((byte[])refImage.Data, (byte[])_data, this.LengthByBytes);
            }
            else
            {
                InitializeWithoutAllocateMemory(refImage.Width, refImage.Height);
                _data = refImage.Data;
            }
        }

        public GreyImage(GreyImage refImage, int l, int t, int w, int h)
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
            _data = Utilities.Crop((byte[])refImage.Data, 1, width, height, l, t, w, h);
        }

        public GreyImage(
            GreyImage refImage,
            int marginX, int marginY,
            bool trueIsExpanding_falseIsCropping,
            bool interpolationForExpandingOnly)
        {
            if (refImage == null)
                throw new System.ArgumentException("Input is invalid!");

            int new_width = 0;
            int new_height = 0;
            byte[] new_data = null;

            if (trueIsExpanding_falseIsCropping) /* expand */
            {
                if (interpolationForExpandingOnly)
                {
                    new_data =
                        Utilities.ExpandAndInterpolate(
                        (byte[])refImage.Data,
                        refImage.Width, refImage.Height,
                        marginX, marginY, ref new_width, ref new_height);
                }
                else
                {
                    new_data =
                        Utilities.Expand(
                        (byte[])refImage.Data,
                        refImage.Width, refImage.Height,
                        marginX, marginY, ref new_width, ref new_height);
                }
            }
            else /* crop */
            {
                new_data =
                    Utilities.Crop(
                    (byte[])refImage.Data, 1, refImage.Width, refImage.Height,
                    marginX, marginY, ref new_width, ref new_height);
            }

            // update image's info
            _width = new_width;
            _height = new_height;
            _length = _width * _height;

            // update image's data
            _data = new_data;
        }

        #endregion Constructors and destructors

        #region Properties
        public override int LengthByBytes
        {
            get { return _length; }
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
                byte[] dst = (byte[])_data;

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
                                    dst[index] = (byte)(*(src));
                                }
                                src += reserved;
                            }
                            break;
                        case 3:
                            for (int y = 0; y < height; y++)
                            {
                                for (int x = 0; x < width; x++, index++, src += 3)
                                {
                                    dst[index] = (byte)((*(src) * 29 + *(src + 1) * 150 + *(src + 2) * 77 + 128) / 256);
                                }
                                src += reserved;
                            }
                            break;
                        case 4:
                            for (int y = 0; y < height; y++)
                            {
                                for (int x = 0; x < width; x++, index++, src += 4)
                                {
                                    dst[index] = (byte)((*(src) * 29 + *(src + 1) * 150 + *(src + 2) * 77 + 128) / 256);
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

                byte* dst = (byte*)bitmapData.Scan0.ToPointer();

                int x, y, index = 0;
                byte intensity;

                fixed (byte* src = (byte[])_data)
                {
                    for (y = 0; y < _height; y++)
                    {
                        for (x = 0; x < _width; x++)
                        {
                            intensity = src[index++];

                            *(dst++) = intensity;
                            *(dst++) = intensity;
                            *(dst++) = intensity;
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

        public bool QuickSave(string fileName)
        {
            using (FileStream fs =
                new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                return QuickSave(fs);
            }
        }

        public bool QuickSave(FileStream fs)
        {
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                return QuickSave(writer);
            }
        }

        public bool QuickSave(BinaryWriter writer)
        {
            try
            {
                writer.Write((int)eImageType.Grey);

                writer.Write(_width);
                writer.Write(_height);

                writer.Write(LengthByBytes);
                writer.Write((byte[])_data);

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

        public bool QuickLoad(string fileName)
        {
            using (FileStream fs =
                new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite))
            {
                return QuickLoad(fs);
            }
        }

        public bool QuickLoad(FileStream fs)
        {
            using (BinaryReader reader = new BinaryReader(fs))
            {
                eImageType imageType = (eImageType)reader.ReadInt32();

                if (imageType != eImageType.Grey)
                    throw new System.Exception("Invalid format!");

                return QuickLoad(reader);
            }
        }

        public bool QuickLoad(BinaryReader reader)
        {
            try
            {
                reader.ReadInt32(); // potential bug here

                _width = reader.ReadInt32();
                _height = reader.ReadInt32();

                int lengthByBytes = reader.ReadInt32();                
                _data = reader.ReadBytes(lengthByBytes);

                _length = _width * _height;

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
            byte[] srcData = (byte[])_data;
            byte[] dstData = new byte[_length];

            for (int i = 0; i < _length; i++)
            {
                dstData[i] = (byte)(~srcData[i]);// (byte)(maxValue - srcData[i]);
            }

            return new GreyImage(dstData, _width, _height, false);
        }

        public void Reset()
        {
            byte[] data = (byte[])this.Data;
            Array.Clear(data, 0, data.Length);
        }

        public double Diff(GreyImage other)
        {
            byte[] src = (byte[])this.Data;
            byte[] dst = (byte[])other.Data;

            int diff = 0;
            int count = 0;

            for (int i = 0; i < Length; i++)
            {
                if (src[i] > 0 || dst[i] > 0)
                    count++;

                if (src[i] != dst[i])
                    diff++;
            }

            if (count == 0)
                return 0;

            return (diff * 1.0 / count);
        }

        public void Binary(double threshold)
        {
            byte[] data = (byte[])this.Data;
            for (int i = 0; i < Length; i++)
            {
                if (data[i] < threshold)
                    data[i] = 0;
                else
                    data[i] = 1;
            }
        }
        #endregion Methods

        public override object Clone()
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }
}
