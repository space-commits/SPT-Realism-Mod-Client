using EFT;
using EFT.InventoryLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ArmorTemplate = GClass2550; //to find again, search for HasHinge field

namespace RealismMod
{
    public static class GearController
    {
        public static bool HasGasMask { get; private set; } = false;
        public static bool HasGasFilter { get; private set; } = false;
        public static bool FSIsActive { get; set; } = false;
        public static bool NVGIsActive { get; set; } = false;


        private static float _currentGasProtection;
        private static float _currentRadProtection;
        private static float _gasMaskDurabilityFactor;

        public static float CurrentGasProtection
        {
            get
            {
                return _currentGasProtection * GasMaskDurabilityFactor;
            }
        }

        public static float CurrentRadProtection 
        {
            get
            {
                return _currentRadProtection * GasMaskDurabilityFactor;
            }
        } 

        public static float GasMaskDurabilityFactor 
        {
            get 
            {
                return _gasMaskDurabilityFactor >= 0.95f ? 1f : _gasMaskDurabilityFactor;
            }
        }

        private static bool _hadGasMask = true;

        private static void HandleGasMaskEffects(Player player, bool hasGasMask, float gasProtection, float radProtection) 
        {
            if (hasGasMask)
            {
                HasGasMask = true;
                _currentGasProtection = gasProtection;
                _currentRadProtection = radProtection;
                _hadGasMask = true;
/*                player.Say(EPhraseTrigger.OnBeingHurt, true, 0f, (ETagStatus)0, 100, false); //force to reset audio*/
                player.SpeechSource.SetLowPassFilterParameters(0.99f, ESoundOcclusionType.Obstruction, 1600, 5000, true);
                player.Muffled = true;
            }
            else
            {
                HasGasMask = false;
                _currentGasProtection = 0f;
                _currentRadProtection = 0f;
                player.SpeechSource.ResetFilters();
            }

            player.UpdateBreathStatus();
            player.UpdateOcclusion();
            player.SendVoiceMuffledState(player.Muffled);

            if (!hasGasMask && _hadGasMask && player.HealthStatus == ETagStatus.Dying) 
            {
                player.Say(EPhraseTrigger.OnBreath, true, 0f, (ETagStatus)0, 100, false); //force to reset audio
                _hadGasMask = false;
            }
        }

        private static void DeviceCheckerHelper(IEnumerable<Item> items) 
        {
            foreach (var item in items)
            {
                if (item != null && item?.TemplateId != null && item.TemplateId == "590a3efd86f77437d351a25b")
                {
                    DeviceController.HasGasAnalyser = true;
                }
                if (item != null && item?.TemplateId != null && item.TemplateId == "5672cb724bdc2dc2088b456b")
                {
                    DeviceController.HasGeiger = true;
                }
            }
        }

        public static void CheckForDevices(Inventory invClass) 
        {
            IEnumerable<Item> vestItems = invClass.GetItemsInSlots(new EquipmentSlot[] { EquipmentSlot.TacticalVest}) ?? Enumerable.Empty<Item>();
            IEnumerable<Item> armbandItems = invClass.GetItemsInSlots(new EquipmentSlot[] { EquipmentSlot.ArmBand }) ?? Enumerable.Empty<Item>();
            IEnumerable<Item> pocketItems = invClass.GetItemsInSlots(new EquipmentSlot[] { EquipmentSlot.Pockets }) ?? Enumerable.Empty<Item>();
            DeviceController.HasGasAnalyser = false;
            DeviceController.HasGeiger = false;
            DeviceCheckerHelper(vestItems);
            DeviceCheckerHelper(armbandItems);
            DeviceCheckerHelper(pocketItems);
        }

        public static float GetModifiedInventoryWeight(Inventory invClass)
        {
            float modifiedWeight = 0f;
            float trueWeight = 0f;
            foreach (EquipmentSlot equipmentSlot in EquipmentClass.AllSlotNames)
            {
                Slot slot = invClass.Equipment.GetSlot(equipmentSlot);
                IEnumerable<Item> items = slot.Items;
                foreach (Item item in items)
                {
                    float itemTotalWeight = item.GetSingleItemTotalWeight();
                    trueWeight += itemTotalWeight;
                    if (equipmentSlot == EquipmentSlot.Backpack || equipmentSlot == EquipmentSlot.TacticalVest || equipmentSlot == EquipmentSlot.ArmorVest || equipmentSlot == EquipmentSlot.Headwear || equipmentSlot == EquipmentSlot.ArmBand)
                    {
                        modifiedWeight += itemTotalWeight * GearStats.ComfortModifier(item);
                    }
                    else
                    {
                        modifiedWeight += itemTotalWeight;
                    }
                }
            }

            if (Plugin.EnableLogging.Value)
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

        public static void UpdateFilterResource(Player player, PlayerHazardBridge phb)
        {
            HasGasFilter = false;
            Item gasmask = GetSlotItem(player, EquipmentSlot.FaceCover);
            if (gasmask == null) return;
            ResourceComponent filter = gasmask?.GetItemComponentsInChildren<ResourceComponent>(false).FirstOrDefault();
            if (filter == null) return;
            float reductionFactor = (phb.TotalGasRate + phb.TotalGasRate) / 9f;
            filter.Value -= reductionFactor;
            if (filter.Value > 0) HasGasFilter = true;
        }

        public static void CalcGasMaskDuraFactor(Player player)
        {
            Item gasmask = GetSlotItem(player, EquipmentSlot.FaceCover);
            if (gasmask == null) return;
            ResourceComponent filter = gasmask?.GetItemComponentsInChildren<ResourceComponent>(false).FirstOrDefault();
            float filterFactor = 0f;
           
            if (filter != null) 
            {
                filterFactor = Mathf.Pow(filter.Value / filter.MaxResource, 0.45f);
            }
          
            ArmorComponent armorComp = gasmask.GetItemComponent<ArmorComponent>();
            if (armorComp == null)
            {
                _gasMaskDurabilityFactor = 1;
                return;
            }

            float gasmaskDuraPerc = armorComp.Repairable.Durability / armorComp.Repairable.MaxDurability;
            _gasMaskDurabilityFactor = gasmaskDuraPerc <= 0.5f || filter == null ? 0 : gasmaskDuraPerc * filterFactor;
        }

        public static EquipmentPenaltyComponent CheckFaceCoverGear(Player player, ref bool isGasMask, ref float gasProtection, ref float radProtection)
        {
            Item containedItem = GetSlotItem(player, EquipmentSlot.FaceCover);
            if (containedItem == null) return null;
            isGasMask = GearStats.IsGasMask(containedItem);
            gasProtection = GearStats.GasProtection(containedItem);
            radProtection = GearStats.RadProtection(containedItem);
            if (isGasMask) 
            {
                CalcGasMaskDuraFactor(player);
            }

            return containedItem.GetItemComponent<EquipmentPenaltyComponent>();
        }

        public static void GetGearPenalty(Player player)
        {
            FaceShieldComponent fsComponent = player.FaceShieldObserver.Component;
            NightVisionComponent nvgComponent = player.NightVisionObserver.Component;
            ThermalVisionComponent thermComponent = player.ThermalVisionObserver.Component;
            bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On);
            bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);
            bool thermalIsOn = thermComponent != null && (thermComponent.Togglable == null || thermComponent.Togglable.On);
            bool hasGasMask = false;
            float gasProtection = 0f;
            float radProtection = 0f;

