using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RealismMod
{
    public class Transmitter : MonoBehaviour
    {
        public List<ActionsTypesClass> Actions = new List<ActionsTypesClass>();
        public bool CanTurnOn { get; private set; } = true;
        public LootItem _LootItem { get; set; }
        public Player _Player { get; set; } = null;
        public IPlayer _IPlayer { get; set; } = null;
        public string[] TargetZones { get; set; }
        public AudioClip AudioClips { get; set; }
        private AudioSource _audioSource;
        private Vector3 _position;
        private Quaternion _rotation;
        private List<IZone> _intersectingZones = new List<IZone>();
        private bool _stalledPreviously = false;

        private void PlaySoundForAI()
        {
            if (Singleton<BotEventHandler>.Instantiated)
            {
                Singleton<BotEventHandler>.Instance.PlaySound(_IPlayer, this.transform.position, 40f, AISoundType.step);
            }
        }

        IEnumerator DoLogic()
        {
            float time = 0f;
            if (IsInRightLcoation())
            {
                _audioSource.clip = Plugin.DeviceAudioClips["numbers.wav"];
                _audioSource.Play();
                CanTurnOn = false;
            }
            else
            {
                _audioSource.clip = Plugin.DeviceAudioClips["beep.wav"];
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

            Instantiate(Plugin.ExplosionGO, new Vector3(1000f, 0f, 317f), new Quaternion(0, 0, 0, 0));

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

        private void CoroutineWrapper()
        {
            Utils.Logger.LogWarning("CoroutineWrapper " + CanTurnOn);
            if (CanTurnOn) StartCoroutine(DoLogic());
        }

        private void SetUpActions()
        {
            Actions.AddRange(new List<ActionsTypesClass>()
            {
                    new ActionsTypesClass
                    {
                        Name = "Turn On",
                        Action = CoroutineWrapper
                    }
            });
        }

        private bool IsInRightLcoation()
        {
            return true;
            foreach (var zone in _intersectingZones) 
            {
                QuestZone questZone;
                if ((questZone = (zone as QuestZone)) != null && TargetZones.Contains(questZone.name)) return true;
            }
            return false;
        }

        void SetUpTransforms()
        {
            _position = _Player.gameObject.transform.position;
            _position.y += 0.05f;
            _position = _position + _Player.gameObject.transform.forward * 1.1f;

            Vector3 eularRotation = _Player.gameObject.transform.rotation.eulerAngles;
            eularRotation.x = -90f;
            eularRotation.y = 0f;
            _rotation = Quaternion.Euler(new Vector3(eularRotation.x, eularRotation.y, eularRotation.z));
        }

        void Start()
        {
            SetUpTransforms();
            SetUpActions();
            _audioSource = SetUpAudio("switch_off.wav", this.gameObject);
        }

        void Update()
        {
            if (this.gameObject == null) return;
            this.gameObject.transform.position = _position;
            this.gameObject.transform.rotation = _rotation;
        }

        private void OnTriggerEnter(Collider other)
        {
            IZone zone;
            if (other.gameObject.TryGetComponent<IZone>(out zone))
            {
                _intersectingZones.Add(zone);
            }
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
        public AudioClip AudioClips { get; set; }
        private AudioSource _audioSource;
        private Vector3 _position;
        private Quaternion _rotation;
        private List<IZone> _intersectingZones = new List<IZone>();
        private bool _stalledPreviously = false;

        private void PlaySoundForAI()
        {
            if (Singleton<BotEventHandler>.Instantiated)
            {
                Singleton<BotEventHandler>.Instance.PlaySound(_IPlayer, this.transform.position, 40f, AISoundType.step);
            }
        }

        IEnumerator DoLogic()
        {
            PlaySoundForAI();

            Utils.Logger.LogWarning("DoLogic ");

            float time = 0f;
            if (ZoneTypeMatches())
            {
                _audioSource.clip = Plugin.DeviceAudioClips["start_success.wav"];
                _audioSource.Play();
                CanTurnOn = false;
            }
            else
            {
                _audioSource.clip = Plugin.DeviceAudioClips["failed_start.wav"];
                _audioSource.Play();
                CanTurnOn = true;
                yield break;
            }

            float clipLength = _audioSource.clip.length;
            while (time <= clipLength)
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
            int loops = UnityEngine.Random.Range(1, 7);
            bool shouldStall = !_stalledPreviously && UnityEngine.Random.Range(1, 100) >= 75;
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

        private void CoroutineWrapper()
        {
            Utils.Logger.LogWarning("CoroutineWrapper " + CanTurnOn);
            if (CanTurnOn) StartCoroutine(DoLogic());
        }

        private void SetUpActions()
        {
            Actions.AddRange(new List<ActionsTypesClass>()
            {
                    new ActionsTypesClass
                    {
                        Name = "Turn On",
                        Action = CoroutineWrapper
                    }
            });
        }

        private bool ZoneTypeMatches()
        {
            if (_intersectingZones.Any(z => z.ZoneType == EZoneType.SafeZone)) return false;
            if (TargetZoneType == EZoneType.Gas && _intersectingZones.Any(z => (z.ZoneType == EZoneType.Gas || z.ZoneType == EZoneType.GasAssets) && z.IsAnalysable == true)) return true;
            if (TargetZoneType == EZoneType.Radiation && _intersectingZones.Any(z => (z.ZoneType == EZoneType.Radiation || z.ZoneType == EZoneType.RadAssets) && z.IsAnalysable == true)) return true;
            return false;
        }

        void SetUpTransforms()
        {
            _position = _Player.gameObject.transform.position;
            _position.y += 0.05f;
            _position = _position + _Player.gameObject.transform.forward * 1.1f;

            Vector3 eularRotation = _Player.gameObject.transform.rotation.eulerAngles;
            eularRotation.x = -90f;
            eularRotation.y = 0f;
            _rotation = Quaternion.Euler(new Vector3(eularRotation.x, eularRotation.y, eularRotation.z));
        }

        void Start()
        {
            SetUpTransforms();
            SetUpActions();
            _audioSource = SetUpAudio("switch_off.wav", this.gameObject);
        }

        void Update()
        {
            if (this.gameObject == null) return;
            this.gameObject.transform.position = _position;
            this.gameObject.transform.rotation = _rotation;
        }

        private void OnTriggerEnter(Collider other)
        {
            IZone hazardZone;
            if (other.gameObject.TryGetComponent<IZone>(out hazardZone))
            {
                _intersectingZones.Add(hazardZone);
            }
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
