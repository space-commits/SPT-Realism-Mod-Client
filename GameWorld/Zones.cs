using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using Comfort.Common;

namespace RealismMod
{
    public interface IHazardZone
    {
        EZoneType ZoneType { get; } 
        float ZoneStrengthModifier { get; set; }
        bool BlocksNav { get; set; }
        bool UsesDistanceFalloff { get; set; }
        bool IsAnalysable { get; set; } 
    }

    public class HazardAnalyser : MonoBehaviour
    {
        public LootItem _LootItem { get; set; }
        public Player _Player { get; set; } = null;
        public IPlayer _IPlayer { get; set; } = null;
        public EZoneType TargetZoneType { get; set; }
        public AudioClip AudioClips { get; set; }   
        private AudioSource _audioSource;
        private Vector3 _position;
        private Quaternion _rotation;
        private List<IHazardZone> _intersectingZones = new List<IHazardZone>();

        private AudioSource SetUpAudio(string clip, GameObject go)
        {
            AudioSource audioSource = go.AddComponent<AudioSource>();
            audioSource.clip = Plugin.DeviceAudioClips[clip];
            audioSource.volume = 1f;
            audioSource.loop = false;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f;
            audioSource.minDistance = 0.75f;
            audioSource.maxDistance = 15f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            return audioSource;
        }

        IEnumerator DoLogic()
        {
            float time = 0f;
            while (time <= 2.5f)
            {
                time += Time.deltaTime;
                yield return null;
            }

            if (ZoneTypeMatches())
            {
                _audioSource.clip = Plugin.DeviceAudioClips["start_success.wav"];
                _audioSource.Play();
            }
            else
            {
                _audioSource.clip = Plugin.DeviceAudioClips["failed_start.wav"];
                _audioSource.Play();
                yield break;
            }

            float clipLength = _audioSource.clip.length;
            while (time <= clipLength)
            {
                time += Time.deltaTime;
                yield return null;
            }

            _audioSource.clip = Plugin.DeviceAudioClips["analyser_loop.wav"];
            _audioSource.loop = true;
            _audioSource.Play();

            time = 0f;
            clipLength = _audioSource.clip.length;
            while (time <= clipLength * 1f) //5f
            {
                time += Time.deltaTime;
                yield return null;
            }
            _audioSource.loop = false;
            ReplaceItem();
        }

        private void ReplaceItem() 
        {
            string templateId = TargetZoneType == EZoneType.Gas ? Utils.GAMU_DATA_ID : Utils.RAMU_DATA_ID;
            Item replacementItem = Singleton<ItemFactory>.Instance.CreateItem(MongoID.Generate(), templateId, null);
            LootItem lootItem = Singleton<GameWorld>.Instance.SetupItem(replacementItem, _IPlayer, this.gameObject.transform.position, this.gameObject.transform.rotation);
            Destroy(this.gameObject);

            AudioSource tempAudio = SetUpAudio("success_end.wav", lootItem.gameObject);
            tempAudio.Play();

        }

        private bool ZoneTypeMatches() 
        {
            if (_intersectingZones.Any(z => z.ZoneType == EZoneType.SafeZone)) return false;
            if (TargetZoneType == EZoneType.Gas && _intersectingZones.Any(z => (z.ZoneType == EZoneType.Gas || z.ZoneType == EZoneType.GasAssets) && z.IsAnalysable == true)) return true;
            if (TargetZoneType == EZoneType.Radiation && _intersectingZones.Any(z => (z.ZoneType == EZoneType.Radiation || z.ZoneType == EZoneType.RadAssets) && z.IsAnalysable == true)) return true;
            return false;
        }

        void Start() 
        {
            _audioSource = SetUpAudio("switch_off.wav", this.gameObject);
            _position = _Player.gameObject.transform.position;
            _position.y += 0.1f;
            _position = _position + _Player.gameObject.transform.forward * 1.1f;

            Vector3 eularRotation = _Player.gameObject.transform.rotation.eulerAngles;
            eularRotation.x = -90f;
            eularRotation.y = 0f;
            _rotation = Quaternion.Euler(new Vector3(eularRotation.x, eularRotation.y, eularRotation.z));

            StartCoroutine(DoLogic());
        }

        void Update() 
        {
            if (this.gameObject == null) return;
            this.gameObject.transform.position = _position; 
            this.gameObject.transform.rotation = _rotation;
        }

        private void OnTriggerEnter(Collider other)
        {
            IHazardZone hazardZone;
            if (other.gameObject.TryGetComponent<IHazardZone>(out hazardZone))
            {
                _intersectingZones.Add(hazardZone);
            }
        }

    }

