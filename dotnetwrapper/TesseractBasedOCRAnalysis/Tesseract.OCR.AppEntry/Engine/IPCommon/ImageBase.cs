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
    [Serializable]
    internal abstract class ImageBase : IImage, IDisposable, ICloneable
    {
        #region Member fields

        protected int _width = 0;
        protected int _height = 0;
        protected int _length = 0;
        protected object _data = null;

        protected int _minValue = 0;
        protected int _maxValue = 255;

        #endregion Member fields

        #region IImage Members

        public virtual int Width
        {
            get { return _width; }
        }

        public virtual int Height
        {
            get { return _height; }
        }

        public virtual int Length
        {
            get { return _length; }
        }

        public abstract int LengthByBytes { get; }

        public virtual object Data
        {
            get { return _data; }
            set { _data = value; }
        }

        public virtual int MinValue
        {
            get { return _minValue; }
            set { _minValue = value; }
        }

        public virtual int MaxValue
        {
            get { return _maxValue; }
            set { _maxValue = value; }
        }

        public abstract bool DoCommand(
            string sCommand, object[] inputs, ref object[] outputs);

        #endregion IImage Members

        #region IDisposable Members

        public virtual void Dispose()
        {
            if (_data != null)
            {
                _data = null;
            }
        }

        #endregion IDisposable Members

        #region ICloneable Members

        public abstract object Clone();

        #endregion ICloneable Members

        protected virtual void InitializeAndAllocateMemory(int width, int height)
        {
            _width = width;
            _height = height;
            _length = _width * _height;

            _data = new byte[this.LengthByBytes];
        }

        protected virtual void InitializeWithoutAllocateMemory(int width, int height)
        {
            _width = width;
            _height = height;
            _length = _width * _height;

            _data = null;
        }
    }
}
