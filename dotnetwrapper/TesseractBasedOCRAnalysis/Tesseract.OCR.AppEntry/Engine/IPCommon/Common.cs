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
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace IPoVn.Engine.IPCommon
{
    public enum eImageType
    {
        Binary = 0,
        Grey = 1,
        Grey16bits = 2,
        Color24bits = 3,
        Color32bits = 4,
        Float = 5,
        Double = 6
    }

    public enum eImageFormat
    {
        Bmp = 0,
        Jpg = 1,
        Png = 2
    }

    public class Common
    {
        #region Definitions

        public const string JpegExtFilter = "Jpeg Images (*.jpg,*.jpeg)|*.jpg;*.jpeg";
        public const string BmpExtFilter = "Bitmap Images (*.bmp)|*.bmp";
        public const string GifExtFilter = "Gif Images (*.gif)|*.gif";
        public const string PngExtFilter = "Png Images (*.png)|*.png";
        public const string TiffExtFilter = "Tiff Images (*.tif,*.tiff)|*.tif;*.tiff";

        public const string CommonExtFilters =
            "Common Images|*.jpg;*.jpeg;*.bmp;*.gif;*.png;*.tif;*.tiff";

        public const string AllExtfilters =
                                            CommonExtFilters + "|" +
                                            JpegExtFilter + "|" +
                                            BmpExtFilter + "|" +
                                            GifExtFilter + "|" +
                                            PngExtFilter + "|" +
                                            TiffExtFilter;

        public const string SavedExtFilters =
            BmpExtFilter + "|" + JpegExtFilter + "|" + GifExtFilter;

        public const string DefaultExtFilters = AllExtfilters;

        #endregion Definitions

        #region Helpers
        public static ImageFormat GetImageFormat(eImageFormat internalImageFormat)
        {
            switch (internalImageFormat)
            {
                case eImageFormat.Bmp:
                    return ImageFormat.Bmp;
                case eImageFormat.Jpg:
                    return ImageFormat.Jpeg;
                case eImageFormat.Png:
                    return ImageFormat.Png;
            }

            return ImageFormat.Bmp;
        }

        public static ImageFormat GetImageFormat(string extension)
        {
            extension = extension.Trim().ToLower();

            switch (extension)
            {
                case ".bmp":
                    return ImageFormat.Bmp;
                case ".jpg":
                    return ImageFormat.Jpeg;
                case ".jpeg":
                    return ImageFormat.Jpeg;
                case ".png":
                    return ImageFormat.Png;
                case ".gif":
                    return ImageFormat.Gif;
                case ".tif":
                    return ImageFormat.Tiff;
                case ".tiff":
                    return ImageFormat.Tiff;
            }

            return ImageFormat.Bmp;
        }
        #endregion Helpers
    }
}
