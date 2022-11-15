using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Silencer = GClass2118;
using FlashHider = GClass2115;
using MuzzleCombo = GClass2116;
using Barrel = GClass2138;
using Mount = GClass2134;
using Receiver = GClass2141;
using Stock = GClass2136;
using Charge = GClass2129;
using CompactCollimator = GClass2122;
using Collimator = GClass2121;
using AssaultScope = GClass2120;
using Scope = GClass2124;
using IronSight = GClass2123;
using SpecialScope = GClass2125;
using AuxiliaryMod = GClass2104;
using Foregrip = GClass2108;
using PistolGrip = GClass2140;
using Gasblock = GClass2109;
using Handguard = GClass2139;
using Bipod = GClass2106;
using Flashlight = GClass2107;
using TacticalCombo = GClass2112;


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

        public static void SafelyAddAttributeToList(GClass2210 itemAttribute, Mod __instance)
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
