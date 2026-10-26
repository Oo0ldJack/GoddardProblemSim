using System;
using System.Collections.Generic;

namespace Goddard.Core
{
    /// <summary>
    /// Офлайн-поиск (s1, s2) — «пробный полёт» как чистая функция, без MonoBehaviour.
    /// Интегрирование — только через Integrator.Solve (MathNet): единая точка,
    /// где держится массовый инвариант (m ≥ p.Mf).
    /// </summary>
    public static class ParameterOptimizer
    {
        public readonly struct Result
        {
            public readonly double S1, S2, HMax;
            public readonly string ExitReason;

            public Result(double s1, double s2, double hMax, string exitReason)
            {
                S1 = s1; S2 = s2; HMax = hMax; ExitReason = exitReason;
            }
        }

        /// <summary>
        /// Результат сканирования сетки: сами сетки, карта h_max и лучшая точка.
        /// </summary>
        public readonly struct GridScan
        {
            public readonly double[] S1Grid;
            public readonly double[] S2Grid;
            public readonly double[,] Landscape;
            public readonly double S1, S2, HMax;

            public GridScan(double[] s1Grid, double[] s2Grid, double[,] landscape)
            {
                S1Grid = s1Grid;
                S2Grid = s2Grid;
                Landscape = landscape;

                double best = double.MinValue;
                S1 = 0.0; S2 = 0.0; HMax = 0.0;
                for (int i = 0; i < s1Grid.Length; i++)
                for (int j = 0; j < s2Grid.Length; j++)
                {
                    if (landscape[i, j] > best)
                    {
                        best = landscape[i, j];
                        S1 = s1Grid[i]; S2 = s2Grid[j]; HMax = best;
                    }
                }
            }
        }
        
        /// <summary>
        /// Решение без указания области: широкие границы из t_b = (M0−Mf)·c/UMax.
        /// </summary>
        public static Result Solve(int steps = 1001, double tEnd = 0.35, SimParams p = null)
        {
            p ??= Params.P;
            double tB = (p.M0 - p.Mf) * p.C / p.UMax;
            return Solve(0.05 * tB, 0.95 * tB, 16, 0.2 * tB, 2.0 * tB, 16, steps, tEnd, p);
        }

        /// <summary>
        /// Двухуровневый поиск в заданной области: coarse по сетке области,
        /// затем fine 11×11 в окне ±1 ячейка вокруг лучшей точки.
        /// </summary>
        public static Result Solve(
            double s1Lo, double s1Hi, int nS1, double s2Lo, double s2Hi, int nS2,
            int steps = 1001, double tEnd = 0.35, SimParams p = null)
        {
            p ??= Params.P;
            var coarse = ScanGrid(s1Lo, s1Hi, nS1, s2Lo, s2Hi, nS2, steps, tEnd, p);
            double ds1 = nS1 > 1 ? (s1Hi - s1Lo) / (nS1 - 1) : 0.0;
            double ds2 = nS2 > 1 ? (s2Hi - s2Lo) / (nS2 - 1) : 0.0;

            double fLo1 = Math.Max(coarse.S1 - ds1, 0.0), fHi1 = coarse.S1 + ds1;
            double fLo2 = Math.Max(coarse.S2 - ds2, 0.0), fHi2 = coarse.S2 + ds2;
            var fine = ScanGrid(fLo1, fHi1, 11, fLo2, fHi2, 11, steps, tEnd, p);

            bool onEdge = fine.S1 <= fLo1 + 1e-12 || fine.S1 >= fHi1 - 1e-12
                                                  || fine.S2 <= fLo2 + 1e-12 || fine.S2 >= fHi2 - 1e-12;
            string exit = onEdge
                ? "предупреждение: best на краю окна — сместите или расширьте область"
                : "coarse+fine grid";
            return new Result(fine.S1, fine.S2, fine.HMax, exit);
        }

