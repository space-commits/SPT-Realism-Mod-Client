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

/*        public float calcEarProtection()
        {
            float protection = 0;
            float helmReduction = 0;
            if (Player.Profile.Inventory.Equipment.GetSlot(EquipmentSlot.Earpiece).ContainedItem != null)
            {
                protection = float.Parse(Player.Profile.Inventory.Equipment.GetSlot(EquipmentSlot.Earpiece).ContainedItem.ConflictingItems[1]);
            }

            if (Player.Profile.Inventory.Equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem != null)
            {
                var helmet = (ArmorComponent)Player.Profile.Inventory.Equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem;
            }

             (Player.Profile.Inventory.Equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem as LootItemClass).Slots.Any(item => item.ContainedItem != null 
             && item.ContainedItem.GetItemComponent<SlotBlockerComponent>() != null 
             && !item.ContainedItem.GetItemComponent<SlotBlockerComponent>().ConflictingSlotNames.Contains("Earpiece")) 

             || Player.Profile.Inventory.Equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem.GetItemComponent<SlotBlockerComponent>() != null 
             && Player.Profile.Inventory.Equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem.GetItemComponent<SlotBlockerComponent>().ConflictingSlotNames.Contains("Earpiece");


            return protection;

        }
*/


        public bool CheckIsReady()
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
            Plugin.vignette = vig;
        }

    }

}
