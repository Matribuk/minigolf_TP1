using UnityEngine;

public class PlaneCollisionShape : CollisionShape
{
    [Header("Plane Properties")]
    [SerializeField] private Vector3 normal = Vector3.up;                // Normale du plan

    // D = N · P où P est la position sur le plan
    private float distance;
    
    public Vector3 Normal => normal.normalized;
    public float Distance => distance;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // D = N·P où P est la position du plan
        distance = Vector3.Dot(normal.normalized, transform.position);
    }

    // Update is called once per frame
    void Update()
    {
        if (!isStatic)
            distance = Vector3.Dot(normal.normalized, transform.position);
    }

    public override CollisionInfo TestCollision(CollisionShape other)
    {
        switch (other.GetShapeType())
        {
            case CollisionShapeType.Sphere:
                CollisionInfo info = other.TestCollision(this);
                if (info.hasCollision)
                    info.otherShape = this;
                return info;
            
            case CollisionShapeType.Box:
                return CollisionInfo.NoCollision();
            
            case CollisionShapeType.Plane:
                return CollisionInfo.NoCollision();
            
            default:
                return CollisionInfo.NoCollision();
        }
    }

    public override CollisionInfo TestCollisionPredictive(CollisionShape other, Vector3 velocity, float deltaTime)
    {
        return TestCollision(other);
    }

    public override CollisionShapeType GetShapeType()
    {
        return CollisionShapeType.Plane;
    }

    public bool IsAbovePlane(Vector3 point)
    {
        float signedDistance = Vector3.Dot(point, normal.normalized) - distance;
        return signedDistance > 0;
    }

    public float SignedDistanceToPoint(Vector3 point)
    {
        return Vector3.Dot(point, normal.normalized) - distance;
    }

    protected override void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        Gizmos.color = gizmoColor;
        
        Vector3 center = transform.position;
        float size = 50f;
        
        Vector3 up = normal.normalized;
        Vector3 right = Vector3.Cross(up, Vector3.forward);
        if (right.sqrMagnitude < PhysicsConstants.NORMAL_EPSILON)
        {
            right = Vector3.Cross(up, Vector3.right);
        }
        right.Normalize();
        Vector3 forward = Vector3.Cross(right, up);
        
        Vector3 p1 = center + right * size + forward * size;
        Vector3 p2 = center - right * size + forward * size;
        Vector3 p3 = center - right * size - forward * size;
        Vector3 p4 = center + right * size - forward * size;
        
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(center, normal.normalized * 2f);
    }
}