        /// <summary>
        /// Полное сканирование сетки. Вырожденный треугольник s1 ≥ s2: структура
        /// bang → дуга → bang вырождается в тривиальную (нет фазы дуги) — это режим
        /// FullThrust, и он считается один раз, а не ~n²/2 раз.
        /// </summary>
        public static GridScan ScanGrid(
            double s1Lo, double s1Hi, int nS1,
            double s2Lo, double s2Hi, int nS2,
            int steps = 1001, double tEnd = 0.35, SimParams p = null)
        {
            p ??= Params.P;
            var s1Grid = Linspace(s1Lo, s1Hi, nS1);
            var s2Grid = Linspace(s2Lo, s2Hi, nS2);
            var landscape = new double[nS1, nS2];
            double hFullThrust = EvaluateFullThrust(steps, tEnd, p);

            for (int i = 0; i < nS1; i++)
            for (int j = 0; j < nS2; j++)
                landscape[i, j] = s1Grid[i] < s2Grid[j]
                    ? Evaluate(s1Grid[i], s2Grid[j], steps, tEnd, p)
                    : hFullThrust;
            return new GridScan(s1Grid, s2Grid, landscape);
        }

        /// <summary>
        /// Пробный полёт с законом (s1, s2). Закон неизменяемый (S1/S2 — только геттеры),
        /// поэтому на каждую точку сетки — новый экземпляр.
        /// Предикат остановки тот же, что в ChatteringDemo.Evaluate: обе целевые
        /// (структурная и наивная) обязаны совпадать по определению.
        /// </summary>
        private static double Evaluate(double s1, double s2, int steps, double tEnd, SimParams p)
        {
            var law = new BangSingularBangLaw(s1, s2, p);
            var (T, Y) = Integrator.Solve(
                new[] { p.H0, 0.0, p.M0 }, tEnd, steps, law.U, p,
                stopAfter: (t, y) => t > 0.01 && y[1] < 0.0);
            double hmax = double.MinValue;
            foreach (var y in Y) hmax = Math.Max(hmax, y[0]);
            return hmax;
        }
        
        /// <summary>
        /// Тривиальная структура (ни одного переключения): полный форсаж до выгорания.
        /// </summary>
        private static double EvaluateFullThrust(int steps, double tEnd, SimParams p)
        {
            var (T, Y) = Integrator.Solve(
                new[] { p.H0, 0.0, p.M0 }, tEnd, steps, t => p.UMax, p,
                stopAfter: (t, y) => t > 0.01 && y[1] < 0.0);
            double hmax = double.MinValue;
            foreach (var y in Y) hmax = Math.Max(hmax, y[0]);
            return hmax;
        }

        /// <summary>
        /// Профиль (t, u) вдоль траектории общего Integrator. Своего интегрирования здесь нет:
        /// массовый инвариант держат Integrator (клэмп p.Mf) и Dynamics.F (u = 0 при m ≤ Mf).
        /// «Сжечь больше топлива, чем есть» здесь невозможно сильнее, чем в самой целевой
        /// функции, — профиль по построению согласован с оценкой (s1, s2).
        /// </summary>
        public static List<(double t, double u)> SampleProfile(
            double s1, double s2, int samples, double tEnd, SimParams p = null)
        {
            p ??= Params.P;
            var law = new BangSingularBangLaw(s1, s2, p);
            var (T, Y) = Integrator.Solve(
                new[] { p.H0, 0.0, p.M0 }, tEnd, samples, law.U, p,
                stopAfter: (t, y) => t > 0.01 && y[1] < 0.0);
            var pts = new List<(double, double)>(T.Length);
            for (int i = 0; i < T.Length; i++)
                pts.Add((T[i], law.U(T[i], Y[i])));
            return pts;
        }

        private static double[] Linspace(double a, double b, int n)
        {
            var r = new double[n];
            for (int i = 0; i < n; i++) r[i] = a + (b - a) * i / (n - 1);
            return r;
        }
    }
}