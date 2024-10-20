using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using UnityEngine;
using static RootMotion.FinalIK.IKSolver;

namespace RealismMod
{
    public class TransmitterHalloweenEvent : Transmitter 
    {
        public bool TriggeredExplosion {  get; private set; }   

        protected override IEnumerator DoLogic()
        {
            CanTurnOn = false;
            float time = 0f;
            float clipLength = 0f;
            Plugin.RequestRealismDataFromServer(EUpdateType.TimeOfDay);

            while (time < 0.5f)
            {
                time += Time.deltaTime;
                yield return null;
            }

            bool canTrigger = GameWorldController.IsRightDateForExp && IsInRightLocation() && Plugin.ModInfo.IsHalloween && !Plugin.ModInfo.HasExploded && !GameWorldController.DidExplosionClientSide && Plugin.ModInfo.IsNightTime;
            if (canTrigger) 
            {
                _audioSource.clip = Plugin.DeviceAudioClips["transmitter_success.wav"];
                _audioSource.Play();
                SpawnQuestTrigger();
                TriggeredExplosion = true;
                AddSelfToDevicesList();
                DeactivateZone();
                PlaySoundForAI();
            }
            else
            {
                _audioSource.clip = Plugin.DeviceAudioClips["transmitter_fail.wav"];
                clipLength = _audioSource.clip.length;
                _audioSource.Play();
                PlaySoundForAI();
                while (time <= clipLength)
                {
                    time += Time.deltaTime;
                    yield return null;
                }
                CanTurnOn = true;
                yield break;
            }

            clipLength = _audioSource.clip.length;
            while (time <= clipLength)
            {
                time += Time.deltaTime;
                yield return null;
            }

            Instantiate(Plugin.ExplosionGO, new Vector3(-700f, 3f, -1200f), new Quaternion(0, 0, 0, 0));
        }
    }

    public class Transmitter : MonoBehaviour
    {
        public List<ActionsTypesClass> Actions = new List<ActionsTypesClass>();
        public bool CanTurnOn { get; protected set; } = true;
        public LootItem _LootItem { get; set; }
        public Player _Player { get; set; } = null;
        public IPlayer _IPlayer { get; set; } = null;
        public string[] TargetQuestZones { get; set; }
        public EZoneType TargetZoneType { get; private set; } = EZoneType.Quest;
        public IZone TargetZone { get; private set; } = null;
        public AudioClip AudioClips { get; set; }
        public string QuestTrigger { get; set; }    
        protected AudioSource _audioSource;
        protected Vector3 _position;
        protected Quaternion _rotation;
        protected List<IZone> _intersectingZones = new List<IZone>();
        public string _instanceId = "";
        protected float _placementTimer = 0;
        protected bool _placedItem = false;

        protected void SetUpTransforms()
        {
            Vector3 eularRotation = _Player.gameObject.transform.rotation.eulerAngles;
            eularRotation.x = -90f;
            eularRotation.y = 0f;
            _rotation = Quaternion.Euler(new Vector3(eularRotation.x, eularRotation.y, eularRotation.z));
        }

        protected AudioSource SetUpAudio(string clip, GameObject go)
        {
            AudioSource audioSource = go.AddComponent<AudioSource>();
            audioSource.clip = Plugin.DeviceAudioClips[clip];
            audioSource.volume = 1f;
            audioSource.loop = false;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f;
            audioSource.minDistance = 0.8f;
            audioSource.maxDistance = 35f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            return audioSource;
        }

        protected void PlaySoundForAI()
        {
            if (Singleton<BotEventHandler>.Instantiated)
            {
                Singleton<BotEventHandler>.Instance.PlaySound(_IPlayer, this.transform.position, 40f, AISoundType.step);
            }
        }

        protected void AddSelfToDevicesList()
        {
            if (TargetZone == null) return;
            TargetZone.ActiveDevices.Add(this.gameObject);
        }

        protected void DeleteSelfFromDevicesList()
        {
            if (TargetZone == null) return;
            TargetZone.ActiveDevices.Remove(this.gameObject);
        }

        public bool ZoneAlreadyHasDevice()
        {
            if (TargetZone == null) return false;
            TargetZone.ActiveDevices.RemoveAll(d => d == null || !d.activeSelf || !d.gameObject.activeSelf);
            List<Transmitter> transmitters = new List<Transmitter>();
            foreach (var device in TargetZone.ActiveDevices)
            {
                if (device.gameObject.TryGetComponent<Transmitter>(out Transmitter analyser))
                {
                    transmitters.Add(analyser);
                }
            }
            foreach (var a in transmitters)
            {
                if (a._instanceId != _instanceId)
                {
                    return true;
                }
            }
            return false;
        }

