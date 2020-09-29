using System;
using System.Collections.Generic;
using System.Text;

namespace BlendMonitor
{
    public class HelperMethods
    {
        public static string gArDebugLevelStrs(int level)
        {
            if (level == 2)
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
    }
}
