using UnityEngine;

public class SphereCollisionShape : CollisionShape
{
    [Header("Sphere Properties")]
    [SerializeField] private float radius = 0.5f;                // Rayon de la sphère
    
    public float Radius => radius;

    public override CollisionInfo TestCollision(CollisionShape other)
    {
        switch (other.GetShapeType())
        {
            case CollisionShapeType.Sphere:
                return TestSphereVsSphere(this, (SphereCollisionShape)other);
            
            case CollisionShapeType.Box:
                return TestSphereVsBox(this, (BoxCollisionShape)other);
            
            case CollisionShapeType.Plane:
                return TestSphereVsPlane(this, (PlaneCollisionShape)other);
            
            default:
                return CollisionInfo.NoCollision();
        }
    }

    public override CollisionInfo TestCollisionPredictive(CollisionShape other, Vector3 velocity, float deltaTime)
    {
        if (velocity.sqrMagnitude < PhysicsConstants.PREDICTIVE_VELOCITY_THRESHOLD_SQ)
            return TestCollision(other);

        // Calculer la future position sans modifier le transform
        Vector3 futureCenter = GetCenter() + velocity * deltaTime;

        // Tester la collision à la future position
        return TestCollisionAtPosition(other, futureCenter);
    }

    private CollisionInfo TestCollisionAtPosition(CollisionShape other, Vector3 centerPosition)
    {
        switch (other.GetShapeType())
        {
            case CollisionShapeType.Sphere:
                return TestSphereVsSphereAtPosition(this, (SphereCollisionShape)other, centerPosition);

            case CollisionShapeType.Box:
                return TestSphereVsBoxAtPosition(this, (BoxCollisionShape)other, centerPosition);

            case CollisionShapeType.Plane:
                return TestSphereVsPlaneAtPosition(this, (PlaneCollisionShape)other, centerPosition);

            default:
                return CollisionInfo.NoCollision();
        }
    }

    public override CollisionShapeType GetShapeType()
    {
        return CollisionShapeType.Sphere;
    }

    private static CollisionInfo TestSphereVsSphere(SphereCollisionShape sphere1, SphereCollisionShape sphere2)
    {
        Vector3 center1 = sphere1.GetCenter();
        Vector3 center2 = sphere2.GetCenter();

        Vector3 delta = center2 - center1;
        float distanceSquared = delta.sqrMagnitude;
        float radiusSum = sphere1.Radius + sphere2.Radius;
        float radiusSumSquared = radiusSum * radiusSum;

        if (distanceSquared > radiusSumSquared)
            return CollisionInfo.NoCollision();

        float distance = Mathf.Sqrt(distanceSquared);
        float depth = radiusSum - distance;

        // Normale de sphere1 à sphere2
        Vector3 normal = distance > PhysicsConstants.NORMAL_EPSILON ? delta / distance : Vector3.up;

        // HitPosition à la surface de sphere1 sur la ligne entre c1 et c2
        Vector3 contactPoint = center1 + normal * sphere1.Radius;

        return CollisionInfo.CreateCollision(contactPoint, normal, depth, sphere2);
    }

    private static CollisionInfo TestSphereVsBox(SphereCollisionShape sphere, BoxCollisionShape box)
    {
        Vector3 sphereCenter = sphere.GetCenter();
        Vector3 boxCenter = box.GetCenter();
        Vector3 boxHalfSize = box.Size * 0.5f;

        // Min Max AABB
        Vector3 boxMin = boxCenter - boxHalfSize;
        Vector3 boxMax = boxCenter + boxHalfSize;

        Vector3 closestPoint = new Vector3(
            Mathf.Clamp(sphereCenter.x, boxMin.x, boxMax.x),
            Mathf.Clamp(sphereCenter.y, boxMin.y, boxMax.y),
            Mathf.Clamp(sphereCenter.z, boxMin.z, boxMax.z)
        );

        Vector3 delta = closestPoint - sphereCenter;
        float distanceSquared = delta.sqrMagnitude;
        float radiusSquared = sphere.Radius * sphere.Radius;

        if (distanceSquared > radiusSquared)
            return CollisionInfo.NoCollision();

        float distance = Mathf.Sqrt(distanceSquared);
        float depth = sphere.Radius - distance;

        Vector3 normal = distance > PhysicsConstants.DISTANCE_EPSILON ? -delta.normalized : Vector3.up;

        return CollisionInfo.CreateCollision(closestPoint, normal, depth, box);
    }