        protected void DeactivateZone()
        {
            if (TargetZone == null) return;
            TargetZone.HasBeenAnalysed = true;
        }

        protected void GetTargetZone(IZone zone, EZoneType[] targetZones)
        {
            if (TargetZone != null || zone == null) return;
            bool foundMatch = TargetZoneType == EZoneType.Quest && zone.ZoneType == EZoneType.Quest;
            if (zone.IsAnalysable && foundMatch) TargetZone = zone;
            return;
        }

        void OnDisable()
        {
            DeleteSelfFromDevicesList();
        }

        protected void SetUpActions()
        {
            Actions.AddRange(new List<ActionsTypesClass>()
            {
                    new ActionsTypesClass
                    {
                        Name = "Activate",
                        Action = Activate
                    }
            });
        }

        protected bool IsInRightLocation()
        {
            foreach (var zone in _intersectingZones)
            {
                QuestZone questZone;
                if ((questZone = (zone as QuestZone)) != null)
                {
                    TargetQuestZones.Contains(questZone.name);
                    return true;
                }

            }
            return false;
        }

        protected void RemovePhysicsInteractions(GameObject go) 
        {
            var rb = go.GetComponent<Rigidbody>();
            var col = go.GetComponent<Collider>();

            if (rb != null)
            {
                Utils.Logger.LogWarning("removing rb");
                rb.useGravity = false;
                rb.isKinematic = true;  
            }

            if (col != null)
            {
                Utils.Logger.LogWarning("removing col");
                col.enabled = false;
            }
        }

        protected void Start()
        {
            RemovePhysicsInteractions(this.gameObject);
            SetUpTransforms();
            SetUpActions();
            _instanceId = MongoID.Generate();
            _audioSource = SetUpAudio("switch_off.wav", this.gameObject);
        }

        protected void Update()
        {
            _placementTimer += Time.deltaTime;
            if (this.gameObject == null) return;

            RaycastHit raycastHit;
            if (!_placedItem && EFTPhysicsClass.Raycast(new Ray(_Player.PlayerBones.LootRaycastOrigin.position + _Player.PlayerBones.LootRaycastOrigin.forward / 2f, _Player.PlayerBones.LootRaycastOrigin.forward), out raycastHit, 2.5f, LayerMaskClass.HighPolyWithTerrainMask))
            {
                Utils.Logger.LogWarning(Mathf.Abs(raycastHit.point.y - _Player.Transform.position.y));
                if (Mathf.Abs(raycastHit.point.y - _Player.Transform.position.y) <= 0.08f)
                {
                    _position = raycastHit.point;
                    _position.y += 0.055f;
                    this.gameObject.transform.position = _position;
                    this.gameObject.transform.rotation = _rotation;
                    _placedItem = true;
                    RemovePhysicsInteractions(this.gameObject);
                }

            }
            if (!_placedItem && _placementTimer >= 0.1f)
            {
                _position = _Player.gameObject.transform.position;
                _position.y += 0.055f;
                this.gameObject.transform.position = _position;
                this.gameObject.transform.rotation = _rotation;
                _placedItem = true;
                RemovePhysicsInteractions(this.gameObject);
            }
        }

        protected void OnTriggerEnter(Collider other)
        {
            IZone hazardZone;
            EZoneType[] targetZones = TargetZoneType == EZoneType.Quest ? new EZoneType[] { EZoneType.Quest } : new EZoneType[] { EZoneType.Quest }; //might change in future
            if (other.gameObject.TryGetComponent<IZone>(out hazardZone))
            {
                _intersectingZones.Add(hazardZone);
                GetTargetZone(hazardZone, targetZones);
            }
        }

        protected void Activate()
        {
            if (CanTurnOn)
            {
                StartCoroutine(DoLogic());
            }
        }

        protected virtual IEnumerator DoLogic()
        {
            float time = 0f;
            if (IsInRightLocation())
            {
                _audioSource.clip = Plugin.DeviceAudioClips["numbers.wav"];
                _audioSource.Play();
                AddSelfToDevicesList();
                CanTurnOn = false;
            }
            else
            {
                _audioSource.clip = Plugin.DeviceAudioClips["switch_off.wav"];
                _audioSource.Play();
                CanTurnOn = true;
                yield break;
            }

            float clipLength = _audioSource.clip.length;
            while (time <= clipLength)
            {
                PlaySoundForAI();
                time += Time.deltaTime;
                yield return null;
            }
            SpawnQuestTrigger();
            DeactivateZone();
            DeleteSelfFromDevicesList();
        }

