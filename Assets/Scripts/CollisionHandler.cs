using UnityEngine;

public class CollisionHandler : MonoBehaviour
{
    public Vector3 boundsSize = new Vector3(10, 10, 10);
    public float damping = 0.8f;
    
    void OnDrawGizmos() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, boundsSize);
    }
    
    public void HandleCollisions(FluidParticle particle) {
        Vector3 pos = particle.transform.position;
        Vector3 halfSize = boundsSize / 2f;
        
        // Límites del contenedor
        if (Mathf.Abs(pos.x) > halfSize.x) {
            particle.velocity.x *= -damping;
            particle.transform.position = new Vector3(Mathf.Sign(pos.x) * halfSize.x, pos.y, pos.z);
        }
        
        if (Mathf.Abs(pos.y) > halfSize.y) {
            particle.velocity.y *= -damping;
            particle.transform.position = new Vector3(pos.x, Mathf.Sign(pos.y) * halfSize.y, pos.z);
        }
        
        if (Mathf.Abs(pos.z) > halfSize.z) {
            particle.velocity.z *= -damping;
            particle.transform.position = new Vector3(pos.x, pos.y, Mathf.Sign(pos.z) * halfSize.z);
        }
    }
}