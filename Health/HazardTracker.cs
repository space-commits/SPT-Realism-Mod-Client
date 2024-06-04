using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Newtonsoft;
using System.IO;
using Newtonsoft.Json;
using Aki.Common.Utils;

namespace RealismMod
{

    public class HazardRecord
    {
        public float RecordedTotalToxicity { get; set; }
        public float RecordedTotalRadiation { get; set; }
    }

    public static class HazardTracker
    {
        public static float TotalToxicityRate { get; set; } = 0f;
        private static float _totalToxicity = 0f;
        private static float _toxicityRateMeds = 0f;
        private static Dictionary<string, HazardRecord> _hazardRecords = new Dictionary<string, HazardRecord>();

        public static float ToxicityRateMeds
        {
            get
            {
                return _toxicityRateMeds;
            }
            set 
            {
                _toxicityRateMeds =  Mathf.Clamp(value, -0.55f, 0f); 
            }
        }

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

        private static string GetFilePath()
        {
            return AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Realism\\data\\hazard_tracker.json";
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

        public static void UpdateHazardValues(string profileId)
        {
            HazardRecord record = null;
            if (_hazardRecords.TryGetValue(profileId, out record))
            {
                record.RecordedTotalToxicity = TotalToxicity;
                record.RecordedTotalRadiation = -1f;
                Utils.Logger.LogWarning("updated record");
            }
        }

        public static void SaveHazardValues()
        {
            string records = JsonConvert.SerializeObject(_hazardRecords, Formatting.Indented);
            File.WriteAllText(GetFilePath(), records);
            Utils.Logger.LogWarning("saved record");
        }

        public static void GetHazardValues(string profileId)
        {
            string json = File.ReadAllText(GetFilePath());
            if (string.IsNullOrWhiteSpace(json) || json.Trim() == "{}")
            {
                Utils.Logger.LogWarning("JSON content is empty or only contains '{}'");
            }
            else 
            {
                Utils.Logger.LogWarning("Reading JSON");

                _hazardRecords = JsonConvert.DeserializeObject<Dictionary<string, HazardRecord>>(json);
            }

            HazardRecord record = null;
            if (_hazardRecords.TryGetValue(profileId, out record))
            {
                Utils.Logger.LogWarning("=================got record");
                TotalToxicity = record.RecordedTotalToxicity;
            }
            else 
            {
                Utils.Logger.LogWarning("==============================created record");
                _hazardRecords.Add(profileId, new HazardRecord { RecordedTotalRadiation = 0, RecordedTotalToxicity = 0 });
                TotalToxicity = 0f;
                SaveHazardValues();
            }
        }

    }
}