        protected void SpawnQuestTrigger()
        {
            var questLocation = ZoneData.QuestZoneLocations.Shoreline.FirstOrDefault(hl => hl.Zones.Any(z => z.Name == QuestTrigger));
            questLocation.IsTriggered = false;
            ZoneSpawner.CreateZone<QuestZone>(questLocation, EZoneType.Quest);
        }
    }

    public class HazardAnalyser : MonoBehaviour
    {
        public List<ActionsTypesClass> Actions = new List<ActionsTypesClass>();
        public bool CanTurnOn { get; private set; } = true;
        public LootItem _LootItem { get; set; }
        public Player _Player { get; set; } = null;
        public IPlayer _IPlayer { get; set; } = null;
        public EZoneType TargetZoneType { get; set; }
        public IZone TargetZone { get; private set; } = null;
        public AudioClip AudioClips { get; set; }
        private AudioSource _audioSource;
        private Vector3 _position;
        private Quaternion _rotation;
        private List<IZone> _intersectingZones = new List<IZone>();
        private float _stallChance = 90f;
        private bool _stalledPreviously = false;
        public string _instanceId = "";
        private bool _deactivated = false;
        private float _placementTimer = 0;
        private bool _placedItem = false;

        void SetUpTransforms()
        {
       /*     _position = _Player.gameObject.transform.position;
            _position.y += 0.04f;
            _position = _position + _Player.gameObject.transform.forward * 1.1f;*/

            Vector3 eularRotation = _Player.gameObject.transform.rotation.eulerAngles;
            eularRotation.x = -90f;
            eularRotation.y = 0f;
            _rotation = Quaternion.Euler(new Vector3(eularRotation.x, eularRotation.y, eularRotation.z));

            if (PluginConfig.ZoneDebug.Value)
            {
                GameObject visualRepresentation = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visualRepresentation.name ="deviceStartPos";
                visualRepresentation.transform.localScale = new Vector3(0.5f, 0.5f, 100f);
                visualRepresentation.transform.position = _position;
                visualRepresentation.transform.rotation = _rotation;
                visualRepresentation.GetComponent<Renderer>().material.color = UnityEngine.Color.green;
                UnityEngine.Object.Destroy(visualRepresentation.GetComponent<Collider>()); // Remove the collider from the visual representation
            }
        }

        private void SetUpActions()
        {
            Actions.AddRange(new List<ActionsTypesClass>()
            {
                    new ActionsTypesClass
                    {
                        Name = "Turn On",
                        Action = TurnOn
                    }
            });
        }

        private AudioSource SetUpAudio(string clip, GameObject go)
        {
            AudioSource audioSource = go.AddComponent<AudioSource>();
            audioSource.clip = Plugin.DeviceAudioClips[clip];
            audioSource.volume = 1f;
            audioSource.loop = false;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f;
            audioSource.minDistance = 0.75f;
            audioSource.maxDistance = 30f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            return audioSource;
        }

        private void GetTargetZone(IZone zone, EZoneType[] targetZones)
        {
            if (TargetZone != null || zone == null) return;
            bool foundRadMatch = TargetZoneType == EZoneType.Radiation && (zone.ZoneType == EZoneType.RadAssets || zone.ZoneType == EZoneType.Radiation);
            bool foundToxMatch = TargetZoneType == EZoneType.Gas && (zone.ZoneType == EZoneType.Gas || zone.ZoneType == EZoneType.GasAssets);
            if (zone.IsAnalysable && (foundRadMatch || foundToxMatch)) TargetZone = zone;
            return;
        }

        public bool CheckIfZoneTypeMatches()
        {
            if (_intersectingZones.Any(z => z.ZoneType == EZoneType.SafeZone)) return false;
            return TargetZone != null;
        }

        private void AddSelfToDevicesList()
        {
            if (TargetZone == null) return;
            TargetZone.ActiveDevices.Add(this.gameObject);
        }

        private void DeleteSelfFromDevicesList()
        {
            if (TargetZone == null) return;
            TargetZone.ActiveDevices.Remove(this.gameObject);
        }

