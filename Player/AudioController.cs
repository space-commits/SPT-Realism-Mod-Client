using Comfort.Common;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using EFT;

namespace RealismMod
{
    public static class AudioController
    {

        private const float _baseBreathVolume = 0.25f;
        public static float CurrentBreathClipLength {  get; private set; }
        private static float _breathTimer = 0f;
        private static float _breathCountdown = 2.5f;
        private static bool _breathedOut = false;

        public static void GasMaskBreathController()
        {
          
/*            Utils.Logger.LogWarning("Busy " + player.Speaker.Busy);
            Utils.Logger.LogWarning("QueuedEvent " + player.Speaker.QueuedEvent);
            Utils.Logger.LogWarning("Speaking " + player.Speaker.Speaking);
            if (player.Speaker.Clip != null) player.Speaker.Clip.ToString();*/
    /*        Utils.Logger.LogWarning("TimeLeft " + player.Speaker.TimeLeft);*/

            _breathTimer += Time.deltaTime;
            if (GearController.HasGasMask && _breathCountdown > 0f)
            {
                _breathCountdown -= Time.deltaTime;
            }
            else 
            {
                _breathCountdown = 2.5f;
            }

            if (_breathTimer > CurrentBreathClipLength && _breathCountdown <= 0f && GearController.HasGasMask) 
            {
                Player player = Utils.GetYourPlayer();
                bool isNotBusy = !player.Speaker.Busy || player.Speaker.Speaking == false;
    
                if (isNotBusy) 
                {
                    PlayGasMaskBreathing(_breathedOut, player);
                    _breathTimer = 0f;
                    _breathedOut = !_breathedOut;
                }
            }
        }

        private static float GetBreathVolume() 
        {
            return _baseBreathVolume * (2f - PlayerState.StaminaPerc);
        }

        private static string GetAudioFromStamina()
        {
            switch (PlayerState.StaminaPerc)
            {
                case <= 0.35f:
                    return "BadlyInjured";
                case <= 0.7f:
                    return "Injured";
                default:
                    return "Healthy";
            }

        }

        public static void PlayGasMaskBreathing(bool breathOut, Player player)
        {
            int rndNumber = UnityEngine.Random.Range(1, 4);
            String breathClip = player.HealthStatus.ToString();
            if (breathClip == "Healthy") breathClip = GetAudioFromStamina();
            string inOut = breathOut ? "out" : "in";
            string clipName = inOut + "_" + breathClip + rndNumber + ".wav";
            AudioClip audioClip = Plugin.GasMaskAudioClips[clipName];
            CurrentBreathClipLength = audioClip.length;
            float playBackVolume = GetBreathVolume();

            Utils.Logger.LogWarning("clip " + clipName);
            Utils.Logger.LogWarning("playBackVolume " + playBackVolume);

            player.SpeechSource.SetLowPassFilterParameters(1f, ESoundOcclusionType.Obstruction, 1600, 5000, true);
            Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), audioClip, 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, playBackVolume, EOcclusionTest.None, null, false);
        }

        public static void HeadsetVolumeAdjust() 
        {
            if (Input.GetKeyDown(Plugin.IncGain.Value.MainKey) && DeafeningController.HasHeadSet)
            {
                if (Plugin.RealTimeGain.Value < 30)
                {
                    Plugin.RealTimeGain.Value += 1f;
                    Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), Plugin.HitAudioClips["beep.wav"], 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, 1.0f, EOcclusionTest.None, null, false);
                }
            }
            if (Input.GetKeyDown(Plugin.DecGain.Value.MainKey) && DeafeningController.HasHeadSet)
            {

                if (Plugin.RealTimeGain.Value > 0)
                {
                    Plugin.RealTimeGain.Value -= 1f;
                    Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), Plugin.HitAudioClips["beep.wav"], 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, 1.0f, EOcclusionTest.None, null, false);
                }
            }

        }
    }
}
