using Comfort.Common;
using EFT;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Val;


namespace RealismMod
{
    public class AmbientAudioPlayer : MonoBehaviour
    {
        public List<AudioClip> AudioClips = new List<AudioClip>();
        public Transform ParentTransform;
        public float MinTimeBetweenClips = 15f;
        public float MaxTimeBetweenClips = 90f;
        private AudioSource _audioSource;

        void Start()
        {
            _audioSource = GetComponent<AudioSource>();

            _audioSource = this.gameObject.AddComponent<AudioSource>();
            _audioSource.volume = 1f;
            _audioSource.loop = false;
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1.25f;
            _audioSource.maxDistance = 25f;
            _audioSource.maxDistance = 130f;
            _audioSource.rolloffMode = AudioRolloffMode.Linear;

            StartCoroutine(PlayRandomAudio());
        }

        private IEnumerator PlayRandomAudio()
        {
            while (true)
            {
                if (Utils.PlayerIsReady)
                {
                    if (ParentTransform == null)
                    {
                        ParentTransform = Utils.GetYourPlayer().gameObject.transform;
                    }

                    AudioClip selectedClip = AudioClips[UnityEngine.Random.Range(0, AudioClips.Count)];

                    float randomDistance = UnityEngine.Random.Range(45f, 95f);
                    Vector3 randomPosition = ParentTransform.position + UnityEngine.Random.onUnitSphere * randomDistance;
                    randomPosition.y = Mathf.Clamp(randomPosition.y, ParentTransform.position.y - 25f, ParentTransform.position.y + 25f);
                    transform.position = randomPosition;

                    if (PluginConfig.ZoneDebug.Value)
                    {
                        GameObject visualRepresentation = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        visualRepresentation.name = "AmbientAudioPlayerVisual";
                        visualRepresentation.transform.parent = transform;
                        visualRepresentation.transform.localScale = Vector3.one;
                        visualRepresentation.transform.position = randomPosition;
                        visualRepresentation.transform.rotation = ParentTransform.transform.rotation;
                        visualRepresentation.GetComponent<Renderer>().material.color = new UnityEngine.Color(1, 0, 0, 1);
                    }

                    _audioSource.clip = selectedClip;
                    _audioSource.Play();

                    yield return new WaitForSeconds(selectedClip.length);
                    float waitTime = UnityEngine.Random.Range(MinTimeBetweenClips, MaxTimeBetweenClips);
                    yield return new WaitForSeconds(waitTime);
                }
            }
        }
    }

