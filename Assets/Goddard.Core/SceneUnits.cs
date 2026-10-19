namespace Goddard.Core
{
    /// <summary>
    /// Единственное место, где безразмерная модель COPS встречается с метрами
    /// сцены. Масштаб высоты — единственный; время сознательно 1:1
    /// (зафиксированный проектный компромисс: события модели совпадают
    /// с игровым временем).
    /// </summary>
    public static class SceneUnits
    {
        public const float ModelHeightToMeters = 1000f;     // H: 1 ед. высоты = 100 м
        public const float SecondsPerModelTime = 30f;       // T: замедление для наблюдения
        public const float ModelSurfaceH = 1f;              // y=0 сцены = h=H0 модели


        public static double ToModelTime(float tWall)     => tWall / SecondsPerModelTime;
        public static double ToModelHeight(float y)       => ModelSurfaceH + y / ModelHeightToMeters;
        public static double ToModelVelocity(float vy)    => vy * SecondsPerModelTime / ModelHeightToMeters;
        // a_unity = a_model * H / T^2; масса передаётся 1:1 → то же для сил
        public static float  ToUnityForce(double fModel)  => (float)(fModel * ModelHeightToMeters
                                                                     / (SecondsPerModelTime * SecondsPerModelTime));
    }
}