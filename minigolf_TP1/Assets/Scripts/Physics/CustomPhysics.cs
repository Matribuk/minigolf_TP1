using UnityEngine;

public class CustomPhysics : MonoBehaviour
{
    [Header("Physics Properties")]
    [SerializeField] private float mass = 0.04593f;                 // Masse balle de golf (kg)
    [SerializeField] private float gravity = 9.81f;                 // Gravité (m/s²)
    [SerializeField] private float linearDrag = 0.5f;               // Coefficient de friction linéaire (1/s)
    [SerializeField] private float restitution = 0.7f;              // Coefficient de rebond (0-1)

    [Header("Velocity Thresholds")]
    [SerializeField] private float stopThreshold = PhysicsConstants.VELOCITY_STOP_THRESHOLD;           // Seuil d'arrêt (m/s)
    [SerializeField] private bool useGravity = true;                // Activer la gravité

    [Header("Integration")]
    [SerializeField] private bool integratePosition = false;        // Désactivé: BallController gère le mouvement avec CCD

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    // Propriétés privées
    private Vector3 velocity = Vector3.zero;                        // Vélocité (m/s)
    private Vector3 acceleration = Vector3.zero;                    // Accélération (m/s²)
    private Vector3 forceAccumulator = Vector3.zero;                // Accumulateur de forces (N)
    
    // Propriétés publiques
    public Vector3 Velocity => velocity;                            // Vélocité (m/s)
    public Vector3 Acceleration => acceleration;                    // Accélération (m/s²)
    public float Speed => velocity.magnitude;                       // Vitesse (m/s)
    public bool IsMoving => Speed > stopThreshold;                  // En mouvement
    public float Mass => mass;                                      // Masse (kg)
    public float Restitution => restitution;                        // Coefficient de rebond (0-1)

    void FixedUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;

        CalculateAcceleration();
        IntegrateVelocity(deltaTime);
        ApplyDrag(deltaTime);

        // Position intégrée par BallController avec CCD si integratePosition est false
        if (integratePosition)
            IntegratePosition(deltaTime);

        forceAccumulator = Vector3.zero;

        if (showDebugInfo && IsMoving) {
            Debug.DrawRay(transform.position, velocity, Color.green);
            Debug.DrawRay(transform.position, acceleration, Color.red);
        }
    }

    // Permet de corriger la position depuis l'extérieur (par ex. après détection de collision)
    public void CorrectPosition(Vector3 correction)
    {
        transform.position += correction;
    }

    // L'accélération d'un objet est proportionnelle à la force nette appliquée et inversement proportionnelle à sa masse.
    private void CalculateAcceleration()
    {
        // Fg = mg
        if (useGravity)
            forceAccumulator += Vector3.down * gravity * mass;
        // a = ΣF / m
        acceleration = forceAccumulator / mass;
    }

    // Euler semi-implicite est plus stable que Euler explicite pour les simulations de jeu, peu couteux en calcul.
    // https://en.wikipedia.org/wiki/Semi-implicit_Euler_method
    private void IntegrateVelocity(float deltaTime)
    {
        // v(t+Δt) = v(t) + a(t) × Δt
        velocity += acceleration * deltaTime;
    }

    // Applique la friction (drag) qui ralentit l'objet selon deltaTime.
    private void ApplyDrag(float deltaTime)
    {
        // acc_drag = -c × v
        Vector3 dragAcceleration = -linearDrag * velocity;

        // v(t+Δt) = v(t) + acc_drag × Δt
        velocity += dragAcceleration * deltaTime;

        if (velocity.magnitude < stopThreshold)
            velocity = Vector3.zero;
    }

    // Intégration de position
    private void IntegratePosition(float deltaTime)
    {
        // p(t+Δt) = p(t) + v(t) × Δt
        transform.position += velocity * deltaTime;
    }

    public void AddForce(Vector3 force)
    {
        forceAccumulator += force;
    }

    public void AddImpulse(Vector3 impulse)
    {
        // Δv = Impulse / masse
        velocity += impulse / mass;
    }

    public void SetVelocity(Vector3 newVelocity)
    {
        velocity = newVelocity;
    }

    // Inverse la composante de vélocité selon une normale (pour les rebonds)
    // v' = v - 2(v·n)n (réflexion parfaite)
    // v'final = v' × e  (avec perte d'énergie)
    //   n est la normale de la surface
    //   e est le coefficient de restitution (0 = inélastique, 0.7-0.8 = balle de golf, 1 = parfaitement élastique)
    public void Reflect(Vector3 normal, float restitutionOverride = -1f)
    {
        float rest = restitution;
        if (restitutionOverride >= 0)
            rest = restitutionOverride;
        
        // v'final = (v - 2(v·n)n) × e
        velocity = Vector3.Reflect(velocity, normal) * rest;
    }

    public void Stop()
    {
        velocity = Vector3.zero;
        acceleration = Vector3.zero;
        forceAccumulator = Vector3.zero;
    }

    public void SetGravityEnabled(bool enabled)
    {
        useGravity = enabled;
    }

    void OnDrawGizmos()
    {
        if (!showDebugInfo) return;
        
        // vert vélocité
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, velocity);
        
        // rouge accélération
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, acceleration);
    }
}
