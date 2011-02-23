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
using System.Drawing;
using IPoVn.UI;
using tesseract;

namespace Tesseract.OCR.AppEntry.UI
{
    internal class OCRAnalysisRender : Render
    {
        public OCRAnalysisRender(ImageViewer owner)
        {
            _owner = owner;            
        }

        public override void DoRender(System.Drawing.Graphics grph, IRenderingData renderingData)
        {
            this.DoRender(grph, renderingData as OCRRenderingData);
        }

        private void DoRender(Graphics grph, OCRRenderingData data)
        {
            Rectangle dstRect = new Rectangle(0, 0, _owner.Width, _owner.Height);
            Rectangle srcRect = new Rectangle(0, 0, data.Image.Width, data.Image.Height);

            grph.DrawImage(data.Image, dstRect, srcRect, GraphicsUnit.Pixel);

            this.RenderWords(grph, data);
        }

        private void RenderWords(System.Drawing.Graphics grph, OCRRenderingData data)
        {
            if (!data.ShowDetectedWords && !data.ShowDetectedCharacters)
                return;

            if (data.WordList == null || data.WordList.Count == 0)
                return;

            float scaleFactor = 1.0f;

            foreach (Word word in data.WordList)
            {
                if (data.ShowDetectedCharacters)
                {
                    this.RenderCharacters(grph, data, word);
                }

                if (data.ShowDetectedWords)
                {
                    grph.DrawRectangle(data.WordPen,
                        scaleFactor * word.Left, scaleFactor * word.Top,
                        scaleFactor * (word.Right - word.Left), 
                        scaleFactor * (word.Bottom - word.Top));
                }
            }
        }

        private void RenderCharacters(System.Drawing.Graphics grph, OCRRenderingData data, Word currentWord)
        {
            if (currentWord == null)
                return;

            foreach (Character c in currentWord.CharList)
            {
                this.RenderCharacter(grph, data, c);
            }
        }

        private void RenderCharacter(System.Drawing.Graphics grph, OCRRenderingData data, Character c)
        {
            float scaleFactor = 1.0f;

            grph.DrawRectangle(data.CharPen,
                scaleFactor * c.Left, scaleFactor * c.Top,
                scaleFactor * (c.Right - c.Left), scaleFactor * (c.Bottom - c.Top));
        }
    }
}
