using Goddard.Core;
using UnityEngine;

namespace Goddard.Stand
{
    /// <summary>
    /// Порядок в FixedUpdate — это порядок действий в уравнениях:
    /// расход за шаг → обновление массы → приложение силы.
    /// Гравитация движка выключена (useGravity = false): применяем свою,
    /// согласованную с моделью через SceneUnits.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class RocketVariableMass : MonoBehaviour
    {
        [Header("Управление")]
        public MonoBehaviour thrustSource;   // IThrustSource (PID на з.2)
        [Header("Телеметрия")]
        public FlightDataLogger logger;
        public GoddardDrag dragProbe;        // для лога величины drag

        private Rigidbody rb;
        private float fuel;                  // модельные ед. массы
        private IThrustSource thrust;
        private float gUnity;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            fuel = (float)Params.P.M0 - (float)Params.P.Mf;
            rb.mass = (float)Params.P.M0;
            thrust = (IThrustSource)thrustSource;
            gUnity = (float)(Params.P.G0 * SceneUnits.ModelHeightToMeters
                             / (SceneUnits.SecondsPerModelTime * SceneUnits.SecondsPerModelTime)); // FIX: /T^2
        }

        void FixedUpdate()
        {
            // 1. расход: dm = -(u/c)·dt; нельзя сжечь больше, чем есть
            double dtModel = Time.fixedDeltaTime / SceneUnits.SecondsPerModelTime;
            double vModel = SceneUnits.ToModelVelocity(rb.linearVelocity.y);
            double hModel = SceneUnits.ToModelHeight(transform.position.y);

            float  u = thrust.GetU((float)SceneUnits.ToModelTime(Time.fixedTime), (float)vModel);

            // 1. расход: нельзя сжечь больше, чем есть
            float  burn    = Mathf.Min(u / (float)Params.P.C * (float)dtModel, fuel);
            fuel -= burn;
            
            float uActual = dtModel > 0 ? burn / (float)dtModel * (float)Params.P.C : 0f; // вернуть в единицы ТЯГИ

            // 2. масса — ДО применения силы (AddForce делит на неё)
            rb.mass = (float)Params.P.Mf + fuel;

            // 3. силы: тяга (модельная) + гравитация
            double thrustModel = uActual;
            rb.AddForce(Vector3.up * SceneUnits.ToUnityForce(thrustModel));             // тяга = u, без *C

            rb.AddForce(Vector3.down * (gUnity * rb.mass));

            // 4. телеметрия — в конце шага, значения этого шага
            logger?.Sample(
                (float)SceneUnits.ToModelTime(Time.fixedTime), 
                (float)hModel, (float)vModel,
                rb.mass, 
                (float)thrustModel, 
                dragProbe != null ? dragProbe.LastDragModel : 0f, 
                uActual);
        }
    }
}