        public bool ZoneAlreadyHasDevice()
        {
            if (TargetZone == null) return false;
            TargetZone.ActiveDevices.RemoveAll(d => d == null || !d.activeSelf || !d.gameObject.activeSelf);
            List<HazardAnalyser> anaylsers = new List<HazardAnalyser>();
            foreach (var device in TargetZone.ActiveDevices)
            {
                if (device.gameObject.TryGetComponent<HazardAnalyser>(out HazardAnalyser analyser))
                {
                    anaylsers.Add(analyser);
                }
            }
            foreach (var a in anaylsers)
            {
                if (a._instanceId != _instanceId)
                {
                    return true;
                }
            }
            return false;
        }

        private void DeactivateZone()
        {
            if (TargetZone == null) return;
            TargetZone.HasBeenAnalysed = true;
        }

        protected void RemovePhysicsInteractions(GameObject go)
        {
            var rb = go.GetComponent<Rigidbody>();
            var col = go.GetComponent<Collider>();

            if (rb != null)
            {
                Utils.Logger.LogWarning("removing rb");
                rb.useGravity = false;
                rb.isKinematic = true;
            }

            if (col != null)
            {
                Utils.Logger.LogWarning("removing col");
                col.enabled = false;
            }
        }

        void Start()
        {
            SetUpTransforms();
            SetUpActions();
            _audioSource = SetUpAudio("switch_off.wav", this.gameObject);
            _instanceId = MongoID.Generate();
            _stallChance = TargetZoneType == EZoneType.Radiation ? 100f : 90f;
        }

        private void OnTriggerEnter(Collider other)
        {
            IZone hazardZone;
            EZoneType[] targetZones = TargetZoneType == EZoneType.Gas ? new EZoneType[] { EZoneType.Gas, EZoneType.GasAssets } : new EZoneType[] { EZoneType.Radiation, EZoneType.RadAssets };
            if (other.gameObject.TryGetComponent<IZone>(out hazardZone))
            {
                _intersectingZones.Add(hazardZone);
                GetTargetZone(hazardZone, targetZones);
            }
        }

        void Update()
        {
            _placementTimer += Time.deltaTime;
            if (this.gameObject == null || _deactivated) return;

            RaycastHit raycastHit;
            if (!_placedItem && EFTPhysicsClass.Raycast(new Ray(_Player.PlayerBones.LootRaycastOrigin.position + _Player.PlayerBones.LootRaycastOrigin.forward / 2f, _Player.PlayerBones.LootRaycastOrigin.forward), out raycastHit, 2.5f, LayerMaskClass.HighPolyWithTerrainMask))
            {
                Utils.Logger.LogWarning(Mathf.Abs(raycastHit.point.y - _Player.Transform.position.y));
                if (Mathf.Abs(raycastHit.point.y - _Player.Transform.position.y) <= 0.08f)
                {
                    _position = raycastHit.point;
                    _position.y += 0.055f;
                    this.gameObject.transform.position = _position;
                    this.gameObject.transform.rotation = _rotation;
                    _placedItem = true;
                    RemovePhysicsInteractions(this.gameObject);
                }

            }
            if (!_placedItem && _placementTimer >= 0.1f) 
            {
                _position = _Player.gameObject.transform.position;
                _position.y += 0.055f;
                this.gameObject.transform.position = _position;
                this.gameObject.transform.rotation = _rotation;
                _placedItem = true;
                RemovePhysicsInteractions(this.gameObject);
            }
        }

        private void PlaySoundForAI()
        {
            if (Singleton<BotEventHandler>.Instantiated)
            {
                Singleton<BotEventHandler>.Instance.PlaySound(_IPlayer, this.transform.position, 40f, AISoundType.step);
            }
        }

        private void TurnOn()
        {
            if (CanTurnOn)
            {
                StartCoroutine(DoLogic());
            }
        }

