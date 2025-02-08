using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using Comfort.Common;
using KeyInteractionResultClass = GClass3344;
using SPT.Common.Utils;
using static CW2.Animations.PhysicsSimulator.UnityValueDevice;
using UnityEngine.Rendering.PostProcessing;

namespace RealismMod
{
    public interface IZone
    {
        public EZoneType ZoneType { get; }
        public float ZoneStrength { get; set; }
        public bool BlocksNav { get; set; }
        public bool UsesDistanceFalloff { get; set; }
        public bool IsAnalysable { get; set; }
        public bool HasBeenAnalysed { get; set; }
        public string Name { get; set; }
        public List<GameObject> ActiveDevices { get; set; }
        public InteractableSubZone Interactable { get; set; }
    }

    //get all child interaction zone objects on start
    //get all target hazard zones on start
    //either use actions/triggers to update the status of the child interactbles
    //if conditons are met (all valves are in correct state), tell zones to turn "off", including any particle systems
    public class InteractableGroupComponent: MonoBehaviour
    {
        public InteractableGroup GroupData { get; set; }
        public List<IZone> _hazardZones = new List<IZone>();

        private List<InteractionZone> _interactionZones = new List<InteractionZone>();
        private bool _isTurnedOff = false;
        private int _completedSteps = 0;


        void Start()
        {
            foreach (var targets in GroupData.TargtetZones)
            {
                foreach (var targetName in targets.ZoneNames)
                {
                    GameObject zone = GameObject.Find(targetName);
                    if (zone != null)
                    {
                        Utils.Logger.LogWarning($"found hazard zone GO {targetName}");
                        IZone hazard = zone.GetComponent<IZone>();
                        if (hazard != null)
                        {
                            Utils.Logger.LogWarning($"found hazard component");
                            _hazardZones.Add(hazard);
                        }
                    }
                }
            }

            InteractionZone[] interactionZones = GetComponentsInChildren<InteractionZone>();
            foreach (var interactable in interactionZones)
            {
                _interactionZones.Add(interactable);
                interactable.OnInteractableStateChanged += HandleValveStateChanged;
            }
        }


        //instead of just setting it to 1 or 0, check if a valve should set a specific zone's modifier to a intermediary value
        //then check what combinatio of off or on should the zones be and that that is met

        private void HandleValveStateChanged(InteractionZone interactable, EInteractableState newState)
        {
            Utils.Logger.LogWarning($"==Valve {interactable.gameObject.name} changed to {newState}");
            bool allOff = true;
            _completedSteps = 0;
            foreach (InteractionZone zone in _interactionZones)
            {
                //if state is equal to desired state, _completedSteps += 1
                //the interactive subzones will need to be able to get thsi value for cases of buttons that can only be toggled if preivous steps are completed
                if (zone.State == EInteractableState.On) allOff = false;
                Utils.Logger.LogWarning($"Valve {interactable.gameObject.name} is currently {newState}");
            }

            if (allOff)
            {
                Utils.Logger.LogWarning($"turning off the gas");
                foreach (var hazard in _hazardZones)
                {
                    GasZone gas = hazard as GasZone;
                    if (gas != null)
                    {
                        Utils.Logger.LogWarning($"{gas.name} strength set to 0");
                        gas.ZoneStrengthTargetModi = 0f;
                    }
                }
                _isTurnedOff = true;
            }
            else 
            {
                Utils.Logger.LogWarning($"turning the gas on");
                foreach (var hazard in _hazardZones)
                {
                    GasZone gas = hazard as GasZone;
                    if (gas != null)
                    {
                        Utils.Logger.LogWarning($"{gas.name} strength set to 1");
                        gas.ZoneStrengthTargetModi = 1f;
                        _isTurnedOff = true;
                    }
                }
                _isTurnedOff = false;
            }
        }
    }

    //look for gameobjects it's supposed to interact with, add actions UI to the collider
    //set its own status
    public class InteractionZone : InteractableObject, IZone 
    {
        public EZoneType ZoneType { get; }
        public float ZoneStrength { get; set; }
        public bool BlocksNav { get; set; }
        public bool UsesDistanceFalloff { get; set; }
        public bool IsAnalysable { get; set; }
        public bool HasBeenAnalysed { get; set; }
        public string Name { get; set; }
        public List<GameObject> ActiveDevices { get; set; }

        public InteractableSubZone Interactable { get; set; }
        public delegate void OnStateChanged(InteractionZone interacatble, EInteractableState newState);
        public event OnStateChanged OnInteractableStateChanged;

        private EInteractableState _state;
        public EInteractableState State 
        {
            get 
            {
                return _state;
            }
            set 
            {
                _state = value;
                OnInteractableStateChanged?.Invoke(this, State);
            }
        }
        public List<GameObject> InteractableGameObjects{ get; set; }
        public List<ActionsTypesClass> InteractableActions = new List<ActionsTypesClass>();

