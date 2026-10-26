using System;
using UnityEngine;

namespace Goddard.Stand
{
    /// <summary>
    /// Проигрывает оптимальный профиль тяги из JSON (экспорт из окна оптимизации).
    /// Ракета и стенд не меняются: это ещё один IThrustSource — см. тезис занятия 2.
    /// </summary>
    public class ThrustProfilePlayer : MonoBehaviour, IThrustSource
    {
        public TextAsset profileJson;

        /// <summary>
        /// Метка из JSON; если её нет (старый файл) — имя ассета.
        /// </summary>
        public string ProfileLabel { get; private set; }

        private ThrustProfilePoint[] pts;
        public ThrustProfilePoint[] Points => pts;

        void Awake()
        {
            var profile = JsonUtility.FromJson<ThrustProfile>(profileJson.text);
            pts = profile.points;
            System.Array.Sort(pts, (a, b) => a.t.CompareTo(b.t));
            ProfileLabel = string.IsNullOrEmpty(profile.label) ? profileJson.name : profile.label;
        }

        public float GetU(float t, float vModel)
        {
            if (pts == null || pts.Length == 0 || t < pts[0].t) return 0f;
            if (t >= pts[pts.Length - 1].t) return 0f;

            // линейный поиск достаточен (сетка равномерная); при желании — двоичный
            int i = 0;
            while (i + 1 < pts.Length && pts[i + 1].t <= t) i++;

            var a = pts[i]; 
            var b = pts[Math.Min(i + 1, pts.Length - 1)];
            float span = Mathf.Max(b.t - a.t, 1e-9f);
            return Mathf.Lerp(a.u, b.u, (t - a.t) / span);
        }
    }
}