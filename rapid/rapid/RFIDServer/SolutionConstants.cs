////////////////////////////////////////////////////////////////////////////////
//
//    Solution Constants
//
////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;

namespace RFID
{
    public static class SolutionConstants
    {
        public const string ReaderHostname = "169.254.173.199";
        
        public static readonly List<string> tagIds = new List<string>() {"757733B2DDD9014000000001" };
        public static readonly List<string> tagLabels = new List<string>() { "Tag 1"};
       
        public static double GaussianVarianceScalingFactor = .1; // Controls how much variance changes by during transition update.

        public const int numModalities = 2;
    }
}