        private GameObject _targetGameObject;
        private bool _isRotating = false;

        void Start()
        {
            _state = Interactable.StartingState;

            var box = this.gameObject.GetComponentInParent<BoxCollider>();
            Bounds bounds = box.bounds; 
            Collider[] colliders = Physics.OverlapBox(bounds.center, bounds.extents, Quaternion.identity);
            Utils.Logger.LogWarning($"Found {colliders.Length} objects in the box collider bounds:");
            foreach (Collider col in colliders)
            {
                if (col.gameObject.name == "Valve_red_01_A_COLLIDER")
                {
                    _targetGameObject = col.gameObject.transform.parent.gameObject;
                    Utils.Logger.LogWarning("===match");
                }
            }
            InitActions();
        }

        IEnumerator RotateOverTime(float maxSpeed, float duration)
        {
            _isRotating = true;

            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                // Normalize time (0 to 1)
                float t = elapsedTime / duration;

                // Apply smooth acceleration & deceleration using a sine wave
                float speedModifier = Mathf.Sin(t * Mathf.PI); // Creates ease-in & ease-out effect

                // Calculate current rotation speed
                float currentSpeed = maxSpeed * speedModifier;

                // Apply rotation
               _targetGameObject.transform.Rotate(0, 0, currentSpeed * Time.deltaTime);

                elapsedTime += Time.deltaTime;
                yield return null; // Wait for the next frame
            }

            _isRotating = false;
        }

        void InitActions()
        {
            this.gameObject.layer = LayerMask.NameToLayer("Interactive");

            InteractableActions.AddRange(
                new List<ActionsTypesClass>()
                {
                    new ActionsTypesClass
                    {
                        Name = "Turn On",
                        Action = TurnON
                    },
                    new ActionsTypesClass
                    {
                        Name = "Turn Off",
                        Action = TurnOff
                    }
                }
             );
        }

        //turn SFX on/off, trigger action
        public void TurnON() 
        {
            if (State == EInteractableState.On || _isRotating) return;
            StartCoroutine(RotateOverTime(PluginConfig.test4.Value, PluginConfig.test1.Value));
            State = EInteractableState.On;
        }

