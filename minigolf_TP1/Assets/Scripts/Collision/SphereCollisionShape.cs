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
            
            case CollisionShapeType.Cylinder:
                return TestSphereVsCylinder(this, (CylinderCollisionShape)other);
            
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

        return TestCollisionCCD(other, velocity, deltaTime);
    }

    private CollisionInfo TestCollisionCCD(CollisionShape other, Vector3 velocity, float deltaTime)
    {
        switch (other.GetShapeType())
        {
            case CollisionShapeType.Sphere:
                return TestSphereVsSphereCCD(this, (SphereCollisionShape)other, velocity, deltaTime);

            case CollisionShapeType.Box:
                return TestSphereVsBoxCCD(this, (BoxCollisionShape)other, velocity, deltaTime);

            case CollisionShapeType.Cylinder:
                return TestSphereVsCylinderCCD(this, (CylinderCollisionShape)other, velocity, deltaTime);

            case CollisionShapeType.Plane:
                return TestSphereVsPlaneCCD(this, (PlaneCollisionShape)other, velocity, deltaTime);

            default:
                return CollisionInfo.NoCollision();
        }
    }

    // Sphere vs Plane - Équation linéaire
    // On cherche t tel que: distance(center + velocity * t, plane) = radius
    // N · (C + V * t) - d = radius
    // t = (radius + d - N · C) / (N · V)
    private static CollisionInfo TestSphereVsPlaneCCD(SphereCollisionShape sphere, PlaneCollisionShape plane, Vector3 velocity, float deltaTime)
    {
        Vector3 sphereCenter = sphere.GetCenter();
        Vector3 planeNormal = plane.Normal;
        float planeDistance = plane.Distance;
        float sphereRadius = sphere.Radius;

        // Vérifier si on s'éloigne du plan
        float normalDotVelocity = Vector3.Dot(planeNormal, velocity);
        if (normalDotVelocity >= 0)
        {
            // On s'éloigne ou on est parallèle, pas de collision
            return CollisionInfo.NoCollision();
        }

        // Distance signée actuelle
        float signedDistance = Vector3.Dot(sphereCenter, planeNormal) - planeDistance;

        // Si déjà en collision
        if (signedDistance <= sphereRadius)
        {
            float depth = sphereRadius - signedDistance;
            Vector3 contactPoint = sphereCenter - planeNormal * signedDistance;
            return CollisionInfo.CreateCollisionCCD(contactPoint, planeNormal, depth, 0f, plane);
        }

        // Calcul du TOI: quand la sphère touchera le plan
        // t = (radius - signedDistance) / (-normalDotVelocity)
        float toi = (sphereRadius - signedDistance) / (-normalDotVelocity);

        // Normaliser par rapport à deltaTime
        float toiNormalized = toi / deltaTime;

        // Vérifier si la collision est dans le frame actuel
        if (toiNormalized < 0 || toiNormalized > 1)
            return CollisionInfo.NoCollision();

        // Position au moment de l'impact
        Vector3 impactCenter = sphereCenter + velocity * toi;
        Vector3 contactPoint2 = impactCenter - planeNormal * sphereRadius;

        return CollisionInfo.CreateCollisionCCD(contactPoint2, planeNormal, 0f, toiNormalized, plane);
    }

    // CCD: Sphere vs Sphere - Équation quadratique
    // |C1(t) - C2(t)|² = (r1 + r2)²
    // où C1(t) = C1 + V1 * t et C2(t) = C2 (statique ou avec vélocité)
    private static CollisionInfo TestSphereVsSphereCCD(SphereCollisionShape sphere1, SphereCollisionShape sphere2, Vector3 velocity, float deltaTime)
    {
        Vector3 center1 = sphere1.GetCenter();
        Vector3 center2 = sphere2.GetCenter();
        float radius1 = sphere1.Radius;
        float radius2 = sphere2.Radius;
        float sumRadii = radius1 + radius2;

        // Vecteur relatif
        Vector3 relativePos = center1 - center2;
        Vector3 relativeVel = velocity; // Supposant sphere2 statique

        // Coefficients de l'équation quadratique: at² + bt + c = 0
        float a = Vector3.Dot(relativeVel, relativeVel);
        float b = 2f * Vector3.Dot(relativePos, relativeVel);
        float c = Vector3.Dot(relativePos, relativePos) - sumRadii * sumRadii;

        // Si déjà en collision (c < 0)
        if (c < 0)
        {
            float distance = relativePos.magnitude;
            float depth = sumRadii - distance;
            Vector3 normal = distance > PhysicsConstants.NORMAL_EPSILON ? relativePos / distance : Vector3.up;
            Vector3 contactPoint = center1 - normal * radius1;
            return CollisionInfo.CreateCollisionCCD(contactPoint, -normal, depth, 0f, sphere2);
        }

        // Pas de mouvement relatif
        if (a < PhysicsConstants.DISTANCE_EPSILON)
            return CollisionInfo.NoCollision();

        // Discriminant
        float discriminant = b * b - 4f * a * c;
        if (discriminant < 0)
            return CollisionInfo.NoCollision();

        // Plus petite racine positive
        float sqrtDisc = UnityEngine.Mathf.Sqrt(discriminant);
        float t1 = (-b - sqrtDisc) / (2f * a);
        float t2 = (-b + sqrtDisc) / (2f * a);

        float toi = t1 >= 0 ? t1 : t2;
        if (toi < 0)
            return CollisionInfo.NoCollision();

        // Normaliser par rapport à deltaTime
        float toiNormalized = toi / deltaTime;
        if (toiNormalized > 1)
            return CollisionInfo.NoCollision();

        // Position au moment de l'impact
        Vector3 impactCenter1 = center1 + velocity * toi;
        Vector3 delta = impactCenter1 - center2;
        Vector3 normal2 = delta.normalized;
        Vector3 contactPoint2 = impactCenter1 - normal2 * radius1;

        return CollisionInfo.CreateCollisionCCD(contactPoint2, -normal2, 0f, toiNormalized, sphere2);
    }

    // CCD: Sphere vs Box - Ray-AABB intersection avec expansion
    // On expand la box par le rayon de la sphère et on fait un raycast
    private static CollisionInfo TestSphereVsBoxCCD(SphereCollisionShape sphere, BoxCollisionShape box, Vector3 velocity, float deltaTime)
    {
        Vector3 sphereCenter = sphere.GetCenter();
        float sphereRadius = sphere.Radius;
        Vector3 boxCenter = box.GetCenter();
        Vector3 boxHalfSize = box.Size * 0.5f;

        // D'abord vérifier si on est déjà en collision
        CollisionInfo currentCollision = TestSphereVsBox(sphere, box);
        if (currentCollision.hasCollision)
        {
            currentCollision.timeOfImpact = 0f;
            return currentCollision;
        }

        // Expand la box par le rayon de la sphère
        Vector3 expandedMin = boxCenter - boxHalfSize - Vector3.one * sphereRadius;
        Vector3 expandedMax = boxCenter + boxHalfSize + Vector3.one * sphereRadius;

        // Ray-AABB intersection (slab method)
        float tMin = 0f;
        float tMax = deltaTime;

        for (int i = 0; i < 3; i++)
        {
            float origin = sphereCenter[i];
            float dir = velocity[i];
            float min = expandedMin[i];
            float max = expandedMax[i];

            if (Mathf.Abs(dir) < PhysicsConstants.DISTANCE_EPSILON)
            {
                // Rayon parallèle à l'axe
                if (origin < min || origin > max)
                    return CollisionInfo.NoCollision();
            }
            else
            {
                float invDir = 1f / dir;
                float t1 = (min - origin) * invDir;
                float t2 = (max - origin) * invDir;

                if (t1 > t2)
                {
                    float temp = t1;
                    t1 = t2;
                    t2 = temp;
                }

                tMin = Mathf.Max(tMin, t1);
                tMax = Mathf.Min(tMax, t2);

                if (tMin > tMax)
                    return CollisionInfo.NoCollision();
            }
        }

        if (tMin < 0 || tMin > deltaTime)
            return CollisionInfo.NoCollision();

        // Position au moment de l'impact
        Vector3 impactCenter = sphereCenter + velocity * tMin;

        // Trouver le point le plus proche sur la box originale
        Vector3 boxMin = boxCenter - boxHalfSize;
        Vector3 boxMax = boxCenter + boxHalfSize;
        Vector3 closestPoint = new Vector3(
            Mathf.Clamp(impactCenter.x, boxMin.x, boxMax.x),
            Mathf.Clamp(impactCenter.y, boxMin.y, boxMax.y),
            Mathf.Clamp(impactCenter.z, boxMin.z, boxMax.z)
        );

        // Normale
        Vector3 delta = impactCenter - closestPoint;
        float distance = delta.magnitude;
        Vector3 normal = distance > PhysicsConstants.DISTANCE_EPSILON ? delta.normalized : Vector3.up;

        // Normaliser TOI
        float toiNormalized = tMin / deltaTime;

        return CollisionInfo.CreateCollisionCCD(closestPoint, normal, 0f, toiNormalized, box);
    }

    private CollisionInfo TestCollisionAtPosition(CollisionShape other, Vector3 centerPosition)
    {
        switch (other.GetShapeType())
        {
            case CollisionShapeType.Sphere:
                return TestSphereVsSphereAtPosition(this, (SphereCollisionShape)other, centerPosition);

            case CollisionShapeType.Box:
                return TestSphereVsBoxAtPosition(this, (BoxCollisionShape)other, centerPosition);

            case CollisionShapeType.Cylinder:
                return TestSphereVsCylinderAtPosition(this, (CylinderCollisionShape)other, centerPosition);

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

    private static CollisionInfo TestSphereVsCylinder(SphereCollisionShape sphere, CylinderCollisionShape cylinder)
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

    private static CollisionInfo TestSphereVsCylinderCCD(SphereCollisionShape sphere, CylinderCollisionShape cylinder, Vector3 velocity, float deltaTime)
    {
        Vector3 sphereCenter = sphere.GetCenter();
        float sphereRadius = sphere.Radius;

        // First check if already in collision
        CollisionInfo currentCollision = TestSphereVsCylinder(sphere, cylinder);
        if (currentCollision.hasCollision)
        {
            currentCollision.timeOfImpact = 0f;
            return currentCollision;
        }

        // For cylinders, use a simplified approach: sample points along the trajectory
        int samples = 5;
        float smallestToi = float.MaxValue;
        CollisionInfo closestCollision = CollisionInfo.NoCollision();

        for (int i = 1; i <= samples; i++)
        {
            float t = (float)i / samples;
            Vector3 samplePos = sphereCenter + velocity * (deltaTime * t);
            
            // Temporarily move sphere for sampling
            Vector3 tempPos = sphere.transform.position;
            sphere.transform.position = samplePos;
            CollisionInfo sampleCollision = TestSphereVsCylinder(sphere, cylinder);
            sphere.transform.position = tempPos;
            
            if (sampleCollision.hasCollision && t < smallestToi)
            {
                smallestToi = t;
                sampleCollision.timeOfImpact = t;
                closestCollision = sampleCollision;
            }
        }

        return closestCollision;
    }

    private static CollisionInfo TestSphereVsCylinderAtPosition(SphereCollisionShape sphere, CylinderCollisionShape cylinder, Vector3 sphereCenterPosition)
    {
        Vector3 cylinderCenter = cylinder.GetCenter();
        
        // Project sphere center onto cylinder axis
        Vector3 toSphere = sphereCenterPosition - cylinderCenter;
        float projectionOnAxis = Vector3.Dot(toSphere, Vector3.up);
        
        // Clamp to cylinder height
        float clampedProjection = Mathf.Clamp(projectionOnAxis, -cylinder.Height * 0.5f, cylinder.Height * 0.5f);
        
        // Closest point on cylinder axis
        Vector3 closestPointOnAxis = cylinderCenter + Vector3.up * clampedProjection;
        
        // Vector from closest point to sphere center
        Vector3 toSphereSide = sphereCenterPosition - closestPointOnAxis;
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
            Vector3 sphereToEdge = sphereCenterPosition - edgeCenter;
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

    protected override void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
