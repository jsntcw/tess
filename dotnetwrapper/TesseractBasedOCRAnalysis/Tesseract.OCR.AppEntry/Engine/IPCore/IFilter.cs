using System;
using System.Text;
using CaptchaOCR.AppEntry.Engine.IPCommon;

namespace CaptchaOCR.AppEntry.Engine.IPCore
{
    internal interface IFilter
    {
        int kWidth { get; }
        int kHeight { get; }

        bool Apply(ref IImage input);
        bool Apply(IImage input, ref IImage output);
    }
}
