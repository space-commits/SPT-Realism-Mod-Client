using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity;
using RealismMod;
using static UnityEngine.ParticleSystem;

public class FogScript : MonoBehaviour
{
    public float ParticleLifeTime { get; set; }
    public float ParticleSize { get; set; }
    public Vector3 Scale { get; set; }
    private ParticleSystem _ps;
    private ParticleSystem.MainModule _mainModule;
    private ParticleSystem.ShapeModule _shapeModule;
    private ParticleSystem.CollisionModule _collisionModule;
    private bool _isLabs = false;
    private int _maxParticles = 600;
    private float _timeExisted = 0f;

    void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
        _mainModule = _ps.main;
        _shapeModule = _ps.shape;
        _collisionModule = _ps.collision;
        _collisionModule.enabled = false;
        _mainModule.gravityModifier = 0f;
        _shapeModule.scale = Scale;
        _isLabs = GameWorldController.CurrentMap == "laboratory";
        if (_isLabs) 
        {
            _maxParticles = 5000;
            ParticleLifeTime = 100f;
            ParticleSize = 50f;
        }
    }

    void Update()
    {
        if (_timeExisted <= 2.5f)
        {
            _timeExisted += Time.deltaTime;
        }

        _shapeModule.scale = Scale;
        _mainModule.maxParticles = _maxParticles; 
        _mainModule.gravityModifier = 0.015f; 
        _mainModule.simulationSpeed = _timeExisted <= 2f ? 100f : 0.01f; //early stages of particle sim have lighting issues, speed it up to get to a later stage of the sim.
        _mainModule.startSizeX = ParticleSize; 
        _mainModule.startSizeY = ParticleSize; 
        _mainModule.startSizeZ = ParticleSize; 
        _mainModule.startColor = new UnityEngine.Color(1f, 1f, 1f, 1f);
        _mainModule.startLifetime = ParticleLifeTime; 
        _mainModule.duration = 5f; 
    }
}
