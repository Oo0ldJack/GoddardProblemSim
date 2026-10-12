using System;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.OdeSolvers;

namespace Goddard.Core
{
    /// <summary>
    /// Интегрирует ОДУ задачи Годдарда методом Рунге–Кутты 4-го порядка
    /// (Math.NET, фиксированный шаг).
    /// </summary>
    public static class Integrator
    {
        /// <summary>
        /// Интегрирует систему (h, v, m) на отрезке [0, <paramref name="tEnd"/>]
        /// при заданном законе управления.
        /// </summary>
        /// <param name="y0">Начальное состояние (h, v, m), безразмерные единицы COPS.</param>
        /// <param name="tEnd">Конец интервала; интегрирование всегда от t = 0.</param>
        /// <param name="steps">
        /// Число узлов выходной сетки. По документации Math.NET это размер
        /// выходного массива («the larger, the finer»);
        /// шаг <c>dt = tEnd / (steps - 1)</c>.
        /// </param>
        /// <param name="uOfT">
        /// Закон управления: расход массы u(t), 0 &lt;= u &lt;= <c>p.UMax</c>.
        /// </param>
        /// <param name="p">Параметры постановки.</param>
        /// <returns>
        /// Кортеж из двух массивов равной длины:
        /// <c>T</c> — равномерная сетка времён (<c>T[i] = i·dt</c>);
        /// <c>Y</c> — состояния в узлах, <c>Y[i] = (h, v, m)</c> в момент <c>T[i]</c>.
        /// </returns>
        public static (double[] T, double[][] Y) Solve(
            double[] y0, double tEnd, int steps,
            Func<double, double> uOfT, SimParams p)
        {
            // Конвертации double[] <-> Vector<double> неизбежны и сделаны намеренно:
            // Dynamics.F работает на «чистых» double[] — ядро модели не зависит
            // от типов Math.NET в сигнатуре; решатель же требует Vector<double>.
            // Адаптер между мирами — локальная функция F.
            Vector<double> F(double t, Vector<double> y) =>
                Vector<double>.Build.DenseOfArray(
                    Dynamics.F(t, y.ToArray(), uOfT(t), p));

            double dt = tEnd / (steps - 1.0);
            var T = new double[steps];
            var Y = new double[steps][];

            double[] y = (double[])y0.Clone();
            T[0] = 0.0; Y[0] = (double[])y0.Clone();

            // Один шаг за вызов, N = 2 → решатель вернёт [y_start, y_end].
            for (int i = 1; i < steps; i++)
            {
                var sol = RungeKutta.FourthOrder(
                    Vector<double>.Build.DenseOfArray(y),
                    (i - 1) * dt, i * dt, 2, F);
                double[] next = sol[1].ToArray();

                next[2] = Math.Max(next[2], p.Mf);  // проекция на допустимое множество

                T[i] = i * dt;
                Y[i] = next;
                y = next;
            }
            return (T, Y);
        }
    }
}