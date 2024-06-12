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
        private const float RadDelay = 1f;
        private const float BaseDeviceVolume = 0.2f;

        private static float _currentGasClipLength = 0f;
        private static float _gasDeviceTimer = 0f;

        private static float _currentGeigerClipLength = 0f;
        private static float _geigerDeviceTimer = 0f;


        private static float GetGasDelayTime() 
        {
            if (Plugin.RealHealthController.PlayerHazardBridge == null) return 4f;
            return GasDelay * (1f - Plugin.RealHealthController.PlayerHazardBridge.GasAmount);
        }

        private static float GeRadDelayTime()
        {
            if (Plugin.RealHealthController.PlayerHazardBridge == null) return 1f;
            if (Plugin.RealHealthController.PlayerHazardBridge.RadAmount >= 0.15f) return 0f;
            return RadDelay * (1f - Plugin.RealHealthController.PlayerHazardBridge.RadAmount);
        }

        public static void GasAnalyserAudioController()
        {
            if (HasGasAnalyser) 
            {
                _gasDeviceTimer += Time.deltaTime;

                if (_gasDeviceTimer > _currentGasClipLength && _gasDeviceTimer >= GetGasDelayTime())
                {
                    Player player = Utils.GetYourPlayer();
                    PlayerHazardBridge bridge = Plugin.RealHealthController.PlayerHazardBridge;
                    if (player != null && bridge != null && bridge.IsInGasZone)
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
                    if (player != null && bridge != null && bridge.IsInRadZone)
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
                case <= 0.3f:
                    return "gasBeep4.wav";
                case <= 0.4f:
                    return "gasBeep5.wav";
                case <= 0.5f:
                    return "gasBeep6.wav";
                case > 0.5f:
                    return "gasBeep7.wav";
                default: 
                    return null;
            }
        }

        public static string GetGeigerClip(float radLevel)
        {
            switch (radLevel)
            {
                case <= 0f:
                    return null;
                case <= 0.01f:
                    return "geiger1.wav";
                case <= 0.025f:
                    return "geiger2.wav";
                case <= 0.05f:
                    return "geiger3.wav";
                case <= 0.075f:
                    return "geiger4.wav";
                case <= 0.1f:
                    return "geiger5.wav";
                case <= 0.15f:
                    return "geiger6.wav";
                case <= 0.2f:
                    return "geiger7.wav";
                case <= 0.25f:
                    return "geiger8.wav";
                case <= 0.3f:
                    return "geiger9.wav";
                case > 0.3f:
                    return "geiger10.wav";
                default:
                    return null;
            }
        }

        public static void PlayGasAnalyserClips(Player player, PlayerHazardBridge bridge)
        {
            string clip = GetGasAnalsyerClip(bridge.GasAmount);
            if (clip == null) return;
            AudioClip audioClip = Plugin.DeviceAudioClips[clip];
            _currentGasClipLength = audioClip.length;
            Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), audioClip, 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, BaseDeviceVolume, EOcclusionTest.None, null, false);
        }

        public static void PlayGeigerClips(Player player, PlayerHazardBridge bridge)
        {
            string clip = GetGeigerClip(bridge.RadAmount);
            if (clip == null) return;
            AudioClip audioClip = Plugin.DeviceAudioClips[clip];
            _currentGeigerClipLength = audioClip.length;
            Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), audioClip, 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, BaseDeviceVolume, EOcclusionTest.None, null, false);
        }
    }
}
