using EFT;
using EFT.Communications;
using EFT.InventoryLogic;
using EFT.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ArmorTemplate = ArmoredEquipmentTemplateClass; //to find again, search for HasHinge field
using MoveOperation = GStruct455<GClass3203>;

namespace RealismMod
{
    public static class GearController
    {
        public const string SAFE_CONTAINER_ID = "66fd588d397ed74159826cf0";
        public static readonly string[] GasAnalysers = { "590a3efd86f77437d351a25b" };
        public static readonly string[] RadDevices = { "5672cb724bdc2dc2088b456b" };
        public static bool HasSafeContainer { get; set; } = false;
        public static bool HasGasMask { get; private set; } = false;
        public static bool HasGasFilter { get; private set; } = false;
        public static bool HasRespirator { get; private set; } = false;
        public static bool FSIsActive { get; set; } = false;
        public static bool GearBlocksMouth { get; set; } = false;
        public static bool NVGIsActive { get; set; } = false;
        public static bool HasGasAnalyser { get; set; } = false;
        public static bool HasGeiger { get; set; } = false;
        public static bool GearAllowsADS { get; set; } = true;
        public static float GearReloadMulti { get; set; } = 1f;

        public static EquipmentSlot[] MainInventorySlots =
        {
          EquipmentSlot.TacticalVest,
          EquipmentSlot.ArmBand,
          EquipmentSlot.Pockets,
          EquipmentSlot.Backpack,
          EquipmentSlot.SecuredContainer
        };

        public static bool HasGasMaskWithFilter
        {
            get
            {
                return HasGasMask && (HasGasFilter || HasRespirator);
            }
        }

        public static float CurrentGasProtection
        {
            get
            {
                float protection = _currentGasProtection * _gasMaskDurabilityFactor;
                return float.IsNaN(protection) ? 0f : protection;
            }
        }

        public static float CurrentRadProtection 
        {
            get
            {
                float protection = _currentRadProtection * _gasMaskDurabilityFactor;
                return float.IsNaN(protection) ? 0f : protection;
            }
        } 

        public static float GasMaskDurabilityFactor 
        {
            get 
            {

                return float.IsNaN(_gasMaskDurabilityFactor) ? 0f : _gasMaskDurabilityFactor;
            }
        }

        private static float _currentGasProtection;
        private static float _currentRadProtection;
        private static float _gasMaskDurabilityFactor;
        private static bool _hadGasMask = true;
        private static ItemAddress _gasMaskStartingAddress;


        private static Item FindGasMask(Inventory invClass)
        {
            IEnumerable<Item> mainSlots = invClass.GetItemsInSlots(new EquipmentSlot[] { EquipmentSlot.Pockets, EquipmentSlot.ArmBand, EquipmentSlot.TacticalVest }) ?? Enumerable.Empty<Item>();
            foreach (var item in mainSlots)
            {
                if (item == null || item?.TemplateId == null) continue;

                var gearStats = TemplateStats.GetDataObj<Gear>(TemplateStats.GearStats, item.TemplateId);
                if (gearStats.IsGasMask)
                {
                    return item;
                }
            }
            return null;
        }

        public static IEnumerable<StashGridClass> GetGrids(InventoryEquipment equipment)
        {
            Slot tacVestslot = equipment.GetSlot(EquipmentSlot.TacticalVest);
            Slot pocketSlot = equipment.GetSlot(EquipmentSlot.Pockets); //in case player has increaesed pocket size
            Slot armBandSlot = equipment.GetSlot(EquipmentSlot.ArmBand);
            VestItemClass vestItemClass = tacVestslot.ContainedItem as VestItemClass;
            PocketsItemClass pocketsItemClass = pocketSlot.ContainedItem as PocketsItemClass; 
            VestItemClass beltItemClass = armBandSlot.ContainedItem as VestItemClass;
            StashGridClass[] vestGrid = (vestItemClass != null) ? vestItemClass.Grids : new StashGridClass[0];
            StashGridClass[] pocketGrid = (pocketsItemClass != null) ? pocketsItemClass.Grids : new StashGridClass[0];
            StashGridClass[] beltGrid = (beltItemClass != null) ? beltItemClass.Grids : new StashGridClass[0];
            return pocketGrid.Concat(vestGrid).Concat(beltGrid);
        }

        //special slots don't use grids, they use slots
        public static IEnumerable<Slot> GetSlots(InventoryEquipment equipment)
        {
            Slot pocketSlot = equipment.GetSlot(EquipmentSlot.Pockets);
            PocketsItemClass pocketsItemClass = pocketSlot.ContainedItem as PocketsItemClass;
            Slot[] pocketGrid = (pocketsItemClass != null) ? pocketsItemClass.Slots : new Slot[0];
            return pocketGrid;
        }

