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
    internal class Polygon
    {
        public int nPoints = 0;
        public double[] XPoints = null;
        public double[] YPoints = null;

        public Polygon()
        {
        }

        public Polygon(double[] xPoints, double[] yPoints, int n, bool bClone)
        {
            if (bClone)
            {
                nPoints = n;
                XPoints = new double[nPoints];
                Array.Copy(xPoints, XPoints, nPoints);
                YPoints = new double[nPoints];
                Array.Copy(yPoints, YPoints, nPoints);
            }
            else
            {
                XPoints = xPoints;
                YPoints = yPoints;
                nPoints = n;
            }
        }
    }
}
