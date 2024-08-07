using EFT;
using EFT.Interactive;
using RealismMod;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShockWave : TriggerWithId
{
    public AudioSource shockwaveSound;

    private int _triggeredCount = 0;
    private Vector3 _maxScale = new Vector3(500f, 500f, 500f); // Adjust as needed

    void Start()
    {
        RadiationZone gas = gameObject.AddComponent<RadiationZone>();
        gas.ZoneStrengthModifier = 1f;
        EFT.Interactive.TriggerWithId trigger = gameObject.AddComponent<EFT.Interactive.TriggerWithId>();
        trigger.SetId("nuke");
        gameObject.layer = LayerMask.NameToLayer("Triggers");
    }

    void Update()
    {
        transform.localScale = Vector3.Min(transform.localScale, _maxScale);
        Debug.LogWarning(transform.localScale);
    }


    public override void TriggerEnter(Player player)
    {
        Debug.LogWarning("trigger");
        if (player != null && player.IsYourPlayer && _triggeredCount == 1)
        {
            Debug.LogWarning("Play Audio Bepinex");
            shockwaveSound.Play();
        }
        _triggeredCount++;
    }
}
