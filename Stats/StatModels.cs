using EFT;
using System;
using System.Collections.Generic;
using System.Text;

namespace RealismMod
{
    public enum TemplateType
    {
        Invalid,
        Gear,
        Ammo,
        Mod,
        Gun,
        Food,
        Med
    }

    public enum ConsumableType 
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

    public enum HeavyBleedHealType 
    {
        None,
        Clot,
        Tourniquet,
        Combo
    }

    public class RealismItem
    {
        public MongoID Id { get; set; }
        public TemplateType TemplateType { get; set; }
    }

    public class Gun : RealismItem
    {
        public string WeapType { get; set; }
        public string BaseTorque { get; set; }
        public bool HasShoulderContact { get; set; }
        public float BaseReloadSpeedMulti { get; set; }
        public string OperationType { get; set; }
        public string WeapAccuracy { get; set; }
        public float RecoilDamping { get; set; }
        public float RecoilHandDamping { get; set; }
        public float WeaponAllowADS { get; set; }
        public float BaseChamberSpeedMulti { get; set; }
        public float MaxChamberSpeed {  get; set; }
        public float MinChamberSpeed { get; set; }
        public bool IsManuallyOperated { get; set; }
        public float MaxReloadSpeed { get; set; }
        public float MinReloadSpeed { get; set; }
        public float BaseChamberCheckSpeed { get; set; }
        public float BaseFixSpeed { get; set; }
        public float VisualMulti {  get; set; }
    }

    public class WeaponMod : RealismItem
    {
        public string ModType { get; set; }
        public float VerticalRecoil {  get; set; }
        public float HorizontalRecoil { get; set; }
        public float Dispersion { get; set; }
        public float CameraRecoil { get; set; }
        public float AutoROF { get; set; }
        public float SemiROF { get; set; }
        public float ModMalfunctionChance { get; set; }
        public float ReloadSpeed { get; set; }
        public float AimSpeed { get; set; }
        public float ChamberSpeed { get; set; }
        public float Convergence { get; set; }
        public float CanCycleSubs { get; set; }
        public float RecoilAngle { get; set; }
        public float StockAllowADS { get; set; }
        public float FixSpeed { get; set; }
        public float ModShotDispersion { get; set; }
        public float MeleeDamage { get; set; }
        public float MeleePen { get; set; }
        public float Flash { get; set; }
        public float AimStability { get; set; }
        public float Handling { get; set; }
    }

    public class Gear : RealismItem 
    {
        public bool AllowADS { get; set; }
        public string ArmorClass { get; set; }
        public bool CanSpall { get; set; }
        public float SpallReduction { get; set; }
        public float ReloadSpeedMulti { get; set; }
        public bool BlocksMouth { get; set; }
        public bool IsGasMask { get; set; }
        public float RadProtection { get; set; }
        public string GasProtection { get; set; }
        public string MaskToUse { get; set; }
        public string dB { get; set; }
        public string Comfort { get; set; }
    }

    public class Consumable : RealismItem 
    {
        public float Duration { get; set; }
        public float Delay { get; set; }
        public float EffectPeriod { get; set; }
        public float WaitPeriod { get; set; }
        public float TunnelVisionStrength { get; set; }
        public float Strength { get; set; } 
        public bool CanBeUsedInRaid { get; set; }
        public ConsumableType ConsumableType { get; set; }
        public HeavyBleedHealType HeavyBleedHealType { get; set; }
        public float TrnqtDamage { get; set; }
        public float HPRestoreAmount { get; set; }
        public float HPRestoreTick { get; set; }
    }

    public static class ItemStats 
    {
        public static List<Gun> GunStats = new List<Gun>();
        public static List<Gear> GearStats = new List<Gear>();
        public static List<WeaponMod> WeaponModStats = new List<WeaponMod>();
        public static List<Consumable> FoodStats = new List<Consumable>();
        public static List<Consumable> MedStats = new List<Consumable>();
    }
}
