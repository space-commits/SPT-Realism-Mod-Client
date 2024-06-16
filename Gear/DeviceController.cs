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
        public static bool HasGasAnalyser { get; set; } = false;
        public static bool HasGeiger { get; set; } = false;
        private const float GasDelay = 5f;
        private const float RadDelay = 4f;
        private const float GasDeviceVolume = 0.15f;
        private const float GeigerDeviceVolume = 0.4f;

        private static float _currentGasClipLength = 0f;
        private static float _gasDeviceTimer = 0f;

        private static float _currentGeigerClipLength = 0f;
        private static float _geigerDeviceTimer = 0f;


        private static float GetGasDelayTime() 
        {
            if (Plugin.RealHealthController.PlayerHazardBridge == null) return 4f;
            return GasDelay * (1f - Plugin.RealHealthController.PlayerHazardBridge.TotalGasAmount);
        }

        private static float GeRadDelayTime()
        {
            if (Plugin.RealHealthController.PlayerHazardBridge == null) return 1f;
            if (Plugin.RealHealthController.PlayerHazardBridge.TotalRadAmount >= 0.15f) return 0f;
            return RadDelay * (1f - Mathf.Pow(Plugin.RealHealthController.PlayerHazardBridge.TotalRadAmount, 0.35f));
        }

        public static void GasAnalyserAudioController()
        {
            if (HasGasAnalyser && GameWorldController.GameStarted && Utils.IsReady) 
            {
                _gasDeviceTimer += Time.deltaTime;

                if (_gasDeviceTimer > _currentGasClipLength && _gasDeviceTimer >= GetGasDelayTime())
                {
                    Player player = Utils.GetYourPlayer();
                    PlayerHazardBridge bridge = Plugin.RealHealthController.PlayerHazardBridge;
                    if (player != null && bridge != null && bridge.GasZoneCount > 0)
                    {
                        PlayGasAnalyserClips(player, bridge);
                        _gasDeviceTimer = 0f;
                    }
                }
            }
        }

        public static void GeigerAudioController()
        {
            if (HasGeiger)
            {
                _geigerDeviceTimer += Time.deltaTime;

                if (_geigerDeviceTimer > _currentGeigerClipLength && _geigerDeviceTimer >= GeRadDelayTime())
                {
                    Player player = Utils.GetYourPlayer();
                    PlayerHazardBridge bridge = Plugin.RealHealthController.PlayerHazardBridge;
                    if (player != null && bridge != null && bridge.RadZoneCount > 0)
                    {
                        PlayGeigerClips(player, bridge);
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
                case <= 0.1f:
                    return "gasBeep1.wav";
                case <= 0.15f:
                    return "gasBeep2.wav";
                case <= 0.2f:
                    return "gasBeep3.wav";
                case <= 0.35f:
                    return "gasBeep4.wav";
                case <= 0.4f:
                    return "gasBeep5.wav";
                case <= 0.6f:
                    return "gasBeep6.wav";
                case > 0.6f:
                    return "gasBeep7.wav";
                default: 
                    return null;
            }
        }

        public static string[] GetGeigerClip(float radLevel)
        {
            switch (radLevel)
            {
                case <= 0f:
                    return null;
                case <= 0.05f:
                    return new string[] { "geiger1.wav", "geiger1_1.wav", "geiger1_2.wav", "geiger1_3.wav", "geiger1_4.wav", "geiger1_5.wav", "geiger1_6.wav" };
                case <= 0.1f:
                    return new string[] { "geiger1.wav", "geiger1_1.wav", "geiger1_2.wav", "geiger1_3.wav", "geiger1_4.wav", "geiger1_5.wav", "geiger1_6.wav", "geiger2.wav", "geiger2_1.wav", "geiger2_2.wav", "geiger2_3.wav", "geiger2_4.wav", "geiger2_5.wav", "geiger2_6.wav", };
                case <= 0.15f:
                    return new string[] { "geiger2.wav", "geiger2_1.wav", "geiger2_2.wav", "geiger2_3.wav", "geiger2_4.wav", "geiger2_5.wav", "geiger2_6.wav" };
                case > 0.15f:
                    return new string[] { "geiger3.wav", "geiger3_1.wav", "geiger3_2.wav", "geiger3_3.wav", "geiger3_4.wav", "geiger3_5.wav", "geiger3_6.wav"};
                default:
                    return null;
            }
        }

        public static void PlayGasAnalyserClips(Player player, PlayerHazardBridge bridge)
        {
            string clip = GetGasAnalsyerClip(bridge.TotalGasAmount);
            if (clip == null) return;
            AudioClip audioClip = Plugin.DeviceAudioClips[clip];
            _currentGasClipLength = audioClip.length;
            Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), audioClip, 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, GasDeviceVolume * Plugin.DeviceVolume.Value, EOcclusionTest.None, null, false);
        }

        public static void PlayGeigerClips(Player player, PlayerHazardBridge bridge)
        {
            string[] clips = GetGeigerClip(bridge.TotalRadAmount);
            if (clips == null) return;
            int rndNumber = UnityEngine.Random.Range(0, clips.Length);
            Utils.Logger.LogWarning("rndNumber " + rndNumber);
            Utils.Logger.LogWarning("clips.Length " + clips.Length);
            Utils.Logger.LogWarning("clip " + clips[rndNumber]);
            string clip = clips[rndNumber];
            AudioClip audioClip = Plugin.DeviceAudioClips[clip];
            _currentGeigerClipLength = audioClip.length;
            Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), audioClip, 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, GeigerDeviceVolume * Plugin.DeviceVolume.Value, EOcclusionTest.None, null, false);
        }
    }
}
