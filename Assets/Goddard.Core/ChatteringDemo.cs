using System;

namespace Goddard.Core
{
    /// <summary>
    /// Наивная прямая оптимизация профиля тяги: свободный поиск по значениям
    /// сегментов без какой-либо структуры решения («акт 1» занятия 3).
    /// Оптимизатор находит околооптимальное h_max, но профиль расползается
    /// в зигзаг: целевая на особой дуге чувствительна к среднему управлению,
    /// а не к его форме (слабая сходимость). Нейтральные ходы, почти не меняющие
    /// h_max, принимаются — как в прямых методах с недоопределённым направлением.
    /// </summary>
    public static class ChatteringDemo
    {
        /// <summary>
        /// Оценка закона: интегрируем траекторию и берём h_max.
        /// Остановка на апогее (v &lt; 0) — та же практика, что и в целевой ParameterOptimizer;
        /// требует перегрузки Integrator.Solve с предикатом stopAfter.
        /// </summary>
        public static double Evaluate(PiecewiseConstantLaw law, int steps, double tEnd, SimParams p)
        {
            var (T, Y) = Integrator.Solve(
                new[] { p.H0, 0.0, p.M0 }, tEnd, steps, law.GetU, p,
                stopAfter: (t, y) => t > 0.01 && y[1] < 0.0);
            double hmax = double.MinValue;
            foreach (var y in Y) hmax = Math.Max(hmax, y[0]);
            return hmax;
        }

        /// <summary>
        /// Детерминированный наивный поиск: покоординатные проходы по узлам,
        /// кандидаты — положения рукоятки {0, 1/4, 1/2, 3/4, 1}·UMax.
        /// Стоп — когда проход не дал улучшения. Дуга синтезируется ШИМом между
        /// положениями: зигзаг — следствие квантования тяги, а не генератора случайности.
        /// </summary>
        public static double CoordinateDescent(
            PiecewiseConstantLaw law, int maxSweeps, double tol,
            int steps, double tEnd, SimParams p)
        {
            double[] detents = { 0.0, 0.25 * p.UMax, 0.5 * p.UMax, 0.75 * p.UMax, p.UMax };
            double best = Evaluate(law, steps, tEnd, p);
            for (int sweep = 0; sweep < maxSweeps; sweep++)
            {
                bool improved = false;
                for (int k = 0; k < law.U.Length; k++)
                {
                    double old = law.U[k];
                    foreach (double cand in detents)
                    {
                        law.U[k] = cand;
                        double h = Evaluate(law, steps, tEnd, p);
                        if (h > best + tol) { best = h; old = cand; improved = true; }
                    }
                    law.U[k] = old;
                }
                if (!improved) break;
            }
            return best;
        }
        
        /// <summary>
        /// Счётчик зигзагов: сколько раз приращение профиля меняет знак (с мёртвой зоной).
        /// </summary>
        public static int CountZigzags(double[] u, double minDelta = 1e-3)
        {
            int count = 0, prevSign = 0;
            for (int k = 1; k < u.Length; k++)
            {
                double d = u[k] - u[k - 1];
                int sign = d > minDelta ? 1 : (d < -minDelta ? -1 : 0);
                if (sign != 0 && prevSign != 0 && sign != prevSign) count++;
                if (sign != 0) prevSign = sign;
            }
            return count;
        }
    }
}