        private static ItemAddress TryFindAddressForMask(Player player, Item item)
        {
            ItemAddress itemAddress = GetSlots(player.InventoryController.Inventory.Equipment)
                .Select(slot => {
                    slot.TryFindLocationForItem(item, out var address);
                    return address;
                })
                .FirstOrDefault(address => address != null)
                ?? GetGrids(player.InventoryController.Inventory.Equipment)
                    .Select(grid => grid.FindLocationForItem(item))
                    .FirstOrDefault(address => address != null);

            return itemAddress;


            /*       //this was a good way to figure out how the inventory is structured, keeping for that reason
                   foreach (var c in player.InventoryController.Inventory.Equipment.Containers)
                   {
                       foreach (var i in c.Items)
                       {
                           CompoundItem comp = i as CompoundItem;
                           if (comp != null)
                           {
                               foreach (var g in comp.Grids)
                               {
                                   Utils.Logger.LogWarning($"grid id {g.ID}");
                                   var location = g.FindLocationForItem(item);
                                   return g.CreateItemAddress(location.LocationInGrid);
                               }
                               foreach (var s in comp.Slots)
                               {
                                   if (s.IsSpecial && s.Items.Count() <= 0)
                                   {
                                       s.TryFindLocationForItem(item, out itemAddress);
                                       if (itemAddress != null) return itemAddress;
                                   }
                               }
                           }
                       }
                   }*/
        }

        private static void EquipGasMask(Item item, Player player)
        {
            ItemAddress itemAddress = player.InventoryController.FindSlotToPickUp(item);
            if (itemAddress != null)
            {
                MoveOperation operation = InteractionsHandlerClass.Move(item, itemAddress, player.InventoryController, true);
                if (operation.Succeeded)
                {
                    ItemUiContext.smethod_0(player.InventoryController, item, operation, null);
                    Gear gear = TemplateStats.GetDataObj<Gear>(TemplateStats.GearStats, item.TemplateId);
                    if (!string.IsNullOrWhiteSpace(gear.MaskToUse)) DoInteractionAnimation(player, EInteraction.FaceshieldOnGear);
                    return;
                }
                else 
                {
                    NotificationManagerClass.DisplayWarningNotification("Face Cover/Glasses Slot Not Empty");
                }
            }
        }

        private static bool TryRemoveGasMask(Item item, Player player, ItemAddress address)
        {
            MoveOperation operation = InteractionsHandlerClass.Move(item, address, player.InventoryController, true);
            if (operation.Succeeded)
            {
                ItemUiContext.smethod_0(player.InventoryController, item, operation, null);
                Gear gear = TemplateStats.GetDataObj<Gear>(TemplateStats.GearStats, item.TemplateId);
                if (!string.IsNullOrWhiteSpace(gear.MaskToUse)) DoInteractionAnimation(player, EInteraction.FaceshieldOffGear); //exclude respitator and such
                return true;
            }
            return false;
        }

        // using the stored address is bad, I need to instead store a reference to the slot or grid the gas mask was originally in, then check that grid or slot somehow to see if it's empty
        private static void RemoveGasMask(Item item, Player player)
        {
            ItemAddress address = _gasMaskStartingAddress;
            bool succeeded = true;
            //if stored address is null or the operation using stored address fails, try again with a different address
            if (address == null || !TryRemoveGasMask(item, player, address)) 
            {
                address = TryFindAddressForMask(player, item);
                if (address == null) return;
                succeeded = TryRemoveGasMask(item, player, address);
            }
            if (!succeeded) NotificationManagerClass.DisplayWarningNotification("Can't Find Space For Gas Mask");
        }

        public static void DoInteractionAnimation(Player player, EInteraction interaction)
        {
            player.MovementContext.PlayerAnimator.AnimatedInteractions.SetInteraction(interaction);
            player.OnAnimatedInteraction(interaction);
        }

        public static void ToggleGasMask(Player player)
        {
            bool animatorBusy = player.InventoryController.IsChangingWeapon || player.MovementContext.StationaryWeapon != null || player.MovementContext.IsAnimatorInteractionOn;
            if (PlayerState.IsSprinting || animatorBusy || PlayerState.IsInReloadOpertation) return; //toggling this while sprinting breaks shit
            var faceCoverSlot = player?.Inventory?.Equipment?.GetSlot(EquipmentSlot.FaceCover);
            if (faceCoverSlot != null)
            {
                var slotItem = faceCoverSlot?.ContainedItem;
                if (slotItem == null)
                {
                    var gasMask = FindGasMask(player.Inventory);
                    if (gasMask != null)
                    {
                        _gasMaskStartingAddress = gasMask.CurrentAddress;
                        EquipGasMask(gasMask, player);
                    }
                }
                else
                {
                    RemoveGasMask(slotItem, player);
                }
            }
        }

