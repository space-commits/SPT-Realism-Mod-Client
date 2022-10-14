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


    public static class DisplayWeaponProperties
    {
        public static float ErgoDelta = 0;

        public static int AutoFireRate = 0;

        public static int SemiFireRate = 0;

        public static float Balance = 0;

        public static float VRecoilDelta = 0;

        public static float HRecoilDelta = 0;

        public static bool HasShoulderContact = true;

        public static float COIDelta = 0;

        public static float CamRecoil = 0;

        public static float Dispersion = 0;

        public static float RecoilAngle = 0;

        public static float TotalVRecoil = 0;

        public static float TotalHRecoil = 0;

        public static float TotalErgo = 0;

        public static float SDTotalErgo = 0;

        public static float SDTotalVRecoil = 0;

        public static float SDTotalHRecoil = 0;

        public static float SDBalance = 0;

        public static string SavedInstanceID = "";

        public static float PureErgoDelta = 0;

        public static float ErgnomicWeight = 0;


    }


    public static class WeaponProperties
    {

        public static string WeaponType(Weapon weapon)
        {
            if (Helper.nullCheck(weapon.ConflictingItems))
            {
                return "";
            }
            return weapon.ConflictingItems[1];
        }

        public static float BaseTorqueDistance(Weapon weapon)
        {
            if (Helper.nullCheck(weapon.ConflictingItems))
            {
                return 0;
            }
            return float.Parse(weapon.ConflictingItems[2]);
        }

        public static bool WepHasShoulderContact(Weapon weapon)
        {
            if (Helper.nullCheck(weapon.ConflictingItems))
            {
                return false;
            }
            return bool.Parse(weapon.ConflictingItems[3]);
        }

        public static string InstanceID(Weapon weapon)
        {
            if (Helper.nullCheck(weapon.ConflictingItems))
            {
                return "";
            }
            return weapon.ConflictingItems[4];
        }

        public static string OperationType(Weapon weapon)
        {
            if (Helper.nullCheck(weapon.ConflictingItems))
            {
                return "";
            }
            return weapon.ConflictingItems[5];
        }

        public static float WeaponAccuracy(Weapon weapon)
        {
            if (Helper.nullCheck(weapon.ConflictingItems))
            {
                return 0;
            }
            return float.Parse(weapon.ConflictingItems[6]);
        }

        public static float RecoilDamping(Weapon weapon)
        {
            if (Helper.nullCheck(weapon.ConflictingItems))
            {
                return 0.7f;
            }
            return float.Parse(weapon.ConflictingItems[7]);
        }

        public static float RecoilHandDamping(Weapon weapon)
        {
            if (Helper.nullCheck(weapon.ConflictingItems))
            {
                return 0.65f;
            }
            return float.Parse(weapon.ConflictingItems[8]);
        }

        public static bool WeaponAllowsADS(Weapon weapon)
        {
            if (Helper.nullCheck(weapon.ConflictingItems))
            {
                return false;
            }
            return bool.Parse(weapon.ConflictingItems[9]);
        }

        public static float AdapterPistolGripBonusVRecoil = -1;

        public static float AdapterPistolGripBonusHRecoil = -2;

        public static float AdapterPistolGripBonusDispersion = -1;

        public static float AdapterPistolGripBonusErgo = 2;

        public static float PumpGripReloadBonus = 10f;

        public static float FoldedErgoFactor = 0.48f;

        public static float FoldedHRecoilFactor = 1.25f;

        public static float FoldedVRecoilFactor = 1.25f;

        public static float FoldedCOIFactor = 0.05f;

        public static float FoldedCamRecoilFactor = 0.35f;

        public static float FoldedDispersionFactor = 1.45f;

        public static float FoldedRecoilAngleFactor = 1.55f;

        public static float ErgoStatFactor = 7f;

        public static float RecoilStatFactor = 3.5f;

        public static float ErgoDelta = 0;

        public static int AutoFireRate = 0;

        public static int SemiFireRate = 0;

        public static float Balance = 0;

        public static float VRecoilDelta = 0;

        public static float HRecoilDelta = 0;

        public static bool HasShoulderContact = true;

        public static float COIDelta = 0;

        public static float CamRecoil = 0;

        public static float Dispersion = 0;

        public static float RecoilAngle = 0;

        public static float TotalVRecoil = 0;

        public static float TotalHRecoil = 0;

        public static float TotalErgo = 0;

        public static float SDTotalErgo = 0;

        public static float SDTotalVRecoil = 0;

        public static float SDTotalHRecoil = 0;

        public static float SDBalance = 0;

        public static float SDCamRecoil = 0;

        public static float SDDispersion = 0;

        public static float SDRecoilAngle = 0;

        public static string SavedInstanceID = "";

        public static float PureErgoDelta = 0;

        public static string Placement = "";

        public static float ErgnomicWeight = 0;

        public static float ADSDelta = 0;

        public static float TotalRecoilDamping;

        public static float TotalRecoilHandDamping;

        public static bool WeaponCanFSADS = false;

        public static bool Folded = false;

        public static float SDReloadSpeedModifier = 0f;

        public static float SDFixSpeedModifier = 0f;

        public static float ReloadSpeedModifier = 0f;

        public static float FixSpeedModifier = 0f;

        public static float AimMoveSpeedModifier = 0f;

        public static float AimSpeedModifier = 0f;

        public static float sensChangeRate = 0.83f;
        public static float sensResetRate = 1.07f;
        public static float sensLimit = 0.1f;

        public static float convergenceChangeRate = 0.98f;
        public static float convergenceResetRate = 1.16f;
        public static float convergenceLimit = 0.3f;

        public static float camRecoilChangeRate = 0.987f;
        public static float camRecoilResetRate = 1.17f;
        public static float camRecoilLimit = 0.45f;

        public static float vRecoilChangeRate = 1.005f;
        public static float vRecoilResetRate = 0.91f;
        public static float vRecoilLimit = 10;

        public static float dampingChangeRate = 0.98f;
        public static float dampingResetRate = 1.07f;
        public static float dampingLimit = 0.5f;

        public static float dispersionChangeRate = 0.95f;
        public static float dispersionResetRate = 1.05f;
        public static float dispersionLimit = 0.5f;
    }
}
