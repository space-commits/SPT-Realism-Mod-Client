using EFT.Interactive;
using EFT;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Comfort.Common;
using static RootMotion.FinalIK.AimPoser;

namespace RealismMod
{
    public class PlayerHazardBridge : MonoBehaviour
    {
        public bool IsInGasZone { get; set; } = false;
        public float GasAmount { get; set; } = 0f;
    }

    public class GasZone : TriggerWithId
    {
        private Dictionary<Player, PlayerHazardBridge> _containedPlayers = new Dictionary<Player, PlayerHazardBridge>();
        private BoxCollider _zoneCollider;
        private float _audioTimer = 0f;
        private float _audioClipLength= 0f;
        private float _tick = 0f;

        void Start()
        {
            _zoneCollider = GetComponentInParent<BoxCollider>();
            if (_zoneCollider == null)
            {
                Utils.Logger.LogError("Realism Mod: No BoxCollider found in parent for GasZone");
            }
        }

        public override void TriggerEnter(Player player)
        {
            if (player != null)
            {
                Utils.Logger.LogWarning("enter " + player.Id);
                PlayerHazardBridge hazardBridge = player.GetComponent<PlayerHazardBridge>();
                hazardBridge.IsInGasZone = true;
                _containedPlayers.Add(player, hazardBridge);
            }
        }

        public override void TriggerExit(Player player)
        {
            if (player != null)
            {
                Utils.Logger.LogWarning("exit " + player.Id);
                PlayerHazardBridge hazardBridge = _containedPlayers[player];
                hazardBridge.GasAmount = 0f;
                hazardBridge.IsInGasZone = false;
                _containedPlayers.Remove(player);
            }
        }

        void Update()
        {

            _tick += Time.deltaTime;

            if (_tick >= 0.25f) 
            {
                _tick = 0f;
                foreach (var p in _containedPlayers)
                {
                    Player player = p.Key;
                    PlayerHazardBridge hazardBridge = p.Value;
                    float gasAmount = CalculateDepthInsideTrigger(player.Transform.position);
                    hazardBridge.GasAmount = gasAmount <= 0f ? 0f : gasAmount; 
                    Utils.Logger.LogWarning("current gas amount " + hazardBridge.GasAmount);
                }
            }

            /*        _audioTimer += Time.deltaTime;
                       if (_audioTimer > _audioClipLength)
                      {
                          AudioClip audioClip = Plugin.HazardZoneClips["gasleak1.wav"];
                          _audioClipLength = audioClip.length;
                          _audioTimer = 0;
                          //this playback does not update its position, it stays as loud as it was when it first started playing.
                          Singleton<BetterAudio>.Instance.PlayAtPoint(this.transform.position, audioClip, CameraClass.Instance.Distance(this.transform.position), BetterAudio.AudioSourceGroupType.Nonspatial, 100, Plugin.test10.Value, EOcclusionTest.Regular);
                      }*/
        }

        float CalculateDepthInsideTrigger(Vector3 playerPosition)
        {
            // Convert the player's world position to the local position relative to the BoxCollider
            Vector3 localPlayerPosition = transform.InverseTransformPoint(playerPosition);

            // Get the extents of the BoxCollider
            Vector3 extents = _zoneCollider.size / 2;

            // Calculate the distances to each face of the BoxCollider
            float distanceToFront = extents.z - localPlayerPosition.z;
            float distanceToBack = extents.z + localPlayerPosition.z;
            float distanceToLeft = extents.x - localPlayerPosition.x;
            float distanceToRight = extents.x + localPlayerPosition.x;
            float distanceToTop = extents.y - localPlayerPosition.y;
            float distanceToBottom = extents.y + localPlayerPosition.y;

            // Determine the minimum distance to any boundary (this is the "depth" inside the trigger)
            float depth = Mathf.Min(distanceToFront, distanceToBack, distanceToLeft, distanceToRight, distanceToTop, distanceToBottom);

            return depth;
        }
    }
}
