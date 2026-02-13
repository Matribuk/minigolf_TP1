using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(CustomPhysics))]
[RequireComponent(typeof(SphereCollisionShape))]
public class BallController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CollisionDetector collisionDetector;
    
    [Header("Collision Response Settings")]
    [SerializeField] private float minBounceVelocity = 0.5f;       // Vitesse min pour rebondir
    [SerializeField] private bool enableCollisionResponse = true;
    
    [Header("Gameplay")]
    [SerializeField] private Vector3 startPosition;                // Position de départ
    [SerializeField] private bool hasWon = false;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private CustomPhysics physics;
    private SphereCollisionShape collisionShape;
    
    private int totalCollisionsThisFrame = 0;
    private int totalBouncesThisShot = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        physics = GetComponent<CustomPhysics>();
        collisionShape = GetComponent<SphereCollisionShape>();
        
        if (collisionDetector == null) {
            collisionDetector = FindFirstObjectByType<CollisionDetector>();
            if (collisionDetector == null)
                Debug.LogError("[BallController] Aucun CollisionDetector trouvé dans la scène !");
        }
        startPosition = transform.position;
        
        Debug.Log("[BallController] Initialisé");
    }

    void FixedUpdate()
    {
        if (hasWon) return;

        if (!physics.IsMoving)
        {
            totalBouncesThisShot = 0;
            return;
        }

        // CCD: Boucle de sous-cycles pour gérer les collisions multiples dans un frame
        float remainingTime = 1.0f;  // Fraction du frame restante (0-1)
        totalCollisionsThisFrame = 0;

        for (int iteration = 0; iteration < PhysicsConstants.CCD_MAX_ITERATIONS && remainingTime > PhysicsConstants.CCD_MIN_REMAINING_TIME; iteration++)
        {
            float subDeltaTime = remainingTime * Time.fixedDeltaTime;

            // Détecter les collisions pour le temps restant
            List<CollisionInfo> collisions = collisionDetector.DetectCollisions(
                collisionShape,
                physics.Velocity,
                subDeltaTime
            );

            // Pas de collision - déplacer la balle pour le temps restant et sortir
            if (collisions.Count == 0)
            {
                transform.position += physics.Velocity * subDeltaTime;
                remainingTime = 0f;  // Marquer comme terminé
                break;
            }

            // Prendre la première collision (la plus proche grâce au tri par TOI)
            CollisionInfo collision = collisions[0];
            totalCollisionsThisFrame++;

            // Si TOI > 0, avancer jusqu'au point de collision
            if (collision.timeOfImpact > PhysicsConstants.CCD_TOI_EPSILON)
            {
                float moveTime = collision.timeOfImpact * subDeltaTime;
                transform.position += physics.Velocity * moveTime;
            }

            // Gérer la collision
            if (!enableCollisionResponse)
            {
                remainingTime = 0f;
                break;
            }

            if (collision.otherShape.IsTrigger)
            {
                HandleTrigger(collision);
                // Pour les triggers, on continue sans changer la vélocité
                remainingTime -= collision.timeOfImpact * remainingTime;
                continue;
            }

            // Collision physique
            float speed = physics.Speed;

            // Correction de pénétration - toujours pousser légèrement hors du mur
            float pushOut = Mathf.Max(collision.penetrationDepth, 0.01f);
            transform.position += collision.normal * pushOut;

            // Vitesse trop faible - arrêter
            if (speed < minBounceVelocity)
            {
                physics.Stop();
                physics.SetGravityEnabled(false);
                remainingTime = 0f;
                break;
            }

            // Rebond
            physics.Reflect(collision.normal);
            totalBouncesThisShot++;

            // Temps restant après cette collision
            remainingTime -= collision.timeOfImpact * remainingTime;

            // Petit déplacement après rebond pour éviter re-collision immédiate
            if (remainingTime > PhysicsConstants.CCD_MIN_REMAINING_TIME)
            {
                float smallStep = Mathf.Min(0.01f, remainingTime * Time.fixedDeltaTime);
                transform.position += physics.Velocity.normalized * smallStep;
            }
        }

    }

    private void HandleCollision(CollisionInfo collision)
    {
        // si on a setup un trigger, on gère que l'événement trigger
        if (collision.otherShape.IsTrigger) {
            HandleTrigger(collision);
            return;
        }
        
        HandlePhysicalCollision(collision);
    }

    private void HandlePhysicalCollision(CollisionInfo collision)
    {
        float speed = physics.Speed;

        transform.position += collision.normal * collision.penetrationDepth;

        if (speed < minBounceVelocity) {
            physics.Stop();
            physics.SetGravityEnabled(false);

            if (showDebugInfo)
                Debug.Log("[BallController] Balle arrêtée (vitesse trop faible)");
            return;
        }

        physics.Reflect(collision.normal);

        totalBouncesThisShot++;
        if (showDebugInfo) {
            Debug.DrawRay(collision.point, collision.normal * 2f, Color.cyan, 0.5f);
            Debug.Log($"[BallController] Rebond #{totalBouncesThisShot} - Vitesse: {speed:F2} m/s");
        }
    }

    private void HandleMultipleCollisions(List<CollisionInfo> collisions)
    {
        List<CollisionInfo> physicalCollisions = new List<CollisionInfo>();

        foreach (CollisionInfo collision in collisions) {
            if (collision.otherShape.IsTrigger)
                HandleTrigger(collision);
            else
                physicalCollisions.Add(collision);
        }

        if (physicalCollisions.Count == 0) return;

        Vector3 averageNormal = Vector3.zero;
        float maxDepth = 0f;
        foreach (CollisionInfo collision in physicalCollisions) {
            averageNormal += collision.normal;
            if (collision.penetrationDepth > maxDepth)
                maxDepth = collision.penetrationDepth;
        }

        averageNormal.Normalize();

        float speed = physics.Speed;
        if (speed < minBounceVelocity) {
            physics.Stop();
            physics.SetGravityEnabled(false);
            if (showDebugInfo)
                Debug.Log("[BallController] Balle arrêtée (vitesse trop faible)");
            return;
        }

        physics.Reflect(averageNormal);

        transform.position += averageNormal * maxDepth;

        totalBouncesThisShot++;
        if (showDebugInfo) {
            Debug.DrawRay(transform.position, averageNormal * 2f, Color.magenta, 0.5f);
            Debug.Log($"[BallController] Collision multiple ({physicalCollisions.Count}) - Vitesse: {speed:F2} m/s");
        }
    }

    private void HandleTrigger(CollisionInfo collision)
    {
        GameObject triggerObject = collision.otherShape.gameObject;
        
        if (triggerObject.CompareTag("Hole"))
            OnBallInHole();
        else if (triggerObject.CompareTag("Water"))
            OnBallInWater();
        else if (triggerObject.CompareTag("Boost"))
            OnBallInBoost(collision.normal);
        
        if (showDebugInfo)
            Debug.Log($"[BallController] Trigger: {triggerObject.name}");
    }

    private void OnBallInHole()
    {
        hasWon = true;
        physics.Stop();
        
        Debug.Log("🏌️ VICTOIRE ! Balle dans le trou !");
        Debug.Log($"Total de rebonds: {totalBouncesThisShot}");
        
        // TODO HUD...
    }

    private void OnBallInWater()
    {
        // on window macOS emoji in debug console ?
        Debug.Log("💧 Balle dans l'eau ! Reset position");
        ResetBall();
    }

    private void OnBallInBoost(Vector3 direction)
    {
        // on window macOS emoji in debug console ?
        Debug.Log("⚡️ Zone boost !");
        physics.AddImpulse(direction * 5f);
    }

    public void ResetBall()
    {
        transform.position = startPosition;
        physics.Stop();
        physics.SetGravityEnabled(true);
        hasWon = false;
        totalBouncesThisShot = 0;
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame) {
            Vector3 hitDirection = transform.forward;
            float hitPower = 6f;

            physics.SetGravityEnabled(true);
            physics.AddImpulse(hitDirection * hitPower);
        }

        if (Keyboard.current.rKey.wasPressedThisFrame) {
            ResetBall();
            Debug.Log("[BallController] Reset manuel");
        }

        if (Keyboard.current.dKey.wasPressedThisFrame) {
            showDebugInfo = !showDebugInfo;
            Debug.Log($"[BallController] Debug: {showDebugInfo}");
        }
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label($"Vitesse: {physics.Speed:F2} m/s");
        GUILayout.Label($"En mouvement: {physics.IsMoving}");
        GUILayout.Label($"Collisions ce frame: {totalCollisionsThisFrame}");
        GUILayout.Label($"Rebonds total: {totalBouncesThisShot}");
        GUILayout.Label($"Victoire: {hasWon}");
        GUILayout.Label("");
        GUILayout.Label("ESPACE = Frapper");
        GUILayout.Label("R = Reset");
        GUILayout.Label("D = Toggle Debug");
        GUILayout.EndArea();
    }
}
