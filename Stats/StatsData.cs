using EFT;
using Newtonsoft.Json;
using SPT.Common.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RealismMod
{
    public enum EConsumableType 
    {
        None,
        Surgical,
        Drug,
        PainDrug,
        Vaseline,
        Medkit,
        Splint,
        Tourniquet,
        Bandage,
        Pills,
        PainPills,
        Alcohol
    }

    public enum EHeavyBleedHealType 
    {
        None,
        Clot,
        Tourniquet,
        Combo,
        Surgical
    }

    public class RealismItem
    {
        public MongoID ItemID { get; set; }
    }

    public class Ammo : RealismItem
    {
    }

    public class Gun : RealismItem
    {
        public string WeapType { get; set; } = string.Empty;
        public float BaseTorque { get; set; } = 0;
        public bool HasShoulderContact { get; set; } = true;
        public float BaseReloadSpeedMulti { get; set; } = 1f;
        public string OperationType { get; set; } = string.Empty;
        public float WeapAccuracy { get; set; } = 1f;
        public float RecoilDamping { get; set; } = 0.6f;
        public float RecoilHandDamping { get; set; } = 0.7f;
        public bool WeaponAllowADS { get; set; } = true;
        public float BaseChamberSpeedMulti { get; set; } = 1f;
        public float MaxChamberSpeed { get; set; } = 1f;
        public float MinChamberSpeed { get; set; } = 1f;
        public bool IsManuallyOperated { get; set; } = false;
        public float MaxReloadSpeed { get; set; } = 1f;
        public float MinReloadSpeed { get; set; } = 1f;
        public float BaseChamberCheckSpeed { get; set; } = 1f;
        public float BaseFixSpeed { get; set; } = 1f;
        public float VisualMulti {  get; set; } = 1f;
    }

    public class WeaponMod : RealismItem
    {
        public string ModType { get; set; } = string.Empty;
        public float VerticalRecoil {  get; set; } = 0f;
        public float HorizontalRecoil { get; set; } = 0f;
        public float Dispersion { get; set; } = 0f;
        public float CameraRecoil { get; set; } = 0f;
        public float AutoROF { get; set; } = 0f;
        public float SemiROF { get; set; } = 0f;
        public float ModMalfunctionChance { get; set; } = 0f;
        public float ReloadSpeed { get; set; } = 0f;
        public float AimSpeed { get; set; } = 0f;
        public float ChamberSpeed { get; set; } = 0f;
        public float Convergence { get; set; } = 0f;
        public bool CanCycleSubs { get; set; } = false;
        public float RecoilAngle { get; set; } = 0f;
        public bool StockAllowADS { get; set; } = false;
        public float FixSpeed { get; set; } = 0f;
        public float ModShotDispersion { get; set; } = 0f;
        public float MeleeDamage { get; set; } = 0f;
        public float MeleePen { get; set; } = 0f;
        public float Flash { get; set; } = 0f;
        public float AimStability { get; set; } = 0f;
        public float Handling { get; set; } = 0f;
    }

    public class Gear : RealismItem 
    {
        public bool AllowADS { get; set; } = true;
        public string ArmorClass { get; set; } = "unclassified";
        public bool CanSpall { get; set; } = false;
        public float SpallReduction { get; set; } = 0f;
        public float ReloadSpeedMulti { get; set; } = 1f;
        public bool BlocksMouth { get; set; } = false;
        public bool IsGasMask { get; set; } = false;
        public float RadProtection { get; set; } = 0f;
        public float GasProtection { get; set; } = 0f;
        public string MaskToUse { get; set; } = string.Empty;
        public float dB { get; set; } = 0f;
        public float Comfort { get; set; } = 1f;
    }

    public class Consumable : RealismItem 
    {
        public float Duration { get; set; } = 0f;
        public float Delay { get; set; } = 0f;
        public float EffectPeriod { get; set; } = 0f;
        public float WaitPeriod { get; set; } = 0f;
        public float TunnelVisionStrength { get; set; } = 0f;
        public float Strength { get; set; } = 0f;
        public bool CanBeUsedInRaid { get; set; } = true;
        public EConsumableType ConsumableType { get; set; } = EConsumableType.None;
        public EHeavyBleedHealType HeavyBleedHealType { get; set; } = EHeavyBleedHealType.None;
        public float TrnqtDamage { get; set; } = 0f;
        public float HPRestoreAmount { get; set; } = 0f;
        public float HPRestoreTick { get; set; } = 0f;
    }

    public static class StatsData
    {
        public static Dictionary<string, Gun> GunStats = new Dictionary<string, Gun>();
        public static Dictionary<string, Gear> GearStats = new Dictionary<string, Gear>();
        public static Dictionary<string, WeaponMod> WeaponModStats = new Dictionary<string, WeaponMod>();
        public static Dictionary<string, Consumable> ConsumableStats = new Dictionary<string, Consumable>();
        public static Dictionary<string, Ammo> AmmoStats = new Dictionary<string, Ammo>();
        public static Dictionary<string, RealismItem> IncompatileItems = new Dictionary<string, RealismItem>();

        public static T GetDataObj<T>(Dictionary<string, T> stats, string itemId) where T : RealismItem, new()
        {
            if (stats.TryGetValue(itemId, out T item)) 
            {
               return item;
            }
            T newItem = new T();
            if (!IncompatileItems.ContainsKey(itemId)) 
            {
                IncompatileItems.Add(itemId, newItem);
            }
            return newItem;
        }

        public static string GetTemplatesFilePath() 
        {
            return Plugin.ModDir.ServerBaseDirectory + "\\db\\templates";
        }

        public static void GetStats() 
        {
            var templateFilePaths = GeAllFilesRecursive(GetTemplatesFilePath());

            foreach (var templateFilePath in templateFilePaths) 
            {
                DeserializeTemplates(templateFilePath);
            }
        }

        public static void DeserializeTemplates(string filepath) 
        {
            var templateJson = File.ReadAllText(filepath);

            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            };

            var templatesBase = JsonConvert.DeserializeObject<Dictionary<string, RealismItem>>(templateJson, settings);

            foreach (var templateKvp in templatesBase)
            {
                var template = templateKvp.Value;
                Utils.Logger.LogWarning($"filepath: {filepath}, id: {template.ItemID}, type: {template.GetType().FullName}");

                if (template is Gun gun)
                {
                    GunStats.Add(gun.ItemID, gun);
                }
                else if (template is Gear gear)
                {
                    GearStats.Add(gear.ItemID, gear);
                }
                else if (template is WeaponMod weaponmod)
                {
                    WeaponModStats.Add(weaponmod.ItemID, weaponmod);
                }
                else if (template is Consumable consumable)
                {
                    ConsumableStats.Add(consumable.ItemID, consumable);
                }
                else if (template is Ammo ammo)
                {
                    AmmoStats.Add(ammo.ItemID, ammo);
                }
                else
                {
                    Utils.Logger.LogFatal($"Realism Mod: Invalid Template found at {filepath}, id: {template.ItemID}");
                }
            }
        }

        public static List<string> GeAllFilesRecursive(string directory, List<string> files) 
        {
            try
            {
                files.AddRange(Directory.GetFiles(directory));

                // Recursively add files from subdirectories
                foreach (var dir in Directory.GetDirectories(directory))
                {
                    files.AddRange(GeAllFilesRecursive(dir));
                }
            }
            catch (Exception ex)
            {
                Utils.Logger.LogFatal($"Realism Mod: Error accessing {directory}: {ex.Message}");
            }

            return files;
        }

        public static List<string> GeAllFilesRecursive(string directory)
        {
            var files = new List<string>();
            GeAllFilesRecursive(directory, files);
            return files;
        }

    }

}