        IEnumerator DoLogic()
        {
            PlaySoundForAI();
            float time = 0f;
            if (CheckIfZoneTypeMatches())
            {
                _audioSource.clip = Plugin.DeviceAudioClips["start_success.wav"];
                _audioSource.Play();
                CanTurnOn = false;
                AddSelfToDevicesList();
            }
            else
            {
                _audioSource.clip = Plugin.DeviceAudioClips["failed_start.wav"];
                _audioSource.Play();
                CanTurnOn = true;
                yield break;
            }

            float clipLength = _audioSource.clip.length;
            while (time <= clipLength - 0.15f)
            {
                time += Time.deltaTime;
                yield return null;
            }

            PlaySoundForAI();

            _audioSource.clip = Plugin.DeviceAudioClips["analyser_loop.wav"];
            _audioSource.loop = true;
            _audioSource.Play();

            time = 0f;
            clipLength = _audioSource.clip.length;
            int loops = UnityEngine.Random.Range(1, 4);
            bool shouldStall = !_stalledPreviously && UnityEngine.Random.Range(1, 100) >= _stallChance;
            if (shouldStall) loops /= 2;

            while (time <= clipLength * loops)
            {
                time += Time.deltaTime;
                yield return null;
            }

            PlaySoundForAI();

            if (shouldStall)
            {
                _audioSource.clip = Plugin.DeviceAudioClips["stalling.wav"];
                _audioSource.Play();
                CanTurnOn = true;
                _stalledPreviously = true;
                yield break;
            }

            _audioSource.loop = false;
            ReplaceItem();
            DeactivateZone();
        }

        private void ReplaceItem()
        {
            DeleteSelfFromDevicesList();
            string templateId = TargetZoneType == EZoneType.Gas ? Utils.GAMU_DATA_ID : Utils.RAMU_DATA_ID;
            Item replacementItem = Singleton<ItemFactory>.Instance.CreateItem(MongoID.Generate(), templateId, null);
            LootItem lootItem = Singleton<GameWorld>.Instance.SetupItem(replacementItem, _IPlayer, _position, _rotation);
            RemovePhysicsInteractions(lootItem.gameObject);
            AudioSource tempAudio = SetUpAudio("success_end.wav", lootItem.gameObject);
            tempAudio.Play();
            _deactivated = true;
            Destroy(this.gameObject, 0.5f);

            if (PluginConfig.ZoneDebug.Value)
            {
                GameObject visualRepresentation = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visualRepresentation.name = "deviceStartPos";
                visualRepresentation.transform.localScale = new Vector3(0.5f, 0.5f, 100f);
                visualRepresentation.transform.position = lootItem.transform.position;
                visualRepresentation.transform.rotation = lootItem.transform.rotation;
                visualRepresentation.GetComponent<Renderer>().material.color = UnityEngine.Color.red;
                UnityEngine.Object.Destroy(visualRepresentation.GetComponent<Collider>()); // Remove the collider from the visual representation
            }
        }

        void OnDisable()
        {
            DeleteSelfFromDevicesList();
        }
    }

    public static class DeviceController
    {
        public static AudioSource GasAnalyserAudioSource;
        public static AudioSource GeigerAudioSource;
        public static bool HasGasAnalyser { get; set; } = false;
        public static bool HasGeiger { get; set; } = false;
        private const float GAS_DELAY = 5f;
        private const float RAD_DELAY = 4f;
        private const float GAS_DEVICE_VOLUME = 0.14f;
        private const float GEIGER_VOLUME = 0.16f;

        private static bool _muteGeiger = false;
        private static bool _muteGasAnalyser = false;

        private static float _currentGasClipLength = 0f;
        private static float _gasDeviceTimer = 0f;

        private static float _currentGeigerClipLength = 0f;
        private static float _geigerDeviceTimer = 0f;

        private static float GetGasDelayTime() 
        {
            if (Plugin.RealHealthController.PlayerHazardBridge == null) return 4f;
            return GAS_DELAY * (1f - HazardTracker.BaseTotalToxicityRate);
        }

        private static float GeRadDelayTime()
        {
            if (Plugin.RealHealthController.PlayerHazardBridge == null) return 1f;
            if (HazardTracker.BaseTotalRadiationRate >= 0.15f) return 0f;
            float radRate = HazardTracker.BaseTotalRadiationRate;
            float delay = RAD_DELAY * (1f - Mathf.Pow(radRate, 0.35f));
            return delay;
        }

