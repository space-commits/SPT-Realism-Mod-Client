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
using EFT.InputSystem;
using EFT.UI.BattleTimer;
using EFT.UI;
using HarmonyLib;

namespace RealismMod
{
    public class ZoneConstants 
    {
        public const float ZONE_LERP_SPEED = 0.05f;
    }

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
        public InteractableSubZone InteractableData { get; set; }
    }

    public class InteractableGroupComponent: MonoBehaviour
    {
        public InteractableGroup GroupData { get; set; }
        public int ComplatedSteps { get { return _completedSteps; } }

        private List<InteractionZone> _interactionZones = new List<InteractionZone>();
        private List<ExfiltrationPoint> _exfils = new List<ExfiltrationPoint>();
        private int _completedSteps = 0;
        private bool _allValvesInCorrectState = false;

        void Start()
        {
            InteractionZone[] interactionZones = GetComponentsInChildren<InteractionZone>();
            foreach (var interactable in interactionZones)
            {
                _interactionZones.Add(interactable);
                interactable.OnInteractableStateChanged += HandleValveStateChanged;
            }

            EvaluateInteractableStates(true);
        }

        private void BlockExtracts() 
        {
            if (_exfils.Count == 0)
            {
                _exfils = GameWorldController.ExfilsInLocation.Where(exfil => GroupData.ExfilsToBlock.Any(name => exfil.Settings.Name == name)).ToList();
                if (PluginConfig.ZoneDebug.Value) Utils.Logger.LogWarning("extract count" + _exfils.Count());
                foreach (var exfil in _exfils)
                {
                    if (PluginConfig.ZoneDebug.Value) Utils.Logger.LogWarning("extract " + exfil.Settings.Name);
                }
            }
            ToggleExtractAvailaibility(false);
        }

        private void ToggleExtractAvailaibility(bool enable)
        {
            foreach (var exfil in _exfils)
            {
                if (PluginConfig.ZoneDebug.Value) Utils.Logger.LogWarning($"{exfil.Settings.Name} set to {enable}");
                BoxCollider[] coliders = exfil.GetComponentsInChildren<BoxCollider>();
                foreach (var col in coliders)
                {
                    col.enabled = enable;
                }
            }
            UpdateExtractPanel();
        }

        private void UpdateExtractPanel()
        {
            if (GameWorldController.GamePlayerOwner == null) return;
            ExtractionTimersPanel extractPanel = (ExtractionTimersPanel)AccessTools.Field(typeof(GamePlayerOwner), "_timerPanel").GetValue(GameWorldController.GamePlayerOwner);
            Dictionary<string, ExitTimerPanel> exitPanels = (Dictionary<string, ExitTimerPanel>)AccessTools.Field(typeof(ExtractionTimersPanel), "dictionary_0").GetValue(extractPanel);
            foreach (var exfil in _exfils)
            {
                Utils.Logger.LogWarning("exfil to block  " + exfil.Settings.Name);
            }
            foreach (var exitPanel in exitPanels)
            {
                ExfiltrationPoint exfil = (ExfiltrationPoint)AccessTools.Field(typeof(EFT.UI.BattleTimer.ExitTimerPanel), "_point").GetValue(exitPanel.Value);
                CustomTextMeshProUGUI pantelText = (CustomTextMeshProUGUI)AccessTools.Field(typeof(EFT.UI.BattleTimer.ExitTimerPanel), "_pointName").GetValue(exitPanel.Value);
                Color defaultColor = (Color)AccessTools.Field(typeof(EFT.UI.BattleTimer.ExitTimerPanel), "_defaultTimerColor").GetValue(exitPanel.Value);

                Utils.Logger.LogWarning("exfil paenl  " + exfil.Settings.Name);

                if (!_exfils.Any(e => e.Settings.Name == exfil.Settings.Name)) continue;
                Utils.Logger.LogWarning("match found");
                Utils.Logger.LogWarning("_allValvesInCorrectState " + _allValvesInCorrectState);
                pantelText.text = exfil.Settings.Name.Localized(null);
                if (!_allValvesInCorrectState) pantelText.text += " BLOCKED BY GAS";
                pantelText.color = _allValvesInCorrectState ? defaultColor : Color.red;

                
            }
        }

        private void HandleValveStateChanged(InteractionZone interactable, EInteractableState newState)
        {
            if (PluginConfig.ZoneDebug.Value) Utils.Logger.LogWarning($"==Valve {interactable.gameObject.name} changed to {newState}");
            EvaluateInteractableStates();
        }

        private void EvaluateInteractableStates(bool isOnInit = false)
        {
            if (PluginConfig.ZoneDebug.Value) Utils.Logger.LogWarning($"is on init? {isOnInit}");
            Dictionary<GasZone, float> gasZones = new Dictionary<GasZone, float>();
            bool allInDesiredState = true;
            _completedSteps = 0;
            foreach (InteractionZone interactable in _interactionZones)
            {
                bool isInDesiredState = interactable.State == interactable.InteractableData.DesiredEndState;
                if (!isInDesiredState)
                {
                    if (PluginConfig.ZoneDebug.Value) Utils.Logger.LogWarning($"Valve {interactable.gameObject.name} is NOT desired state");
                    allInDesiredState = false;
                }
                else
                {
                    if (PluginConfig.ZoneDebug.Value) Utils.Logger.LogWarning($"Valve {interactable.gameObject.name} is in desired state");
                    _completedSteps++;
                }

                foreach (var zone in interactable.HazardZones)
                {
                    GasZone gas = zone as GasZone;
                    if (gas != null)
                    {
                        if (!gasZones.ContainsKey(gas))
                        {
                            gasZones.Add(gas, interactable.InteractableData.FullCompletionModifer);
                        }
        
                        if (isInDesiredState) 
                        {
                            gas.ZoneStrengthTargetModi = interactable.InteractableData.PartialCompletionModifier;
                        }
                        else
                        {
                            gas.ZoneStrengthTargetModi = 1f;
                        }

                        if (PluginConfig.ZoneDebug.Value) Utils.Logger.LogWarning($"Valve {interactable.gameObject.name} is currently {interactable.State}, strength for {gas.gameObject.name} is {gas.ZoneStrengthTargetModi}");
                    }
                }
            }
            _allValvesInCorrectState = allInDesiredState;
            if (allInDesiredState)
            {
                if (PluginConfig.ZoneDebug.Value) Utils.Logger.LogWarning($"All valves in desired state ");
                foreach (var gas in gasZones)
                {
                    gas.Key.ZoneStrengthTargetModi = gas.Value;
                    if (PluginConfig.ZoneDebug.Value) Utils.Logger.LogWarning($"zone {gas.Key.gameObject.name} strength is {gas.Key.ZoneStrengthTargetModi}");
                }
                ToggleExtractAvailaibility(true);
            }
            else 
            {
                BlockExtracts();
            }
        }
    }

    //look for gameobjects it's supposed to interact with, add actions UI to the collider
    //set its own status
    public class InteractionZone : InteractableObject, IZone 
    {
        public const float VALVE_AUDIO_VOL = 0.75f;
        public const float LEAK_AUDIO_VOL = 0.25f;
        public EZoneType ZoneType { get; }
        public float ZoneStrength { get; set; }
        public bool BlocksNav { get; set; }
        public bool UsesDistanceFalloff { get; set; }
        public bool IsAnalysable { get; set; }
        public bool HasBeenAnalysed { get; set; }
        public string Name { get; set; }
        public List<GameObject> ActiveDevices { get; set; }

        public InteractableSubZone InteractableData { get; set; }
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
        public List<IZone> HazardZones = new List<IZone>();

        private AudioSource _valveAudioSource;
        private AudioSource _gasLeakAudioSource;
        private AudioSource _valveStuckSource;
        private GameObject _targetGameObject;
        private InteractableGroupComponent _groupParent;
        private bool _isRotating = false;
        private bool _isDoingStuckRotation = false;

        private float _targetLoopVolume = LEAK_AUDIO_VOL;

        void Start()
        {
            if (InteractableData.Randomize) _state = UnityEngine.Random.Range(0, 100) >= 50 ? EInteractableState.On : EInteractableState.Off;
            else _state = InteractableData.StartingState;
            _groupParent = GetComponentInParent<InteractableGroupComponent>();
            SetUpValveAudio();
            SetUpValveStuckAudio();
            SetUpLeakAudio();
            GetChildObjects();
            GetHazardZones();
            InitActions();
        }

        void Update() 
        {
            _targetLoopVolume = State != this.InteractableData.DesiredEndState ? LEAK_AUDIO_VOL : 0f;
            if (_gasLeakAudioSource.volume != _targetLoopVolume)
            {
                _gasLeakAudioSource.volume = Mathf.MoveTowards(_gasLeakAudioSource.volume, _targetLoopVolume, 0.1f * Time.deltaTime);
            }
        }

        private void GetChildObjects() 
        {
            var box = this.gameObject.GetComponentInParent<BoxCollider>();
            Bounds bounds = box.bounds;
            Collider[] colliders = Physics.OverlapBox(bounds.center, bounds.extents, Quaternion.identity);
            if (PluginConfig.ZoneDebug.Value) Utils.Logger.LogWarning($"Found {colliders.Length} objects in the box collider bounds:");
            foreach (Collider col in colliders)
            {
                if (col.gameObject.name == InteractableData.TargeObject)
                {
                    _targetGameObject = col.gameObject.transform.parent.gameObject;
                    if (PluginConfig.ZoneDebug.Value) Utils.Logger.LogWarning("===match");
                }
            }
        }

        private void GetHazardZones() 
        {
            foreach (var targetName in InteractableData.ZoneNames)
            {
                GameObject zone = GameObject.Find(targetName);
                if (zone != null)
                {
                    if (PluginConfig.ZoneDebug.Value) Utils.Logger.LogWarning($"found hazard zone GO {targetName}");
                    IZone hazard = zone.GetComponent<IZone>();
                    if (hazard != null)
                    {
                        if (PluginConfig.ZoneDebug.Value) Utils.Logger.LogWarning($"found hazard component");
                        HazardZones.Add(hazard);
                    }
                }
            }
        }

        private void SetUpValveStuckAudio()
        {
            _valveStuckSource = this.gameObject.AddComponent<AudioSource>();
            _valveStuckSource.clip = Plugin.InteractableClips["valve_stuck.wav"];
            _valveStuckSource.volume = VALVE_AUDIO_VOL;
            _valveStuckSource.loop = false;
            _valveStuckSource.playOnAwake = false;
            _valveStuckSource.spatialBlend = 1.0f;
            _valveStuckSource.minDistance = 1.5f;
            _valveStuckSource.maxDistance = 5f;
            _valveStuckSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        private void SetUpValveAudio()
        {
            _valveAudioSource = this.gameObject.AddComponent<AudioSource>();
            _valveAudioSource.clip = Plugin.InteractableClips["valve_loop_3.wav"];
            _valveAudioSource.volume = VALVE_AUDIO_VOL;
            _valveAudioSource.loop = false;
            _valveAudioSource.playOnAwake = false;
            _valveAudioSource.spatialBlend = 1.0f;
            _valveAudioSource.minDistance = 1.5f;
            _valveAudioSource.maxDistance = 5f;
            _valveAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        private void SetUpLeakAudio()
        {
            _gasLeakAudioSource = this.gameObject.AddComponent<AudioSource>();
            _gasLeakAudioSource.clip = Plugin.InteractableClips["gas_leak.wav"];
            _gasLeakAudioSource.volume = LEAK_AUDIO_VOL;
            _gasLeakAudioSource.loop = true;
            _gasLeakAudioSource.playOnAwake = false;
            _gasLeakAudioSource.spatialBlend = 1.0f;
            _gasLeakAudioSource.minDistance = 1.5f;
            _gasLeakAudioSource.maxDistance = 5f;
            _gasLeakAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            _gasLeakAudioSource.Play();
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

        private IEnumerator PlayRandomValveClipLoop()
        {
            while (_isRotating)
            {
                if (!_valveAudioSource.isPlaying) 
                {
                    string[] clips = { "valve_loop_1.wav", "valve_loop_2.wav", "valve_loop_3.wav" };
                    string clip = clips[UnityEngine.Random.Range(0, clips.Length)];
                    _valveAudioSource.clip = Plugin.InteractableClips[clip];
                    _valveAudioSource.Play();
                }
                yield return new WaitForSeconds(_valveAudioSource.clip.length - 1f);
            }
        }

        private IEnumerator StopValveAudioLoop()
        {
            if (_valveAudioSource.isPlaying)
            {
                yield return new WaitForSeconds(_valveAudioSource.clip.length - _valveAudioSource.time);
            }
            _valveAudioSource.Stop();
        }

        private void Rotate(ref float elapsedTime, float duration, float maxSpeed) 
        {
            float t = elapsedTime / duration;
            float speedModifier = Mathf.Sin(t * Mathf.PI);
            float currentSpeed = maxSpeed * speedModifier;
            _targetGameObject.transform.Rotate(0, 0, currentSpeed * Time.deltaTime);
            elapsedTime += Time.deltaTime;
        }

        IEnumerator RotateValveOverTime(float maxSpeed, float duration)
        {
            if (_isRotating || _isDoingStuckRotation) yield break;
            _isRotating = true;
            float elapsedTime = 0f;
            StartCoroutine(PlayRandomValveClipLoop());
            StartCoroutine(AdjustVolume(VALVE_AUDIO_VOL, 1f, _valveAudioSource));
            while (elapsedTime < duration)
            {
                Rotate(ref elapsedTime, duration, maxSpeed);
                yield return null;
            }
            StartCoroutine(AdjustVolume(0f, 1f, _valveAudioSource));
            yield return StartCoroutine(StopValveAudioLoop());
            _isRotating = false;
        }


        IEnumerator RotateStuck(float maxSpeed, float duration)
        {
            if (_isRotating || _isDoingStuckRotation) yield break;
            _isDoingStuckRotation = true;
            _valveStuckSource.Play();
            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                Rotate(ref elapsedTime, duration, maxSpeed);
                yield return null;
            }
            elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                Rotate(ref elapsedTime, duration, -maxSpeed);
                yield return null;
            }
            _isDoingStuckRotation = false;
        }

        void InitActions()
        {
            this.gameObject.layer = LayerMask.NameToLayer("Interactive");

            InteractableActions.AddRange(
                new List<ActionsTypesClass>()
                {
                    new ActionsTypesClass
                    {
                        Name = "Turn Clockwise",
                        Action = TurnON
                    },
                    new ActionsTypesClass
                    {
                        Name = "Turn Anticlockwise",
                        Action = TurnOff
                    }
                }
             );
        }

        public void Log() 
        {
            Utils.Logger.LogWarning($"{gameObject.name} state is {State}");
        }

        private bool CanTurn(EInteractableState nextState) 
        {
            bool completed = _groupParent.ComplatedSteps >= InteractableData.CompletionStep;
            bool isSameState = State == nextState;
            if (!completed || isSameState) 
            {
                float direction = nextState == EInteractableState.On ? 100f : -100f;
                StartCoroutine(RotateStuck(direction, 0.25f));
            }
            return completed && !isSameState;
        }

        //turn SFX on/off, trigger action
        public void TurnON() 
        {
            if (_isRotating || _isDoingStuckRotation || !CanTurn(EInteractableState.On)) return;
            StopAllCoroutines();
            StartCoroutine(RotateValveOverTime(300f, 5f));
            State = EInteractableState.On;
        }

        public void TurnOff()
        {
            if (_isRotating || _isDoingStuckRotation || !CanTurn(EInteractableState.Off)) return;
            StopAllCoroutines();
            StartCoroutine(RotateValveOverTime(-300f, 5f));
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
        public InteractableSubZone InteractableData { get; set; }

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
        public InteractableSubZone InteractableData { get; set; }

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

            _strengthModi = Mathf.Lerp(_strengthModi, ZoneStrengthTargetModi, ZoneConstants.ZONE_LERP_SPEED * Time.deltaTime);
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
        public InteractableSubZone InteractableData { get; set; }

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
        public InteractableSubZone InteractableData { get; set; }

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
