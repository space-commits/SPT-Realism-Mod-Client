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
    private MinMaxCurve _startSpeed = new ParticleSystem.MinMaxCurve(1, 1);

    void Awake()
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
            _maxParticles = 5000;
            ParticleRate = 5000f; //ensure max particles
            ParticleSize = new ParticleSystem.MinMaxCurve(16f, 24f);
            _alpha = 0.12f;
            _speed = 3f;
        }
    }

    void Update()
    {
        if (_timeExisted <= 2.5f)
        {
            _timeExisted += Time.deltaTime;
        }

        _shapeModule.scale = Scale * PluginConfig.test1.Value; //0.85 
        _mainModule.maxParticles = _maxParticles; 
        _mainModule.gravityModifier = 0f; 
        _mainModule.simulationSpeed = (_timeExisted <= 2f ? 1000 : _speed) * SpeedModi * PluginConfig.test2.Value; 
        _mainModule.startSpeed = _startSpeed; 
        _mainModule.startSize = ParticleSize; 
        _mainModule.startLifetime = new ParticleSystem.MinMaxCurve(20f, 30f); 
        _emissionModule.rateOverTime = ParticleRate;
        _mainModule.startColor = new Color(1f, 1f, 1f, _alpha * OpacityModi * PluginConfig.test3.Value);
    }
}
