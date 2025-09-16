using EFT;
using EFT.Animations;
using EFT.InventoryLogic;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ExistanceClass = GClass2855;

namespace RealismMod
{
    public class PlayerZoneBridge : MonoBehaviour
    {
        private const float TIME_CHECK_INTERVAL = 10f;
        private const float SPAWN_TIME = 40f;
        private const float BOT_SPAWN_TIME = 20f;
        private const float TIME_MOVEMENT_THRESHOLD = 20f;
        private const float BOT_TIME_MOVEMENT_THRESHOLD = 10f;
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
        string[] targetTags = { "Radiation", "Gas", "RadAssets", "GasAssets" };
        private bool _isInHazardZone = false;
        private float _safeZoneCheckTimer = 0f;
        private float _botTimer = 0f;
        private float _timeActive = 0f;
        private bool _checkedSpawn = false;

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

        private float SpawnTimeTheshold
        {
            get
            {
                return IsBot ? BOT_SPAWN_TIME : SPAWN_TIME;
            }
        }

        private float MoveTimeTheshold
        {
            get
            {
                return IsBot ? BOT_TIME_MOVEMENT_THRESHOLD : TIME_MOVEMENT_THRESHOLD;
            }
        }

        void Start()
        {
            StartCoroutine(IsInHazardZone());
        }

        void Update()
        {
            _timeActive += Time.deltaTime;
            BotCheck();
            CheckIfInHazard();
            CheckSafeZones();
        }

        private IEnumerator IsInHazardZone() 
        {
            // Physics.OverlapSphere(_Player.Transform.position, 1f).Any(col => targetTags.Contains(col.tag));
            while (!_isInHazardZone && !_checkedSpawn)
            {
                var zone = HazardPlayerSpawnManager.GetZoneContaining(_Player.Transform.position);

                if (zone != null) _isInHazardZone = true;

                if (PluginConfig.ZoneDebug.Value) Utils.Logger.LogWarning($"player spawned at hazard zone? {zone != null}");

                yield return new WaitForSeconds(1f);
            }
        }


        private bool BotHasGasmask()
        {
            if (_Player?.Inventory == null || _Player?.Equipment == null) return true;
            Item containedItem = _Player.Inventory?.Equipment?.GetSlot(EquipmentSlot.FaceCover)?.ContainedItem;
            if (containedItem == null) return false;
            var gearStats = TemplateStats.GetDataObj<Gear>(TemplateStats.GearStats, containedItem.TemplateId);
            return gearStats.IsGasMask;
        }

        private void HandleBotGas(bool hasGasmask)
        {
            if ((GasZoneCount > 0 || GameWorldController.DoMapGasEvent) && !IsProtectedFromSafeZone)
            {
                float gasRate = TotalGasRate + (GameWorldController.CurrentGasEventStrengthBot * (_Player.Environment == EnvironmentType.Indoor ? 0.5f : 1f));
                if (!hasGasmask && TotalGasRate > 0.05f)
                {
                    _Player.ActiveHealthController.ApplyDamage(EBodyPart.Chest, TotalGasRate * TIME_CHECK_INTERVAL * 0.75f, ExistanceClass.PoisonDamage);

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
                    _Player.ActiveHealthController.ApplyDamage(EBodyPart.Chest, realRadRate * TIME_CHECK_INTERVAL, ExistanceClass.RadiationDamage);
                    if (GameWorldController.DidExplosionClientSide && _Player.Environment == EnvironmentType.Outdoor)
                    {
                        _Player.Speaker.Play(EPhraseTrigger.OnBreath, ETagStatus.Dying | ETagStatus.Aware, true, null);
                    }
                }
            }
        }

        private void CheckIfInHazard()
        {
            if (!_checkedSpawn)
            {
                if (_isInHazardZone || (GasZoneCount > 0 || RadZoneCount > 0) && !IsProtectedFromSafeZone)
                {
                    SpawnedInZone = true;
                    MoveEntityToSafeLocation();
                }
                bool isMoving = _Player.IsSprintEnabled || (_Player.ProceduralWeaponAnimation.Mask & EProceduralAnimationMask.Walking) != (EProceduralAnimationMask)0;
                if (SpawnedInZone || _timeActive >= SpawnTimeTheshold || (_timeActive >= MoveTimeTheshold && isMoving)) _checkedSpawn = true;
            }
        }

        private void MoveEntityToSafeLocation()
        {
            Vector3 originalPos = _Player.Transform.position;
            _Player.Transform.position = ZoneSpawner.TryGetSafeSpawnPoint(_Player, IsBot, ZonesThatBlockNavCount > 0, RadZoneCount > 0);
            if (PluginConfig.ZoneDebug.Value) Utils.Logger.LogWarning($"Realism Mod: Spawned in Hazard, moved to: {_Player.Transform.position}, Original Position: {originalPos},  Was Bot? {IsBot}, time remaining {_timeActive}, ProfileId {_Player.ProfileId}");
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

        private void BotCheck() 
        {
            _botTimer += Time.deltaTime;
            IsBot = (_Player.IsAI || _Player?.AIData?.BotOwner != null) && !_Player.IsYourPlayer;
            if (_botTimer >= TIME_CHECK_INTERVAL)
            {
                bool isAliveBot = IsBot && _Player != null && _Player?.ActiveHealthController != null && !_Player.AIData.BotOwner.IsDead && _Player.HealthController.IsAlive;
                if (!isAliveBot) return;
                bool hasGasmask = BotHasGasmask();
                HandleBotGas(hasGasmask);
                HandleBotRads(hasGasmask);
                _botTimer = 0f;
            }
        }
    }
}
