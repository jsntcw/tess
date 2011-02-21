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

namespace IPoVn.Engine.IPCommon
{
    unsafe internal class Histogram
    {
        #region Member fields
        public const int DefaultNumBins = 256;

        public int NumBins = DefaultNumBins;
        public float[] Bins = null;

        public float MinBin = 0;
        public float MedianBin = 0;
        public float MaxBin = 0;
        public float MinCount = 0;
        public float MaxCount = 0;

        public int TotalPixels = 0;
        public uint TotalIntensity = 0;
        public float MinIntensity = 255;
        public float MeanIntensity = 0;
        public float MedianIntensity = 0;
        public float MaxIntensity = 0;
        public float IntensityStdv = 0;
        #endregion Member fields

        #region Constructors and destructors
        public Histogram()
        {
        }

        public Histogram(GreyImage image)
        {
            if (image == null)
                throw new System.ArgumentNullException("Input is invalid.");

            // calculate histogram's info
            this.Calc(image);
        }

        private void Initialize()
        {
            if (Bins == null)
                Bins = new float[NumBins];

            MinBin = 0;
            MedianBin = 0;
            MaxBin = 0;

            TotalIntensity = 0;
            MinIntensity = 255;
            MeanIntensity = 0;
            MedianIntensity = 0;
            MaxIntensity = 0;
            IntensityStdv = 0;
        }
        #endregion Constructors and destructors

        #region Methods
        public void Calc(GreyImage image)
        {
            if (image == null)
                throw new System.ArgumentNullException("Input is invalid.");

            // general initialization
            this.Initialize();

            // get image's info
            int width = image.Width;
            int height = image.Height;
            int length = image.Length;

            TotalPixels = width * height;

            fixed (byte* data = (byte[])image.Data)
            {
                for (int i = 0; i < length; i++)
                {
                    Bins[data[i]]++;
                    TotalIntensity += data[i];
                }
            }

            MeanIntensity = (float)(TotalIntensity * 1.0 / length);

            MedianIntensity = -1;
            int posMedian = length / 2;
            float count = 0;
            MinCount = float.MaxValue;
            MaxCount = float.MinValue;

            double totalSquareIntensity = 0;

            for (int i = 0; i < NumBins; i++)
            {
                if (Bins[i] == 0) continue;

                if (MinCount > Bins[i])
                {
                    MinCount = Bins[i];
                    MinBin = i;
                }

                if (MaxCount < Bins[i])
                {
                    MaxCount = Bins[i];
                    MaxBin = i;
                }

                if (MinIntensity >= i) MinIntensity = i;
                if (MaxIntensity <= i) MaxIntensity = i;

                totalSquareIntensity += Bins[i] * (i * i);

                if (MedianIntensity < 0)
                {
                    count += Bins[i];
                    if (count > posMedian) MedianIntensity = i;
                }
            }

            double squareStdv =
                length * MeanIntensity * MeanIntensity
                - 2.0 * MeanIntensity * TotalIntensity
                + totalSquareIntensity;

            IntensityStdv = (float)Math.Sqrt(squareStdv);

            // search median of bin count
            // In case of the bins's size is large, 
            // search engine should be implement based on The Z algorithm
            float[] tmp = new float[NumBins];
            int[] indices = new int[NumBins];
            Array.Copy(Bins, tmp, NumBins);
            for (int i = 0; i < NumBins; i++)
                indices[i] = i;
            Array.Sort(tmp, indices);
            MedianBin = indices[(NumBins - 1) / 2];
        }

        public void Normalize()
        {
            for (int i = 0; i < NumBins; i++)
            {
                Bins[i] = Bins[i] / TotalPixels;
            }
        }
        #endregion Methods

        public void Save(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                Save(fs);
            }
        }

        public void Save(FileStream fs)
        {
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                Save(writer);
            }
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(NumBins);
            for (int i = 0; i < NumBins; i++)
            {
                writer.Write(Bins[i]);
            }
        }

        public static Histogram Load(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                return Histogram.Load(fs);
            }

            return null;
        }

        public static Histogram Load(FileStream fs)
        {
            using (BinaryReader reader = new BinaryReader(fs))
            {
                return Histogram.Load(reader);
            }
            return null;
        }

        public static Histogram Load(BinaryReader reader)
        {
            Histogram hist = new Histogram();
            hist.NumBins = reader.ReadInt32();

            hist.Bins = new float[hist.NumBins];
            for (int i = 0; i < hist.NumBins; i++)
            {
                hist.Bins[i] = reader.ReadSingle();
            }

            return hist;
        }
    }
}