    private static CollisionInfo TestSphereVsPlane(SphereCollisionShape sphere, PlaneCollisionShape plane)
    {
        Vector3 sphereCenter = sphere.GetCenter();
        Vector3 planeNormal = plane.Normal;
        float planeDistance = plane.Distance;

        // d = (centre · normale) - distance_plan
        float signedDistance = Vector3.Dot(sphereCenter, planeNormal) - planeDistance;

        // No collision alors sphere au dessus du plan
        if (signedDistance > sphere.Radius)
            return CollisionInfo.NoCollision();

        float depth = sphere.Radius - signedDistance;

        Vector3 contactPoint = sphereCenter - planeNormal * signedDistance;
        Vector3 normal = planeNormal;

        return CollisionInfo.CreateCollision(contactPoint, normal, depth, plane);
    }

    private static CollisionInfo TestSphereVsSphereAtPosition(SphereCollisionShape sphere1, SphereCollisionShape sphere2, Vector3 center1Position)
    {
        Vector3 center2 = sphere2.GetCenter();

        Vector3 delta = center2 - center1Position;
        float distanceSquared = delta.sqrMagnitude;
        float radiusSum = sphere1.Radius + sphere2.Radius;
        float radiusSumSquared = radiusSum * radiusSum;

        if (distanceSquared > radiusSumSquared)
            return CollisionInfo.NoCollision();

        float distance = Mathf.Sqrt(distanceSquared);
        float depth = radiusSum - distance;

        // Normale de sphere1 à sphere2
        Vector3 normal = distance > PhysicsConstants.NORMAL_EPSILON ? delta / distance : Vector3.up;

        // HitPosition à la surface de sphere1 sur la ligne entre c1 et c2
        Vector3 contactPoint = center1Position + normal * sphere1.Radius;

        return CollisionInfo.CreateCollision(contactPoint, normal, depth, sphere2);
    }

    private static CollisionInfo TestSphereVsBoxAtPosition(SphereCollisionShape sphere, BoxCollisionShape box, Vector3 sphereCenterPosition)
    {
        Vector3 boxCenter = box.GetCenter();
        Vector3 boxHalfSize = box.Size * 0.5f;

        // Min Max AABB
        Vector3 boxMin = boxCenter - boxHalfSize;
        Vector3 boxMax = boxCenter + boxHalfSize;

        Vector3 closestPoint = new Vector3(
            Mathf.Clamp(sphereCenterPosition.x, boxMin.x, boxMax.x),
            Mathf.Clamp(sphereCenterPosition.y, boxMin.y, boxMax.y),
            Mathf.Clamp(sphereCenterPosition.z, boxMin.z, boxMax.z)
        );

        Vector3 delta = closestPoint - sphereCenterPosition;
        float distanceSquared = delta.sqrMagnitude;
        float radiusSquared = sphere.Radius * sphere.Radius;

        if (distanceSquared > radiusSquared)
            return CollisionInfo.NoCollision();

        float distance = Mathf.Sqrt(distanceSquared);
        float depth = sphere.Radius - distance;

        Vector3 normal = distance > PhysicsConstants.DISTANCE_EPSILON ? -delta.normalized : Vector3.up;

        return CollisionInfo.CreateCollision(closestPoint, normal, depth, box);
    }

    private static CollisionInfo TestSphereVsPlaneAtPosition(SphereCollisionShape sphere, PlaneCollisionShape plane, Vector3 sphereCenterPosition)
    {
        Vector3 planeNormal = plane.Normal;
        float planeDistance = plane.Distance;

        // d = (centre · normale) - distance_plan
        float signedDistance = Vector3.Dot(sphereCenterPosition, planeNormal) - planeDistance;

        // No collision alors sphère au dessus du plan
        if (signedDistance > sphere.Radius)
            return CollisionInfo.NoCollision();

        float depth = sphere.Radius - signedDistance;

        Vector3 contactPoint = sphereCenterPosition - planeNormal * signedDistance;
        Vector3 normal = planeNormal;

        return CollisionInfo.CreateCollision(contactPoint, normal, depth, plane);
    }

    protected override void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