        private static void HandleGasMaskEffects(Player player, float gasProtection, float radProtection) 
        {
            if (HasGasMask)
            {
                _currentGasProtection = gasProtection;
                _currentRadProtection = radProtection;
                _hadGasMask = true;
/*                player.Say(EPhraseTrigger.OnBeingHurt, true, 0f, (ETagStatus)0, 100, false); //force to reset audio*/
                player.SpeechSource.SetLowPassFilterParameters(0.99f, ESoundOcclusionType.Obstruction, 1600, 5000, 1, true);
                player.Muffled = true;
            }
            else
            {
                _currentGasProtection = 0f;
                _currentRadProtection = 0f;
                player.SpeechSource.ResetFilters();
            }

            player.UpdateBreathStatus();
            player.UpdateMuffledState();
            player.SendVoiceMuffledState(player.Muffled);

            if (!HasGasMask && _hadGasMask && player.HealthStatus == ETagStatus.Dying)
            {
                player.Say(EPhraseTrigger.OnBreath, true, 0f, (ETagStatus)0, 100, false); //force to reset audio
                _hadGasMask = false;
            }
        }

        private static void DeviceCheckerHelper(IEnumerable<Item> items) 
        {
            foreach (var item in items)
            {
                if (item == null || item?.TemplateId == null) continue;

                if (Array.Exists(GasAnalysers, i => i == item.TemplateId)) HasGasAnalyser = true;
                if (Array.Exists(RadDevices, i => i == item.TemplateId)) HasGeiger = true;
            }
        }

        public static void CheckForDevices(Inventory invClass) 
        {
            IEnumerable<Item> items = invClass.GetItemsInSlots(new EquipmentSlot[] { EquipmentSlot.TacticalVest, EquipmentSlot.ArmBand, EquipmentSlot.Pockets }) ?? Enumerable.Empty<Item>();
            HasGasAnalyser = false;
            HasGeiger = false;
            DeviceCheckerHelper(items);
        }

        public static float GetModifiedInventoryWeight(Inventory invClass)
        {
            float modifiedWeight = 0f;
            float trueWeight = 0f;
            foreach (EquipmentSlot equipmentSlot in InventoryEquipment.AllSlotNames)
            {
                Slot slot = invClass.Equipment.GetSlot(equipmentSlot);
                IEnumerable<Item> items = slot.Items;
                foreach (Item item in items)
                {
                    float itemTotalWeight = Utils.GetSingleItemTotalWeight(item);
                    trueWeight += itemTotalWeight;
                    if (equipmentSlot == EquipmentSlot.Backpack || equipmentSlot == EquipmentSlot.TacticalVest || equipmentSlot == EquipmentSlot.ArmorVest || equipmentSlot == EquipmentSlot.Headwear || equipmentSlot == EquipmentSlot.ArmBand)
                    {
                        var gearStats = TemplateStats.GetDataObj<Gear>(TemplateStats.GearStats, item.TemplateId);
                        modifiedWeight += itemTotalWeight * gearStats.Comfort;
                    }
                    else
                    {
                        modifiedWeight += itemTotalWeight;
                    }
                }
            }

            if (PluginConfig.EnableGeneralLogging.Value)
            {
                Utils.Logger.LogWarning("==================");
                Utils.Logger.LogWarning("Total Modified Weight " + modifiedWeight);
                Utils.Logger.LogWarning("Total Unmodified Weight " + trueWeight);
                Utils.Logger.LogWarning("==================");
            }

            return modifiedWeight;
        }


        private static EquipmentPenaltyComponent GetRigGearPenalty(Player player)
        {
            Item containedItem = GetSlotItem(player, EquipmentSlot.TacticalVest);
            if (containedItem == null) return null;
            return containedItem.GetItemComponent<EquipmentPenaltyComponent>();
        }

        public static Item GetSlotItem(Player player, EquipmentSlot slot)
        {
           return player?.Inventory?.Equipment?.GetSlot(slot)?.ContainedItem;
        }

