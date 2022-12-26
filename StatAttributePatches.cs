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
            FragmentationChance
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
                default:
                    return id.ToString();
            }
        }
    }
}