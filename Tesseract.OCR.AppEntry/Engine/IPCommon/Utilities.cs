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

namespace IPoVn.Engine.IPCommon
{
    unsafe internal class Utilities
    {
        /// <summary>
        /// Create Gaussian's weights on 1D
        /// </summary>
        /// <param name="kSz"> kernel size </param>
        /// <returns> a vector presents Gaussian's weights </returns>
        public static float[] CreateGaussianWeights1D(int kSz)
        {
            float[] weights = new float[kSz];

            int kSzHalf = kSz / 2;

            weights[kSzHalf] = 1.0f;
            float factor = 3.0f / kSzHalf;
            factor = factor * factor * 0.5f;
            float sum = 1.0f;
            for (int weight = kSzHalf; weight >= 1; weight--)
            {
                // exp ( -x*x / 2 )
                float exp_x = (float)Math.Exp(-weight * weight * factor);

                weights[kSzHalf - weight] = exp_x;
                weights[kSzHalf + weight] = exp_x;

                sum += exp_x + exp_x;
            }

            float inv_k = 1.0f / sum;
            for (int weight = 0; weight < kSz; weight++)
                weights[weight] *= inv_k;

            return weights;
        }

        /// <summary>
        /// Calculate integral image
        /// </summary>
        /// <param name="src"> the image's buffer that will be calculated </param>
        /// <param name="width"> image's width </param>
        /// <param name="height"> image's height </param>
        /// <returns> calculated intergal image </returns>
        public static uint[] CalcIntegralImage(byte[] src, int width, int height)
        {
            // get data's length
            int length = width * height;

            // create integral image buffer
            uint[] integralImage = new uint[length];

            // temporate variables
            int x, y, index;
            uint accumulatedVal;

            // calculate integral image
            fixed (byte* pSrc = src)
            {
                // first item
                integralImage[0] = pSrc[0];

                // first line
                for (index = 1; index < width; index++)
                    integralImage[index] = integralImage[index - 1] + pSrc[index];

                // first column
                for (index = width, y = 1; y < height; y++, index += width)
                    integralImage[index] = integralImage[index - width] + pSrc[index];

                // remains
                for (index = width, y = 1; y < height; y++)
                {
                    accumulatedVal = pSrc[index++];
                    for (x = 1; x < width; x++, index++)
                    {
                        accumulatedVal += pSrc[index];
                        integralImage[index] = integralImage[index - width] + accumulatedVal;
                    }
                }
            }

            // return caluclated integral image
            return integralImage;
        }

        /// <summary>
        /// Transpose a byte-matrix
        /// </summary>
        /// <param name="src"> a vector presents a byte-matrix [Width x Height] items </param>
        /// <param name="width"> an integer presents width of matrix </param>
        /// <param name="height"> an integer presents height of matrix </param>
        /// <returns> a vector presents transposed byte-matrix </returns>
        public static byte[] Transpose(byte[] src, int width, int height)
        {
            int length = width * height;

            byte[] dst = new byte[length];

            int srcIndex = 0;
            int dstIndex = 0;
            for (int x = 0; x < width; x++)
            {
                srcIndex = x;
                for (int y = 0; y < height; y++, srcIndex += width, dstIndex++)
                {
                    dst[dstIndex] = src[srcIndex];
                }
            }

            return dst;
        }

        /// <summary>
        /// Transpose a float-matrix
        /// </summary>
        /// <param name="src"> a vector presents a float-matrix [Width x Height] items </param>
        /// <param name="width"> an integer presents width of matrix </param>
        /// <param name="height"> an integer presents height of matrix </param>
        /// <returns> a vector presents transposed float-matrix </returns>
        public static float[] Transpose(float[] src, int width, int height)
        {
            int length = width * height;

            float[] dst = new float[length];

            int srcIndex = 0;
            int dstIndex = 0;
            for (int x = 0; x < width; x++)
            {
                srcIndex = x;
                for (int y = 0; y < height; y++, srcIndex += width, dstIndex++)
                {
                    dst[dstIndex] = src[srcIndex];
                }
            }

            return dst;
        }