        public static void UpdateFilterResource(Player player, PlayerZoneBridge phb)
        {
            HasGasFilter = false;
            Item gasmask = GetSlotItem(player, EquipmentSlot.FaceCover);
            if (gasmask == null) return;
            ResourceComponent filter = gasmask?.GetItemComponentsInChildren<ResourceComponent>(false).FirstOrDefault();
            if (filter == null) return;
            float inventoryFactor = (Plugin.RealHealthController.ToxicItemCount * RealismHealthController.TOXIC_ITEM_FACTOR) + (Plugin.RealHealthController.RadItemCount * RealismHealthController.RAD_ITEM_FACTOR);
            float reductionFactor = (phb.TotalGasRate + phb.TotalRadRate + GameWorldController.CurrentGasEventStrength + GameWorldController.CurrentMapRadStrength + inventoryFactor) / 3f;
            filter.Value = Mathf.Clamp(filter.Value - reductionFactor, 0f, 100f);
            if (filter.Value > 0) HasGasFilter = true;
        }

        public static void CalcGasMaskDuraFactor(Player player)
        {
            HasRespirator = false;
            HasGasFilter = false;
            Item gasmask = GetSlotItem(player, EquipmentSlot.FaceCover);
            if (gasmask == null) return;
            ResourceComponent filter = gasmask?.GetItemComponentsInChildren<ResourceComponent>(false).FirstOrDefault();
            float filterFactor = 0f;
           
            if (filter != null) 
            {
                if (filter.Value > 0) HasGasFilter = true;
                filterFactor = Mathf.Pow(filter.Value / filter.MaxResource, 0.15f);
                filterFactor = filterFactor > 0.85f ? 1f : filterFactor;
            }
          
            //masks like the respirator are not given an armor rating, so we can safely assume this is a gas mask/respirator that does not take filters,
            //therfore should not be subject to filter or durability factors
            //this may fall apart if ever I need a gas mask without a filter that has an armor rating...
            ArmorComponent armorComp = gasmask.GetItemComponent<ArmorComponent>();
            if (armorComp == null)
            {
                HasRespirator = true;
                _gasMaskDurabilityFactor = 1;
                return;
            }

            float gasmaskDuraPerc = armorComp.Repairable.Durability / armorComp.Repairable.MaxDurability;
            _gasMaskDurabilityFactor = gasmaskDuraPerc <= 0.5f || filter == null ? 0 : gasmaskDuraPerc * filterFactor;
        }

        public static EquipmentPenaltyComponent CheckFaceCoverGear(Player player, ref float gasProtection, ref float radProtection)
        {
            Item faceCoverItem = GetSlotItem(player, EquipmentSlot.FaceCover);
            if (faceCoverItem == null) return null;
            var gearStats = TemplateStats.GetDataObj<Gear>(TemplateStats.GearStats, faceCoverItem.TemplateId);
            HasGasMask = gearStats.IsGasMask;
            gasProtection = gearStats.GasProtection;
            radProtection = gearStats.RadProtection;
            if (HasGasMask) 
            {
                CalcGasMaskDuraFactor(player);
            }

            return faceCoverItem.GetItemComponent<EquipmentPenaltyComponent>();
        }

        public static void CheckGear(Player player)
        {
            FaceShieldComponent fsComponent = player.FaceShieldObserver.Component;
            NightVisionComponent nvgComponent = player.NightVisionObserver.Component;
            ThermalVisionComponent thermComponent = player.ThermalVisionObserver.Component;
            HasGasMask = false;
            bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On);
            bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);
            bool thermalIsOn = thermComponent != null && (thermComponent.Togglable == null || thermComponent.Togglable.On);
            float gasProtection = 0f;
            float radProtection = 0f;

