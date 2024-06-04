using EFT;
using EFT.Interactive;
using System;
using System.Collections.Generic;
using UnityEngine;
using ExistanceClass = GClass2456;

namespace RealismMod
{
    public class PlayerHazardBridge : MonoBehaviour
    {
        public Player _Player { get; set; }
        public bool IsInGasZone { get; set; } = false;
        public float GasAmount { get; set; } = 0f;
        private float _bridgeTimer = 0f;

        void Update() 
        {
            _bridgeTimer += Time.deltaTime;
            if (_bridgeTimer >= 5f)
            {
                //temporary solution to dealing with bots
                if (_Player != null && _Player.IsAI && IsInGasZone)
                {
                    bool hasMask = false;
                    float protectionLevel = 0f;
                    GearController.CheckFaceCoverGear(_Player, ref hasMask, ref protectionLevel);
                    if (protectionLevel < 0.9f && GasAmount > 0.05f) 
                    {
                        protectionLevel = 1f - protectionLevel;
                        _Player.ActiveHealthController.ApplyDamage(EBodyPart.Chest, GasAmount * protectionLevel * 5f, ExistanceClass.PoisonDamage);
                    }
                }
                _bridgeTimer = 0f;
            }
        }
    }

    public class GasZone : TriggerWithId
    {
        public float GasStrengthModifier = 1f;
        private Dictionary<Player, PlayerHazardBridge> _containedPlayers = new Dictionary<Player, PlayerHazardBridge>();
        private BoxCollider _zoneCollider;
 /*       private float _audioTimer = 0f;
        private float _audioClipLength= 0f;*/
        private float _tick = 0f;
        private float _maxDistance = 0f;

        void Start()
        {
            _zoneCollider = GetComponentInParent<BoxCollider>();
            if (_zoneCollider == null)
            {
                Utils.Logger.LogError("Realism Mod: No BoxCollider found in parent for GasZone");
            }
            Vector3 boxSize = _zoneCollider.size;
            _maxDistance = boxSize.magnitude / 2f;
        }

        public override void TriggerEnter(Player player)
        {
            if (player != null)
            {
                Utils.Logger.LogWarning("enter " + player.ProfileId);
                PlayerHazardBridge hazardBridge;
                player.TryGetComponent<PlayerHazardBridge>(out hazardBridge);
                if(hazardBridge == null)
                {
                    Utils.Logger.LogWarning("null ");
                    hazardBridge = player.gameObject.AddComponent<PlayerHazardBridge>();
                    hazardBridge._Player = player;
                }
                hazardBridge.IsInGasZone = true;
                _containedPlayers.Add(player, hazardBridge);
            }
        }

        public override void TriggerExit(Player player)
        {
            if (player != null)
            {
                Utils.Logger.LogWarning("exit " + player.ProfileId);
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
                    float gasAmount = CalculateGasStrength(player.gameObject.transform.position);
                    hazardBridge.GasAmount = gasAmount <= 0f ? 0f : gasAmount;
         /*           Utils.Logger.LogWarning("Gas strength " + hazardBridge.GasAmount);*/
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

        float CalculateGasStrength(Vector3 playerPosition)
        {
            float distance = Vector3.Distance(playerPosition, _zoneCollider.bounds.center);
            float invertedDistance = _maxDistance - distance;  // Invert the distance
            invertedDistance = Mathf.Clamp(invertedDistance, 0, _maxDistance); //clamp the inverted distance
            return invertedDistance / GasStrengthModifier;
        }
    }
}
