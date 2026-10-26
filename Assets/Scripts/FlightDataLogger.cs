using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Goddard.Stand
{
    /// <summary>
    /// Стенд не рисует графики — стенд пишет телеметрию.
    /// Буфер в памяти → запись на диск по Flush() (диск в FixedUpdate исказил бы
    /// сам эксперимент). Пара неизменяемых файлов на запуск: CSV + meta.json.
    /// </summary>
    public class FlightDataLogger : MonoBehaviour
    {
        [Serializable]
        public class RunMeta
        {
            public string runId;
            public string controllerType;   // pid | fullthrust | optimal (способ задания управления)
            public string profileName;      // какой именно профиль: "naive-pwm" | "greedy-junction" | "pmp-clean"
            public int profilePoints;       // узлов в профиле (санити-чек при разборе логов)

            // --- настройки контроллера (pid: заполнено; прочее: 0) ---
            public float kp, ki, kd, targetSpeed, rampTime;

            // --- постановка COPS (модельные безразмерные параметры) ---
            public double h0, g0, hc;          // 1, 1, 500
            public double dc;                  // 310
            public double cExhaust;            // 0.5 — скорость истечения
            public double m0, mf;              // 1.0, 0.6
            public double thrustMax;           // 3.5 = Tc·m0·g0 — управление u есть ТЯГА

            // --- шкалы стенда: без них столбцы CSV не воспроизводить ---
            public float modelHeightToMeters;  // H = 100
            public float secondsPerModelTime;  // T = 10
            public float fixedDtWall;          // Time.fixedDeltaTime (стендовый шаг)
            public double fixedDtModel;        // = fixedDtWall / T

            // --- эталоны для сверки на месте ---
            public double copsOptimalApogee = 1.01284;      // литература по COPS
            public double fullThrustApogee  = 1.012516;     // расчет RK4, full thrust
        }

        private readonly List<string> buffer = new List<string>();
        private RunMeta meta;

        public void Begin(RunMeta m)
        {
            meta = m;
            meta.fixedDtWall = Time.fixedDeltaTime;
            buffer.Clear();
            buffer.Add("t,h,v,m,thrust,drag,u_cmd");
        }

        public void Sample(float t, float h, float v, float m,
            float thrust, float drag, float uCmd)
        {
            var inv = CultureInfo.InvariantCulture; // иначе в русской локали — запятые
            buffer.Add(string.Join(",", new[] { t, h, v, m, thrust, drag, uCmd }
                .Select(x => x.ToString(inv))));
        }

        public void Flush()
        {
            string dir = Path.Combine(Application.dataPath, "runs");
            Directory.CreateDirectory(dir);
            File.WriteAllLines(Path.Combine(dir, meta.runId + ".csv"), buffer);
            File.WriteAllText(Path.Combine(dir, meta.runId + ".json"),
                JsonUtility.ToJson(meta, prettyPrint: true));
            Debug.Log($"[FlightDataLogger] run {meta.runId}: {buffer.Count - 1} samples → {dir}");
        }
    }
}