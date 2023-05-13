using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using EFT;
using UnityEngine;

namespace RealismMod
{

    public static class ModdingUtils 
    {
        public static string[] disallowedSlots = { "FirstPrimaryWeapon", "SecondPrimaryWeapon", "Holster" };

        public static Weapon GetParentWeapon(Item item)
        {
            if (item != null)
            {
                IEnumerable<Item> parents = item.GetAllParentItems();

                foreach (Item i in parents)
                {
                    if (i is Weapon)
                    {
                        return i as Weapon;
                    }
                }
            }
            return null;
        }

        private static bool checkId(string id)
        {
            return id == "544fb5454bdc2df8738b456a";
        }

        public static bool hasTool(EquipmentClass equipment)
        {
            if (equipment != null)
            {
                IEnumerable<Slot> slots = equipment.GetAllSlots();

                foreach (Slot slot in slots)
                {
                    Item slotItem = slot.ContainedItem;
                    if (slotItem != null)
                    {
                        bool isContainer = slotItem.IsContainer;
                        if (isContainer)
                        {
                            IEnumerable<Item> items = slotItem.GetAllItems();
                            foreach (Item item in items)
                            {
                                if (checkId(item.TemplateId))
                                {
                                    return true;
                                }
                            }
                        }
                        else
                        {
                            if (checkId(slotItem.TemplateId))
                            {
                                return true;
                            }
                        }
                    }

                }
            }
            return false;
        }
    }


