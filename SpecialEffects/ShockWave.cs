using EFT;
using EFT.Interactive;
using RealismMod;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DamageTypeClass = GClass2470;

//Credit to AT for the assets
public class ShockWave : TriggerWithId
{
    public AudioSource shockwaveSound;

    private int _triggeredCount = 0;
    private Vector3 _maxScale = new Vector3(1000f, 1000f, 1000f); // Adjust as needed

    void Start()
    {
        RadiationZone gas = gameObject.AddComponent<RadiationZone>();
        gas.ZoneStrengthModifier = 4000f;
        EFT.Interactive.TriggerWithId trigger = gameObject.AddComponent<EFT.Interactive.TriggerWithId>();
        trigger.SetId("nuke");
        gameObject.layer = LayerMask.NameToLayer("Triggers");
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
        if (PlayerState.EnviroType == EnvironmentType.Indoor) return;

        HazardTracker.TotalRadiation = 100f;

        if (ApplyDamage(player, EBodyPart.Head, 15f)) return;
        if (ApplyDamage(player, EBodyPart.Chest, 110f)) return;
        if (ApplyDamage(player, EBodyPart.Stomach, 110f)) return; 
        if (ApplyDamage(player, EBodyPart.LeftArm, 60f)) return; 
        if (ApplyDamage(player, EBodyPart.RightArm, 60f)) return;
        if (ApplyDamage(player, EBodyPart.LeftLeg, 60f)) return;
        if (ApplyDamage(player, EBodyPart.RightLeg, 60f)) return;

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
