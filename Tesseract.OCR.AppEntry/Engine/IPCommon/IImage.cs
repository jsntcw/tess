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
    internal class SupportedImageActions
    {
        public const string Save = "Save";
        public const string Load = "Load";
        public const string ToImage = "ToImage";
        public const string QuickSave = "QuickSave";
        public const string QuickLoad = "QuickLoad";
        public const string InvertColor = "InvertColor";
    }

    internal interface IImage
    {
        int Width { get; }
        int Height { get; }
        int Length { get; }

        int MinValue { get; set; }
        int MaxValue { get; set; }

        int LengthByBytes { get; }

        object Data { get; set; }

        bool DoCommand(
            string sCommand, object[] inputs, ref object[] outputs);
    }
}
