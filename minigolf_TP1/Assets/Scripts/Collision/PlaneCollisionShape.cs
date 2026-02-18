using UnityEngine;

public class PlaneCollisionShape : CollisionShape
{
    // The local normal is always up in the plane's local space
    // The actual world normal is calculated from the transform's rotation

    // D = N · P où P est la position sur le plan
    private float distance;
    
    // Retourne la normale en coordonnées monde (applique la rotation du transform)
    // Local normal est toujours Vector3.up, rotation vient du Transform
    public Vector3 Normal 
    { 
        get 
        {
            Vector3 worldNormal = (transform.rotation * Vector3.up).normalized;
            return worldNormal;
        }
    }
    public float Distance => distance;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // D = N·P où P est la position du plan
        distance = Vector3.Dot(Normal, transform.position);
        Debug.Log($"[PlaneCollisionShape] Initialized - Rotation: {transform.eulerAngles}, Normal: {Normal}, Distance: {distance}");
    }

    // Update is called once per frame
    void Update()
    {
        if (!isStatic)
            distance = Vector3.Dot(Normal, transform.position);
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
        float signedDistance = Vector3.Dot(point, Normal) - distance;
        return signedDistance > 0;
    }

    public float SignedDistanceToPoint(Vector3 point)
    {
        return Vector3.Dot(point, Normal) - distance;
    }

    protected override void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        Gizmos.color = gizmoColor;
        
        Vector3 center = transform.position;
        float size = 50f;
        
        Vector3 up = Normal;
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
        Gizmos.DrawRay(center, Normal * 2f);
    }
}
