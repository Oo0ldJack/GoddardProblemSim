namespace Goddard.Stand
{
    /// <summary>
    /// Источник управления u(t). Занятие 2: PidController, FullThrustController.
    /// Занятие 3: ThrustProfilePlayer (проигрывает оптимальный профиль) —
    /// ракета при этом не меняется вообще.
    /// </summary>
    public interface IThrustSource
    {
        float GetU(float t, float vModel);
    }
}