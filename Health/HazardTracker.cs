using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Newtonsoft;
using System.IO;
using Newtonsoft.Json;

namespace RealismMod
{

    public class HazardRecord
    {
        public float RecordedTotalToxicity { get; set; }
        public float RecordedTotalRadiation { get; set; }
    }

    public static class HazardTracker
    {
        public static bool IsPreExplosion { get; set; } = false;
        public static bool HasExploded { get; set; } = false;
        public static float TotalToxicityRate { get; set; } = 0f;
        public static float TotalRadiationRate { get; set; } = 0f;
        private static float _totalToxicity = 0f;
        private static float _totalRadiation = 0f;
        private static float _toxicityRateMeds = 0f;
        private static float _radiationRateMeds = 0f;
        private static Dictionary<string, HazardRecord> _hazardRecords = new Dictionary<string, HazardRecord>();

        public static float RadiationRateMeds
        {
            get
            {
                return _radiationRateMeds;
            }
            set
            {
                _radiationRateMeds = Mathf.Clamp(value, -0.2f, 0f);
            }
        }

        public static float ToxicityRateMeds
        {
            get
            {
                return _toxicityRateMeds;
            }
            set 
            {
                _toxicityRateMeds =  Mathf.Clamp(value, -0.5f, 0f); 
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

        public static float TotalRadiation
        {
            get
            {
                return _totalRadiation;
            }
            set
            {
                _totalRadiation = Mathf.Clamp(value, 0f, 100f);
            }
        }

        private static string GetFilePath()
        {
            return AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Realism\\data\\hazard_tracker.json";
        }


        public static int GetNextLowestHazardLevel(int value)
        {
            return (value / 10) * 10;
        }

        public static void ResetTracker() 
        {
            _totalToxicity = GetNextLowestHazardLevel((int)_totalToxicity);
            ToxicityRateMeds = 0f;
            RadiationRateMeds = 0f;
            TotalToxicityRate = 0f;
            TotalRadiationRate = 0f;
        }

        public static void WipeTracker() 
        {
            _totalToxicity = 0f;
            _totalRadiation = 0f;
        }

        public static void UpdateHazardValues(string profileId)
        {
            HazardRecord record = null;
            if (_hazardRecords.TryGetValue(profileId, out record))
            {
                record.RecordedTotalToxicity = TotalToxicity;
                record.RecordedTotalRadiation = TotalRadiation;
            }
        }

        public static void SaveHazardValues()
        {
            string records = JsonConvert.SerializeObject(_hazardRecords, Formatting.Indented);
            File.WriteAllText(GetFilePath(), records);
        }

        public static void GetHazardValues(string profileId)
        {
            string json = File.ReadAllText(GetFilePath());
            if (string.IsNullOrWhiteSpace(json) || json.Trim() == "{}")
            {
                Utils.Logger.LogWarning("Realism Mod: JSON File is empty");
            }
            else 
            {
                _hazardRecords = JsonConvert.DeserializeObject<Dictionary<string, HazardRecord>>(json);
            }

            HazardRecord record = null;
            if (_hazardRecords.TryGetValue(profileId, out record))
            {
                TotalToxicity = record.RecordedTotalToxicity;
                TotalRadiation = record.RecordedTotalRadiation;
            }
            else 
            {
                _hazardRecords.Add(profileId, new HazardRecord { RecordedTotalRadiation = 0, RecordedTotalToxicity = 0 });
                TotalToxicity = 0f;
                TotalRadiation = 0f;
                SaveHazardValues();
            }
        }

    }
}
