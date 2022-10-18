using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aki.Reflection.Patching;
using Comfort.Common;
using EFT.InventoryLogic;
using EFT.UI;
using RealismMod;
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
        private static bool PatchPrefix(ref Sprite __result, Enum id)
        {

            if (id.Equals(Attributes.ENewItemAttributeId.Balance))
            {
                Logger.LogWarning("========2=======");
                Logger.LogWarning("RecoilAngle Enum Found 1");
                Logger.LogWarning("===============");
            }

            if (id == null || !Plugin.iconCache.ContainsKey(id))
            {
                return true;
            }

            Sprite sprite = Plugin.iconCache[id];



            if (id.Equals(Attributes.ENewItemAttributeId.Balance))
            {
                Logger.LogWarning("========1=======");
                Logger.LogWarning("RecoilAngle Enum Found 2");
                Logger.LogWarning("===============");
            }

            if (sprite != null)
            {
                Logger.LogWarning("Sprite Found");
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
            CameraRecoil = 41,
            Dispersion = 42,
            MalfunctionChance,
            AutoROF,
            SemiROF,
            RecoilAngle,
            ReloadSpeed,
            FixSpeed,
            AimSpeed
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
                default:
                    return id.ToString();
            }
        }
    }
}