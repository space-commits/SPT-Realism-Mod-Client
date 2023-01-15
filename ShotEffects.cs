using EFT;
using System;
using UnityEngine;
using System.Linq;
using Comfort.Common;
using System.Reflection;
using EFT.InventoryLogic;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using static Val;
using HarmonyLib;
using Aki.Reflection.Utils;
using UnityEngine.Rendering.PostProcessing;

namespace RealismMod
{
    public class PlayerState
    {
        public static GameWorld Gameworld
        {
            get
            {
                return Singleton<GameWorld>.Instance;
            }
        }

        public static Player.FirearmController FC
        {
            get
            {
                return Player.HandsController as Player.FirearmController;
            }
        }

        public static Player Player
        {
            get
            {
                return Gameworld.AllPlayers[0];
            }
        }

        public static bool CheckIsReady()
        {
            var sessionResultPanel = Singleton<SessionResultPanel>.Instance;

            if (Gameworld == null || Gameworld.AllPlayers == null || Gameworld.AllPlayers.Count <= 0 || sessionResultPanel != null)
            {
                return false;
            }
            return true;
        }

    }

    public class VignettePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EffectsController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void PatchPostfix(EffectsController __instance)
        {
            CC_FastVignette vig = (CC_FastVignette)AccessTools.Field(typeof(EffectsController), "cc_FastVignette_0").GetValue(__instance);
            Plugin.Vignette = vig;
        }
    }

    public class UpdatePhonesPatch : ModulePatch
    {

        private static float HelmDeafFactor(EquipmentClass equipment)
        {
            string deafStrength = "None";
            if (equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem != null)
            {
                var helmet = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem as ArmorClass;
                deafStrength = helmet.Armor.Deaf.ToString();

            }
            return HelmDeafFactorHelper(deafStrength);
        }

        private static float HelmDeafFactorHelper(string deafStr)
        {
            switch (deafStr)
            {
                case "Low":
                    return 0.95f;
                case "High":
                    return 0.9f;
                default:
                    return 1f;
            }
        }

        private static float HeadsetDeafFactor(EquipmentClass equipment)
        {
            float deafenFactor = 1f;

            LootItemClass headwear = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem as LootItemClass;
            GClass2108 headset = (equipment.GetSlot(EquipmentSlot.Earpiece).ContainedItem as GClass2108) ?? ((headwear != null) ? headwear.GetAllItemsFromCollection().OfType<GClass2108>().FirstOrDefault<GClass2108>() : null);

            if (headset != null)
            {
                GClass2015 headphone = headset.Template;
                deafenFactor = (headphone.DryVolume / 100f) + 1;
                Plugin.MainVolume = headphone.DryVolume;
                Plugin.GunsVolume = headphone.DryVolume;
                Plugin.CompressorDistortion = headphone.Distortion;
                Plugin.CompressorResonance = headphone.Resonance;
                Plugin.CompressorLowpass = headphone.LowpassFreq;
                Plugin.Compressor = headphone.CompressorVolume;
            }
            else
            {
                deafenFactor = 1f;
            }

            return deafenFactor;
        }


        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("UpdatePhones", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance)
        {
            if (__instance.IsYourPlayer)
            {
                EquipmentClass equipment = (EquipmentClass)AccessTools.Property(typeof(Player), "Equipment").GetValue(__instance);
                Plugin.EarProtectionFactor = HeadsetDeafFactor(equipment) * HelmDeafFactor(equipment);
            }

        }
    }

}
