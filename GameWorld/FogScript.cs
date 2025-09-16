using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity;
using RealismMod;
using static UnityEngine.ParticleSystem;
using static MuzzleJet;
using static Systems.Effects.Effects.Effect;

public class FogScript : MonoBehaviour
{
    public MinMaxCurve ParticleSize { get; set; }
    public Vector3 Scale { get; set; }
    public float ParticleRate { get; set; }
    public float OpacityModi { get; set; }
    public float DynamicOpacityModiTarget { get; set; } = 1f;
    public float SpeedModi { get; set; }
    public bool UsePhysics { get; set; }
    private ParticleSystem _ps;
    private ParticleSystem.MainModule _mainModule;
    private ParticleSystem.ShapeModule _shapeModule;
    private ParticleSystem.CollisionModule _collisionModule;
    private ParticleSystem.EmissionModule _emissionModule;
    private bool _isLabs = false;
    private int _maxParticles = 1500;
    private float _timeExisted = 0f;
    private float _alpha= 0.08f;
    private float _speed = 2.5f;
    private float _dynamicOpacityModi = 1f;
    private MinMaxCurve _startSpeedCurve = new ParticleSystem.MinMaxCurve(1, 1);
    private MinMaxCurve _lifeTimeCurve = new ParticleSystem.MinMaxCurve(20f, 30f);

    void Start()
    {
        _ps = GetComponent<ParticleSystem>();
        _mainModule = _ps.main;
        _shapeModule = _ps.shape;
        _emissionModule = _ps.emission;
        _collisionModule = _ps.collision;
        _collisionModule.enabled = UsePhysics;

        _isLabs = GameWorldController.CurrentMap == "laboratory";
        if (_isLabs)
        {
            Scale = new Vector3(Scale.x * 1.1f, Scale.y, Scale.z * 1.1f);
            _maxParticles = 5000;
            ParticleRate *= 6f; 
            ParticleSize = new ParticleSystem.MinMaxCurve(18f, 30f);
            _alpha *= 2.5f;
        }
    }

    void Update()
    {
        //don#t wnt timer to go on forever
        if (_timeExisted <= 4f)
        {
            _timeExisted += Time.deltaTime;
        }

        _dynamicOpacityModi = Mathf.Lerp(_dynamicOpacityModi, DynamicOpacityModiTarget, ZoneConstants.ZONE_LERP_SPEED * Time.deltaTime);
        _shapeModule.scale = Scale; //0.85 
        _mainModule.maxParticles = _maxParticles; 
        _mainModule.gravityModifier = 0f; 
        _mainModule.simulationSpeed = (_timeExisted <= 2f ? 1000 : _speed) * SpeedModi; 
        _mainModule.startSpeed = _startSpeedCurve; 
        _mainModule.startSize = ParticleSize; 
        _mainModule.startLifetime = _lifeTimeCurve; 
        _emissionModule.rateOverTime = ParticleRate;
        _mainModule.startColor = new Color(1f, 1f, 1f, _alpha * OpacityModi * _dynamicOpacityModi * GameWorldController.GAS_OPACITY_GLOBAL_MODI);
    }
}
