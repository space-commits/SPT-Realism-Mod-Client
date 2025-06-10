using Comfort.Common;
using EFT;
using EFT.Interactive;
using RealismMod;
using UnityEngine;
using DamageTypeClass = GClass2788;

//Credit to AT for the assets
public class ShockWave : TriggerWithId
{
    public AudioSource shockwaveSound;

    private int _triggeredCount = 0;
    private Vector3 _maxScale = new Vector3(1000f, 1000f, 1000f); // Adjust as needed

    void Start()
    {
        RadiationZone rads = gameObject.AddComponent<RadiationZone>();
        rads.ZoneStrength = 700f;
        EFT.Interactive.TriggerWithId trigger = gameObject.AddComponent<EFT.Interactive.TriggerWithId>();
        trigger.SetId("nuke");
        gameObject.layer = LayerMask.NameToLayer("Triggers");
        float gameVolume = GameWorldController.GetGameVolumeAsFactor();
        shockwaveSound.volume *= gameVolume;
    }

    void Update()
    {
        transform.localScale = Vector3.Min(transform.localScale, _maxScale);
    }

    private bool ApplyDamage(Player player, EBodyPart bodyPart, float damage) 
    {
        player.ActiveHealthController.ApplyDamage(bodyPart, damage, DamageTypeClass.RadiationDamage);
        return player.ActiveHealthController.IsAlive;
    }

    private void HandlePlayerEffects(Player player)
    {
        if (PlayerValues.EnviroType == EnvironmentType.Indoor || PlayerValues.BtrState == EPlayerBtrState.Inside) return;

        ApplyDamage(player, EBodyPart.Head, 15f);
        ApplyDamage(player, EBodyPart.Chest, 110f);
        ApplyDamage(player, EBodyPart.Stomach, 110f); 
        ApplyDamage(player, EBodyPart.LeftArm, 60f); 
        ApplyDamage(player, EBodyPart.RightArm, 60f);
        ApplyDamage(player, EBodyPart.LeftLeg, 60f);
        ApplyDamage(player, EBodyPart.RightLeg, 60f);

        if (!player.IsInPronePose) player.ToggleProne();
    }

    private void HandleBotEffects(Player player)
    {
        if (player.Environment == EnvironmentType.Indoor) return;
        if (ApplyDamage(player, EBodyPart.Chest, 9001f)) return;
    }


    public override void TriggerEnter(Player player)
    {
        if (player == null) return;
        if (!player.IsYourPlayer) 
        {
            HandleBotEffects(player);
            return;
        }

        if (_triggeredCount == 0) 
        {
            shockwaveSound.Play();
            HandlePlayerEffects(player);
        }
        _triggeredCount++;
    }
}