    public class LootItemClassPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(LootItemClass).GetMethod("Apply", BindingFlags.Instance | BindingFlags.Public);

        }

        private static bool isModable(bool inRaid, Mod mod) 
        {
            if (inRaid) 
            {
                Weapon weapon = ModdingUtils.GetParentWeapon(mod);
                Player player = Utils.GetPlayer();
                EquipmentClass equipment = (EquipmentClass)AccessTools.Property(typeof(Player), "Equipment").GetValue(player);
                if (equipment != null && player != null && weapon != null && (ModdingUtils.disallowedSlots.Contains(weapon.Parent.Container.ID) || !ModdingUtils.hasTool(equipment)))
                {
                    return false;
                }
            }
            return true; 
        }

        [PatchPrefix]
        private static bool Prefix(LootItemClass __instance, TraderControllerClass itemController, Item item, int count, bool simulate, ref GStruct324 __result)
        {

            Logger.LogWarning("ApplyPatch");

            if (!item.ParentRecursiveCheck(__instance))
            {
                __result = new GClass2855(item, __instance);
                return false;
            }
            bool inRaid = GClass1757.InRaid;
            GClass2823 gclass = null;
            GClass2823 gclass2 = null;
            Mod mod = item as Mod;
            Slot[] array = (mod != null && inRaid) ? Enumerable.ToArray<Slot>(__instance.VitalParts) : null;
            Slot.GClass2866 gclass3;

            if (inRaid && mod != null && !mod.RaidModdable && !isModable(inRaid, mod))
            {
                Logger.LogWarning("not raid moddable and no tool");
                gclass2 = new GClass2852(mod);
            }
            else if (!GClass2429.CheckMissingParts(mod, __instance.CurrentAddress, itemController, out gclass3))
            {
                Logger.LogWarning("CheckMissingParts");
                gclass2 = gclass3;
            }
            bool flag = false;
            foreach (Slot slot in __instance.AllSlots)
            {
                if ((gclass2 == null || !flag) && slot.CanAccept(item))
                {
                    Logger.LogWarning("gclass 2 is null or flag is false and slot can accept item");
                    if (gclass2 != null)
                    {
                        Logger.LogWarning("gclass 2 not null");
                        Slot.GClass2866 gclass4;
                        if ((gclass4 = (gclass2 as Slot.GClass2866)) != null)
                        {
                            Logger.LogWarning("Slot.GClass2866");
                            gclass2 = new Slot.GClass2866(gclass4.Item, slot, gclass4.MissingParts);
                        }
                        Logger.LogWarning("flag true");
                        flag = true;
                    }
                    else if (array != null && Enumerable.Contains<Slot>(array, slot) )
                    {
                        Logger.LogWarning("gclass 2 null and slot array not null and contains vital parts");
                        if (inRaid && isModable(inRaid, mod))
                        {
                            Logger.LogWarning("Has tool and is moddable, attempting to move");
                            GClass2422 to = new GClass2422(slot);
                            GStruct325<GClass2441> value = GClass2429.Move(item, to, itemController, simulate);
                            if (value.Succeeded)
                            {
                                Logger.LogWarning("move succeeeded");
                                __result = value;
                                return false;
                            }
                        }

                        Logger.LogWarning("array contains vital parts and no tool");
                        gclass = new GClass2853(mod);
                    }
                    else
                    {
                        Logger.LogWarning("gclass 2 null and no vital parts in array or array is null");
                        GClass2422 to = new GClass2422(slot);
                        GStruct325<GClass2441> value = GClass2429.Move(item, to, itemController, simulate);
                        if (value.Succeeded)
                        {
                            Logger.LogWarning("Move");
                            __result = value;
                            return false;
                        }
                        GStruct325<GClass2450> value2 = GClass2429.SplitMax(item, int.MaxValue, to, itemController, itemController, simulate);
                        if (value2.Succeeded)
                        {
                            Logger.LogWarning("SplitMax");
                            __result = value2;
                            return false;
                        }
                        gclass = value.Error;
                        if (!GClass770.DisabledForNow && GClass2430.CanSwap(item, slot))
                        {
                            Logger.LogWarning("ambiguous, CanSwap");
                            //ambiguous refernce, wrong ref might bug it out
                            __result = new GStruct324((GInterface265)null);
                            return false;
                        }
                    }
                }
            }
            if (!flag)
            {
                Logger.LogWarning("!flag");
                gclass2 = null;
            }
            GStruct325<GInterface266> value3 = GClass2429.QuickFindAppropriatePlace(item, itemController, __instance.ToEnumerable<LootItemClass>(), GClass2429.EMoveItemOrder.Apply, simulate);
            if (value3.Succeeded)
            {
                Logger.LogWarning("QuickFindAppropriatePlace");
                __result = value3;
                return false;
            }
            if (!(value3.Error is GClass2848))
            {
                Logger.LogWarning("value3.Error");
                gclass = value3.Error;
            }
            GClass2823 error;
            if ((error = gclass2) == null)
            {
                Logger.LogWarning("GClass2823");
                error = (gclass ?? new GClass2855(item, __instance));
            }
            Logger.LogWarning("end");
            __result = error;
            return false;
        }
    }

    public class Smethod : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass2429).GetMethod("smethod_1", BindingFlags.Static | BindingFlags.NonPublic);

        }

        

        [PatchPrefix]
        private static bool Prefix(ref GStruct326<GClass2897> __result, Item item, ItemAddress to, TraderControllerClass itemController)
        {
            Logger.LogWarning("smethod_1");
            if (to.Container.ID == "Dogtag")
            {
                __result = new GClass2896("Cannot move to Dogtag slot");
                return false;
            }
            if (GClass1757.InRaid)
            {
                Logger.LogWarning("In Raid checks");
                Player player = Utils.GetPlayer();
                EquipmentClass equipment = (EquipmentClass)AccessTools.Property(typeof(Player), "Equipment").GetValue(player);
                Mod mod = item as Mod;
                Weapon weapon = ModdingUtils.GetParentWeapon(mod);

                if (equipment != null && ModdingUtils.hasTool(equipment)) 
                {
                    if (weapon != null && !ModdingUtils.disallowedSlots.Contains(weapon.Parent.Container.ID))
                    {
                        Logger.LogWarning("allowed1");
                        __result = GClass2897._;
                        return false;
                    }

                    if (weapon == null)
                    {
                        Logger.LogWarning("allowed2");
                        __result = GClass2897._;
                        return false;
                    }
                }

                if ((mod = (item as Mod)) != null && !mod.RaidModdable && (to is GClass2422 || item.Parent is GClass2422))
                {
                    Logger.LogWarning("is not raid modable");
                    __result = new GClass2852(mod);
                    return false;
                }
                GClass2422 gclass;
                if (((gclass = (to as GClass2422)) != null  || (gclass = (item.Parent as GClass2422)) != null) && gclass.Slot.Required)
                {
                    Logger.LogWarning("required slot and address or parent = GClass2422");
                    __result = new GClass2854(gclass.Slot);
                    return false;
                }
            }
            if (item is LootItemClass && item.Parent is GClass2422)
            {
                Logger.LogWarning("item is lootitemclass and parent is gclass 2422");
                CantRemoveFromSlotsDuringRaidComponent itemComponent = item.GetItemComponent<CantRemoveFromSlotsDuringRaidComponent>();
                IContainer container = item.Parent.Container;
                if (itemComponent != null && !(container is IItemOwner) && container.ParentItem is EquipmentClass && !itemComponent.CanRemoveFromSlotDuringRaid(container.ID))
                {
                    __result = new GClass2429.GClass2888(item, container.ID);
                    return false;
                }
            }
            GStruct326<bool> gstruct = GClass2429.DestinationCheck(item.Parent, to, itemController.OwnerType);
            if (gstruct.Failed)
            {
                Logger.LogWarning("gstruct.Failed");
                __result = gstruct.Error;
            }
            Logger.LogWarning("end");
            __result = GClass2897._;
            return false;
        }
    }


    public class RaidmoddalbePatch : ModulePatch
    {
        private static Type _targetType;
        private static MethodInfo _targetMethod;

        public RaidmoddalbePatch()
        {
            _targetType = AccessTools.TypeByName("ItemSpecificationPanel");
            _targetMethod = AccessTools.Method(_targetType, "method_17");
        }

        protected override MethodBase GetTargetMethod()
        {
            return _targetMethod;
        }

        private static bool checkSlot(Slot slot, List<string> itemList, Item item)
        {
            if (!GClass1757.InRaid)
            {
                return false;
            }
            if ((slot.ID.StartsWith("chamber") || slot.ID.StartsWith("patron_in_weapon")) && item is Weapon)
            {
                return !itemList.Contains(item.Id);
            }
            Weapon weapon;
            return slot.ID.StartsWith("camora") && (weapon = (item as Weapon)) != null && weapon.GetCurrentMagazine() is CylinderMagazineClass && !itemList.Contains(item.Id);
        }

        [PatchPrefix]
        private static bool Prefix(ref KeyValuePair<EModLockedState, string> __result, Slot slot, Item ___item_0, InventoryControllerClass ___gclass2417_0, List<string> ___list_0)
        {
            string text = (slot.ContainedItem != null) ? slot.ContainedItem.Name.Localized(null) : string.Empty;
            Mod mod;

            //in raid = unlocked
            //in raid + multi tool + not in equipment slot = unlocked & change mod to raidmoddable? patch method that gets the raidmoddable value and modify it based on presence of multi-tool?
            //out of raid = unlocked, but if you spawn in raid with it in hands it gets thrown out of hands

            //problem with out of raid is that if I unlock the slot, some other method determines if the item can be removed, I need to find that method, or do the discard workaround
            //I will need the discard workaround if I allow tools to make items raid-moddable or not vital
            //I need to find the method that determines if I can attach/detatch items in vital slot?



            /*            if (GClass1757.InRaid && ___gclass2417_0.IsItemEquipped(___item_0))
                        {
                            __result = new KeyValuePair<EModLockedState, string>(EModLockedState.RaidLock, "<color=red>" + "Raid lock".Localized(null) + "</color>\n\n" + text);
                            return false;
                        }*/
            /*            if (GClass1757.InRaid && (((mod = (slot.ContainedItem as Mod)) != null && !mod.RaidModdable) || Enumerable.Contains<Slot>(((LootItemClass)___item_0).VitalParts, slot)))
                        {
                            __result = new KeyValuePair<EModLockedState, string>(EModLockedState.RaidLock, "<color=red>" + "Raid lock".Localized(null) + "</color>\n\n" + text);
                            return false;
                        }
                        if (!GClass1757.InRaid && Enumerable.Contains<Slot>(((LootItemClass)___item_0).VitalParts, slot) && ___gclass2417_0.IsItemEquipped(___item_0))
                        {
                            __result = new KeyValuePair<EModLockedState, string>(EModLockedState.RaidLock, "<color=red>" + "Vital mod weapon in hands".Localized(null) + "</color>\n\n" + text);
                            return false;
                        }*/
            if (!checkSlot(slot, ___list_0, ___item_0))
            {
                __result = new KeyValuePair<EModLockedState, string>(EModLockedState.Unlocked, text);
                return false;
            }
            if (slot.ID.StartsWith("camora"))
            {
                __result = new KeyValuePair<EModLockedState, string>(EModLockedState.ChamberUnchecked, "<color=#d77400>" + "You need to check the revolver drum".Localized(null) + "</color>");
                return false;
            }
            __result = new KeyValuePair<EModLockedState, string>(EModLockedState.ChamberUnchecked, "<color=#d77400>" + "You need to check chamber in weapon".Localized(null) + "</color>");
            return false;
        }
    }

    public class CanBeMovedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Mod).GetMethod("CanBeMoved", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(ref Mod __instance, ref GStruct326<bool> __result, IContainer toContainer)
        {

            Logger.LogWarning("CanBeMoved patch");

            if (!GClass1757.InRaid)
            {
                __result = true;
                return false;
            }
            else 
            {
                Logger.LogWarning("In Raid checks");
                Player player = Utils.GetPlayer();
                EquipmentClass equipment = (EquipmentClass)AccessTools.Property(typeof(Player), "Equipment").GetValue(player);
                Weapon weapon = ModdingUtils.GetParentWeapon(__instance);
                if (weapon == null) 
                {
                    Logger.LogWarning("weapon is null");

                }
                else 
                {
                    Logger.LogWarning("weapon = " + weapon.LocalizedName());
                    Logger.LogWarning("container = " + weapon.Parent.Container.ID);

                }


                if (equipment != null && ModdingUtils.hasTool(equipment)) 
                {
                    if (weapon != null && !ModdingUtils.disallowedSlots.Contains(weapon.Parent.Container.ID))
                    {
                        Logger.LogWarning("not allowd, is in hands");
                        __result = new GClass2852(__instance);
                        return false;
                    }
                    if (weapon == null) 
                    {
                        Logger.LogWarning("allowed4");
                        __result = true;
                        return false;
                    }
                }
            }

            Slot slot = toContainer as Slot;
            if (slot != null)
            {
                if (!__instance.RaidModdable)
                {
                    Logger.LogWarning("not raid moddable");
                    __result = new GClass2852(__instance);
                    return false;
                }
                if (slot.Required)
                {
                    Logger.LogWarning("vital part");
                    __result = new GClass2854(slot);
                    return false;
                }
            }
            __result = true;
            return false;
        }
    }
}