    public class QuestZone : TriggerWithId, IHazardZone
    {
        public EZoneType ZoneType { get; } = EZoneType.Quest;
        public float ZoneStrengthModifier { get; set; } = 1f;
        public bool BlocksNav { get; set; }
        public bool UsesDistanceFalloff { get; set; }
        public bool IsAnalysable { get; set; } = false;
        private Dictionary<Player, PlayerZoneBridge> _containedPlayers = new Dictionary<Player, PlayerZoneBridge>();
        private BoxCollider _zoneCollider;
        private float _tick = 0f;

        void Start()
        {
            _zoneCollider = GetComponentInParent<BoxCollider>();
            if (_zoneCollider == null)
            {
                Utils.Logger.LogError("Realism Mod: No BoxCollider found in parent for RadiationZone");
            }
        }

        public override void TriggerEnter(Player player)
        {
            if (player != null)
            {
                PlayerZoneBridge playerBridge;
                player.TryGetComponent<PlayerZoneBridge>(out playerBridge);
                if (playerBridge == null) playerBridge = player.gameObject.AddComponent<PlayerZoneBridge>();
                if (playerBridge._Player == null) playerBridge._Player = player;
                _containedPlayers.Add(player, playerBridge);
            }
        }

        public override void TriggerExit(Player player)
        {
            if (player != null)
            {
                PlayerZoneBridge playerBridge = _containedPlayers[player];
                _containedPlayers.Remove(player);
            }
        }