        public void TurnOff()
        {
            if (State == EInteractableState.Off || _isRotating) return;
            StartCoroutine(RotateOverTime(PluginConfig.test5.Value, PluginConfig.test1.Value));
            State = EInteractableState.Off;
        }
    }

    public class QuestZone : TriggerWithId, IZone
    {
        public EZoneType ZoneType { get; } = EZoneType.Quest;
        public float ZoneStrength { get; set; } = 1f;
        public bool BlocksNav { get; set; }
        public bool UsesDistanceFalloff { get; set; }
        public bool IsAnalysable { get; set; } = false;
        public bool HasBeenAnalysed { get; set; } = false;
        public string Name { get; set; }
        public List<GameObject> ActiveDevices { get; set; }
        public InteractableSubZone Interactable { get; set; }

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
            Name = name;
            ActiveDevices = new List<GameObject>();
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

    public class GasZone : TriggerWithId, IZone
    {
        public EZoneType ZoneType { get; } = EZoneType.Gas;
        public float ZoneStrength { get; set; } = 1f;
        public float ZoneStrengthTargetModi { get; set; } = 1f;
        public bool BlocksNav { get; set; }
        public bool UsesDistanceFalloff { get; set; }
        public bool IsAnalysable { get; set; } = false;
        public bool HasBeenAnalysed { get; set; } = false;
        public string Name { get; set; }
        public List<GameObject> ActiveDevices { get; set; }
        public InteractableSubZone Interactable { get; set; }

        private Dictionary<Player, PlayerZoneBridge> _containedPlayers = new Dictionary<Player, PlayerZoneBridge>();
        private Collider _zoneCollider;
        private bool _isSphere = false;
        private float _tick = 0f;
        private float _maxDistance = 0f;
        private FogScript[] _fogScript;
        private float _strengthModi = 1f;

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
            Name = name;
            ActiveDevices = new List<GameObject>();
            _fogScript = GetComponentsInChildren<FogScript>();
            ModifyVisualEffects(true);
        }

        private void ModifyVisualEffects(bool onInit) 
        {
            foreach (var fog in _fogScript)
            {
                if (onInit)
                {
                    Utils.Logger.LogWarning($"fog found {fog.transform.parent.name}");
                    float strengthFactor = Mathf.Pow((1f + CalculateGasStrengthBox(Vector3.zero, true)), 0.1f);
                    fog.ParticleRate *= strengthFactor;
                    fog.OpacityModi *= strengthFactor;
                }
                else 
                {
                    fog.DynamicOpacityModiTarget = ZoneStrengthTargetModi;
                }
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
                    playerBridge.GasRates[this.name] = Mathf.Max(gasAmount * _strengthModi, 0f);
                }

                foreach (var p in playersToRemove)
                {
                    _containedPlayers.Remove(p);
                }

                ModifyVisualEffects(false);

                _tick = 0f;
            }

            _strengthModi = Mathf.Lerp(_strengthModi, ZoneStrengthTargetModi, PluginConfig.test3.Value);
        }

        float CalculateGasStrengthBox(Vector3 playerPosition, bool getStaticValue = false)
        {
            if (_strengthModi == 0f) return 0f;
            if (!UsesDistanceFalloff) return (ZoneStrength * (PluginConfig.ZoneDebug.Value ? PluginConfig.test10.Value : 1f)) / 1000f;
            float distance = getStaticValue ? 1f : Vector3.Distance(playerPosition, _zoneCollider.bounds.center);
            float invertedDistance = _maxDistance - distance;  // invert the distance
            invertedDistance = Mathf.Clamp(invertedDistance, 0, _maxDistance); //clamp the inverted distance
            return invertedDistance / (ZoneStrength * (PluginConfig.ZoneDebug.Value ? PluginConfig.test10.Value : 1f));
        }

        float CalculateGasStrengthSphere(Vector3 playerPosition)
        {
            if (_strengthModi == 0f) return 0f;
            if (!UsesDistanceFalloff) return (ZoneStrength * (PluginConfig.ZoneDebug.Value ? PluginConfig.test10.Value : 1f)) / 1000f;
            float distanceToCenter = Vector3.Distance(playerPosition, _zoneCollider.bounds.center);
            float radius = _zoneCollider.bounds.extents.magnitude;
            float effectiveDistance = Mathf.Max(0, distanceToCenter - radius); 
            float invertedDistance = _maxDistance - effectiveDistance; 
            invertedDistance = Mathf.Clamp(invertedDistance, 0, _maxDistance); 
            return invertedDistance / (ZoneStrength * (PluginConfig.ZoneDebug.Value ? PluginConfig.test10.Value : 1f));
        }
    }

    public class RadiationZone : TriggerWithId, IZone
    {
        public EZoneType ZoneType { get; } = EZoneType.Radiation;
        public float ZoneStrength { get; set; } = 1f;
        public bool BlocksNav { get; set; }
        public bool UsesDistanceFalloff { get; set; }
        public bool IsAnalysable { get; set; } = false;
        public bool HasBeenAnalysed { get; set; } = false;
        public string Name { get; set; }
        public List<GameObject> ActiveDevices { get; set; }
        public InteractableSubZone Interactable { get; set; }

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
            Name = name;
            ActiveDevices = new List<GameObject>();
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
            if (!UsesDistanceFalloff) return (ZoneStrength * (PluginConfig.ZoneDebug.Value ? PluginConfig.test10.Value : 1f)) / 1000f;
            float distance = Vector3.Distance(playerPosition, _zoneCollider.bounds.center);
            float invertedDistance = _maxDistance - distance;  // invert the distance
            invertedDistance = Mathf.Clamp(invertedDistance, 0, _maxDistance); //clamp the inverted distance
            return invertedDistance / (ZoneStrength * (PluginConfig.ZoneDebug.Value ? PluginConfig.test10.Value : 1f));
        }

        float CalculateRadStrengthSphere(Vector3 playerPosition)
        {
            if (!UsesDistanceFalloff) return (ZoneStrength * (PluginConfig.ZoneDebug.Value ? PluginConfig.test10.Value : 1f)) / 1000f;
            float distanceToCenter = Vector3.Distance(playerPosition, _zoneCollider.bounds.center);
            float radius = (_zoneCollider as SphereCollider).radius * transform.localScale.magnitude;
            float distanceFromSurface = radius - distanceToCenter;
            float clampedDistance = Mathf.Max(0f, distanceFromSurface);
            return clampedDistance / (ZoneStrength * (PluginConfig.ZoneDebug.Value ? PluginConfig.test10.Value : 1f));
        }
    }

    public class LabsSafeZone : TriggerWithId, IZone
    {
        const float MAIN_VOLUME = 0.45f;
        const float SHUT_VOLUME = 0.41f;
        const float OPEN_VOLUME = 0.12f;
        public EZoneType ZoneType { get; } = EZoneType.SafeZone;
        public float ZoneStrength { get; set; } = 0f;
        public bool BlocksNav { get; set; }
        public bool UsesDistanceFalloff { get; set; }
        public bool IsActive { get; set; } = true;
        public bool? DoorType { get; set; }
        public bool IsAnalysable { get; set; } = false;
        public bool HasBeenAnalysed { get; set; } = false;
        public string Name { get; set; }
        public List<GameObject> ActiveDevices { get; set; }
        public InteractableSubZone Interactable { get; set; }

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
            Name = name;
            ActiveDevices = new List<GameObject>();
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
