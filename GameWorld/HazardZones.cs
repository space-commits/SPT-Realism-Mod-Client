using Comfort.Common;
using EFT;
using EFT.Interactive;
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

            if (_tick >= 0.001f)
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

            if (_tick >= 0.001f)
            {
                var playersToRemove = new List<Player>();
                foreach (var p in _containedPlayers)
                {
                    Player player = p.Key;
                    PlayerHazardBridge hazardBridge = p.Value;
                    if (player == null || hazardBridge == null)
                    {
                        playersToRemove.Add(player);
                        continue; 
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
        public bool IsActive { get; set; } = true;
        private Dictionary<Player, PlayerHazardBridge> _containedPlayers = new Dictionary<Player, PlayerHazardBridge>();
        private Collider _zoneCollider;
#pragma warning disable CS0414
        private bool _isSphere = false;
#pragma warning disable CS0414
        private float _tick = 0f;
        private float _maxDistance = 0f;
        private AudioSource _audioSource;
        private List<Door> _doors = new List<Door>();
        private List<Door> _keycardDoors = new List<Door>();

        void Start()
        {
            SetUpAndPlayAudio();
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

            CheckForDoors();
        }

        void CheckForDoors()
        {
            // Get the center and size of the box collider
            BoxCollider box = (BoxCollider)_zoneCollider;
            Vector3 boxCenter = box.transform.position + box.center;
            Vector3 boxSize = box.size / 2; 

            // Perform the OverlapBox check
            Collider[] colliders = Physics.OverlapBox(boxCenter, boxSize, Quaternion.identity);

            Utils.Logger.LogWarning($"======================={this.name}");

            foreach (Collider col in colliders)
            {
                KeycardDoor keycardDoor = col.GetComponent<KeycardDoor>();
                Door door = col.GetComponent<Door>();
                if (keycardDoor != null)
                {
                    Utils.Logger.LogWarning($"Found Keycard door: {keycardDoor.name} is {keycardDoor.DoorState}");
                    _keycardDoors.Add(keycardDoor);    
 
                }
                if (door != null)
                {
                    Utils.Logger.LogWarning($"Found door: {door.name} is {door.DoorState}");
                    _doors.Add(door);
                }
            }
        }

        void CheckDoors()
        {
            foreach (var door in _doors) 
            {
                if (door.DoorState == EDoorState.Open) 
                {
                    Utils.Logger.LogWarning("==door open");
                    IsActive = false;
                    return;
                }
            }
            foreach (var keycardDoors in _keycardDoors)
            {
                if (keycardDoors.DoorState == EDoorState.Open)
                {
                    Utils.Logger.LogWarning("==labs door open");
                    IsActive = false;
                    return;
                }
            }
            IsActive = true;
        }

        void CheckDoorAudio() 
        {
            if (!IsActive) _audioSource.Stop();
            if (!_audioSource.isPlaying && IsActive) _audioSource.Play();
        }

        private void SetUpAndPlayAudio() 
        {
            _audioSource = this.gameObject.AddComponent<AudioSource>();
            _audioSource.clip = Plugin.HazardZoneClips["decontamination.wav"];
            _audioSource.loop = true;
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1.0f;
            _audioSource.minDistance = 1f;
            _audioSource.maxDistance = 100.0f;
            _audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            _audioSource.Play();
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
                hazardBridge.SafeZones.Add(this.name, IsActive);
                _containedPlayers.Add(player, hazardBridge);
            }
        }

        public override void TriggerExit(Player player)
        {
            if (player != null)
            {
                PlayerHazardBridge hazardBridge = _containedPlayers[player];
                hazardBridge.SafeZoneCount--;
                hazardBridge.SafeZones.Remove(this.name);
                hazardBridge.ZoneBlocksNav = false;
                _containedPlayers.Remove(player);
            }
        }

        void Update()
        {
            _tick += Time.deltaTime;

            if (_tick >= 0.25f)
            {
                CheckDoors();
                CheckDoorAudio();

                var playersToRemove = new List<Player>();
                foreach (var p in _containedPlayers)
                {
                    Player player = p.Key;
                    PlayerHazardBridge hazardBridge = p.Value;
                    if (player == null || hazardBridge == null)
                    {
                        playersToRemove.Add(player);
                        continue;
                    }
                    hazardBridge.SafeZones[this.name] = IsActive;
                    Utils.Logger.LogWarning("is active " + IsActive);
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