        void Update()
        {
            _tick += Time.deltaTime;

            if (_tick >= 0.5f)
            {
                var playersToRemove = new List<Player>();
                foreach (var p in _containedPlayers)
                {
                    Player player = p.Key;
                    PlayerZoneBridge playerBridge = p.Value;
                    if (player == null || playerBridge == null)
                    {
                        playersToRemove.Add(player);
                        continue;
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

    public class GasZone : TriggerWithId, IHazardZone
    {
        public EZoneType ZoneType { get; } = EZoneType.Gas;
        public float ZoneStrengthModifier { get; set; } = 1f;
        public bool BlocksNav { get; set; }
        public bool UsesDistanceFalloff { get; set; }
        public bool IsAnalysable { get; set; } = false;
        private Dictionary<Player, PlayerZoneBridge> _containedPlayers = new Dictionary<Player, PlayerZoneBridge>();
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
                PlayerZoneBridge playerBridge;
                player.TryGetComponent<PlayerZoneBridge>(out playerBridge);
                if (playerBridge == null) playerBridge = player.gameObject.AddComponent<PlayerZoneBridge>();
                if (playerBridge._Player == null) playerBridge._Player = player;
                playerBridge.GasZoneCount++;
                if (BlocksNav) playerBridge.ZonesThatBlockNavCount++;
                playerBridge.GasRates.Add(this.name, 0f);
                _containedPlayers.Add(player, playerBridge);
            }
        }

        public override void TriggerExit(Player player)
        {
            if (player != null)
            {
                PlayerZoneBridge playerBridge = _containedPlayers[player];
                playerBridge.GasZoneCount--;
                if (BlocksNav) playerBridge.ZonesThatBlockNavCount--;
                playerBridge.GasRates.Remove(this.name);
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
                    PlayerZoneBridge playerBridge = p.Value;
                    if (player == null || playerBridge == null)
                    {
                        playersToRemove.Add(player);
                        return;
                    }
                    float gasAmount = _isSphere ? CalculateGasStrengthSphere(player.gameObject.transform.position) : CalculateGasStrengthBox(player.gameObject.transform.position);
                    playerBridge.GasRates[this.name] = Mathf.Max(gasAmount, 0f);
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
        public bool IsAnalysable { get; set; } = false;
        private Dictionary<Player, PlayerZoneBridge> _containedPlayers = new Dictionary<Player, PlayerZoneBridge>();
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
                PlayerZoneBridge playerBridge;
                player.TryGetComponent<PlayerZoneBridge>(out playerBridge);
                if (playerBridge == null) playerBridge = player.gameObject.AddComponent<PlayerZoneBridge>();
                if (playerBridge._Player == null) playerBridge._Player = player;
                playerBridge.RadZoneCount++;
                if (BlocksNav) playerBridge.ZonesThatBlockNavCount++;
                playerBridge.RadRates.Add(this.name, 0f);
                _containedPlayers.Add(player, playerBridge);
            }
        }

        public override void TriggerExit(Player player)
        {
            if (player != null)
            {
                PlayerZoneBridge playerBridge = _containedPlayers[player];
                playerBridge.RadZoneCount--;
                if (BlocksNav) playerBridge.ZonesThatBlockNavCount--;
                playerBridge.RadRates.Remove(this.name);
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
                    PlayerZoneBridge playerBridge = p.Value;
                    if (player == null || playerBridge == null)
                    {
                        playersToRemove.Add(player);
                        continue; 
                    }  
                    float radAmount = _isSphere ? CalculateRadStrengthSphere(player.gameObject.transform.position) : CalculateRadStrengthBox(player.gameObject.transform.position);
                    playerBridge.RadRates[this.name] = Mathf.Max(radAmount, 0f);
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

    public class LabsSafeZone : TriggerWithId, IHazardZone
    {
        const float MAIN_VOLUME = 0.6f;
        const float SHUT_VOLUME = 0.55f;
        const float OPEN_VOLUME = 0.35f;
        public EZoneType ZoneType { get; } = EZoneType.SafeZone;
        public float ZoneStrengthModifier { get; set; } = 0f;
        public bool BlocksNav { get; set; }
        public bool UsesDistanceFalloff { get; set; }
        public bool IsActive { get; set; } = true;
        public bool? DoorType { get; set; }
        public bool IsAnalysable { get; set; } = false;
        private Dictionary<Player, PlayerZoneBridge> _containedPlayers = new Dictionary<Player, PlayerZoneBridge>();
        private BoxCollider _zoneCollider;
        private float _tick = 0f;
        private float _distanceToCenter = 0f;
        private AudioSource _mainAudioSource;
        private AudioSource _doorShutAudioSource;
        private AudioSource _doorOpenAudioSource;
        private List<Door> _doors = new List<Door>();
        private Dictionary<WorldInteractiveObject, EDoorState> _previousDoorStates = new Dictionary<WorldInteractiveObject, EDoorState>();

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
            _mainAudioSource.minDistance = 3.5f;
            _mainAudioSource.maxDistance = 15f;
            _mainAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            _mainAudioSource.Play();
        }

        private void SetUpDoorShutAudio()
        {
            _doorShutAudioSource = this.gameObject.AddComponent<AudioSource>();
            _doorShutAudioSource.clip = Plugin.HazardZoneClips["door_shut.wav"];
            _doorShutAudioSource.volume = SHUT_VOLUME;
            _doorShutAudioSource.loop = false;
            _doorShutAudioSource.playOnAwake = false;
            _doorShutAudioSource.spatialBlend = 1.0f;
            _doorShutAudioSource.minDistance = 3.5f;
            _doorShutAudioSource.maxDistance = 15f;
            _doorShutAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        private void SetUpDoorOpenAudio()
        {
            _doorOpenAudioSource = this.gameObject.AddComponent<AudioSource>();
            _doorOpenAudioSource.clip = Plugin.HazardZoneClips["door_open.wav"];
            _doorOpenAudioSource.volume = OPEN_VOLUME;
            _doorOpenAudioSource.loop = false;
            _doorOpenAudioSource.playOnAwake = false;
            _doorOpenAudioSource.spatialBlend = 1.0f;
            _doorOpenAudioSource.minDistance = 3.5f;
            _doorOpenAudioSource.maxDistance = 15f;
            _doorOpenAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        IEnumerator AdjustVolume(float targetVolume, float speed, AudioSource audioSource)
        {
            while (audioSource.volume != targetVolume)
            {
                audioSource.volume = Mathf.MoveTowards(audioSource.volume, targetVolume, speed * Time.deltaTime);
                yield return null;
            }
            audioSource.volume = targetVolume;
        }

        void CheckForDoors()
        {
            BoxCollider box = (BoxCollider)_zoneCollider;
            Vector3 boxCenter = box.transform.position + box.center;
            Vector3 boxSize = box.size / 2;

            Collider[] colliders = Physics.OverlapBox(boxCenter, boxSize, Quaternion.identity);

            foreach (Collider col in colliders)
            {
                Door door = col.GetComponent<Door>();
                if (door != null && door.Operatable)
                {
                    if (door.KeyId == "5c1d0f4986f7744bb01837fa" || door.KeyId == "5c1d0c5f86f7744bb2683cf0") door.name = "automatic_door";
                    _doors.Add(door);
                    _previousDoorStates.Add(door, door.DoorState);
                }
            }
        }

        private bool KeysMatch(Player player, string doorKey)
        {
            if (player.MovementContext.InteractionInfo.Result != null)
            {
                KeyComponent key = ((KeyInteractionResultClass)player.MovementContext.InteractionInfo.Result).Key;
                return doorKey == key.Template.KeyId;
            }
            return false;
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
            return false;    
        }

        IEnumerator PlayDoorInteractionSound(WorldInteractiveObject door, EDoorState prevState, EDoorState currentState)
        {
            bool isOpening = (prevState == EDoorState.Locked && currentState == EDoorState.Interacting) || (prevState == EDoorState.Shut && currentState == EDoorState.Interacting);
            bool isClosing = prevState == EDoorState.Open && currentState == EDoorState.Interacting;

            float time = 0;
            float timeLimit = prevState == EDoorState.Locked && door.name == "automatic_door" ? 4f : isOpening ? 0.75f : 1f;

            while (time < timeLimit)
            {
                time += Time.deltaTime; 
                yield return null;
            }

            bool otherDoorsCurrentlyOpen = AnyDoorsOpen(door);
            if (!otherDoorsCurrentlyOpen && isOpening && Mathf.Abs(door.CurrentAngle) > Mathf.Abs(door.GetAngle(EDoorState.Shut)))
            {
                _doorOpenAudioSource.Play();
                StartCoroutine(AdjustVolume(OPEN_VOLUME, 1f, _doorOpenAudioSource));
                StartCoroutine(AdjustVolume(0f, 0.25f, _doorShutAudioSource));
                IsActive = false;    
      
            }
            else if (!otherDoorsCurrentlyOpen && isClosing && Mathf.Abs(door.CurrentAngle) < Mathf.Abs(door.GetAngle(EDoorState.Open)))
            {
                _doorShutAudioSource.Play();
                StartCoroutine(AdjustVolume(SHUT_VOLUME, 1f, _doorShutAudioSource));
                StartCoroutine(AdjustVolume(0f, 0.25f, _doorOpenAudioSource));
                IsActive = true;
            }

            if (!IsActive) StartCoroutine(AdjustVolume(0f, 0.1f, _mainAudioSource));
            if (IsActive) StartCoroutine(AdjustVolume(MAIN_VOLUME, 0.1f, _mainAudioSource));
        }

        private void CheckDoorState(WorldInteractiveObject door)
        {
            EDoorState prevState = _previousDoorStates[door];
            EDoorState currentState = door.DoorState;
            if (currentState != prevState) 
            {
                StartCoroutine(PlayDoorInteractionSound(door, prevState, currentState));
            }
            _previousDoorStates[door] = door.DoorState;
        }

        void CalculateSafeZoneDepth(Vector3 playerPosition)
        {
            Vector3 extents = _zoneCollider.bounds.extents;
            float distance = Vector3.Distance(playerPosition, _zoneCollider.bounds.center);
            float maxDistance = extents.magnitude;
            float distancePercentage = distance / maxDistance;
            _distanceToCenter = Mathf.Clamp01(distancePercentage);
        }

        public override void TriggerEnter(Player player)
        {
            if (player != null)
            {
                PlayerZoneBridge playerBridge;
                player.TryGetComponent<PlayerZoneBridge>(out playerBridge);
                if (playerBridge == null) playerBridge = player.gameObject.AddComponent<PlayerZoneBridge>();
                if (playerBridge._Player == null) playerBridge._Player = player;
                playerBridge.SafeZoneCount++;
                if (BlocksNav) playerBridge.ZonesThatBlockNavCount++;
                playerBridge.SafeZones.Add(this.name, IsActive);
                _containedPlayers.Add(player, playerBridge);
            }
        }

        public override void TriggerExit(Player player)
        {
            if (player != null)
            {
                PlayerZoneBridge playerBridge = _containedPlayers[player];
                playerBridge.SafeZoneCount--;
                if (BlocksNav) playerBridge.ZonesThatBlockNavCount--;
                playerBridge.SafeZones.Remove(this.name);   
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
                    PlayerZoneBridge playerBridge = p.Value;
                    if (player == null || playerBridge == null)
                    {
                        playersToRemove.Add(player);
                        continue;
                    }

                    CalculateSafeZoneDepth(player.Position);
                    playerBridge.SafeZones[this.name] = IsActive && _distanceToCenter <= 0.69f;
                }

                foreach (var p in playersToRemove)
                {
                    _containedPlayers.Remove(p);
                }

                foreach (var door in _doors) 
                {
                    CheckDoorState(door);
                }

                _tick = 0f;
            }
        }
    }
}
