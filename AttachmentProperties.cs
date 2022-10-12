using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static RealismMod.Helper;
using Aki.Common.Http;
using Aki.Common.Utils;
using System.IO;
using System.Collections;


namespace RealismMod
{
    public static class AttatchmentProperties
    {

        public static string ModType(Mod mod)
        {
            return mod.ConflictingItems[0];
        }

        public static float VerticalRecoil(Mod mod)
        {
            return float.Parse(mod.ConflictingItems[1]);
        }

        public static float HorizontalRecoil(Mod mod)
        {
            return float.Parse(mod.ConflictingItems[2]);
        }

        public static float Dispersion(Mod mod)
        {
            return float.Parse(mod.ConflictingItems[3]);
        }

        public static float CameraRecoil(Mod mod)
        {
            return float.Parse(mod.ConflictingItems[4]);
        }

        public static float AutoROF(Mod mod)
        {
            return float.Parse(mod.ConflictingItems[5]);
        }

        public static float SemiROF(Mod mod)
        {
            return float.Parse(mod.ConflictingItems[6]);
        }

        public static float ModMalfunctionChance(Mod mod)
        {
            return float.Parse(mod.ConflictingItems[7]);
        }

        public static float ReloadSpeed(Mod mod)
        {
            return float.Parse(mod.ConflictingItems[8]);
        }

        public static float AimSpeed(Mod mod)
        {
            return float.Parse(mod.ConflictingItems[9]);
        }

        public static float DrawSpeed(Mod mod)
        {
            return float.Parse(mod.ConflictingItems[10]);
        }

        public static float Length(Mod mod)
        {
            return float.Parse(mod.ConflictingItems[11]);
        }

        public static bool CanCylceSubs(Mod mod)
        {
            return bool.Parse(mod.ConflictingItems[12]);
        }

        public static float RecoilAngle(Mod mod)
        {
            return float.Parse(mod.ConflictingItems[13]);
        }

        public static bool StockAllowADS(Mod mod)
        {
            return bool.Parse(mod.ConflictingItems[14]);
        }

        public static float FixSpeed(Mod mod)
        {
            return float.Parse(mod.ConflictingItems[15]);
        }

        public static float ModShotDispersion(Mod mod)
        {
            return float.Parse(mod.ConflictingItems[16]);
        }
    }
}
