using EFT;
using System;
using UnityEngine;
using System.Linq;
using Comfort.Common;
using System.Reflection;
using EFT.InventoryLogic;
using System.Threading.Tasks;
using Aki.Reflection.Patching;



namespace RealismMod
{
    internal struct PlayerInfo
    {
        internal static GameWorld gameWorld
        {
            get
            {
                return Singleton<GameWorld>.Instance;
            }
        }

        internal static Player.FirearmController FC
        {
            get
            {
                return player.HandsController as Player.FirearmController;
            }
        }

        internal static Player player
        {
            get
            {
                return gameWorld.AllPlayers[0];
            }
        }

        internal static Player audio
        {
            get
            {
                return gameWorld.AllPlayers[0];
            }
        }

        internal static bool PlayerHasEarPro() => player.Profile.Inventory.Equipment.GetSlot(EquipmentSlot.Earpiece).ContainedItem != null || player.Profile.Inventory.Equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem != null && (player.Profile.Inventory.Equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem as LootItemClass).Slots.Any(item => item.ContainedItem != null && item.ContainedItem.GetItemComponent<SlotBlockerComponent>() != null && !item.ContainedItem.GetItemComponent<SlotBlockerComponent>().ConflictingSlotNames.Contains("Earpiece")) || player.Profile.Inventory.Equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem != null && player.Profile.Inventory.Equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem.GetItemComponent<SlotBlockerComponent>() != null && player.Profile.Inventory.Equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem.GetItemComponent<SlotBlockerComponent>().ConflictingSlotNames.Contains("Earpiece");
    }

}