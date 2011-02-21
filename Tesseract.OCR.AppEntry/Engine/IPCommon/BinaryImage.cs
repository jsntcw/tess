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
using System.Text;

namespace IPoVn.Engine.IPCommon
{
    internal class BinaryImage : ImageBase
    {
        private static int[] bit1CountFromByte = null;
        private static int[] Bit1CountFromByte
        {
            get
            {
                if (bit1CountFromByte == null)
                {
                    bit1CountFromByte = new int[256];
                    for (int i = 0; i < 256; i++)
                    {
                        byte b = (byte)i;
                        int count = 0;

                        byte[] mask1 = BinaryImage.MaskBit1FromByte;

                        for (int j = 0; j < 8; j++)
                        {
                            int v = b & mask1[j];
                            if (v > 0)
                                count++;
                        }

                        bit1CountFromByte[i] = count;
                    }
                }

                return bit1CountFromByte;
            }
        }

        private static byte[] maskBit1FromByte = null;
        private static byte[] MaskBit1FromByte
        {
            get
            {
                if (maskBit1FromByte == null)
                {
                    maskBit1FromByte = new byte[] {
                        0x1, 0x2, 0x4, 0x8, 0x10, 0x20, 0x40, 0x80
                    };
                }

                return maskBit1FromByte;
            }
        }

        public override int LengthByBytes
        {
            get { return (_width * _height + 7) / 8; }
        }

        public BinaryImage()
        {
        }

        public BinaryImage(int width, int height)
        {
            InitializeAndAllocateMemory(width, height);
        }

        public BinaryImage(byte[] data, int width, int height, bool cloneData)
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

        public override bool DoCommand(string sCommand, object[] inputs, ref object[] outputs)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override object Clone()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void Reset()
        {
            byte[] data = (byte[])_data;
            Array.Clear(data, 0, data.Length);
        }

        public byte this[int x, int y]
        {
            get
            {
                int index = y * _width + x;
                int ibyte = index / 8;
                int offset = index % 8;

                return 0;
            }

            set
            {

            }
        }

        public double Diff(BinaryImage other)
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }
}
