using EFT;
using EFT.Interactive;
using RealismMod;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Credit to AT for the assets
public class ShockWave : TriggerWithId
{
    public AudioSource shockwaveSound;

    private int _triggeredCount = 0;
    private Vector3 _maxScale = new Vector3(1000f, 1000f, 1000f); // Adjust as needed

    void Start()
    {
        RadiationZone gas = gameObject.AddComponent<RadiationZone>();
        gas.ZoneStrengthModifier = 1000f;
        EFT.Interactive.TriggerWithId trigger = gameObject.AddComponent<EFT.Interactive.TriggerWithId>();
        trigger.SetId("nuke");
        gameObject.layer = LayerMask.NameToLayer("Triggers");
    }

    void Update()
    {
        transform.localScale = Vector3.Min(transform.localScale, _maxScale);
    }


    public override void TriggerEnter(Player player)
    {
        if (player == null || !player.IsYourPlayer) return;
        Debug.LogWarning("trigger");
        if (_triggeredCount == 0) shockwaveSound.Play();
        _triggeredCount++;
    }
}
