public static class PhysicsConstants
{
    // Seuil pour arrêter un objet en mouvement
    public const float VELOCITY_STOP_THRESHOLD = 0.01f;

    // Epsilon pour les comparaisons de distance
    public const float DISTANCE_EPSILON = 0.0001f;

    // Epsilon pour les calculs de normales
    public const float NORMAL_EPSILON = 0.0001f;

    // Seuil pour la détection prédictive
    public const float PREDICTIVE_VELOCITY_THRESHOLD_SQ = 0.0001f;

    // CCD (Continuous Collision Detection)
    public const float CCD_TOI_EPSILON = 0.0001f;           // Seuil TOI minimum
    public const int CCD_MAX_ITERATIONS = 16;               // Max sous-cycles par frame (augmenté pour haute vélocité)
    public const float CCD_MIN_REMAINING_TIME = 0.001f;     // Temps minimum restant pour continuer
}
