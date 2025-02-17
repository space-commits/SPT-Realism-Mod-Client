using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.BattleTimer;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KeyInteractionResultClass = GClass3344;

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
        private List<ExfiltrationPoint> _exfilsToBlock = new List<ExfiltrationPoint>();
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
            if (_exfilsToBlock.Count == 0)
            {
                _exfilsToBlock = GameWorldController.ExfilsInLocation.Where(exfil => GroupData.ExfilsToBlock.Any(name => exfil.Settings.Name == name)).ToList();
                if (PluginConfig.ZoneDebug.Value) Utils.Logger.LogWarning("extract count" + _exfilsToBlock.Count());
                foreach (var exfil in _exfilsToBlock)
                {
                    if (PluginConfig.ZoneDebug.Value) Utils.Logger.LogWarning("extract " + exfil.Settings.Name);
                }
            }
            ToggleExtractAvailaibility(_exfilsToBlock, false);
        }

        private void ToggleExtractAvailaibility(List<ExfiltrationPoint> exfils, bool enable)
        {
            foreach (var exfil in exfils)
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

            foreach (var exitPanel in exitPanels)
            {
                ExfiltrationPoint exfil = (ExfiltrationPoint)AccessTools.Field(typeof(EFT.UI.BattleTimer.ExitTimerPanel), "_point").GetValue(exitPanel.Value);
#pragma warning disable CS0618
                CustomTextMeshProUGUI pantelText = (CustomTextMeshProUGUI)AccessTools.Field(typeof(EFT.UI.BattleTimer.ExitTimerPanel), "_pointName").GetValue(exitPanel.Value);
#pragma warning restore CS0618
                Color defaultColor = (Color)AccessTools.Field(typeof(EFT.UI.BattleTimer.ExitTimerPanel), "_defaultTimerColor").GetValue(exitPanel.Value);

                if (!_exfilsToBlock.Any(e => e.Settings.Name == exfil.Settings.Name)) continue;
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

        private void EvaluateInteractableStates(bool isInit = false)
        {
            if (PluginConfig.ZoneDebug.Value) Utils.Logger.LogWarning($"is init? {isInit}");
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
                ToggleExtractAvailaibility(_exfilsToBlock, true);
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
        public const float VALVE_AUDIO_VOL = 0.6f;
        public const float VALVE_STUCK_AUDIO_VOL = 0.5f;
        public const float LEAK_AUDIO_VOL = 0.15f;
        public const float BUTTON_AUDIO_VOL = 0.2f;
        public const float PANEL_LOOP_VOL = 0.4f;
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
        private AudioSource _loopAudioSource;
        private AudioSource _valveStuckSource;
        private AudioSource _buttonSource;
        private AudioSource _panelAudioSource;
        private GameObject _targetGameObject;
        private InteractableGroupComponent _groupParent;
        private bool _isRotating = false;
        private bool _buttonIsPressing = false;
        private bool _isDoingStuckRotation = false;

        private float _targetLoopVolume = 1f;
        private float _loopLerpSpeed = 0.1f;
        private float _baseLoopAudioVol = 1f;
        private bool _playLoopOnDesiredState = false;
        private string _onString = "On";
        private string _offString = "Off";
        private System.Action _onAction;
        private System.Action _offAction;

        void Start()
        {
            _onAction = InteractableData.InteractionType == EIneractableType.Valve ? TurnONValve : TurnOnButton;
            _offAction = InteractableData.InteractionType == EIneractableType.Valve ? TurnOffValve : TurnOffButton;
            _onString = InteractableData.InteractionType == EIneractableType.Valve ? "Turn Clockwise" : "Turn On";
            _offString = InteractableData.InteractionType == EIneractableType.Valve ? "Turn AntiClockwise" : "Turn Off";
            _playLoopOnDesiredState = InteractableData.InteractionType != EIneractableType.Valve;
            if (InteractableData.Randomize) _state = UnityEngine.Random.Range(0, 100) >= 50 ? EInteractableState.On : EInteractableState.Off;
            else _state = InteractableData.StartingState;
            _groupParent = GetComponentInParent<InteractableGroupComponent>();
            SetUpAudio();
            if (!string.IsNullOrWhiteSpace(InteractableData.TargeObject)) GetChildObjects();
            GetHazardZones();
            InitActions();
        }

        void Update() 
        {
            LoopAudio(_loopAudioSource, _baseLoopAudioVol, _loopLerpSpeed);
        }

        private void LoopAudio(AudioSource audioSource, float baseVolume, float speed) 
        {
            if (audioSource == null) return;
            bool isDesiredState = State == this.InteractableData.DesiredEndState;
            bool shouldDoLoop = (!_playLoopOnDesiredState && !isDesiredState) || (_playLoopOnDesiredState && isDesiredState);
            _targetLoopVolume = (shouldDoLoop ? baseVolume : 0f);
            if (audioSource.volume != _targetLoopVolume)
            {
                audioSource.volume = Mathf.MoveTowards(audioSource.volume, _targetLoopVolume, speed * Time.deltaTime);
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

        private void SetUpAudio() 
        {
            SetUpValveAudio();
            SetUpValveStuckAudio();
            SetUpButtonAudio();
            SetUpPanelLoopAudio();
            SetUpLeakAudio();
            _loopAudioSource.Play();
        }

        private void SetUpButtonAudio()
        {
            _buttonSource = this.gameObject.AddComponent<AudioSource>();
            _buttonSource.clip = Plugin.InteractableClips["buttonpress.wav"];
            _buttonSource.volume = BUTTON_AUDIO_VOL * GameWorldController.GetGameVolumeAsFactor(); 
            _buttonSource.loop = false;
            _buttonSource.playOnAwake = false;
            _buttonSource.spatialBlend = 1.0f;
            _buttonSource.minDistance = 1.5f;
            _buttonSource.maxDistance = 5f;
            _buttonSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        private void SetUpValveStuckAudio()
        {
            _valveStuckSource = this.gameObject.AddComponent<AudioSource>();
            _valveStuckSource.clip = Plugin.InteractableClips["valve_stuck.wav"];
            _valveStuckSource.volume = VALVE_STUCK_AUDIO_VOL * GameWorldController.GetGameVolumeAsFactor();
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
            _valveAudioSource.volume = VALVE_AUDIO_VOL * GameWorldController.GetGameVolumeAsFactor();
            _valveAudioSource.loop = false;
            _valveAudioSource.playOnAwake = false;
            _valveAudioSource.spatialBlend = 1.0f;
            _valveAudioSource.minDistance = 1.5f;
            _valveAudioSource.maxDistance = 5f;
            _valveAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        private void SetUpPanelLoopAudio()
        {
            _panelAudioSource = this.gameObject.AddComponent<AudioSource>();
            _panelAudioSource.clip = Plugin.InteractableClips["panel_hum_loop.wav"];
            _panelAudioSource.volume = PANEL_LOOP_VOL * GameWorldController.GetGameVolumeAsFactor();
            _panelAudioSource.loop = true;
            _panelAudioSource.playOnAwake = false;
            _panelAudioSource.spatialBlend = 1.0f;
            _panelAudioSource.minDistance = 1.5f;
            _panelAudioSource.maxDistance = 15f;
            _panelAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        private void SetUpLeakAudio()
        {
            _loopAudioSource = this.gameObject.AddComponent<AudioSource>();
            _loopAudioSource.clip = InteractableData.InteractionType == EIneractableType.Valve ? Plugin.InteractableClips["gas_leak.wav"] : Plugin.InteractableClips["panel_hum_loop.wav"];
            _loopAudioSource.volume = (InteractableData.InteractionType == EIneractableType.Valve ? LEAK_AUDIO_VOL : PANEL_LOOP_VOL) * GameWorldController.GetGameVolumeAsFactor(); 
            _baseLoopAudioVol = _loopAudioSource.volume;
            _loopLerpSpeed = InteractableData.InteractionType == EIneractableType.Valve ? 0.075f :0.5f;
            _loopAudioSource.loop = true;
            _loopAudioSource.playOnAwake = false;
            _loopAudioSource.spatialBlend = 1.0f;
            _loopAudioSource.minDistance = 1.5f;
            _loopAudioSource.maxDistance = 5f;
            _loopAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            _loopAudioSource.Play();
        }

        IEnumerator AdjustVolume(float targetVolume, float speed, AudioSource audioSource)
        {
            targetVolume *= GameWorldController.GetGameVolumeAsFactor();
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

        private IEnumerator PlayButtonPressAudio(bool isTurningOn, bool isBlocked, bool sameState)
        {
            if (_buttonIsPressing && !_buttonSource.isPlaying)
            {
                if (isBlocked || isTurningOn) PlaySoundForAI();
                string file = isTurningOn ? "buttonpress.wav" : isBlocked ? "buttonpress_blocked.wav" : "buttonpress_do_nothing.wav";
                _buttonSource.clip = Plugin.InteractableClips[file]; 
                _buttonSource.Play();
            }
            yield return new WaitForSeconds(_buttonSource.clip.length + 1f);
        }

        private IEnumerator StopValveAudioLoop()
        {
            if (_valveAudioSource.isPlaying)
            {
                yield return new WaitForSeconds(_valveAudioSource.clip.length - _valveAudioSource.time);
            }
            _valveAudioSource.Stop();
        }

        private void RotateValve(ref float elapsedTime, float duration, float maxSpeed) 
        {
            float t = elapsedTime / duration;
            float speedModifier = Mathf.Sin(t * Mathf.PI);
            float currentSpeed = maxSpeed * speedModifier;
            _targetGameObject.transform.Rotate(0, 0, currentSpeed * Time.deltaTime);
            elapsedTime += Time.deltaTime;
        }

        IEnumerator ToggleButton(bool isTurningOn, bool isBlocked, bool sameState)
        {
            if (_buttonIsPressing) yield break;
            _buttonIsPressing = true;
            yield return StartCoroutine(PlayButtonPressAudio(isTurningOn, isBlocked, sameState));
            _buttonIsPressing = false;
        }

        IEnumerator RotateStuck(float maxSpeed, float duration)
        {
            if (_isRotating || _isDoingStuckRotation) yield break;
            PlaySoundForAI();
            _isDoingStuckRotation = true;
            _valveStuckSource.Play();
            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                RotateValve(ref elapsedTime, duration, maxSpeed);
                yield return null;
            }
            elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                RotateValve(ref elapsedTime, duration, -maxSpeed);
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
                        Name = _onString,
                        Action = _onAction
                    },
                    new ActionsTypesClass
                    {
                        Name = _offString,
                       Action = _offAction
                    }
                }
             );
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
                RotateValve(ref elapsedTime, duration, maxSpeed);
                yield return null;
            }
            StartCoroutine(AdjustVolume(0f, 1f, _valveAudioSource));
            yield return StartCoroutine(StopValveAudioLoop());
            _isRotating = false;
        }

        public void Log() 
        {
            Utils.Logger.LogWarning($"{gameObject.name} state is {State}");
        }

        protected void PlaySoundForAI()
        {
            Utils.GetYourPlayer().MovementContext.PlayBreachSound();
        }

        private bool CanTurnValve(EInteractableState nextState) 
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

        private bool CanTurnOn(EInteractableState nextState)
        {
            bool completed = _groupParent.ComplatedSteps >= InteractableData.CompletionStep;
            bool isSameState = State == nextState;
            bool isGoingToBeOn = State != nextState && nextState == EInteractableState.On;
            if (!completed || isSameState)
            {
                StartCoroutine(ToggleButton(false, !completed, isSameState));
            }
            return completed && !isSameState;
        }

        //need a "can turn on button" method with its own sfx
        public void TurnOnButton()
        {
            //GearController.DoInteractionAnimation(Utils.GetYourPlayer(), EInteraction.DoorCardOpen); other button presses in the game don't use an animation, and it looks a bit jank
            if (_buttonIsPressing || !CanTurnOn(EInteractableState.On)) return;
            StopAllCoroutines();
            StartCoroutine(ToggleButton(true, false, false));
            State = EInteractableState.On;
        }

        public void TurnOffButton()
        {
            //GearController.DoInteractionAnimation(Utils.GetYourPlayer(), EInteraction.DoorCardOpen);
            if (_buttonIsPressing || !CanTurnOn(EInteractableState.Off)) return;
            StopAllCoroutines();
            StartCoroutine(ToggleButton(true, false, false));
            State = EInteractableState.Off;
        }

        public void TurnONValve()
        {
            if (_isRotating || _isDoingStuckRotation || !CanTurnValve(EInteractableState.On)) return;
            StopAllCoroutines();
            StartCoroutine(RotateValveOverTime(300f, 5f));
            State = EInteractableState.On;
        }

        public void TurnOffValve()
        {
            if (_isRotating || _isDoingStuckRotation || !CanTurnValve(EInteractableState.Off)) return;
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
            _mainAudioSource.volume = MAIN_VOLUME * GameWorldController.GetGameVolumeAsFactor();
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
            _doorShutAudioSource.volume = SHUT_VOLUME * GameWorldController.GetGameVolumeAsFactor();
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
            _doorOpenAudioSource.volume = OPEN_VOLUME * GameWorldController.GetGameVolumeAsFactor();
            _doorOpenAudioSource.loop = false;
            _doorOpenAudioSource.playOnAwake = false;
            _doorOpenAudioSource.spatialBlend = 1.0f;
            _doorOpenAudioSource.minDistance = 3.5f;
            _doorOpenAudioSource.maxDistance = 15f;
            _doorOpenAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        IEnumerator AdjustVolume(float targetVolume, float speed, AudioSource audioSource)
        {
            targetVolume *= GameWorldController.GetGameVolumeAsFactor();
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
