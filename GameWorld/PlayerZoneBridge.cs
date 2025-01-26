using EFT;
using EFT.InventoryLogic;
using Comfort.Common;
using System.Collections.Generic;
using UnityEngine;
using ExistanceClass = GClass2788;
using System.Linq;
using EFT.Animations;
using Diz.LanguageExtensions;

namespace RealismMod
{
    public class PlayerZoneBridge : MonoBehaviour
    {
        private const float BOT_INTERVAL = 10f;
        private const float SPAWNTIME = 30f;
        public Player _Player { get; set; }
        public bool IsBot { get; private set; } = false;
        public bool SpawnedInZone { get; private set; } = false;
        public int ZonesThatBlockNavCount { get; set; } = 0;
        public int RadZoneCount { get; set; } = 0;
        public int GasZoneCount { get; set; } = 0;
        public int SafeZoneCount { get; set; } = 0;
        public Dictionary<string, bool> SafeZones = new Dictionary<string, bool>();
        public Dictionary<string, float> GasRates = new Dictionary<string, float>(); //to accomodate being in multiple zones
        public Dictionary<string, float> RadRates = new Dictionary<string, float>(); //to accomodate being in multiple zones
        private bool _isProtectedFromSafeZone = false;

        public bool IsProtectedFromSafeZone
        {
            get
            {
                return _isProtectedFromSafeZone;
            }
        }

        public float TotalGasRate
        {
            get
            {
                if (IsProtectedFromSafeZone) return 0f;
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
                if (IsProtectedFromSafeZone) return 0f;
                float totalRads = 0f;
                foreach (var rad in RadRates)
                {
                    totalRads += rad.Value;
                }
                return totalRads;
            }
        }

        private float _safeZoneCheckTimer = 0f;
        private float _botTimer = 0f;
        private float _timeActive = 0f;
        private bool _checkedSpawn = false;

        private bool BotHasGasmask()
        {
            if (_Player?.Inventory == null || _Player?.Equipment == null) return true;
            Item containedItem = _Player.Inventory?.Equipment?.GetSlot(EquipmentSlot.FaceCover)?.ContainedItem;
            if (containedItem == null) return false;
            var gearStats = Stats.GetDataObj<Gear>(Stats.GearStats, containedItem.TemplateId);
            return gearStats.IsGasMask;
        }

        private void HandleBotGas(bool hasGasmask)
        {
            if ((GasZoneCount > 0 || GameWorldController.DoMapGasEvent) && !IsProtectedFromSafeZone)
            {
                float gasRate = TotalGasRate + (GameWorldController.CurrentGasEventStrengthBot * (_Player.Environment == EnvironmentType.Indoor ? 0.5f : 1f));
                if (!hasGasmask && TotalGasRate > 0.05f)
                {
                    _Player.ActiveHealthController.ApplyDamage(EBodyPart.Chest, TotalGasRate * BOT_INTERVAL * 0.75f, ExistanceClass.PoisonDamage);

                    if (_Player.ActiveHealthController.GetBodyPartHealth(EBodyPart.Chest).Current <= 115f)
                    {
                        _Player.Speaker.Play(EPhraseTrigger.OnBreath, ETagStatus.Dying | ETagStatus.Aware, true, null);
                    }
                }
            }
        }

        private void HandleBotRads(bool hasGasmask)
        {
            if ((RadZoneCount > 0 || GameWorldController.DoMapRads) && !IsProtectedFromSafeZone)
            {
                bool isOutside = _Player.Environment == EnvironmentType.Outdoor;
                float explFactor = GameWorldController.DidExplosionClientSide && isOutside ? 2f : 1f;
                float mapRadsFactor = GameWorldController.DoMapRads ? 0.05f + (isOutside ? Plugin.RealismWeatherComponent.TargetRain * GameWorldController.RAD_RAIN_MODI : 0f) : 0f;
                float totalRads = (TotalRadRate + mapRadsFactor) * explFactor;
                float realRadRate = hasGasmask ? totalRads * 0.5f : totalRads;
                if (realRadRate > 0.4f || GameWorldController.DidExplosionClientSide || GameWorldController.DoMapRads && !hasGasmask)
                {
                    _Player.ActiveHealthController.ApplyDamage(EBodyPart.Chest, realRadRate * BOT_INTERVAL, ExistanceClass.RadiationDamage);
                    if (GameWorldController.DidExplosionClientSide && _Player.Environment == EnvironmentType.Outdoor)
                    {
                        _Player.Speaker.Play(EPhraseTrigger.OnBreath, ETagStatus.Dying | ETagStatus.Aware, true, null);
                    }
                }
            }
        }

        private void CheckSpawnPoint()
        {
            if (!_checkedSpawn)
            {
                _timeActive += Time.deltaTime;
                if ((GasZoneCount > 0 || RadZoneCount > 0) && !IsProtectedFromSafeZone)
                {
                    SpawnedInZone = true;
                    MoveEntityToSafeLocation();
                }
                bool isMoving = _Player.IsSprintEnabled || (_Player.ProceduralWeaponAnimation.Mask & EProceduralAnimationMask.Walking) != (EProceduralAnimationMask)0;
                if (_timeActive >= SPAWNTIME || SpawnedInZone || (_timeActive >= 5f && isMoving)) _checkedSpawn = true;
            }
        }

        private void MoveEntityToSafeLocation()
        {
            Vector3 originalPos = _Player.Transform.position;
            _Player.Transform.position = ZoneSpawner.TryGetSafeSpawnPoint(_Player, IsBot, ZonesThatBlockNavCount > 0, RadZoneCount > 0);
            if(PluginConfig.ZoneDebug.Value) Utils.Logger.LogWarning($"Realism Mod: Spawned in Hazard, moved to: {_Player.Transform.position}, Original Position: {originalPos},  Was Bot? {IsBot}, time remaining {_timeActive}");
        }

        private void CheckSafeZones()
        {
            _safeZoneCheckTimer += Time.deltaTime;
            if (_safeZoneCheckTimer >= 1f)
            {
                _isProtectedFromSafeZone = false;
                foreach (var zone in SafeZones)
                {
                    if (zone.Value == true) _isProtectedFromSafeZone = true;
                }
                _safeZoneCheckTimer = 0f;
            }
        }

        //temporary solution to dealing with bots
        private void BotCheck() 
        {
            _botTimer += Time.deltaTime;
            IsBot = _Player?.AIData?.BotOwner != null || _Player.IsAI;
            if (_botTimer >= BOT_INTERVAL)
            {
                bool isAliveBot = IsBot && _Player != null && _Player?.ActiveHealthController != null && !_Player.AIData.BotOwner.IsDead && _Player.HealthController.IsAlive;
                if (!isAliveBot) return;
                bool hasGasmask = BotHasGasmask();
                HandleBotGas(hasGasmask);
                HandleBotRads(hasGasmask);
                _botTimer = 0f;
            }
        } 

        //for bots
        void Update()
        {
            BotCheck();
            CheckSpawnPoint();
            CheckSafeZones();
        }
    }

}
