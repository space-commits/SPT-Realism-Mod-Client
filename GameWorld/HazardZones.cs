using EFT;
using EFT.Interactive;
using System;
using System.Collections.Generic;
using UnityEngine;
using ExistanceClass = GClass2456;

namespace RealismMod
{

    public enum EZoneType 
    {
        Radiation,
        Toxic
    }

    public interface IHazardZone
    {
        EZoneType ZoneType { get; } 
        float ZoneStrengthModifier { get; set; }
    }

    public class PlayerHazardBridge : MonoBehaviour
    {
        public Player _Player { get; set; }
        public int GasZoneCount { get; set; } = 0;
        public int RadZoneCount { get; set; } = 0;
        public Dictionary<string, float> GasAmounts = new Dictionary<string, float>();
        public Dictionary<string, float> RadAmounts = new Dictionary<string, float>();

        public float TotalGasAmount
        {
            get
            {
                float totalGas = 0f;
                foreach (var gas in GasAmounts)
                {
                    totalGas += gas.Value;
                }
                return totalGas;
            }
        }

        public float TotalRadAmount 
        { 
            get 
            {
                float totalRads = 0f;
                foreach (var rad in RadAmounts) 
                {
                    totalRads += rad.Value;   
                }
                return totalRads;
            }
        }

        private float _bridgeTimer = 0f;
        private const float Interval = 6f;

        void Update() 
        {
            _bridgeTimer += Time.deltaTime;
            if (_bridgeTimer >= Interval)
            {
                //temporary solution to dealing with bots
                if (GasZoneCount > 0 && _Player != null && _Player?.ActiveHealthController != null && _Player?.AIData?.BotOwner != null && !_Player.AIData.BotOwner.IsDead)
                {
                    bool hasMask = false;
                    float gasProtection = 0f;
                    float radProtection = 0f;
                    GearController.CheckFaceCoverGear(_Player, ref hasMask, ref gasProtection, ref radProtection);
                    if (gasProtection <= 0f && TotalGasAmount > 0.05f) 
                    {
                        gasProtection = 1f - gasProtection;
                        _Player.ActiveHealthController.ApplyDamage(EBodyPart.Chest, TotalGasAmount * gasProtection * Interval, ExistanceClass.PoisonDamage);
                        _Player.Speaker.Play(EPhraseTrigger.OnBreath, ETagStatus.Dying | ETagStatus.Aware, true, null);
                    }
                }
                _bridgeTimer = 0f;
            }
        }
    }

    public class GasZone : TriggerWithId, IHazardZone
    {
        public EZoneType ZoneType { get; } = EZoneType.Toxic;
        public float ZoneStrengthModifier { get; set; } = 1f;
        private Dictionary<Player, PlayerHazardBridge> _containedPlayers = new Dictionary<Player, PlayerHazardBridge>();
        private BoxCollider _zoneCollider;
        private float _tick = 0f;
        private float _maxDistance = 0f;

        /* private float _audioTimer = 0f;
       private float _audioClipLength= 0f;*/

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
                PlayerHazardBridge hazardBridge;
                player.TryGetComponent<PlayerHazardBridge>(out hazardBridge);
                if(hazardBridge == null)
                {
                    hazardBridge = player.gameObject.AddComponent<PlayerHazardBridge>();
                    hazardBridge._Player = player;
                }
                hazardBridge.GasZoneCount++;
                hazardBridge.GasAmounts.Add(this.name, 0f);
                _containedPlayers.Add(player, hazardBridge);
            }
        }

        public override void TriggerExit(Player player)
        {
            if (player != null)
            {
                PlayerHazardBridge hazardBridge = _containedPlayers[player];
                hazardBridge.GasZoneCount--;
                hazardBridge.GasAmounts.Remove(this.name);
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
                    hazardBridge.GasAmounts[this.name] = Mathf.Max(gasAmount, 0f);
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
            float invertedDistance = _maxDistance - distance;  // invert the distance
            invertedDistance = Mathf.Clamp(invertedDistance, 0, _maxDistance); //clamp the inverted distance
            return invertedDistance / (ZoneStrengthModifier * (Plugin.ZoneDebug.Value ? Plugin.test10.Value : 1f));
        }
    }

    public class RadiationZone : TriggerWithId, IHazardZone
    {
        public EZoneType ZoneType { get; } = EZoneType.Radiation;
        public float ZoneStrengthModifier { get; set; } = 1f;
        private Dictionary<Player, PlayerHazardBridge> _containedPlayers = new Dictionary<Player, PlayerHazardBridge>();
        private BoxCollider _zoneCollider;
        private float _tick = 0f;
        private float _maxDistance = 0f;

        void Start()
        {
            _zoneCollider = GetComponentInParent<BoxCollider>();
            if (_zoneCollider == null)
            {
                Utils.Logger.LogError("Realism Mod: No BoxCollider found in parent for RadiationZone");
            }
            Vector3 boxSize = _zoneCollider.size;
            _maxDistance = boxSize.magnitude / 2f;
        }

        public override void TriggerEnter(Player player)
        {
            if (player != null)
            {
                PlayerHazardBridge hazardBridge;
                player.TryGetComponent<PlayerHazardBridge>(out hazardBridge);
                if (hazardBridge == null)
                {
                    hazardBridge = player.gameObject.AddComponent<PlayerHazardBridge>();
                    hazardBridge._Player = player;
                }
                hazardBridge.RadZoneCount++;
                hazardBridge.RadAmounts.Add(this.name, 0f);
                _containedPlayers.Add(player, hazardBridge);
            }
        }

        public override void TriggerExit(Player player)
        {
            if (player != null)
            {
                PlayerHazardBridge hazardBridge = _containedPlayers[player];
                hazardBridge.RadZoneCount--;
                hazardBridge.RadAmounts.Remove(this.name);
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
                    float radAmount = CalculateRadStrength(player.gameObject.transform.position);
                    hazardBridge.RadAmounts[this.name] = Mathf.Max(radAmount, 0f);
                }
            }
        }

        float CalculateRadStrength(Vector3 playerPosition)
        {
            float distance = Vector3.Distance(playerPosition, _zoneCollider.bounds.center);
            float invertedDistance = _maxDistance - distance;  // invert the distance
            invertedDistance = Mathf.Clamp(invertedDistance, 0, _maxDistance); //clamp the inverted distance
            return invertedDistance / (ZoneStrengthModifier * (Plugin.ZoneDebug.Value ? Plugin.test10.Value : 1f));
        }
    }
}
