using UnityEngine;
using Goddard.Core;

namespace Goddard.Stand
{
    public class PidController : MonoBehaviour, IThrustSource
    {
        [Header("Целевой профиль скорости v*(t) — модельные ед.")]
        public float targetSpeed = 0.1f;
        public float rampTime = 0.01f;          // плавный выход на цель

        [Header("Коэффициенты")]
        public float kp = 20f;
        public float ki = 5f;
        public float kd = 0.005f;

        private float integral;
        private float prevErr;
        private bool initialized;

        public float GetU(float t, float vModel)
        {
            // dt в МОДЕЛЬНОМ времени — как и err
            float dt = Time.fixedDeltaTime / SceneUnits.SecondsPerModelTime;
            float vStar = targetSpeed * Mathf.Clamp01(t / rampTime);
            float err = vStar - vModel;

            if (!initialized) { prevErr = err; initialized = true; }
    
            integral += err * dt;
            // анти-windup: интегралу нельзя уходить дальше, чем способен отработать u
            float iLim = Params_UMax() / Mathf.Max(ki, 1e-6f);
            integral = Mathf.Clamp(integral, -iLim, iLim);

            float deriv = (err - prevErr) / dt;
            prevErr = err;

            float u = kp * err + ki * integral + kd * deriv;
            return Mathf.Clamp(u, 0f, Params_UMax());
        }

        private float Params_UMax() => (float)Goddard.Core.Params.P.UMax;
    }
}