        /// <summary>
        /// Expand a matrix with margin-x and margin-y
        /// </summary>
        /// <param name="src"> a byte-array presents original matrix </param>
        /// <param name="width"> an integer presents original matrix width </param>
        /// <param name="height"> an integer presents original matrix height </param>
        /// <param name="marginX"> an integer presents margin-x </param>
        /// <param name="marginY"> an integer presents margin-y </param>
        /// <param name="new_width"> an integer presents width of matrix after expanding </param>
        /// <param name="new_height"> an integer presents height of matrix after expanding </param>
        /// <returns> a byte-array presents expanded matrix </returns>
        public static byte[] Expand(
            byte[] src,
            int width, int height,
            int marginX, int marginY,
            ref int new_width, ref int new_height)
        {
            new_width = width + 2 * marginX;
            new_height = height + 2 * marginY;

            int length = new_width * new_height;

            byte[] dst = new byte[length];

            int srcIndex = 0;
            int dstIndex = marginY * new_width + marginX;

            for (int y = 0; y < height; y++, srcIndex += width, dstIndex += new_width)
            {
                Array.Copy(src, srcIndex, dst, dstIndex, width);
            }

            return dst;
        }

        /// <summary>
        /// Expand a matrix with margin-x and margin-y, 
        /// after that interpolate new elements using symetric method
        /// </summary>
        /// <param name="src"> a byte-array presents original matrix </param>
        /// <param name="width"> an integer presents original matrix width </param>
        /// <param name="height"> an integer presents original matrix height </param>
        /// <param name="marginX"> an integer presents margin-x </param>
        /// <param name="marginY"> an integer presents margin-y </param>
        /// <param name="new_width"> an integer presents width of matrix after expanding </param>
        /// <param name="new_height"> an integer presents height of matrix after expanding </param>
        /// <returns> a byte-array presents expanded matrix </returns>
        public static byte[] ExpandAndInterpolate(
            byte[] src,
            int width, int height,
            int marginX, int marginY,
            ref int new_width, ref int new_height)
        {
            new_width = width + 2 * marginX;
            new_height = height + 2 * marginY;

            int length = new_width * new_height;

            byte[] dst = new byte[length];

            int srcIndex = 0;
            int dstIndex = marginY * new_width + marginX;

            int yEnd = marginY + height;
            int xEnd = marginX + width;

            int x, y;

            for (y = 0; y < height; y++, srcIndex += width, dstIndex += new_width)
            {
                Array.Copy(src, srcIndex, dst, dstIndex, width);
            }

            for (y = marginY; y < yEnd; y++)
            {
                // left side                
                srcIndex = y * new_width + marginX;
                dstIndex = srcIndex - 1;
                for (x = 0; x < marginX; x++, srcIndex++, dstIndex--)
                {
                    dst[dstIndex] = dst[srcIndex];
                }

                // right side
                srcIndex = y * new_width + width + marginX - 1;
                dstIndex = srcIndex + 1;
                for (x = 0; x < marginX; x++, srcIndex--, dstIndex++)
                {
                    dst[dstIndex] = dst[srcIndex];
                }
            }

            // top side
            srcIndex = marginY * new_width;
            dstIndex = srcIndex - new_width;
            for (y = 0; y < marginY; y++, dstIndex -= new_width, srcIndex += new_width)
            {
                Array.Copy(dst, srcIndex, dst, dstIndex, new_width);
            }

            // bottom side
            srcIndex = (marginY + height - 1) * new_width;
            dstIndex = srcIndex + new_width;
            for (y = 0; y < marginY; y++, dstIndex += new_width, srcIndex -= new_width)
            {
                Array.Copy(dst, srcIndex, dst, dstIndex, new_width);
            }

            return dst;
        }

        public static byte[] Crop(byte[] src, int bytesPerPixel,
            int width, int height, int l, int t, int w, int h)
        {
            int lengthByBytes = w * h;
            int bytesPerRow = bytesPerPixel * width;
            int bytesToCopyForEach = bytesPerPixel * w;

            byte[] dst = new byte[lengthByBytes];

            int srcIndex = (t * width + l) * bytesPerPixel;
            int dstIndex = 0;

            for (int y = 0; y < h; y++, srcIndex += bytesPerRow, dstIndex += bytesToCopyForEach)
            {
                Array.Copy(src, srcIndex, dst, dstIndex, bytesToCopyForEach);
            }

            return dst;
        }

        public static byte[] Crop(byte[] src, int bytesPerPixel, int width, int height,
            int marginX, int marginY, ref int new_width, ref int new_height)
        {
            new_width = width - 2 * marginX;
            new_height = height - 2 * marginY;

            if (marginX < 0) marginX = 0;
            if (marginX >= width) marginX = width - 1;
            if (marginY < 0) marginY = 0;
            if (marginY >= height) marginY = height - 1;

            if (new_width < 1) new_width = 1;
            if (new_height < 1) new_height = 1;

            return Crop(src, bytesPerPixel, width, height, marginX, marginY, new_width, new_height);
        }
    }
}
