using EFT;
using Newtonsoft.Json;
using SPT.Common.Http;
using System;
using System.Collections.Generic;

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
        Alcohol,
        Stimulator
    }

    public enum EHeavyBleedHealType 
    {
        None,
        Clot,
        Tourniquet,
        Combo,
        Surgical
    }

 /*   public struct BaseAmmoTemplate
    {
        public float BulletMass { get; } = 0f;
        public float BulletDiameter { get; } = 0f;
        public float InitialVelocity { get; } = 0f;
        public float ArmorDamage { get; } = 0f;
        public int ProjectileCount { get; } = 0;
        public float FragmentationChance { get; } = 0f;
        public float RicochetChance { get; } = 0f;

        public BaseAmmoTemplate(float bulletMass, float bulletDiameter, float initialVelocity, float armorDamage, int projectileCount, float fragmentationChance, float ricochetChance)
        {
            BulletMass = bulletMass;
            BulletDiameter = bulletDiameter;
            InitialVelocity = initialVelocity;
            ArmorDamage = armorDamage;
            ProjectileCount = projectileCount;
            FragmentationChance = fragmentationChance;
            RicochetChance = ricochetChance;
        }
    }*/

    public class RealismItem
    {
        public MongoID ItemID { get; set; }
        public string TemplateID { get; set; }
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

    public static class TemplateStats
    {
        public static Dictionary<string, Gun> GunStats = new Dictionary<string, Gun>();
        public static Dictionary<string, Gear> GearStats = new Dictionary<string, Gear>();
        public static Dictionary<string, WeaponMod> WeaponModStats = new Dictionary<string, WeaponMod>();
        public static Dictionary<string, Consumable> ConsumableStats = new Dictionary<string, Consumable>();
        //public static Dictionary<string, BaseAmmoTemplate> BaseAmmoTemplateStats = new Dictionary<string, BaseAmmoTemplate>();
        public static Dictionary<string, Ammo> RealismAmmoStats = new Dictionary<string, Ammo>();
        public static Dictionary<string, RealismItem> IncompatileItems = new Dictionary<string, RealismItem>();

        public static T GetDataObj<T>(Dictionary<string, T> stats, string itemId) where T : RealismItem, new()
        {
            if (stats.TryGetValue(itemId, out T item)) 
            {
                //if the item is a reskin, it uses a different item as a template, get that item
                if (item.TemplateID != null)
                {
                    if (stats.TryGetValue(item.TemplateID, out T baseItem))
                    {
                        return baseItem;
                    }
                }
                //otherwise return the intended item
                return item;
            }

            //no item found, try and see if it's an incompatible item that's already been given a class instance
            if (IncompatileItems.TryGetValue(itemId, out RealismItem incompatibleItem))
            {
                return incompatibleItem as T;
            }

            //if there's still no item found, add it to the incomaptible item dictionary
            T newItem = new T();
            IncompatileItems.Add(itemId, newItem);
            return newItem;
        }

        public static void AddItemToDict<T>(Dictionary<string, T> dict, RealismItem item) where T : RealismItem
        {
            if (!dict.ContainsKey(item.ItemID))
            {
                dict.Add(item.ItemID, item as T);
            }
            else 
            {
                Utils.Logger.LogWarning($"Realism Mod: item with duplicate Id already added to dictionary {item.ItemID}");
            }
        }

        public static void RequestAndProcessDataFromServer() 
        {
            string route = "/RealismMod/GetTemplateData";
            var settings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
     
            try
            {
                var json = RequestHandler.GetJson(route);
                DeserializeTemplates(json);
            }
            catch (Exception ex)
            {
                Utils.Logger.LogError($"REALISM MOD ERROR: FAILED TO FETCH DATA FROM SERVER USING ROUTE {route}: {ex.Message}");
            }
        }

        public static void DeserializeTemplates(string templateData) 
        {
            //enables typenamehandling to automatically choose what model class represents the json
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
            };

            var templatesBase = JsonConvert.DeserializeObject<Dictionary<string, RealismItem>>(templateData, settings);

            foreach (var templateKvp in templatesBase)
            {
                var templateType = templateKvp.Value;
                if (templateType is Gun gun)
                {
                    AddItemToDict<Gun>(GunStats, gun);
                }
                else if (templateType is Gear gear)
                {
                    AddItemToDict<Gear>(GearStats, gear);
                }
                else if (templateType is WeaponMod weaponmod)
                {
                    AddItemToDict<WeaponMod>(WeaponModStats, weaponmod);
                }
                else if (templateType is Consumable consumable)
                {
                    AddItemToDict<Consumable>(ConsumableStats, consumable);
                }
                else if (templateType is Ammo ammo)
                {
                    AddItemToDict<Ammo>(RealismAmmoStats, ammo);
                }
                else
                {
                    Utils.Logger.LogFatal($"Realism Mod: Invalid Template found at {templateData}, id: {templateType.ItemID}");
                }
            }
        }
    }
}
