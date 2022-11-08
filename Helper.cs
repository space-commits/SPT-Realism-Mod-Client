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


using Silencer = GClass2112;
using FlashHider = GClass2109;
using MuzzleCombo = GClass2110;
using Barrel = GClass2132;
using Mount = GClass2128;
using Receiver = GClass2135;
using Stock = GClass2130;
using Charge = GClass2123;
using CompactCollimator = GClass2116;
using Collimator = GClass2115;
using AssaultScope = GClass2114;
using Scope = GClass2118;
using IronSight = GClass2117;
using SpecialScope = GClass2119;
using Magazine = MagazineClass;
using AuxiliaryMod = GClass2098;
using Foregrip = GClass2102;
using PistolGrip = GClass2134;
using Gasblock = GClass2103;
using Handguard = GClass2133;
using Bipod = GClass2100;
using Flashlight = GClass2101;
using TacticalCombo = GClass2106;
using System.Collections;
using Aki.Reflection.Utils;
using System.Threading.Tasks;

namespace RealismMod
{
    public static class Helper
    {

        public static bool ProgramKEnabled = false;

        public static bool IsAllowedAim = true;

        public static bool IsAttemptingToReloadInternalMag = false;

        public static bool IsMagReloading = false;

        public static bool IsInReloadOpertation = false;

        public static bool NoMagazineReload = false;

        public static bool IsAttemptingRevolverReload = false;

        public static bool IsReady = false;


        public static bool NullCheck(string[] confItemArray)
        {
            if (confItemArray != null && confItemArray.Length > 0)
            {
                if (confItemArray[0] == "SPTRM") // if the array has SPTRM, but is set up incorrectly, it will probably cause null errors
                {
                    return false;
                }
            }
            return true;
        }

        public static bool CheckIsReady()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            var sessionResultPanel = Singleton<SessionResultPanel>.Instance;

            if (gameWorld == null || gameWorld.AllPlayers == null || gameWorld.AllPlayers.Count <= 0 || sessionResultPanel != null)
            {
                return false;
            }
            return true;
        }

        public static void SafelyAddAttributeToList(GClass2203 itemAttribute, Mod __instance)
        {
            if (itemAttribute.Base() != 0f)
            {
                __instance.Attributes.Add(itemAttribute);
            }
        }

        public static bool IsSight(Mod mod)
        {
            if (mod is Scope || mod is AssaultScope || mod is Collimator || mod is CompactCollimator || mod is IronSight || mod is SpecialScope) {
                return true;
            }
            return false;
        }

        public static bool IsStock(Mod mod)
        {
            return (mod is Stock);
        }

        public static bool IsSilencer(Mod mod)
        {
            return (mod is Silencer);
        }

        public static bool IsMagazine(Mod mod)
        {
            return (mod is MagazineClass);
        }

        public static bool IsFlashHider(Mod mod)
        {
            return (mod is FlashHider);
        }
        public static bool IsMuzzleCombo(Mod mod)
        {
            return (mod is MuzzleCombo);
        }
        public static bool IsBarrel(Mod mod)
        {
            return (mod is Barrel);
        }
        public static bool IsMount(Mod mod)
        {
            return (mod is Mount);
        }
        public static bool IsReceiver(Mod mod)
        {
            return (mod is Receiver);
        }
        public static bool IsCharge(Mod mod)
        {
            return (mod is Charge);
        }
        public static bool IsCompactCollimator(Mod mod)
        {
            return (mod is CompactCollimator);
        }
        public static bool IsCollimator(Mod mod)
        {
            return (mod is Collimator);
        }
        public static bool IsAssaultScope(Mod mod)
        {
            return (mod is AssaultScope);
        }
        public static bool IsScope(Mod mod)
        {
            return (mod is Scope);
        }
        public static bool IsIronSight(Mod mod)
        {
            return (mod is IronSight);
        }
        public static bool IsSpecialScope(Mod mod)
        {
            return (mod is SpecialScope);
        }
        public static bool IsAuxiliaryMod(Mod mod)
        {
            return (mod is AuxiliaryMod);
        }
        public static bool IsForegrip(Mod mod)
        {
            return (mod is Foregrip);
        }
        public static bool IsPistolGrip(Mod mod)
        {
            return (mod is PistolGrip);
        }
        public static bool IsGasblock(Mod mod)
        {
            return (mod is Gasblock);
        }
        public static bool IsHandguard(Mod mod)
        {
            return (mod is Handguard);
        }
        public static bool IsBipod(Mod mod)
        {
            return (mod is Bipod);
        }
        public static bool IsFlashlight(Mod mod)
        {
            return (mod is Flashlight);
        }
        public static bool IsTacticalCombo(Mod mod)
        {
            return (mod is TacticalCombo);
        }

    }
}