    public static class HeadsetGainController 
    {
        public static void AdjustHeadsetVolume()
        {
            if (Input.GetKeyDown(PluginConfig.IncGain.Value.MainKey) && DeafeningController.HasHeadSet)
            {
                if (PluginConfig.RealTimeGain.Value < 30)
                {
                    PluginConfig.RealTimeGain.Value += 1f;
                    Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), Plugin.DeviceAudioClips["beep.wav"], 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, 1.0f, EOcclusionTest.None, null, false);
                }
            }
            if (Input.GetKeyDown(PluginConfig.DecGain.Value.MainKey) && DeafeningController.HasHeadSet)
            {

                if (PluginConfig.RealTimeGain.Value > 0)
                {
                    PluginConfig.RealTimeGain.Value -= 1f;
                    Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), Plugin.DeviceAudioClips["beep.wav"], 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, 1.0f, EOcclusionTest.None, null, false);
                }
            }
        }
    }

    public static class AudioController 
    {
        public static void CreateAudioComponent()
        {
            Utils.Logger.LogWarning("create audio game object");
            GameObject audioGO = new GameObject("HazardAudioController");
            var audioPlayer = audioGO.AddComponent<AudioControllerComponent>();
            Utils.Logger.LogWarning("created");
        }
    }

    public class AudioControllerComponent: MonoBehaviour
    {
        private Player _player;
        private AudioSource _gasMaskAudioSource;
        private AudioSource _gasAnalyserSource;
        private AudioSource _geigerSource;
        private const float _baseBreathVolume = 0.2f;
        private static float _currentBreathClipLength = 0f;
        private static float _breathTimer = 0f;
        private static float _breathCountdown = 2.5f;
        private static float _coughTimer = 0f;
        private static bool _breathedOut = false;

        void Start()
        {
            _gasMaskAudioSource = this.gameObject.AddComponent<AudioSource>();
            _gasAnalyserSource = this.gameObject.AddComponent<AudioSource>();
            _geigerSource = this.gameObject.AddComponent<AudioSource>();

            AudioSource[] sources = { _gasMaskAudioSource, _gasAnalyserSource , _geigerSource };

            foreach (var source in sources) 
            {
                source.volume = 1f;
                source.loop = false;
                source.playOnAwake = false;
                source.spatialBlend = 1f;
                source.minDistance = 5f;
                source.maxDistance = 10f;
                source.rolloffMode = AudioRolloffMode.Linear;
            };

            //I know this is scuffed, temporary solution
            DeviceController.GeigerAudioSource = _geigerSource;
            DeviceController.GasAnalyserAudioSource = _gasAnalyserSource;
        }

        void Update()
        {
            if (Utils.IsInHideout || !Utils.PlayerIsReady) return;

            if (_player == null)
            {
                _player = Utils.GetYourPlayer();
                this.gameObject.transform.parent = _player.gameObject.transform;
            }

            _breathTimer += Time.deltaTime;
            _coughTimer += Time.deltaTime;  

            if (GearController.HasGasMask && _breathCountdown > 0f)
            {
                _breathCountdown -= Time.deltaTime;
            }
            else 
            {
                _breathCountdown = 2.5f;
            }

            if (_breathTimer > _currentBreathClipLength && _breathCountdown <= 0f && GearController.HasGasMask)
            {
                bool isNotBusy = !_player.Speaker.Busy || !_player.Speaker.Speaking;
                if (isNotBusy)
                {
                    PlayGasMaskBreathing(_breathedOut);
                    _breathTimer = 0f;
                    _breathedOut = !_breathedOut;
                }
            }

            if (_coughTimer > 5f)
            {
                CoughController();
                _coughTimer = 0f;
            }

            DeviceController.DoGasAnalyserAudio();
            DeviceController.DoGeigerAudio();

        }

        private void CoughController() 
        {
            if (Plugin.RealHealthController.DoCoughingAudio) _player.Speaker.Play(EPhraseTrigger.OnBreath, ETagStatus.Dying | ETagStatus.Aware, true, null);
        }

        private float GetBreathVolume() 
        {
            return _baseBreathVolume * (2f - PlayerState.StaminaPerc) * PluginConfig.GasMaskBreathVolume.Value;
        }

        private string GetAudioFromOtherStates()
        {
            if (HazardTracker.TotalToxicity >= 70f || HazardTracker.TotalRadiation >= 85f)
            {
                return "Dying";
            }
            if (HazardTracker.TotalToxicity >= 50f || PlayerState.StaminaPerc <= 0.55 || HazardTracker.TotalRadiation >= 70f || Plugin.RealHealthController.HasAdrenalineEffect)
            {
                return "BadlyInjured";
            }
            if (HazardTracker.TotalToxicity >= 30f || PlayerState.StaminaPerc <= 0.8f || HazardTracker.TotalRadiation >= 50f)
            {
                return "Injured";
            }
            return "Healthy";
        }

        private string ChooseAudioClip(string healthStatus, string desiredClip)
        {
            if (desiredClip == "Dying" || healthStatus == "Dying")
            {
                return "Dying";
            }
            if (desiredClip == "BadlyInjured" || healthStatus == "BadlyInjured")
            {
                return "BadlyInjured";
            }
            if (desiredClip == "Injured" || healthStatus == "Injured")
            {
                return "Injured";
            }
            return "Healthy";
        }

        public void PlayGasMaskBreathing(bool breathOut)
        {
            int rndNumber = UnityEngine.Random.Range(1, 4);
            string healthStatus = _player.HealthStatus.ToString();
            string desiredClip = GetAudioFromOtherStates();
            string clipToUse = ChooseAudioClip(healthStatus, desiredClip);
            string inOut = breathOut ? "out" : "in";
            string clipName = inOut + "_" + clipToUse + rndNumber + ".wav";
            AudioClip audioClip = Plugin.GasMaskAudioClips[clipName];
            _currentBreathClipLength = audioClip.length;
            float playBackVolume = GetBreathVolume();
            _player.SpeechSource.SetLowPassFilterParameters(0.99f, ESoundOcclusionType.Obstruction, 1600, 5000, true); //muffles player voice
            Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), audioClip, 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, playBackVolume, EOcclusionTest.None, null, false);
        }
    }
}
