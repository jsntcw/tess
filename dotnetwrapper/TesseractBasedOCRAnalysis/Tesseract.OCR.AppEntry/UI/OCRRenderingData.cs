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
using IPoVn.UI;
using System.Drawing;
using tesseract;

namespace Tesseract.OCR.AppEntry.UI
{
    internal class OCRRenderingData : RenderingData
    {
        public List<Word> WordList = null;
        public bool ShowDetectedWords = true;
        public bool ShowDetectedCharacters = true;
        public Pen WordPen = null;
        public Pen CharPen = null;
        public Pen HoverWordPen = null;
        public Pen HoverCharPen = null;
        public Pen BlockPen = null;
        //private float _scaleFactor = 1.0f;

        public bool ShowDetectedBlocks = true;
        public BlockList Blocks = null;

        public OCRRenderingData()
        {
            base.Initialize();

            WordPen = new Pen(Color.Red, 1.0f);
            CharPen = new Pen(Color.Blue, 1.0f);
            BlockPen = new Pen(Color.DarkGreen, 5.0f);
        }

        public override void ClearData()
        {
            if (WordList != null)
            {
                WordList.Clear();
                WordList = null;
            }

            if (Blocks != null)
            {
                Blocks.DeleteRawData();
                Blocks = null;
            }
        }

        public void UpdateFlags(bool showChars, bool showWords, bool showBlocks)
        {
            ShowDetectedCharacters = showChars;
            ShowDetectedWords = showWords;
            ShowDetectedBlocks = showBlocks;
        }
    }
}
