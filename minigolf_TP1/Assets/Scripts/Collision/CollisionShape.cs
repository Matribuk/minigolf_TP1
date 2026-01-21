using UnityEngine;

public abstract class CollisionShape : MonoBehaviour
{
    [Header("Collision Properties")]
    [SerializeField] protected bool isStatic = true;                  // Objet statique ou dynamique
    [SerializeField] protected bool isTrigger = false;                // Trigger de collision
    
    [Header("Debug")]
    [SerializeField] protected bool showGizmos = true;
    [SerializeField] protected Color gizmoColor = Color.green;

    public abstract CollisionInfo TestCollision(CollisionShape other);
    public abstract CollisionInfo TestCollisionPredictive(CollisionShape other, Vector3 velocity, float deltaTime);
    public abstract CollisionShapeType GetShapeType();

    public virtual Vector3 GetCenter()
    {
        return transform.position;
    }

    protected abstract void OnDrawGizmos();

    public bool IsStatic => isStatic;
    public bool IsTrigger => isTrigger;
}

public enum CollisionShapeType
{
    Sphere,
    Box,
    Plane
}
