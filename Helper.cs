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


        public static bool IsAllowedAim = true;

        public static bool IsAttemptingToReloadInternalMag = false;

        public static bool IsMagReloading = false;

        public static bool IsInReloadOpertation = false;

        public static bool noMagazineReload = false;

        public static bool isReady = false;


        public static bool nullCheck(string[] confItemArray)
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

        public static bool checkIsReady()
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

        public static bool isStock(Mod mod)
        {
            return (mod is Stock);
        }

        public static bool isSilencer(Mod mod)
        {
            return (mod is Silencer);
        }

        public static bool isMagazine(Mod mod)
        {
            return (mod is MagazineClass);
        }

        public static bool isFlashHider(Mod mod)
        {
            return (mod is FlashHider);
        }
        public static bool isMuzzleCombo(Mod mod)
        {
            return (mod is MuzzleCombo);
        }
        public static bool isBarrel(Mod mod)
        {
            return (mod is Barrel);
        }
        public static bool isMount(Mod mod)
        {
            return (mod is Mount);
        }
        public static bool isReceiver(Mod mod)
        {
            return (mod is Receiver);
        }
        public static bool isCharge(Mod mod)
        {
            return (mod is Charge);
        }
        public static bool isCompactCollimator(Mod mod)
        {
            return (mod is CompactCollimator);
        }
        public static bool isCollimator(Mod mod)
        {
            return (mod is Collimator);
        }
        public static bool isAssaultScope(Mod mod)
        {
            return (mod is AssaultScope);
        }
        public static bool isScope(Mod mod)
        {
            return (mod is Scope);
        }
        public static bool isIronSight(Mod mod)
        {
            return (mod is IronSight);
        }
        public static bool isSpecialScope(Mod mod)
        {
            return (mod is SpecialScope);
        }
        public static bool isAuxiliaryMod(Mod mod)
        {
            return (mod is AuxiliaryMod);
        }
        public static bool isForegrip(Mod mod)
        {
            return (mod is Foregrip);
        }
        public static bool isPistolGrip(Mod mod)
        {
            return (mod is PistolGrip);
        }
        public static bool isGasblock(Mod mod)
        {
            return (mod is Gasblock);
        }
        public static bool isHandguard(Mod mod)
        {
            return (mod is Handguard);
        }
        public static bool isBipod(Mod mod)
        {
            return (mod is Bipod);
        }
        public static bool isFlashlight(Mod mod)
        {
            return (mod is Flashlight);
        }
        public static bool isTacticalCombo(Mod mod)
        {
            return (mod is TacticalCombo);
        }

    }
}