            List<ArmorComponent> preAllocatedArmorComponents = new List<ArmorComponent>(20);
            player.Inventory.GetPutOnArmorsNonAlloc(preAllocatedArmorComponents);
            float totalErgo = 0f;
            float totalSpeed = 0f;
            for (int i = 0; i < preAllocatedArmorComponents.Count; i++)
            {
                ArmorComponent armorComponent = preAllocatedArmorComponents[i];
                if (armorComponent.Item.Template._parent == "5448e5284bdc2dcb718b4567" || armorComponent.Item.Template._parent == "5a341c4686f77469e155819e") continue;
                if (player.FaceShieldObserver.Component != null && player.FaceShieldObserver.Component.Item.TemplateId == armorComponent.Item.TemplateId)
                {
                    if (!fsIsON || !GearStats.BlocksMouth(armorComponent.Item)) continue;
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
            EquipmentPenaltyComponent faceCover = CheckFaceCoverGear(player, ref hasGasMask, ref gasProtection, ref radProtection);
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
            PlayerState.GearErgoPenalty = 1f + totalErgo;
            PlayerState.GearSpeedPenalty = 1f + totalSpeed;

            HandleGasMaskEffects(player, hasGasMask, gasProtection, radProtection);

            Player.FirearmController fc = player.HandsController as Player.FirearmController;
            if (fc != null)
            {
                StatCalc.UpdateAimParameters(fc, player.ProceduralWeaponAnimation);
            }

            if (Plugin.EnableLogging.Value)
            {
                Utils.Logger.LogWarning("gear speed " + PlayerState.GearSpeedPenalty);
                Utils.Logger.LogWarning("gear ergo " + PlayerState.GearErgoPenalty);
            }
        }

        public static float GetBeltReloadSpeed(Player player)
        {
            Item tacVest = player.Equipment.GetSlot(EquipmentSlot.ArmBand).ContainedItem;
            if (tacVest != null)
            {
                return GearStats.ReloadSpeedMulti(tacVest);
            }
            else
            {
                return 1;
            }
        }

        public static float GetRigReloadSpeed(Player player)
        {
            Item tacVest = player.Equipment.GetSlot(EquipmentSlot.TacticalVest).ContainedItem;
            if (tacVest != null)
            {
                return GearStats.ReloadSpeedMulti(tacVest);
            }
            else
            {
                return 1;
            }
        }

        public static bool GetFacecoverADS(Player player)
        {
            Item faceCover = player.Equipment.GetSlot(EquipmentSlot.FaceCover).ContainedItem;

            if (faceCover != null)
            {
                return GearStats.AllowsADS(faceCover);
            }
            else
            {
                return true;
            }
        }

        public static void SetGearParamaters(Player player)
        {
            float reloadMulti = 1f;
            bool allowADS = true;
            List<ArmorComponent> preAllocatedArmorComponents = new List<ArmorComponent>(20);
            player.Inventory.GetPutOnArmorsNonAlloc(preAllocatedArmorComponents);

            reloadMulti *= GetRigReloadSpeed(player);
            reloadMulti *= GetBeltReloadSpeed(player);
            allowADS = GetFacecoverADS(player);

            foreach (ArmorComponent armorComponent in preAllocatedArmorComponents)
            {
                if (armorComponent.Item.Template._parent == "5448e5284bdc2dcb718b4567" || armorComponent.Item.Template._parent == "5a341c4686f77469e155819e")
                {
                    break;
                }

                reloadMulti *= GearStats.ReloadSpeedMulti(armorComponent.Item);
                ArmorTemplate armorTemplate = armorComponent.Template as ArmorTemplate;

                if (!GearStats.AllowsADS(armorComponent.Item))
                {
                    allowADS = false;
                }
            }

            PlayerState.GearReloadMulti = reloadMulti;
            PlayerState.GearAllowsADS = allowADS;
        }


    }
}
