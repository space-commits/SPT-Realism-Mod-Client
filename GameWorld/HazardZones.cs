using EFT;
using EFT.Interactive;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealismMod
{

    public enum EZoneType 
    {
        Radiation,
        Toxic
    }

    public interface IHazardZone
    {
        EZoneType ZoneType { get; } 
        float ZoneStrengthModifier { get; set; }
    }

    public class GasZone : TriggerWithId, IHazardZone
    {
        public EZoneType ZoneType { get; } = EZoneType.Toxic;
        public float ZoneStrengthModifier { get; set; } = 1f;
        private Dictionary<Player, PlayerHazardBridge> _containedPlayers = new Dictionary<Player, PlayerHazardBridge>();
        private BoxCollider _zoneCollider;
        private float _tick = 0f;
        private float _maxDistance = 0f;

        void Start()
        {
            _zoneCollider = GetComponentInParent<BoxCollider>();
            if (_zoneCollider == null)
            {
                Utils.Logger.LogError("Realism Mod: No BoxCollider found in parent for GasZone");
            }
            Vector3 boxSize = _zoneCollider.size;
            _maxDistance = boxSize.magnitude / 2f;
        }

        public override void TriggerEnter(Player player)
        {
            if (player != null)
            {
                PlayerHazardBridge hazardBridge;
                player.TryGetComponent<PlayerHazardBridge>(out hazardBridge);
                if(hazardBridge == null)
                {
                    hazardBridge = player.gameObject.AddComponent<PlayerHazardBridge>();
                    hazardBridge._Player = player;
                }
                hazardBridge.GasZoneCount++;
                hazardBridge.GasAmounts.Add(this.name, 0f);
                _containedPlayers.Add(player, hazardBridge);
            }
        }

        public override void TriggerExit(Player player)
        {
            if (player != null)
            {
                PlayerHazardBridge hazardBridge = _containedPlayers[player];
                hazardBridge.GasZoneCount--;
                hazardBridge.GasAmounts.Remove(this.name);
                _containedPlayers.Remove(player);
            }
        }

        void Update()
        {
            _tick += Time.deltaTime;

            if (_tick >= 0.25f) 
            {
                _tick = 0f;
                foreach (var p in _containedPlayers)
                {
                    Player player = p.Key;
                    PlayerHazardBridge hazardBridge = p.Value;
                    float gasAmount = CalculateGasStrength(player.gameObject.transform.position);
                    hazardBridge.GasAmounts[this.name] = Mathf.Max(gasAmount, 0f);
                }
            }
        }

        float CalculateGasStrength(Vector3 playerPosition)
        {
            float distance = Vector3.Distance(playerPosition, _zoneCollider.bounds.center);
            float invertedDistance = _maxDistance - distance;  // invert the distance
            invertedDistance = Mathf.Clamp(invertedDistance, 0, _maxDistance); //clamp the inverted distance
            return invertedDistance / (ZoneStrengthModifier * (Plugin.ZoneDebug.Value ? Plugin.test10.Value : 1f));
        }
    }

    public class RadiationZone : TriggerWithId, IHazardZone
    {
        public EZoneType ZoneType { get; } = EZoneType.Radiation;
        public float ZoneStrengthModifier { get; set; } = 1f;
        private Dictionary<Player, PlayerHazardBridge> _containedPlayers = new Dictionary<Player, PlayerHazardBridge>();
        private BoxCollider _zoneCollider;
        private float _tick = 0f;
        private float _maxDistance = 0f;

        void Start()
        {
            _zoneCollider = GetComponentInParent<BoxCollider>();
            if (_zoneCollider == null)
            {
                Utils.Logger.LogError("Realism Mod: No BoxCollider found in parent for RadiationZone");
            }
            Vector3 boxSize = _zoneCollider.size;
            _maxDistance = boxSize.magnitude / 2f;
        }

        public override void TriggerEnter(Player player)
        {
            if (player != null)
            {
                PlayerHazardBridge hazardBridge;
                player.TryGetComponent<PlayerHazardBridge>(out hazardBridge);
                if (hazardBridge == null)
                {
                    hazardBridge = player.gameObject.AddComponent<PlayerHazardBridge>();
                    hazardBridge._Player = player;
                }
                hazardBridge.RadZoneCount++;
                hazardBridge.RadAmounts.Add(this.name, 0f);
                _containedPlayers.Add(player, hazardBridge);
            }
        }

        public override void TriggerExit(Player player)
        {
            if (player != null)
            {
                PlayerHazardBridge hazardBridge = _containedPlayers[player];
                hazardBridge.RadZoneCount--;
                hazardBridge.RadAmounts.Remove(this.name);
                _containedPlayers.Remove(player);
            }
        }

        void Update()
        {
            _tick += Time.deltaTime;

            if (_tick >= 0.25f)
            {
                foreach (var p in _containedPlayers)
                {
                    Player player = p.Key;
                    PlayerHazardBridge hazardBridge = p.Value;
                    if (player == null || hazardBridge == null) return; //to do: find way to remove null entries
                    float radAmount = CalculateRadStrength(player.gameObject.transform.position);
                    hazardBridge.RadAmounts[this.name] = Mathf.Max(radAmount, 0f);
                }
                _tick = 0f;
            }
        }

        float CalculateRadStrength(Vector3 playerPosition)
        {
            float distance = Vector3.Distance(playerPosition, _zoneCollider.bounds.center);
            float invertedDistance = _maxDistance - distance;  // invert the distance
            invertedDistance = Mathf.Clamp(invertedDistance, 0, _maxDistance); //clamp the inverted distance
            return invertedDistance / (ZoneStrengthModifier * (Plugin.ZoneDebug.Value ? Plugin.test10.Value : 1f));
        }
    }
}
