using Comfort.Common;
using EFT;
using EFT.Interactive;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Val;


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
        bool UsesDistanceFalloff { get; set; }
    }

    public class GasZone : TriggerWithId, IHazardZone
    {
        public EZoneType ZoneType { get; } = EZoneType.Gas;
        public float ZoneStrengthModifier { get; set; } = 1f;
        public bool BlocksNav { get; set; }
        public bool UsesDistanceFalloff { get; set; }
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
            if (!UsesDistanceFalloff) return (ZoneStrengthModifier * (PluginConfig.ZoneDebug.Value ? PluginConfig.test10.Value : 1f)) / 1000f;
            float distance = Vector3.Distance(playerPosition, _zoneCollider.bounds.center);
            float invertedDistance = _maxDistance - distance;  // invert the distance
            invertedDistance = Mathf.Clamp(invertedDistance, 0, _maxDistance); //clamp the inverted distance
            return invertedDistance / (ZoneStrengthModifier * (PluginConfig.ZoneDebug.Value ? PluginConfig.test10.Value : 1f));
        }

        float CalculateGasStrengthSphere(Vector3 playerPosition)
        {
            if (!UsesDistanceFalloff) return (ZoneStrengthModifier * (PluginConfig.ZoneDebug.Value ? PluginConfig.test10.Value : 1f)) / 1000f;
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
        public bool UsesDistanceFalloff { get; set; }
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
            if (!UsesDistanceFalloff) return (ZoneStrengthModifier * (PluginConfig.ZoneDebug.Value ? PluginConfig.test10.Value : 1f)) / 1000f;
            float distance = Vector3.Distance(playerPosition, _zoneCollider.bounds.center);
            float invertedDistance = _maxDistance - distance;  // invert the distance
            invertedDistance = Mathf.Clamp(invertedDistance, 0, _maxDistance); //clamp the inverted distance
            return invertedDistance / (ZoneStrengthModifier * (PluginConfig.ZoneDebug.Value ? PluginConfig.test10.Value : 1f));
        }

        float CalculateRadStrengthSphere(Vector3 playerPosition)
        {
            if (!UsesDistanceFalloff) return (ZoneStrengthModifier * (PluginConfig.ZoneDebug.Value ? PluginConfig.test10.Value : 1f)) / 1000f;
            float distanceToCenter = Vector3.Distance(playerPosition, _zoneCollider.bounds.center);
            float radius = (_zoneCollider as SphereCollider).radius * transform.localScale.magnitude;
            float distanceFromSurface = radius - distanceToCenter;
            float clampedDistance = Mathf.Max(0f, distanceFromSurface);
            return clampedDistance / (ZoneStrengthModifier * (PluginConfig.ZoneDebug.Value ? PluginConfig.test10.Value : 1f));
        }
    }

    public class SafeZone : TriggerWithId, IHazardZone
    {
        const float MAIN_VOLUME = 0.5f;
        public EZoneType ZoneType { get; } = EZoneType.SafeZone;
        public float ZoneStrengthModifier { get; set; } = 0f;
        public bool BlocksNav { get; set; }
        public bool UsesDistanceFalloff { get; set; }
        public bool IsActive { get; set; } = true;
        private Dictionary<Player, PlayerHazardBridge> _containedPlayers = new Dictionary<Player, PlayerHazardBridge>();
        private BoxCollider _zoneCollider;
        private float _tick = 0f;
        private float _distanceToCenter = 0f;
        private AudioSource _mainAudioSource;
        private AudioSource _doorShutAudioSource;
        private AudioSource _doorOpenAudioSource;
        private List<Door> _doors = new List<Door>();
        private List<KeycardDoor> _keycardDoors = new List<KeycardDoor>();

        void Start()
        {
            _zoneCollider = GetComponentInParent<BoxCollider>();
            if (_zoneCollider == null)
            {
                Utils.Logger.LogError("Realism Mod: No BoxCollider found in parent for SafeZone");
                return;
            }
            SetUpAndPlayMainAudio();
            SetUpDoorShutAudio();
            SetUpDoorOpenAudio();
            CheckForDoors();
        }


        private void SetUpAndPlayMainAudio()
        {
            _mainAudioSource = this.gameObject.AddComponent<AudioSource>();
            _mainAudioSource.clip = Plugin.HazardZoneClips["labs-hvac.wav"];
            _mainAudioSource.volume = MAIN_VOLUME;
            _mainAudioSource.loop = true;
            _mainAudioSource.playOnAwake = false;
            _mainAudioSource.spatialBlend = 1.0f;
            _mainAudioSource.minDistance = 4f;
            _mainAudioSource.maxDistance = 20f;
            _mainAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            _mainAudioSource.Play();
        }

        private void SetUpDoorShutAudio()
        {
            _doorShutAudioSource = this.gameObject.AddComponent<AudioSource>();
            _doorShutAudioSource.clip = Plugin.HazardZoneClips["door_shut.wav"];
            _doorShutAudioSource.volume = 0.5f;
            _doorShutAudioSource.loop = false;
            _doorShutAudioSource.playOnAwake = false;
            _doorShutAudioSource.spatialBlend = 1.0f;
            _doorShutAudioSource.minDistance = 4f;
            _doorShutAudioSource.maxDistance = 20f;
            _doorShutAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        private void SetUpDoorOpenAudio()
        {
            _doorOpenAudioSource = this.gameObject.AddComponent<AudioSource>();
            _doorOpenAudioSource.clip = Plugin.HazardZoneClips["door_open.wav"];
            _doorOpenAudioSource.volume = 0.3f;
            _doorOpenAudioSource.loop = false;
            _doorOpenAudioSource.playOnAwake = false;
            _doorOpenAudioSource.spatialBlend = 1.0f;
            _doorOpenAudioSource.minDistance = 4f;
            _doorOpenAudioSource.maxDistance = 20f;
            _doorOpenAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        IEnumerator AdjustVolume(float targetVolume)
        {
            while (_mainAudioSource.volume != targetVolume)
            {
                _mainAudioSource.volume = Mathf.MoveTowards(_mainAudioSource.volume, targetVolume, 0.1f * Time.deltaTime);
                yield return null;
            }
            _mainAudioSource.volume = targetVolume;
        }

        bool AnyDoorsOpen(WorldInteractiveObject activeWorldObject)
        {
            foreach (var door in _doors) 
            {
                if (ReferenceEquals(door, activeWorldObject))
                {
                    continue;
                }
                if (door.DoorState == EDoorState.Open) return true;
            }
            foreach (var keyCardDoor in _keycardDoors)
            {
                if (ReferenceEquals(keyCardDoor, activeWorldObject))
                {
                    continue;
                }
                if (keyCardDoor.DoorState == EDoorState.Open) return true;
            }
            return false;    
        }

        void OnDoorStateChange(WorldInteractiveObject obj, EDoorState prevState, EDoorState nextState) 
        {
            bool otherDoorsCurrentlyOpen = AnyDoorsOpen(obj);

            if (!otherDoorsCurrentlyOpen && ((prevState == EDoorState.Shut && nextState == EDoorState.Interacting) || (prevState == EDoorState.Locked && nextState == EDoorState.Interacting)))
            {
                _doorShutAudioSource.Stop();
                _doorOpenAudioSource.Play();
            } 
            if (!otherDoorsCurrentlyOpen && prevState == EDoorState.Interacting && nextState == EDoorState.Shut)
            {
                _doorOpenAudioSource.Stop();
                _doorShutAudioSource.Play(); 
            }

            if (!otherDoorsCurrentlyOpen && nextState == EDoorState.Shut) IsActive = true;
            if (otherDoorsCurrentlyOpen || nextState == EDoorState.Open) IsActive = false;

            if (!IsActive) StartCoroutine(AdjustVolume(0f));
            if (IsActive) StartCoroutine(AdjustVolume(MAIN_VOLUME));

        }

        void CheckForDoors()
        {
            BoxCollider box = (BoxCollider)_zoneCollider;
            Vector3 boxCenter = box.transform.position + box.center;
            Vector3 boxSize = box.size / 2; 

            Collider[] colliders = Physics.OverlapBox(boxCenter, boxSize, Quaternion.identity);

            foreach (Collider col in colliders)
            {
                KeycardDoor keycardDoor = col.GetComponent<KeycardDoor>();
                Door door = col.GetComponent<Door>();

                //need to check explicitly for type because KeyCardDoor inherits from Door, and Unity will treat them the same when getting component
                if (keycardDoor != null && keycardDoor.Operatable && keycardDoor.GetType() == typeof(KeycardDoor))
                {
                    keycardDoor.OnDoorStateChanged += OnDoorStateChange;
                    _keycardDoors.Add(keycardDoor);

                }
                if (door != null && door.Operatable && door.GetType() == typeof(Door)) 
                {
                    door.OnDoorStateChanged += OnDoorStateChange;
                    _doors.Add(door);
                }
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

                    CalculateSafeZoneDepth(player.Position);
                    hazardBridge.SafeZones[this.name] = IsActive && _distanceToCenter <= 0.69f;
                }

                foreach (var p in playersToRemove)
                {
                    _containedPlayers.Remove(p);
                }

                _tick = 0f;
            }
        }

        void CalculateSafeZoneDepth(Vector3 playerPosition)
        {
            Vector3 extents = _zoneCollider.bounds.extents;
            float distance = Vector3.Distance(playerPosition, _zoneCollider.bounds.center);
            float maxDistance = extents.magnitude;
            float distancePercentage = distance / maxDistance;
            _distanceToCenter = Mathf.Clamp01(distancePercentage);
        }
    }
}
