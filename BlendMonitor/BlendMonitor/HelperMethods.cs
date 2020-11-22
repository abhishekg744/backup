using System;
using System.Collections.Generic;
using System.Text;

namespace BlendMonitor
{
    public class HelperMethods
    {
        public static string gArDebugLevelStrs(int level)
        {
            if (level == 1)
            {
                return "MEDIUM";
            }
            else if (level == 2)
            {
                return "HIGH";
            }
            else
            {
                return "LOW";
            }
        }

        public static T[,] ResizeArray<T>(T[,] original, int rows, int cols)
        {
            var newArray = new T[rows, cols];
            int minRows = Math.Min(rows, original.GetLength(0));
            int minCols = Math.Min(cols, original.GetLength(1));
            for (int i = 0; i < minRows; i++)
                for (int j = 0; j < minCols; j++)
                    newArray[i, j] = original[i, j];
            return newArray;
        }
        public static double SSF2CST(double sngOrigValue)
        {
            //'introducing conversion of viscosity from SSF to CST
            //'get the value in centistokes
            double value;
            if (sngOrigValue <= 10.99438)
            {
                value = (3.009145 * sngOrigValue - 18.08368);
            }
            else if (sngOrigValue > 10.99438 && sngOrigValue <= 25.14179)
            {
                value = (2.31782 * sngOrigValue - 10.27414);
            }
            else if (sngOrigValue > 25.14179 && sngOrigValue <= 48.58634)
            {
                value = (2.227016 * sngOrigValue - 8.202528);
            }
            else if (sngOrigValue > 48.58634 && sngOrigValue <= 95.14252)
            {
                value = (2.14569 * sngOrigValue - 4.146395);
            }
            else if (sngOrigValue > 95.14252 && sngOrigValue <= 613.0389)
            {
                value = (2.121941 * sngOrigValue - 0.832281);
            }
            else
            {
                value = (2.119992 * sngOrigValue);
            }
            return value;
        }

        public static double CST2SSF(double sngOrigValue)
        {
            //        'Introducing conversion of viscosity from CST TO SSF
            //'get the value in SSF
            double value;
            if (sngOrigValue <= 15)
            {
                value = 0.33232 * sngOrigValue + 6.009572;
            }
            else if (sngOrigValue > 15 && sngOrigValue <= 48)
            {
                value = 0.43144 * sngOrigValue + 4.432674;
            }
            else if (sngOrigValue > 48 && sngOrigValue <= 100)
            {
                value = 0.449031 * sngOrigValue + 3.683193;
            }
            else if (sngOrigValue > 100 && sngOrigValue <= 200)
            {
                value = 0.46605 * sngOrigValue + 1.932429;
            }
            else if (sngOrigValue > 200 && sngOrigValue <= 1300)
            {
                value = 0.471267 * sngOrigValue + 0.392273;
            }
            else
            {
                value = 0.4717 * sngOrigValue;
            }

            return value;
        }

        public static double SG2API(double sngOrigValue)
        {
            // 'introducing conversion of API to SPG
            return (141.5 / sngOrigValue) - 131.5;
        }

        public static double API2SG(double sngOrigValue)
        {
            //'introducing conversion of SPG TO API
            return (141.5 / (sngOrigValue + 131.5));
        }

        public static double DEGC2DEGF(double sngOrigValue)
        {
            // 'introducing conversion of Degrees C TO Degrees F
            return (1.8 * sngOrigValue + 32);
        }

        public static double DEGF2DEGC(double sngOrigValue)
        {
            //'introducing conversion of Degrees F TO Degrees C
            return ((sngOrigValue - 32) / 1.8);
        }
    }
}
