using System;
using System.Reflection;
using Aki.Reflection.Patching;
using EFT.UI;
using UnityEngine;


namespace RealismMod
{
    public class GetAttributeIconPatches : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(StaticIcons).GetMethod("GetAttributeIcon", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPrefix]
        private static bool Prefix(ref Sprite __result, Enum id)
        {

            if (id == null || !Plugin.IconCache.ContainsKey(id))
            {
                return true;
            }

            Sprite sprite = Plugin.IconCache[id];

            if (sprite != null)
            {
                __result = sprite;
                return false;
            }

            return true;
        }
    }

    public static class Attributes
    {
        public enum ENewItemAttributeId
        {
            HorizontalRecoil,
            VerticalRecoil,
            Balance = 11,
            CameraRecoil,
            Dispersion,
            MalfunctionChance,
            AutoROF,
            SemiROF,
            RecoilAngle,
            ReloadSpeed,
            FixSpeed,
            AimSpeed,
            ChamberSpeed,
            Firerate,
            Damage,
            Penetration,
            ArmorDamage,
            FragmentationChance,
            BluntThroughput,
            ShotDispersion,
            GearReloadSpeed,
            CanSpall,
            SpallReduction,
            CanAds,
            NoiseReduction,
            ProjectileCount,
            Convergence,
            HBleedType,
            LimbHpPerTick,
            HpPerTick,
            RemoveTrnqt,
            Comfort,
            PainKillerStrength
        }

        public static string GetName(this ENewItemAttributeId id)
        {
            switch (id)
            {
                case ENewItemAttributeId.HorizontalRecoil:
                    return "HORIZONTAL RECOIL";
                case ENewItemAttributeId.VerticalRecoil:
                    return "VERTICAL RECOIL";
                case ENewItemAttributeId.Balance:
                    return "BALANCE";
                case ENewItemAttributeId.Dispersion:
                    return "DISPERSION";
                case ENewItemAttributeId.CameraRecoil:
                    return "CAMERA RECOIL";
                case ENewItemAttributeId.MalfunctionChance:
                    return "MALFUNCTION CHANCE";
                case ENewItemAttributeId.AutoROF:
                    return "AUTO FIRE RATE";
                case ENewItemAttributeId.SemiROF:
                    return "SEMI FIRE RATE";
                case ENewItemAttributeId.RecoilAngle:
                    return "RECOIL ANGLE";
                case ENewItemAttributeId.ReloadSpeed:
                    return "RELOAD SPEED";
                case ENewItemAttributeId.FixSpeed:
                    return "FIX SPEED";
                case ENewItemAttributeId.AimSpeed:
                    return "AIM SPEED";
                case ENewItemAttributeId.ChamberSpeed:
                    return "CHAMBER SPEED";
                case ENewItemAttributeId.Firerate:
                    return "FIRE RATE";
                case ENewItemAttributeId.Damage:
                    return "DAMAGE";
                case ENewItemAttributeId.Penetration:
                    return "PENETRATION";
                case ENewItemAttributeId.ArmorDamage:
                    return "ARMOR DAMAGE";
                case ENewItemAttributeId.FragmentationChance:
                    return "FRAGMENTATION CHANCE";
                case ENewItemAttributeId.BluntThroughput:
                    return "BLUNT DAMAGE REDUCTION";
                case ENewItemAttributeId.ShotDispersion:
                    return "SHOT SPREAD REDUCTION";
                case ENewItemAttributeId.CanSpall:
                    return "CAN SPALL";
                case ENewItemAttributeId.SpallReduction:
                    return "SPALLING REDUCTION";
                case ENewItemAttributeId.GearReloadSpeed:
                    return "RELOAD SPEED";
                case ENewItemAttributeId.CanAds:
                    return "ALLOWS ADS";
                case ENewItemAttributeId.NoiseReduction:
                    return "NOISE REDUCTION RATING";
                case ENewItemAttributeId.ProjectileCount:
                    return "PROJECTILES";
                case ENewItemAttributeId.Convergence:
                    return "FLATNESS";
                case ENewItemAttributeId.HBleedType:
                    return "HEAVY BLEED HEAL TYPE";
                case ENewItemAttributeId.LimbHpPerTick:
                    return "TOURNIQUET HP LOSS PER TICK";
                case ENewItemAttributeId.HpPerTick:
                    return "HP CHANGE PER TICK";
                case ENewItemAttributeId.RemoveTrnqt:
                    return "REMOVES TOURNIQUET EFFECT";
                case ENewItemAttributeId.Comfort:
                    return "COMFORT MODIFIER";
                case ENewItemAttributeId.PainKillerStrength:
                    return "PAINKILLER STRENGTH";
                default:
                    return id.ToString();
            }
        }
    }
}