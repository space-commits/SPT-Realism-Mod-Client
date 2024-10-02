using Comfort.Common;
using EFT;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RealismMod
{
    public static class DeviceController
    {
        public static AudioSource GasAnalyserAudioSource;
        public static AudioSource GeigerAudioSource;
        public static bool HasGasAnalyser { get; set; } = false;
        public static bool HasGeiger { get; set; } = false;
        private const float GasDelay = 5f;
        private const float RadDelay = 4f;
        private const float GasDeviceVolume = 0.14f;
        private const float GeigerDeviceVolume = 0.16f;

        private static float _currentGasClipLength = 0f;
        private static float _gasDeviceTimer = 0f;

        private static float _currentGeigerClipLength = 0f;
        private static float _geigerDeviceTimer = 0f;


        private static float GetGasDelayTime() 
        {
            if (Plugin.RealHealthController.PlayerHazardBridge == null) return 4f;
            return GasDelay * (1f - HazardTracker.BaseTotalToxicityRate);
        }

        private static float GeRadDelayTime()
        {
            if (Plugin.RealHealthController.PlayerHazardBridge == null) return 1f;
            if (HazardTracker.BaseTotalRadiationRate >= 0.15f) return 0f;
            float radRate = HazardTracker.BaseTotalRadiationRate;
            float delay = RadDelay * (1f - Mathf.Pow(radRate, 0.35f));
            return delay;
        }

        public static void DoGasAnalyserAudio(Player player)
        {
            if (HasGasAnalyser && GameWorldController.GameStarted && Utils.PlayerIsReady) 
            {
                _gasDeviceTimer += Time.deltaTime;

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
            if (HasGeiger)
            {
                _geigerDeviceTimer += Time.deltaTime;

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
                case <= 0.035f:
                    return "gasBeep1.wav";
                case <= 0.075f:
                    return "gasBeep2.wav";
                case <= 0.11f:
                    return "gasBeep3.wav";
                case <= 0.15f:
                    return "gasBeep4.wav";
                case <= 0.2f:
                    return "gasBeep5.wav";
                case <= 0.25f:
                    return "gasBeep6.wav";
                case > 0.25f:
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
                case <= 0.07f:
                    return new string[] { "geiger3.wav", "geiger3_1.wav", "geiger3_2.wav", "geiger3_3.wav" };
                case <= 0.1f:
                    return new string[] { "geiger4.wav", "geiger4_1.wav", "geiger4_2.wav", "geiger4_3.wav" };
                case <= 0.14f:
                    return new string[] { "geiger5.wav", "geiger5_1.wav", "geiger5_2.wav", "geiger5_3.wav" };
                case <= 0.2f:
                    return new string[] { "geiger6.wav", "geiger6_1.wav", "geiger6_2.wav", "geiger6_3.wav" };
                case > 0.2f:
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
            float volume = GasDeviceVolume * PluginConfig.DeviceVolume.Value;
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
            float volume = GeigerDeviceVolume * PluginConfig.DeviceVolume.Value;
            Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), audioClip, 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, volume, EOcclusionTest.None, null, false);
        }
    }
}
