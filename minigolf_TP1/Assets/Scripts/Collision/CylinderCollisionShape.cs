using UnityEngine;

public class CylinderCollisionShape : CollisionShape
{
    [Header("Cylinder Properties")]
    [SerializeField] private float radius = 1.0f;                   // Rayon du cylindre
    [SerializeField] private float height = 1.0f;                    // Hauteur du cylindre

    public float Radius => radius;
    public float Height => height;

    public Vector3 TopCenter => transform.position + Vector3.up * height * 0.5f;
    public Vector3 BottomCenter => transform.position - Vector3.up * height * 0.5f;

    public override CollisionInfo TestCollision(CollisionShape other)
    {
        switch (other.GetShapeType())
        {
            case CollisionShapeType.Sphere:
                return TestCylinderVsSphere(this, (SphereCollisionShape)other);
            
            case CollisionShapeType.Box:
                CollisionInfo boxInfo = other.TestCollision(this);
                if (boxInfo.hasCollision) {
                    boxInfo.normal = -boxInfo.normal;
                    boxInfo.otherShape = this;
                }
                return boxInfo;
            
            case CollisionShapeType.Cylinder:
                return TestCylinderVsCylinder(this, (CylinderCollisionShape)other);
            
            case CollisionShapeType.Plane:
                return CollisionInfo.NoCollision();
            
            default:
                return CollisionInfo.NoCollision();
        }
    }

    public override CollisionInfo TestCollisionPredictive(CollisionShape other, Vector3 velocity, float deltaTime)
    {
        if (isStatic)
            return TestCollision(other);
        
        Vector3 futurePosition = transform.position + velocity * deltaTime;
        Vector3 originalPos = transform.position;
        
        transform.position = futurePosition;
        CollisionInfo collision = TestCollision(other);
        transform.position = originalPos;

        return collision;
    }

    public override CollisionShapeType GetShapeType()
    {
        return CollisionShapeType.Cylinder;
    }

    private static CollisionInfo TestCylinderVsSphere(CylinderCollisionShape cylinder, SphereCollisionShape sphere)
    {
        Vector3 sphereCenter = sphere.GetCenter();
        Vector3 cylinderCenter = cylinder.GetCenter();
        
        // Project sphere center onto cylinder axis
        Vector3 toSphere = sphereCenter - cylinderCenter;
        float projectionOnAxis = Vector3.Dot(toSphere, Vector3.up);
        
        // Clamp to cylinder height
        float clampedProjection = Mathf.Clamp(projectionOnAxis, -cylinder.Height * 0.5f, cylinder.Height * 0.5f);
        
        // Closest point on cylinder axis
        Vector3 closestPointOnAxis = cylinderCenter + Vector3.up * clampedProjection;
        
        // Vector from closest point to sphere center
        Vector3 toSphereSide = sphereCenter - closestPointOnAxis;
        toSphereSide.y = 0; // Only horizontal component
        
        float horizontalDistance = toSphereSide.magnitude;
        float minDistance = cylinder.Radius + sphere.Radius;
        
        if (horizontalDistance > minDistance)
            return CollisionInfo.NoCollision();
        
        // Check if we need to handle edge collision (sphere beyond cylinder caps)
        bool beyondTop = projectionOnAxis > cylinder.Height * 0.5f;
        bool beyondBottom = projectionOnAxis < -cylinder.Height * 0.5f;
        
        if (beyondTop || beyondBottom)
        {
            Vector3 edgeCenter = beyondTop ? cylinder.TopCenter : cylinder.BottomCenter;
            Vector3 sphereToEdge = sphereCenter - edgeCenter;
            float distToEdge = sphereToEdge.magnitude;
            
            if (distToEdge > sphere.Radius)
                return CollisionInfo.NoCollision();
            
            // Collision with edge
            float depth = sphere.Radius - distToEdge;
            Vector3 normal = distToEdge > PhysicsConstants.DISTANCE_EPSILON ? sphereToEdge / distToEdge : Vector3.up;
            Vector3 contactPoint = edgeCenter + normal * cylinder.Radius;
            
            return CollisionInfo.CreateCollision(contactPoint, normal, depth, cylinder);
        }
        
        // Collision with side of cylinder
        float depth2 = minDistance - horizontalDistance;
        Vector3 normal2 = horizontalDistance > PhysicsConstants.DISTANCE_EPSILON ? toSphereSide / horizontalDistance : Vector3.right;
        Vector3 contactPoint2 = closestPointOnAxis + normal2 * cylinder.Radius;
        
        return CollisionInfo.CreateCollision(contactPoint2, normal2, depth2, cylinder);
    }

    private static CollisionInfo TestCylinderVsCylinder(CylinderCollisionShape cylinder1, CylinderCollisionShape cylinder2)
    {
        Vector3 center1 = cylinder1.GetCenter();
        Vector3 center2 = cylinder2.GetCenter();
        
        // Horizontal distance between cylinder axes
        Vector3 horizontalDiff = new Vector3(center2.x - center1.x, 0, center2.z - center1.z);
        float horizontalDistance = horizontalDiff.magnitude;
        
        float sumRadii = cylinder1.Radius + cylinder2.Radius;
        
        if (horizontalDistance > sumRadii)
            return CollisionInfo.NoCollision();
        
        // Check vertical overlap
        float top1 = center1.y + cylinder1.Height * 0.5f;
        float bottom1 = center1.y - cylinder1.Height * 0.5f;
        float top2 = center2.y + cylinder2.Height * 0.5f;
        float bottom2 = center2.y - cylinder2.Height * 0.5f;
        
        bool overlapY = top1 >= bottom2 && bottom1 <= top2;
        
        if (!overlapY)
            return CollisionInfo.NoCollision();
        
        // Calculate collision details
        float depth = sumRadii - horizontalDistance;
        Vector3 normal = horizontalDistance > PhysicsConstants.DISTANCE_EPSILON ? horizontalDiff.normalized : Vector3.right;
        Vector3 contactPoint = center1 + normal * cylinder1.Radius;
        
        return CollisionInfo.CreateCollision(contactPoint, normal, depth, cylinder2);
    }

    protected override void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        Gizmos.color = gizmoColor;
        
        Vector3 top = TopCenter;
        Vector3 bottom = BottomCenter;
        
        // Draw cylinder edges
        int segments = 16;
        float angleStep = 360f / segments;
        
        Vector3[] topCircle = new Vector3[segments];
        Vector3[] bottomCircle = new Vector3[segments];
        
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            
            topCircle[i] = top + new Vector3(x, 0, z);
            bottomCircle[i] = bottom + new Vector3(x, 0, z);
        }
        
        // Draw circles
        for (int i = 0; i < segments; i++)
        {
            int nextI = (i + 1) % segments;
            Gizmos.DrawLine(topCircle[i], topCircle[nextI]);
            Gizmos.DrawLine(bottomCircle[i], bottomCircle[nextI]);
            Gizmos.DrawLine(topCircle[i], bottomCircle[i]);
        }
        
        // Draw axis
        Gizmos.color = Color.green;
        Gizmos.DrawLine(bottom, top);
    }
}
