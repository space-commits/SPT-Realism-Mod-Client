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
        Gas,
        RadAssets,
        GasAssets,
        SafeZone
    }

    public interface IHazardZone
    {
        EZoneType ZoneType { get; } 
        float ZoneStrengthModifier { get; set; }
        bool BlocksNav { get; set; }    
    }

    public class GasZone : TriggerWithId, IHazardZone
    {
        public EZoneType ZoneType { get; } = EZoneType.Gas;
        public float ZoneStrengthModifier { get; set; } = 1f;
        public bool BlocksNav { get; set; }
        private Dictionary<Player, PlayerHazardBridge> _containedPlayers = new Dictionary<Player, PlayerHazardBridge>();
        private Collider _zoneCollider;
        private bool _isSphere = false;
        private float _tick = 0f;
        private float _maxDistance = 0f;

        void Start()
        {
            _zoneCollider = GetComponentInParent<Collider>();
            if (_zoneCollider == null)
            {
                Utils.Logger.LogError("Realism Mod: No BoxCollider found in parent for GasZone");
            }

            SphereCollider sphereCollider = _zoneCollider as SphereCollider;
            if (sphereCollider != null)
            {
                _isSphere = true;
                _maxDistance = sphereCollider.radius;
            }
            else 
            {
                BoxCollider box = _zoneCollider as BoxCollider;
                Vector3 boxSize = box.size;
                _maxDistance = boxSize.magnitude / 2f;
            }
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
                hazardBridge.GasZoneCount++;
                hazardBridge.GasRates.Add(this.name, 0f);
                _containedPlayers.Add(player, hazardBridge);
            }
        }

        public override void TriggerExit(Player player)
        {
            if (player != null)
            {
                PlayerHazardBridge hazardBridge = _containedPlayers[player];
                hazardBridge.GasZoneCount--;
                hazardBridge.GasRates.Remove(this.name);
                _containedPlayers.Remove(player);
            }
        }

        void Update()
        {
            _tick += Time.deltaTime;

            if (_tick >= 0.25f)
            {
                var playersToRemove = new List<Player>();

                foreach (var p in _containedPlayers)
                {
                    Player player = p.Key;
                    PlayerHazardBridge hazardBridge = p.Value;
                    if (player == null || hazardBridge == null)
                    {
                        playersToRemove.Add(player);
                        return;
                    }
                    float gasAmount = _isSphere ? CalculateGasStrengthSphere(player.gameObject.transform.position) : CalculateGasStrengthBox(player.gameObject.transform.position);
                    hazardBridge.GasRates[this.name] = Mathf.Max(gasAmount, 0f);
                }

                foreach (var p in playersToRemove)
                {
                    _containedPlayers.Remove(p);
                }

                _tick = 0f;
            }
        }

        float CalculateGasStrengthBox(Vector3 playerPosition)
        {
            float distance = Vector3.Distance(playerPosition, _zoneCollider.bounds.center);
            float invertedDistance = _maxDistance - distance;  // invert the distance
            invertedDistance = Mathf.Clamp(invertedDistance, 0, _maxDistance); //clamp the inverted distance
            return invertedDistance / (ZoneStrengthModifier * (PluginConfig.ZoneDebug.Value ? PluginConfig.test10.Value : 1f));
        }

        float CalculateGasStrengthSphere(Vector3 playerPosition)
        {
            float distanceToCenter = Vector3.Distance(playerPosition, _zoneCollider.bounds.center);
            float radius = _zoneCollider.bounds.extents.magnitude;
            float effectiveDistance = Mathf.Max(0, distanceToCenter - radius); 
            float invertedDistance = _maxDistance - effectiveDistance; 
            invertedDistance = Mathf.Clamp(invertedDistance, 0, _maxDistance); 
            return invertedDistance / (ZoneStrengthModifier * (PluginConfig.ZoneDebug.Value ? PluginConfig.test10.Value : 1f));
        }
    }

    public class RadiationZone : TriggerWithId, IHazardZone
    {
        public EZoneType ZoneType { get; } = EZoneType.Radiation;
        public float ZoneStrengthModifier { get; set; } = 1f;
        public bool BlocksNav { get; set; }
        private Dictionary<Player, PlayerHazardBridge> _containedPlayers = new Dictionary<Player, PlayerHazardBridge>();
        private Collider _zoneCollider;
        private bool _isSphere = false;
        private float _tick = 0f;
        private float _maxDistance = 0f;

        void Start()
        {
            _zoneCollider = GetComponentInParent<Collider>();
            if (_zoneCollider == null)
            {
                Utils.Logger.LogError("Realism Mod: No BoxCollider found in parent for RadiationZone");
            }
            SphereCollider sphereCollider = _zoneCollider as SphereCollider;
            if (sphereCollider != null)
            {
                _isSphere = true;
                _maxDistance = sphereCollider.radius;
            }
            else
            {
                BoxCollider box = _zoneCollider as BoxCollider;
                Vector3 boxSize = box.size;
                _maxDistance = boxSize.magnitude / 2f;
            }
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
                hazardBridge.RadRates.Add(this.name, 0f);
                hazardBridge.ZoneBlocksNav = BlocksNav;
                _containedPlayers.Add(player, hazardBridge);
            }
        }

        public override void TriggerExit(Player player)
        {
            if (player != null)
            {
                PlayerHazardBridge hazardBridge = _containedPlayers[player];
                hazardBridge.RadZoneCount--;
                hazardBridge.RadRates.Remove(this.name);
                hazardBridge.ZoneBlocksNav = false;
                _containedPlayers.Remove(player);
            }
        }

        void Update()
        {
            _tick += Time.deltaTime;

            if (_tick >= 0.25f)
            {
                var playersToRemove = new List<Player>();
                foreach (var p in _containedPlayers)
                {
                    Player player = p.Key;
                    PlayerHazardBridge hazardBridge = p.Value;
                    if (player == null || hazardBridge == null)
                    {
                        playersToRemove.Add(player);
                        return; 
                    }  
                    float radAmount = _isSphere ? CalculateRadStrengthSphere(player.gameObject.transform.position) : CalculateRadStrengthBox(player.gameObject.transform.position);
                    hazardBridge.RadRates[this.name] = Mathf.Max(radAmount, 0f);
                }

                foreach (var p in playersToRemove) 
                {
                    _containedPlayers.Remove(p);
                }

                _tick = 0f;
            }
        }

        float CalculateRadStrengthBox(Vector3 playerPosition)
        {
            float distance = Vector3.Distance(playerPosition, _zoneCollider.bounds.center);
            float invertedDistance = _maxDistance - distance;  // invert the distance
            invertedDistance = Mathf.Clamp(invertedDistance, 0, _maxDistance); //clamp the inverted distance
            return invertedDistance / (ZoneStrengthModifier * (PluginConfig.ZoneDebug.Value ? PluginConfig.test10.Value : 1f));
        }

        float CalculateRadStrengthSphere(Vector3 playerPosition)
        {
            float distanceToCenter = Vector3.Distance(playerPosition, _zoneCollider.bounds.center);
            float radius = (_zoneCollider as SphereCollider).radius * transform.localScale.magnitude;
            float distanceFromSurface = radius - distanceToCenter;
            float clampedDistance = Mathf.Max(0f, distanceFromSurface);
            return clampedDistance / (ZoneStrengthModifier * (PluginConfig.ZoneDebug.Value ? PluginConfig.test10.Value : 1f));
        }
    }

    public class SafeZone : TriggerWithId, IHazardZone
    {
        public EZoneType ZoneType { get; } = EZoneType.SafeZone;
        public float ZoneStrengthModifier { get; set; } = 0f;
        public bool BlocksNav { get; set; }
        private Dictionary<Player, PlayerHazardBridge> _containedPlayers = new Dictionary<Player, PlayerHazardBridge>();
        private Collider _zoneCollider;
        private bool _isSphere = false;
        private float _tick = 0f;
        private float _maxDistance = 0f;

        void Start()
        {
            _zoneCollider = GetComponentInParent<Collider>();
            if (_zoneCollider == null)
            {
                Utils.Logger.LogError("Realism Mod: No BoxCollider found in parent for SafeZone");
            }
            SphereCollider sphereCollider = _zoneCollider as SphereCollider;
            if (sphereCollider != null)
            {
                _isSphere = true;
                _maxDistance = sphereCollider.radius;
            }
            else
            {
                BoxCollider box = _zoneCollider as BoxCollider;
                Vector3 boxSize = box.size;
                _maxDistance = boxSize.magnitude / 2f;
            }
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
                hazardBridge.SafeZoneCount++;
                hazardBridge.ZoneBlocksNav = BlocksNav;
                _containedPlayers.Add(player, hazardBridge);
            }
        }

        public override void TriggerExit(Player player)
        {
            if (player != null)
            {
                PlayerHazardBridge hazardBridge = _containedPlayers[player];
                hazardBridge.SafeZoneCount--;
                hazardBridge.RadRates.Remove(this.name);
                hazardBridge.ZoneBlocksNav = false;
                _containedPlayers.Remove(player);
            }
        }

        void Update()
        {
            _tick += Time.deltaTime;

            if (_tick >= 0.25f)
            {
                var playersToRemove = new List<Player>();
                foreach (var p in _containedPlayers)
                {
                    Player player = p.Key;
                    PlayerHazardBridge hazardBridge = p.Value;
                    if (player == null || hazardBridge == null)
                    {
                        playersToRemove.Add(player);
                        return;
                    }
                }

                foreach (var p in playersToRemove)
                {
                    _containedPlayers.Remove(p);
                }

                _tick = 0f;
            }
        }
    }
}
