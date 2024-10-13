using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RealismMod;
using EFT.Interactive;
using System.Linq;
using System.Reflection;

public class Bomb : MonoBehaviour
{
    public AnimationCurve SizeCurve;

    [Range(1.0f, 2500.0f)]
    public float NukeDuration = 2500f;
    [Range(1.0f, 1024.0f)]
    public float SizeCurve_multiply;

    public float LightRadius = 2048;
    public float LightPower = 64;
    public AnimationCurve Light2Power_curve;
    public AnimationCurve LightPower_curve;

    public AnimationCurve LightXCurve;

    public Transform ShockWaveTransform;
    public float sizeSpeed = 1.0f;
    private Vector3 _currentShockwaveVector;
    private float _currentShockwaveSize;

    public Light BlastLight;
    public Light BlastLight2;

    public float Emmis_mush;
    public float Emmis_steam;
    public AnimationCurve Mat_SizeCurve;
    public float _mat_SizeCurve_multiply;

    private float _elapsedTime;
    private float _xPos;
    private float _yPos;
    private float _zPos;

    void Start()
    {
        _currentShockwaveVector = new Vector3(0f, 0f, 0f);
        _elapsedTime = 0.0f;
        _currentShockwaveSize = 0.0f;
        GameWorldController.DidExplosionClientSide = true;
    }


    void Update()
    {
        _currentShockwaveSize += Time.deltaTime * sizeSpeed;
        _currentShockwaveVector = new Vector3(_currentShockwaveSize, _currentShockwaveSize, _currentShockwaveSize);
        ShockWaveTransform.localScale = _currentShockwaveVector;

        _elapsedTime += Time.deltaTime;

        BlastLight.intensity = LightPower_curve.Evaluate(_elapsedTime / NukeDuration);
        //BlastLight2.intensity = Light2Power_curve.Evaluate(_elapsedTime / NukeDuration);

        /*     BlastLight.intensity = LightPower; //* LightPower_curve.Evaluate(FinalCurveVaue)*/
        BlastLight.range = LightRadius; // Mathf.Lerp(LightRadius, 0.0f, LightRadius_curve.Evaluate(FinalCurveVaue))
        //BlastLight2.range = LightRadius;

        _xPos = LightXCurve.Evaluate(_elapsedTime / NukeDuration);
        _yPos = LightXCurve.Evaluate(_elapsedTime / NukeDuration);
        _zPos = LightXCurve.Evaluate(_elapsedTime / NukeDuration);
        //BlastLight2.transform.localPosition = new Vector3(-_xPos * 4f, _yPos * 0.25f, _zPos * 4f);

        if (_elapsedTime > NukeDuration)
        {
            Destroy(gameObject);
            return;
        }
    }
}
