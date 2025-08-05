using System;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Rendering;
using Math = System.Math;

public class Sim2D : MonoBehaviour
{
    public float gravity;

    public int numParticles = 1;
    Vector3[] positions;
    Vector3[] velocities;
    float[] densities;
    public float dampingFactor = 1.0f;
    public float mass = 1.0f;

    public float targetDensity;
    public float pressureMultiplier;
    Mesh esferaMesh;
    public Material material;

    public float particleSize = 1.0f;
    public float particleSpacing = 1.0f;
    public float smoothingRadius = 1.0f;

    public Vector3 halfBoundSize = new Vector3(10, 10, 10);

    private float deltaTime;
    private System.Random random;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        esferaMesh = temp.GetComponent<MeshFilter>().sharedMesh;
        Destroy(temp); // Eliminas la esfera pero conservas el mesh

        random = new System.Random();

        positions = new Vector3[numParticles];
        velocities = new Vector3[numParticles];
        densities = new float[numParticles];

        /*
        int particlesPerRow = (int)Math.Sqrt(numParticles);
        int particlesPerCol = (numParticles - 1) / particlesPerRow + 1;
        float spacing = particleSize * 2 + particleSpacing;

        for (int i = 0; i < numParticles; i++)
        {
            float x = (i % particlesPerRow - particlesPerRow / 2f + 0.5f) * spacing;
            float y = (i / particlesPerRow - particlesPerCol / 2f + 0.5f) * spacing;
            positions[i] = new Vector3(x, y, 0.0f);
            particleProperties[i] = 0;
            densities[i] = 1;
        }
        */

        for (int i = 0; i < numParticles; i++)
        {
            float x = (float) (random.NextDouble() * 2 - 1) * halfBoundSize.x;
            float y = (float) (random.NextDouble() * 2 - 1) * halfBoundSize.y;
            positions[i] = new Vector3(x, y, 0.0f);
            densities[i] = 1;
        }
    }

    // Update is called once per frame
    void Update()
    {
        deltaTime = Time.deltaTime;
        SimulationStep();

        for (int i = 0; i < positions.Length; i++)
        {
            Graphics.DrawMesh(
                esferaMesh,
                Matrix4x4.TRS(positions[i], quaternion.identity, Vector3.one * particleSize),
                material,
                0);
        }
    }

    void SimulationStep()
    {
        Parallel.For(0, numParticles, i =>
        {
            velocities[i] += Vector3.down * gravity * deltaTime;
            densities[i] = CalculateDensity(positions[i]);
        });

        Parallel.For(0, numParticles, i =>
        {
            Vector3 pressureForce = CalculatePressureForce(i);
            Vector3 pressureAcceleration = pressureForce / densities[i];
            velocities[i] += pressureAcceleration * deltaTime;
        });

        Parallel.For(0, numParticles, i =>
        {
            positions[i] += velocities[i] * deltaTime;
            resolveColissions(ref positions[i], ref velocities[i]);
        });
    }

    void resolveColissions(ref Vector3 position, ref Vector3 velocity)
    {
        if (Math.Abs(position.x) > halfBoundSize.x - particleSize / 2f)
        {
            position.x = (halfBoundSize.x - particleSize / 2f) * Math.Sign(position.x);
            velocity.x *= -1 * dampingFactor;
        }

        if (Math.Abs(position.y) > halfBoundSize.y - particleSize / 2f)
        {
            position.y = (halfBoundSize.y - particleSize / 2f) * Math.Sign(position.y);
            velocity.y *= -1 * dampingFactor;
        }
    }

    float ConvertDensityToPressure(float density)
    {
        float densityError = density - targetDensity;
        float pressure = densityError * pressureMultiplier;
        return pressure;
    }

    static float SmoothingKernel(float dst, float radius)
    {
        if (dst >= radius) return 0;

        float volume = (float)(Math.PI * Math.Pow(radius, 4) / 6);
        return (radius - dst) * (radius - dst) / volume;
    }
    
    static float SmoothingKernelDerivative(float dst, float radius)
    {
        if (dst >= radius) return 0;
        
        float scale = 12.0f / (float)(Math.PI * Math.Pow(radius, 4));
        return scale * (dst - radius);
    }

    float CalculateDensity(Vector3 samplePoint)
    {
        float density = 0;

        foreach (Vector3 position in positions)
        {
            float dst = (position - samplePoint).magnitude;
            float influence = SmoothingKernel(dst, smoothingRadius);
            density += mass * influence;
        }

        return density;
    }

    Vector3 CalculatePressureForce(int particleIndex)
    {
        Vector3 pressureForce = Vector3.zero;

        for (int otherParticleIndex = 0; otherParticleIndex < numParticles; otherParticleIndex++)
        {
            if (particleIndex == otherParticleIndex) continue;

            Vector3 offset = positions[otherParticleIndex] - positions[particleIndex];
            float dst = offset.magnitude;
            Vector3 dir = dst == 0? GetRandomDir() : offset / dst;
            float slope = SmoothingKernelDerivative(dst, smoothingRadius);
            float density = densities[otherParticleIndex];
            float sharedPressure = CalculateSharedPressure(density, densities[particleIndex]);
            pressureForce += sharedPressure * dir * slope * mass / density;
        }

        return pressureForce;
    }

    private float CalculateSharedPressure(float densityA, float densityB)
    {
        float pressureA = ConvertDensityToPressure(densityA);
        float pressureB = ConvertDensityToPressure(densityB);
        return (pressureA + pressureB) / 2;
    }

    private Vector3 GetRandomDir()
    {
        float angle = 2 * (float)random.NextDouble() * (float)Math.PI;
        return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
    }
}
