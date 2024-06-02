using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RealismMod
{
    public static class HazardTracker
    {


        private static float _totalToxicity = 0f;
        public static float ToxicityRateMeds { get; set; } = 0f;
        public static float TotalToxicityRate { get; set; } = 0f;



        public static float TotalToxicity
        {
            get
            {
                return _totalToxicity;
            }
            set
            {
                _totalToxicity = Mathf.Clamp(value, 0f, 100f);
            }
        }



        public static int GetNextLowestToxicityLevel(int value)
        {
            return (value / 10) * 10;
        }


        public static void ResetTracker() 
        {
            _totalToxicity = GetNextLowestToxicityLevel((int)_totalToxicity);
            ToxicityRateMeds = 0f;
            TotalToxicityRate = 0f;
        }

        public static void SaveValues()
        {
            //save total toxicity and profileid to file
        }

        public static void ReadValues()
        {
            //load total toxicity from file usign profileid
        }

    }
}
