using UnityEngine;

public class BoxCollisionShape : CollisionShape
{
    [Header("Box Properties")]
    [SerializeField] private Vector3 size = Vector3.one;                // Taille de la box

    public Vector3 Size => size;

    public Vector3 Min => transform.position - size * 0.5f;
    public Vector3 Max => transform.position + size * 0.5f;

    public override CollisionInfo TestCollision(CollisionShape other)
    {
        switch (other.GetShapeType())
        {
            case CollisionShapeType.Sphere:
                CollisionInfo info = other.TestCollision(this);
                if (info.hasCollision) {
                    info.normal = -info.normal;
                    info.otherShape = this;
                }
                return info;
            
            case CollisionShapeType.Box:
                return TestBoxVsBox(this, (BoxCollisionShape)other);
            
            case CollisionShapeType.Cylinder:
                CollisionInfo cylInfo = other.TestCollision(this);
                if (cylInfo.hasCollision) {
                    cylInfo.normal = -cylInfo.normal;
                    cylInfo.otherShape = this;
                }
                return cylInfo;
            
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
        return CollisionShapeType.Box;
    }

    private static CollisionInfo TestBoxVsBox(BoxCollisionShape box1, BoxCollisionShape box2)
    {
        Vector3 min1 = box1.Min;
        Vector3 max1 = box1.Max;
        Vector3 min2 = box2.Min;
        Vector3 max2 = box2.Max;
        
        bool overlapX = max1.x >= min2.x && min1.x <= max2.x;
        bool overlapY = max1.y >= min2.y && min1.y <= max2.y;
        bool overlapZ = max1.z >= min2.z && min1.z <= max2.z;
        
        if (!overlapX || !overlapY || !overlapZ)
            return CollisionInfo.NoCollision();
        
        float depthX = Mathf.Min(max1.x - min2.x, max2.x - min1.x);
        float depthY = Mathf.Min(max1.y - min2.y, max2.y - min1.y);
        float depthZ = Mathf.Min(max1.z - min2.z, max2.z - min1.z);
        
        Vector3 normal;
        float depth;
        
        if (depthX < depthY && depthX < depthZ) {
            normal = box1.GetCenter().x < box2.GetCenter().x ? Vector3.left : Vector3.right;
            depth = depthX;
        } else if (depthY < depthZ) {
            normal = box1.GetCenter().y < box2.GetCenter().y ? Vector3.down : Vector3.up;
            depth = depthY;
        } else {
            normal = box1.GetCenter().z < box2.GetCenter().z ? Vector3.back : Vector3.forward;
            depth = depthZ;
        }
        
        // HitPosition, centre de la zone d'overlap
        Vector3 contactPoint = new Vector3(
            (Mathf.Max(min1.x, min2.x) + Mathf.Min(max1.x, max2.x)) * 0.5f,
            (Mathf.Max(min1.y, min2.y) + Mathf.Min(max1.y, max2.y)) * 0.5f,
            (Mathf.Max(min1.z, min2.z) + Mathf.Min(max1.z, max2.z)) * 0.5f
        );
        
        return CollisionInfo.CreateCollision(contactPoint, normal, depth, box2);
    }

    protected override void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(transform.position, size);
    }
}
