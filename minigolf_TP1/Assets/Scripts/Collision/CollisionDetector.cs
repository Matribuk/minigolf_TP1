using System.Collections.Generic;
using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private bool useCCD = true;                                // Continuous Collision Detection

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showCollisionPoints = true;

    private List<CollisionShape> dynamicShapes = new List<CollisionShape>();
    private List<CollisionShape> staticShapes = new List<CollisionShape>();

    private int totalTestsThisFrame = 0;
    private int collisionsDetectedThisFrame = 0;

    private void RegisterAllShapes()
    {
        dynamicShapes.Clear();
        staticShapes.Clear();
        
        CollisionShape[] allShapes = FindObjectsByType<CollisionShape>(FindObjectsSortMode.None);
        
        foreach (CollisionShape shape in allShapes) {
            if (shape.IsStatic)
                staticShapes.Add(shape);
            else
                dynamicShapes.Add(shape);
        }
        
        Debug.Log($"[CollisionDetector] Enregistré: {dynamicShapes.Count} objets dynamiques, {staticShapes.Count} objets statiques");
    }

    // Détection de collisions avec CCD - retourne les collisions triées par TOI
    public List<CollisionInfo> DetectCollisions(CollisionShape shape, Vector3 velocity)
    {
        return DetectCollisions(shape, velocity, Time.fixedDeltaTime);
    }

    // Surcharge avec deltaTime personnalisé pour les sous-cycles CCD
    public List<CollisionInfo> DetectCollisions(CollisionShape shape, Vector3 velocity, float deltaTime)
    {
        List<CollisionInfo> collisions = new List<CollisionInfo>();

        totalTestsThisFrame = 0;
        collisionsDetectedThisFrame = 0;

        // Complexité Temporelle O(n)
        // TODO : Quadtree pour optimisation
        foreach (CollisionShape staticShape in staticShapes)
        {
            CollisionInfo collision;
            totalTestsThisFrame++;

            collision = (useCCD && velocity.sqrMagnitude > PhysicsConstants.PREDICTIVE_VELOCITY_THRESHOLD_SQ)
                ? shape.TestCollisionPredictive(staticShape, velocity, deltaTime)
                : shape.TestCollision(staticShape);

            if (collision.hasCollision)
            {
                collisions.Add(collision);
                collisionsDetectedThisFrame++;

                if (showDebugInfo && showCollisionPoints)
                    Debug.DrawRay(collision.point, collision.normal, Color.red, 0.1f);
            }
        }

        foreach (CollisionShape otherShape in dynamicShapes)
        {
            if (otherShape == shape) continue;

            CollisionInfo collision;
            totalTestsThisFrame++;

            collision = (useCCD && velocity.sqrMagnitude > PhysicsConstants.PREDICTIVE_VELOCITY_THRESHOLD_SQ)
                ? shape.TestCollisionPredictive(otherShape, velocity, deltaTime)
                : shape.TestCollision(otherShape);

            if (collision.hasCollision)
            {
                collisions.Add(collision);
                collisionsDetectedThisFrame++;

                if (showDebugInfo && showCollisionPoints)
                    Debug.DrawRay(collision.point, collision.normal, Color.yellow, 0.1f);
            }
        }

        // Trier par TOI (collision la plus proche en premier)
        collisions.Sort((a, b) => a.timeOfImpact.CompareTo(b.timeOfImpact));

        return collisions;
    }

    public void RegisterShape(CollisionShape shape)
    {
        if (shape.IsStatic) {
            if (!staticShapes.Contains(shape))
                staticShapes.Add(shape);
        } else {
            if (!dynamicShapes.Contains(shape))
                dynamicShapes.Add(shape);
        }
    }

    public void UnregisterShape(CollisionShape shape)
    {
        staticShapes.Remove(shape);
        dynamicShapes.Remove(shape);
    }

    public void PrintStatistics()
    {
        Debug.Log($"[CollisionDetector] Tests ce frame: {totalTestsThisFrame}, Collisions: {collisionsDetectedThisFrame}");
    }

    public int GetTotalTests() => totalTestsThisFrame;
    public int GetCollisionsDetected() => collisionsDetectedThisFrame;

    void OnDrawGizmos()
    {
        if (!showDebugInfo) return;
        
        if (Application.isPlaying)
            Gizmos.color = Color.white;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RegisterAllShapes();
    }
}
