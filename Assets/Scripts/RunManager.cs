using UnityEngine;

namespace Goddard.Stand
{
    /// <summary>
    /// Запускает и завершает прогон: границы жизни телеметрии.
    /// </summary>
    public class RunManager : MonoBehaviour
    {
        public RocketVariableMass rocket;
        public FlightDataLogger logger;
        public PidController pid;
        public string controllerType;
        private float prevH;
        private bool apogeeSeen;

        void Start()
        {
            
            var src = rocket.thrustSource;
            var player = src as ThrustProfilePlayer;   // Awake всех компонентов уже отработал
            controllerType = src switch
            {
                PidController => "pid",
                FullThrustController => "fullthrust",
                ThrustProfilePlayer => "optimal",
                _ => src != null ? src.GetType().Name : "none",
            };

            var P = Goddard.Core.Params.P;

            logger.Begin(new FlightDataLogger.RunMeta
            {
                runId          = System.DateTime.Now.ToString("yyyyMMdd_HHmmss"),
                controllerType = controllerType,
                profileName = player != null ? player.ProfileLabel : "",
                profilePoints = player != null && player.Points != null ? player.Points.Length : 0,
                kp = pid != null ? pid.kp : 0f,
                ki = pid != null ? pid.ki : 0f,
                kd = pid != null ? pid.kd : 0f,
                targetSpeed = pid != null ? pid.targetSpeed : 0f,
                rampTime    = pid != null ? pid.rampTime    : 0f,

                h0 = P.H0, g0 = P.G0, hc = P.Hc,
                dc = P.Dc, cExhaust = P.C,
                m0 = P.M0, mf = P.Mf,
                thrustMax = P.UMax,

                modelHeightToMeters = Goddard.Core.SceneUnits.ModelHeightToMeters,
                secondsPerModelTime = Goddard.Core.SceneUnits.SecondsPerModelTime,
                fixedDtWall  = Time.fixedDeltaTime,
                fixedDtModel = Time.fixedDeltaTime / Goddard.Core.SceneUnits.SecondsPerModelTime,
            });
        }

        void Update()
        {
            float h = rocket.transform.position.y;
            if (apogeeSeen || (prevH > 0f && h < prevH - 1e-3f)) apogeeSeen = true;
            prevH = h;

            if (apogeeSeen || Input.GetKeyDown(KeyCode.S))
            {
                logger.Flush();
                enabled = false;
                Debug.Log("[RunManager] run finished, telemetry flushed");
            }
        }
    }
}