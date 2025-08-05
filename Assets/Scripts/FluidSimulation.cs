using System.Collections.Generic;
using UnityEngine;

public class FluidSimulation : MonoBehaviour
{
    [Header("Simulation Settings")]
    public int particleCount = 500;
    public GameObject particlePrefab;
    public float smoothingRadius = 1f;
    public float targetDensity = 100f;
    public float pressureMultiplier = 200f;
    public float vaporPressureMultiplier = 50f;
    public float viscosity = 10f;
    public Vector3 gravity = new Vector3(0, -9.81f, 0);

    [Header("Fluid Properties")]
    [Range(0f, 1f)] public float vaporRatio = 0.2f;
    public float vaporTransitionThreshold = 200f;

    private List<FluidParticle> _particles = new List<FluidParticle>();
    private CollisionHandler _collisionHandler;
    
    void Start() {
        _collisionHandler = FindObjectOfType<CollisionHandler>();
        InitializeParticles();
    }

    void FixedUpdate() {
        UpdateDensities();
        CalculateForces();
        UpdateParticles(Time.fixedDeltaTime);
        HandlePhaseTransitions();
    }

    private void InitializeParticles() {
        for (int i = 0; i < particleCount; i++) {
            Vector3 pos = transform.position + Random.insideUnitSphere * 3f;
            var particleObj = Instantiate(particlePrefab, pos, Quaternion.identity);
            var particle = particleObj.GetComponent<FluidParticle>();
            
            particle.velocity = Random.insideUnitSphere;
            particle.mass = 1f;
            particle.isVapor = (i < particleCount * vaporRatio);
            
            _particles.Add(particle);
        }
    }

    private void UpdateDensities() {
        foreach (var p in _particles) {
            p.density = 0;
            foreach (var neighbor in _particles) {
                float dist = Vector3.Distance(p.transform.position, neighbor.transform.position);
                if (dist < smoothingRadius) {
                    p.density += p.mass * Kernel(dist, smoothingRadius);
                }
            }
        }
    }

    private void CalculateForces() {
        foreach (var p in _particles) {
            Vector3 pressureForce = Vector3.zero;
            Vector3 viscosityForce = Vector3.zero;

            foreach (var neighbor in _particles) {
                if (p == neighbor) continue;
                
                Vector3 dir = neighbor.transform.position - p.transform.position;
                float dist = dir.magnitude;
                
                if (dist < smoothingRadius && dist > 0) {
                    // Fuerza de presión (repulsión)
                    float pressureFactor = -(neighbor.pressure + p.pressure) / 2f;
                    pressureForce += dir.normalized * pressureFactor * DerivativeKernel(dist, smoothingRadius) / neighbor.density;
                    
                    // Fuerza de viscosidad (suavizado)
                    viscosityForce += (neighbor.velocity - p.velocity) * ViscosityKernel(dist, smoothingRadius) / neighbor.density;
                }
            }
            
            // Aplicar presión según tipo
            float pressureMult = p.isVapor ? vaporPressureMultiplier : pressureMultiplier;
            p.pressure = pressureMult * (p.density - targetDensity);
            
            // Suma de fuerzas
            p.force = 
                pressureForce * p.mass + 
                viscosityForce * viscosity * p.mass + 
                gravity * p.mass;
        }
    }

    private void UpdateParticles(float deltaTime) {
        foreach (var p in _particles) {
            p.velocity += p.force / p.mass * deltaTime;
            p.transform.position += p.velocity * deltaTime;
            _collisionHandler.HandleCollisions(p);
        }
    }

    private void HandlePhaseTransitions() {
        foreach (var p in _particles) {
            // Transición agua -> vapor
            if (!p.isVapor && p.density < vaporTransitionThreshold && Random.value > 0.99f) {
                p.isVapor = true;
                p.velocity += Random.insideUnitSphere * 2f;
            }
            // Transición vapor -> agua
            else if (p.isVapor && p.density > vaporTransitionThreshold * 1.5f && Random.value > 0.99f) {
                p.isVapor = false;
            }
        }
    }

    // Kernels SPH
    private float Kernel(float dist, float radius) {
        float volume = Mathf.PI * Mathf.Pow(radius, 8) / 4f;
        float v = Mathf.Max(0, radius * radius - dist * dist);
        return v * v * v / volume;
    }

    private float DerivativeKernel(float dist, float radius) {
        float scale = -45f / (Mathf.PI * Mathf.Pow(radius, 6));
        float v = radius - dist;
        return scale * v * v;
    }

    private float ViscosityKernel(float dist, float radius) {
        return 45f / (Mathf.PI * Mathf.Pow(radius, 6)) * (radius - dist);
    }
}