            List<ArmorComponent> preAllocatedArmorComponents = new List<ArmorComponent>(20);
            player.Inventory.GetPutOnArmorsNonAlloc(preAllocatedArmorComponents);
            float totalErgo = 0f;
            float totalSpeed = 0f;
            for (int i = 0; i < preAllocatedArmorComponents.Count; i++)
            {
                ArmorComponent armorComponent = preAllocatedArmorComponents[i];
                if (armorComponent.Item.Template.ParentId == "5448e5284bdc2dcb718b4567" || armorComponent.Item.Template.ParentId == "5a341c4686f77469e155819e") continue;
                if (player.FaceShieldObserver.Component != null && player.FaceShieldObserver.Component.Item.TemplateId == armorComponent.Item.TemplateId)
                {
                    var gearStats = TemplateStats.GetDataObj<Gear>(TemplateStats.GearStats, armorComponent.Item.TemplateId);
                    if (!fsIsON || !gearStats.BlocksMouth) continue;
                    totalErgo += armorComponent.WeaponErgonomicPenalty;
                    totalSpeed += armorComponent.SpeedPenalty + -15f;
                    continue;
                }

                totalErgo += armorComponent.WeaponErgonomicPenalty;
                totalSpeed += armorComponent.SpeedPenalty;
            }
            EquipmentPenaltyComponent bag = player.Inventory.GetPutOnBackpack();
            if (bag != null)
            {
                totalErgo += bag.Template.WeaponErgonomicPenalty;
                totalSpeed += bag.Template.SpeedPenaltyPercent;
            }
            EquipmentPenaltyComponent rig = GetRigGearPenalty(player);
            if (rig != null)
            {
                totalErgo += rig.Template.WeaponErgonomicPenalty;
                totalSpeed += rig.Template.SpeedPenaltyPercent;
            }
            EquipmentPenaltyComponent faceCover = CheckFaceCoverGear(player, ref gasProtection, ref radProtection);
            if (faceCover != null)
            {
                totalErgo += faceCover.Template.WeaponErgonomicPenalty;
                totalSpeed += faceCover.Template.SpeedPenaltyPercent;
            }

            if (nvgIsOn || thermalIsOn)
            {
                totalErgo += -30f;
                totalSpeed += -12.5f;
            }

            totalErgo /= 100f;
            totalSpeed /= 100f;
            PlayerState.GearErgoPenalty = Mathf.Clamp(1f + totalErgo, 0.1f, 2f);
            PlayerState.GearSpeedPenalty = Mathf.Clamp(1f + totalSpeed, 0.1f, 2f);

            HandleGasMaskEffects(player, gasProtection, radProtection);

            Player.FirearmController fc = player.HandsController as Player.FirearmController;
            if (fc != null && Plugin.ServerConfig.recoil_attachment_overhaul)
            {
                StatCalc.UpdateAimParameters(fc, player.ProceduralWeaponAnimation);
            }

            if (PluginConfig.EnableGeneralLogging.Value)
            {
                Utils.Logger.LogWarning("gear speed " + PlayerState.GearSpeedPenalty);
                Utils.Logger.LogWarning("gear ergo " + PlayerState.GearErgoPenalty);
            }
        }

        public static float GetGearReloadSpeed(Player player, EquipmentSlot[] slots)
        {
            float reloadSpeed = 1f;
            foreach (var slot in slots) 
            {
                Item gear = player.Equipment.GetSlot(slot).ContainedItem;
                if (gear != null)
                {
                    var gearStats = TemplateStats.GetDataObj<Gear>(TemplateStats.GearStats, gear.TemplateId);
                    reloadSpeed *= gearStats.ReloadSpeedMulti;
                }
                else
                {
                    reloadSpeed *= 1;
                }
            }    
            return reloadSpeed;
        }

        public static void GetFacecoverADS(Player player)
        {
            Item faceCover = player.Equipment.GetSlot(EquipmentSlot.FaceCover).ContainedItem;
            GearAllowsADS = true;
            GearBlocksMouth = false;
            if (faceCover != null)
            {
                var gearStats = TemplateStats.GetDataObj<Gear>(TemplateStats.GearStats, faceCover.TemplateId);
                GearAllowsADS = gearStats.AllowADS;
            }
        }

        public static void SetGearParamaters(Player player)
        {
            float reloadMulti = 1f;
            List<ArmorComponent> preAllocatedArmorComponents = new List<ArmorComponent>(20);
            player.Inventory.GetPutOnArmorsNonAlloc(preAllocatedArmorComponents);

            reloadMulti *= GetGearReloadSpeed(player, [EquipmentSlot.ArmBand, EquipmentSlot.TacticalVest]);
            GetFacecoverADS(player);
        
            foreach (ArmorComponent armorComponent in preAllocatedArmorComponents)
            {
                //should I not continue instead of breaking?
                //vest and facecover have been checked already, skip them
                if (armorComponent.Item.Template.ParentId == "5448e5284bdc2dcb718b4567" || armorComponent.Item.Template.ParentId == "5a341c4686f77469e155819e")
                {
                    continue;
                }

                var gearStats = TemplateStats.GetDataObj<Gear>(TemplateStats.GearStats, armorComponent.Item.TemplateId);
                reloadMulti *= gearStats.ReloadSpeedMulti;
                ArmorTemplate armorTemplate = armorComponent.Template as ArmorTemplate;

                if (!gearStats.AllowADS) GearAllowsADS = false;
                if (gearStats.BlocksMouth) GearBlocksMouth = true;
            }

            GearReloadMulti = reloadMulti;
        }
    }
}
