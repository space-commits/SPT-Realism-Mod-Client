using Comfort.Common;
using EFT;
using EFT.Hideout;
using EFT.InventoryLogic;
using EFT.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static RootMotion.FinalIK.IKSolver;

namespace RealismMod
{

    public class HazardRecord
    {
        public float RecordedTotalToxicity { get; set; }
        public float RecordedTotalRadiation { get; set; }
        public bool IsPreExplosion { get; set; }
        public bool HasExploded { get; set; }
    }

    public static class HazardTracker
    {
        public static bool IsPreExplosion { get; set; } = false;
        public static bool HasExploded { get; set; } = false;
        public static float TotalToxicityRate { get; set; } = 0f;
        public static float TotalRadiationRate { get; set; } = 0f;
        private static Dictionary<string, HazardRecord> _hazardRecords = new Dictionary<string, HazardRecord>();

        public static float RadTreatmentRate
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

        public static float DetoxicationRate
        {
            get
            {
                return _toxicityRateMeds;
            }
            set 
            {
                _toxicityRateMeds =  Mathf.Clamp(value, -0.15f, 0f); 
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

        private static float _totalToxicity = 0f;
        private static float _totalRadiation = 0f;
        private static float _toxicityRateMeds = 0f;
        private static float _radiationRateMeds = 0f;
        private static float _upateTimer = 0f;
        private static float _hideoutRegenTick = 0f;
        public static bool _loadedData = false;

        public static void OutOfRaidUpdate() 
        {
            if (GClass1864.InRaid) return;
            _upateTimer += Time.deltaTime;
            _hideoutRegenTick += Time.deltaTime;

            if (_hideoutRegenTick > 1f)
            {
                var profileData = Singleton<ClientApplication<ISession>>.Instance.GetClientBackEndSession()?.Profile;
                var genController = Singleton<HideoutClass>.Instance?.EnergyController;
                if (profileData != null && _loadedData && genController != null && genController.IsEnergyGenerationOn)
                {
                    float ventsFactor = -(profileData.Hideout.Areas[0].level * 0.01f);
                    float medFactor = -(profileData.Hideout.Areas[7].level * 0.025f);

                    float totalFactor = ventsFactor + medFactor;
                    TotalToxicityRate = totalFactor;
                    TotalToxicity += TotalToxicityRate;
                }
                else 
                {
                    TotalToxicityRate = 0f;
                }
               
                _hideoutRegenTick = 0f;
            }
            if (_upateTimer > 10f && _loadedData)
            {
                HazardTracker.UpdateHazardValues(ProfileData.PMCProfileId);
                HazardTracker.SaveHazardValues();
                _upateTimer = 0f;
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
            DetoxicationRate = 0f;
            RadTreatmentRate = 0f;
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
                bool isWipedProfle = IsWipedProfile();
                record.RecordedTotalToxicity = isWipedProfle ? 0f : TotalToxicity;
                record.RecordedTotalRadiation = isWipedProfle ? 0f : TotalRadiation;
            }
        }

        public static void SaveHazardValues()
        {
            string records = JsonConvert.SerializeObject(_hazardRecords, Formatting.Indented);
            File.WriteAllText(GetFilePath(), records);
        }

        //for cases where player wiped profile
        private static bool IsWipedProfile() 
        {
            return ProfileData.PMCLevel <= 1;
        }

        public static void GetHazardValues(string profileId)
        {
            string json = File.ReadAllText(GetFilePath());
            if (string.IsNullOrWhiteSpace(json) || json.Trim() == "{}")
            {
                Utils.Logger.LogWarning("Realism Mod: Hazard Data JSON File is empty");
            }
            else 
            {
                _hazardRecords = JsonConvert.DeserializeObject<Dictionary<string, HazardRecord>>(json);
            }

            HazardRecord record = null;
            if (_hazardRecords.TryGetValue(profileId, out record))
            {
                bool isWipedProfle = IsWipedProfile();
                TotalToxicity = isWipedProfle ? 0f : record.RecordedTotalToxicity;
                TotalRadiation = isWipedProfle ? 0f : record.RecordedTotalRadiation;
            }
            else 
            {
                _hazardRecords.Add(profileId, new HazardRecord { RecordedTotalRadiation = 0, RecordedTotalToxicity = 0 });
                TotalToxicity = 0f;
                TotalRadiation = 0f;
                SaveHazardValues();
            }
            _loadedData = true;
        }

    }
}
