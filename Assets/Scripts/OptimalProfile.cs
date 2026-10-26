using System;

namespace Goddard.Stand
{
    /// <summary>
    /// DTO профиля тяги: общий формат для экспорта (Editor) и проигрывания (Player).
    /// </summary>
    [Serializable] public class ThrustProfilePoint { public float t; public float u; }
    [Serializable]
    public class ThrustProfile
    {
        /// <summary>"pmp-clean" | "naive-pwm" | "greedy-junction"; в старых JSON отсутствует (null).</summary>
        public string label;
        public ThrustProfilePoint[] points;
    }
}