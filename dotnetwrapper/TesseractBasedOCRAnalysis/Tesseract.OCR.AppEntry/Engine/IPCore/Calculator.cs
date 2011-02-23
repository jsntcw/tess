using System;
using System.Collections.Generic;
using System.Text;
using CaptchaOCR.AppEntry.Engine.IPCommon;

namespace CaptchaOCR.AppEntry.Engine.IPCore
{
    public enum eOperator
    {
        Addition = 0,
        Subtraction = 1,
        Multiplication = 2,
        Division = 3
    }

    internal class Calculator
    {
        private SimpleOperator _operator = new SimpleOperator(eOperator.Addition, 0);

        public IImage Apply(IImage srcImg, eOperator oper, double constant)
        {
            if (srcImg == null)
                return null;

            if (oper == eOperator.Division && constant == 0)
                throw new System.Exception(
                    "Cannot perform calculation with: Operator is Division and Constant is 0");

            _operator.Operator = oper;
            _operator.Constant = constant;

            if (srcImg is GreyImage)
                return Apply(srcImg as GreyImage);

            if (srcImg is RGBImage)
                return Apply(srcImg as RGBImage);

            return null;
        }

        private IImage Apply(GreyImage greyImage)
        {
            byte[] dst = _operator.Apply(
                (byte[])greyImage.Data, greyImage.Width, greyImage.Height);

            return new GreyImage(dst, greyImage.Width, greyImage.Height, false);
        }

        private IImage Apply(RGBImage rgbImage)
        {
            byte[][] src = (byte[][])rgbImage.Data;

            byte[][] dst = new byte[3][];
            dst[0] = _operator.Apply(src[0], rgbImage.Width, rgbImage.Height);
            dst[1] = _operator.Apply(src[1], rgbImage.Width, rgbImage.Height);
            dst[2] = _operator.Apply(src[2], rgbImage.Width, rgbImage.Height);

            return new RGBImage(dst, rgbImage.Width, rgbImage.Height, false);
        }
    }

    public class SimpleOperator
    {
        private eOperator _operator = eOperator.Addition;
        private double _constant = 0;

        public eOperator Operator
        {
            get { return _operator; }
            set { _operator = value; }
        }

        public double Constant
        {
            get { return _constant; }
            set { _constant = value; }
        }

        public SimpleOperator(eOperator oper, double constant)
        {
            _operator = oper;
            _constant = constant;
        }

        public byte[] Apply(byte[] src, int imgWidth, int imgHeight)
        {
            switch (_operator)
            {
                case eOperator.Addition:
                    return Add(src, imgWidth, imgHeight, _constant);
                case eOperator.Subtraction:
                    return Sub(src, imgWidth, imgHeight, _constant);
                case eOperator.Multiplication:
                    return Mul(src, imgWidth, imgHeight, _constant);
                case eOperator.Division:
                    return Div(src, imgWidth, imgHeight, _constant);
            }

            return null;
        }

        public byte[] Add(byte[] src, int imgWidth, int imgHeight, double addVal)
        {
            int length = imgWidth * imgHeight;
            byte[] dst = new byte[length];

            int maxVal = 255;
            int minVal = 0;

            double newVal = 0;

            for (int index = 0; index < length; index++)
            {
                newVal = src[index] + addVal;
                if (newVal > maxVal)
                    newVal = maxVal;
                if (newVal < minVal)
                    newVal = minVal;

                dst[index] = (byte)newVal;
            }

            return dst;
        }

        public byte[] Sub(byte[] src, int imgWidth, int imgHeight, double subVal)
        {
            int length = imgWidth * imgHeight;
            byte[] dst = new byte[length];

            int maxVal = 255;
            int minVal = 0;

            double newVal = 0;

            for (int index = 0; index < length; index++)
            {
                newVal = src[index] - subVal;
                if (newVal > maxVal)
                    newVal = maxVal;
                if (newVal < minVal)
                    newVal = minVal;

                dst[index] = (byte)newVal;
            }

            return dst;
        }

        public byte[] Mul(byte[] src, int imgWidth, int imgHeight, double factor)
        {
            int length = imgWidth * imgHeight;
            byte[] dst = new byte[length];

            int maxVal = 255;
            int minVal = 0;

            double newVal = 0;

            for (int index = 0; index < length; index++)
            {
                newVal = src[index] * factor;
                if (newVal > maxVal)
                    newVal = maxVal;
                if (newVal < minVal)
                    newVal = minVal;

                dst[index] = (byte)newVal;
            }

            return dst;
        }

        public byte[] Div(byte[] src, int imgWidth, int imgHeight, double factor)
        {
            int length = imgWidth * imgHeight;
            byte[] dst = new byte[length];

            int maxVal = 255;
            int minVal = 0;

            double newVal = 0;

            for (int index = 0; index < length; index++)
            {
                newVal = src[index] / factor;
                if (newVal > maxVal)
                    newVal = maxVal;
                if (newVal < minVal)
                    newVal = minVal;

                dst[index] = (byte)newVal;
            }

            return dst;
        }
    }
}
