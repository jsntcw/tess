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

namespace IPoVn.Engine.IPMath
{
    internal class Line
    {
        public double X1 = 0;
        public double Y1 = 0;
        public double X2 = 0;
        public double Y2 = 0;

        public bool MajorLengthBeAlongX = true;

        public Line(double x1, double y1, double x2, double y2)
        {
            if (Math.Abs(x1 - x2) > Math.Abs(y1 - y2))
            {
                if (x1 < x2)
                {
                    X1 = x1;
                    Y1 = y1;
                    X2 = x2;
                    Y2 = y2;
                }
                else
                {
                    X1 = x2;
                    Y1 = y2;
                    X2 = x1;
                    Y2 = y1;
                }
            }
            else
            {
                MajorLengthBeAlongX = false;

                if (y1 < y2)
                {
                    X1 = x1;
                    Y1 = y1;
                    X2 = x2;
                    Y2 = y2;
                }
                else
                {
                    X1 = x2;
                    Y1 = y2;
                    X2 = x1;
                    Y2 = y1;
                }
            }
        }
    }  
}