        private static void PlayToggleSfx(string clip)
        {

            Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), Plugin.DeviceAudioClips[clip], 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, 0.5f, EOcclusionTest.None, null, false);
        }

        public static void DoGasAnalyserAudio(Player player)
        {
            if (HasGasAnalyser && GameWorldController.GameStarted && Utils.PlayerIsReady)
            {
                _gasDeviceTimer += Time.deltaTime;

                if (Input.GetKeyDown(PluginConfig.MuteGasAnalyserKey.Value.MainKey) && PluginConfig.MuteGasAnalyserKey.Value.Modifiers.All(Input.GetKey))
                {
                    _muteGasAnalyser = !_muteGasAnalyser;
                    PlayToggleSfx("switch_off.wav");
                }


                if (_gasDeviceTimer > _currentGasClipLength && _gasDeviceTimer >= GetGasDelayTime())
                {
                    PlayerZoneBridge bridge = Plugin.RealHealthController.PlayerHazardBridge;
                    if (player != null && bridge != null && HazardTracker.BaseTotalToxicityRate > 0)
                    {
                        PlayGasAnalyserClips(player);
                        _gasDeviceTimer = 0f;
                    }
                }
            }
        }

        public static void DoGeigerAudio(Player player)
        {
            if (HasGeiger && GameWorldController.GameStarted && Utils.PlayerIsReady)
            {
                _geigerDeviceTimer += Time.deltaTime;

                if (Input.GetKeyDown(PluginConfig.MuteGeigerKey.Value.MainKey) && PluginConfig.MuteGeigerKey.Value.Modifiers.All(Input.GetKey))
                {
                    _muteGeiger = !_muteGeiger;
                    PlayToggleSfx("switch_off.wav");
                }

                if (_geigerDeviceTimer > _currentGeigerClipLength && _geigerDeviceTimer >= GeRadDelayTime())
                {
                    PlayerZoneBridge bridge = Plugin.RealHealthController.PlayerHazardBridge;
                    if (player != null && bridge != null && HazardTracker.BaseTotalRadiationRate > 0)
                    {
                        PlayGeigerClips(player);
                        _geigerDeviceTimer = 0f;
                    }
                }
            }
        }

        public static string GetGasAnalsyerClip(float gasLevel) 
        {
            switch (gasLevel) 
            {
                case <= 0f:
                    return null;
                case <= 0.02f:
                    return "gasBeep1.wav";
                case <= 0.04f:
                    return "gasBeep2.wav";
                case <= 0.08f:
                    return "gasBeep3.wav";
                case <= 0.12f:
                    return "gasBeep4.wav";
                case <= 0.16f:
                    return "gasBeep5.wav";
                case <= 0.19f:
                    return "gasBeep6.wav";
                case > 0.19f:
                    return "gasBeep7.wav";
                default: 
                    return null;
            }
        }

        public static string[] GetGeigerClip(float radLevel)
        {
            switch (radLevel)
            {
                case <= 0.02f:
                    return new string[] { "geiger1.wav", "geiger1_1.wav", "geiger1_2.wav", "geiger1_3.wav"};
                case <= 0.04f:
                    return new string[] { "geiger2.wav", "geiger2_1.wav", "geiger2_2.wav", "geiger2_3.wav"};
                case <= 0.08f:
                    return new string[] { "geiger3.wav", "geiger3_1.wav", "geiger3_2.wav", "geiger3_3.wav" };
                case <= 0.14f:
                    return new string[] { "geiger4.wav", "geiger4_1.wav", "geiger4_2.wav", "geiger4_3.wav" };
                case <= 0.2f:
                    return new string[] { "geiger5.wav", "geiger5_1.wav", "geiger5_2.wav", "geiger5_3.wav" };
                case <= 0.3f:
                    return new string[] { "geiger6.wav", "geiger6_1.wav", "geiger6_2.wav", "geiger6_3.wav" };
                case > 0.3f:
                    return new string[] { "geiger7.wav", "geiger7_1.wav", "geiger7_2.wav", "geiger7_3.wav" };
                default:
                    return null;
            }
        }

        public static void PlayGasAnalyserClips(Player player)
        {
            string clip = GetGasAnalsyerClip(HazardTracker.BaseTotalToxicityRate);
            if (clip == null) return;
            AudioClip audioClip = Plugin.DeviceAudioClips[clip];
            _currentGasClipLength = audioClip.length;
            float volume = _muteGasAnalyser ? 0f : GAS_DEVICE_VOLUME * PluginConfig.DeviceVolume.Value;
            Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), audioClip, 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, volume, EOcclusionTest.None, null, false);
        }

        public static void PlayGeigerClips(Player player)
        {
            string[] clips = GetGeigerClip(HazardTracker.BaseTotalRadiationRate);
            if (clips == null) return;
            int rndNumber = UnityEngine.Random.Range(0, clips.Length);
            string clip = clips[rndNumber];
            AudioClip audioClip = Plugin.DeviceAudioClips[clip];
            _currentGeigerClipLength = audioClip.length;
            float volume = _muteGeiger ? 0f : GEIGER_VOLUME * PluginConfig.DeviceVolume.Value;
            Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), audioClip, 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, volume, EOcclusionTest.None, null, false);
        }
    }
}
