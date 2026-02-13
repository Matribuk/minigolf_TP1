using UnityEngine;

public struct CollisionInfo
{
    public bool hasCollision;                        // Si collision a été détectée
    public Vector3 point;                            // Point de contact (position de collision)
    public Vector3 normal;                           // Normale de la surface au point de contact
    public float penetrationDepth;                   // Profondeur de pénétration (si collision déjà arrivée)
    public float timeOfImpact;                       // Temps d'impact normalisé (0 = maintenant, 1 = fin du frame)
    public CollisionShape otherShape;                // L'autre objet (forme) impliquée dans la collision

    // Params de non-collision
    public static CollisionInfo NoCollision()
    {
        return new CollisionInfo
        {
            hasCollision = false,
            point = Vector3.zero,
            normal = Vector3.up,
            penetrationDepth = 0f,
            timeOfImpact = float.MaxValue,
            otherShape = null
        };
    }

    // Params de collision avec les params donnés
    public static CollisionInfo CreateCollision(Vector3 point, Vector3 normal, float depth, CollisionShape other)
    {
        return new CollisionInfo
        {
            hasCollision = true,
            point = point,
            normal = normal.normalized,
            penetrationDepth = depth,
            timeOfImpact = 0f,
            otherShape = other
        };
    }

    public static CollisionInfo CreateCollisionCCD(Vector3 point, Vector3 normal, float depth, float toi, CollisionShape other)
    {
        return new CollisionInfo
        {
            hasCollision = true,
            point = point,
            normal = normal.normalized,
            penetrationDepth = depth,
            timeOfImpact = toi,
            otherShape = other
        };
    }
}
