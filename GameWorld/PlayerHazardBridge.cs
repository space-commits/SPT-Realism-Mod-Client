using EFT;
using EFT.InventoryLogic;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ExistanceClass = GClass2470;

namespace RealismMod
{
    public class PlayerHazardBridge : MonoBehaviour
    {
        public Player _Player { get; set; }
        public int GasZoneCount { get; set; } = 0;
        public int RadZoneCount { get; set; } = 0;
        public Dictionary<string, float> GasRates = new Dictionary<string, float>();
        public Dictionary<string, float> RadRates = new Dictionary<string, float>();

        public float TotalGasRate
        {
            get
            {
                float totalGas = 0f;
                foreach (var gas in GasRates)
                {
                    totalGas += gas.Value;
                }
                return totalGas;
            }
        }

        public float TotalRadRate
        {
            get
            {
                float totalRads = 0f;
                foreach (var rad in RadRates)
                {
                    totalRads += rad.Value;
                }
                return totalRads;
            }
        }

        private float _bridgeTimer = 0f;
        private const float Interval = 10f;

        private bool BotHasGasMask() 
        {
            if (_Player?.Inventory == null || _Player?.Equipment == null) return true;
            Item containedItem = _Player.Inventory?.Equipment?.GetSlot(EquipmentSlot.FaceCover)?.ContainedItem;
            if (containedItem == null) return false;
            return GearStats.IsGasMask(containedItem);
        }

        //for bots
        void Update()
        {
            _bridgeTimer += Time.deltaTime;
            if (_bridgeTimer >= Interval)
            {
                //temporary solution to dealing with bots
                if (GasZoneCount > 0 && _Player != null && _Player?.ActiveHealthController != null && _Player?.AIData?.BotOwner != null && !_Player.AIData.BotOwner.IsDead && _Player.HealthController.IsAlive)
                {
                    if (!BotHasGasMask() && TotalGasRate > 0.05f)
                    {
                        _Player.ActiveHealthController.ApplyDamage(EBodyPart.Chest, TotalGasRate * Interval * 0.75f, ExistanceClass.PoisonDamage);
         
                        if (_Player.ActiveHealthController.GetBodyPartHealth(EBodyPart.Chest).Current <= 110f) 
                        {
                            _Player.Speaker.Play(EPhraseTrigger.OnBreath, ETagStatus.Dying | ETagStatus.Aware, true, null);
                        }
                    }
                }
                _bridgeTimer = 0f;
            }
        }
    }

}
