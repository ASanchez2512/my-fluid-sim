using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class FluidParticle : MonoBehaviour
{
    public Vector3 velocity;
    public float density;
    public float pressure;
    public bool isVapor;
    
    [HideInInspector] public Vector3 force;
    [HideInInspector] public float mass;
    [HideInInspector] public Color particleColor;
    
    private Renderer _renderer;

    void Start() {
        _renderer = GetComponent<Renderer>();
        UpdateVisuals();
    }

    void Update() {
        UpdateVisuals();
    }

    private void UpdateVisuals() {
        particleColor = isVapor ? 
            Color.Lerp(Color.white, Color.cyan, pressure * 0.1f) : 
            Color.Lerp(Color.blue, new Color(0, 0.5f, 1, 0.8f), density * 0.001f);
        
        _renderer.material.color = particleColor;
        transform.localScale = Vector3.one * (isVapor ? 0.3f : 0.5f);
    }
}