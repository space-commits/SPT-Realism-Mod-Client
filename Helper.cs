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


using Silencer = GClass2106;
using FlashHider = GClass2103;
using MuzzleCombo = GClass2104;
using Barrel = GClass2126;
using Mount = GClass2122;
using Receiver = GClass2129;
using Stock = GClass2124;
using Charge = GClass2117;
using CompactCollimator = GClass2110;
using Collimator = GClass2109;
using AssaultScope = GClass2108;
using Scope = GClass2112;
using IronSight = GClass2111;
using SpecialScope = GClass2113;
using Magazine = MagazineClass;
using AuxiliaryMod = GClass2092;
using Foregrip = GClass2096;
using PistolGrip = GClass2128;
using Gasblock = GClass2097;
using Handguard = GClass2127;
using Bipod = GClass2094;
using Flashlight = GClass2095;
using TacticalCombo = GClass2100;
using System.Collections;

namespace RealismMod
{

    public static class Helper
    {
        public static bool isReloading = false;

        public static bool IsInReloadOperation = false;


        public static bool isReady()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            var sessionResultPanel = Singleton<SessionResultPanel>.Instance;

            if (gameWorld == null || gameWorld.AllPlayers == null || gameWorld.AllPlayers.Count <= 0 || sessionResultPanel != null)
            {
                return false;
            }
            return true;
        }

        public static void SafelyAddAttributeToList(GClass2197 itemAttribute, Mod __instance)
        {
            if (itemAttribute.Base() != 0f)
            {
                __instance.Attributes.Add(itemAttribute);
            }
        }


        public static bool isNotStock(Mod mod)
        {
            return !(mod is Stock);
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
