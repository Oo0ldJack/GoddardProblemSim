using System;
using System.Collections.Generic;

namespace Goddard.Core
{
    /// <summary>
    /// «Наивный стык»: структура (форсаж/дуга) известна из теории,
    /// момент переключения — нет. На каждом узле контроллер сравнивает прогноз
    /// апогея для двух кандидатов и выбирает лучшего. Детерминированно.
    /// Результат: чистый вход на дугу, пакет переключений у выгорания —
    /// честное стыковое заикание без использования найденных s1/s2.
    /// </summary>
    public static class GreedySwitcher
    {
        public static PiecewiseConstantLaw Build(
            double segDt, int stepsPerSeg, int rolloutSteps, double tHorizon, SimParams p)
        {
            var nodes = new List<double>();
            double[] y = { p.H0, 0.0, p.M0 };
            double t = 0.0;
            while (t < tHorizon && y[2] > p.Mf)
            {
                double uArc = ArcU(y, p);
                double hBang = PredictApogee(y, p.UMax, segDt, rolloutSteps, tHorizon, p);
                double hArc = PredictApogee(y, uArc, segDt, rolloutSteps, tHorizon, p);
                double u0 = hBang >= hArc ? p.UMax : uArc;
                nodes.Add(u0);
                y = Advance(y, u0, segDt, stepsPerSeg, p);
                t += segDt;
            }
            var u = new double[(int)Math.Ceiling(tHorizon / segDt)];
            for (int k = 0; k < nodes.Count; k++) u[k] = nodes[k];
            return new PiecewiseConstantLaw(u, segDt, p);
        }

        private static double ArcU(double[] y, SimParams p)
            => Math.Min(Math.Max(SingularControl.U(y[0], y[1], y[2], p), 0.0), p.UMax);

        /// <summary>
        /// Прогноз: один сегмент кандидатом, дальше дуга до выгорания. Модель прогноза грубее полёта.
        /// </summary>
        private static double PredictApogee(
            double[] y0, double u0, double segDt, int rolloutSteps, double tHorizon, SimParams p)
        {
            var (T, Y) = Integrator.Solve(
                (double[])y0.Clone(), tHorizon, rolloutSteps,
                (t, y) => t < segDt ? u0 : ArcU(y, p), p,
                stopAfter: (t, y) => t > 0.01 && y[1] < 0.0);
            double hmax = double.MinValue;
            foreach (var yy in Y) hmax = Math.Max(hmax, yy[0]);
            return hmax;
        }

        private static double[] Advance(double[] y0, double u0, double segDt, int stepsPerSeg, SimParams p)
        {
            var (T, Y) = Integrator.Solve((double[])y0.Clone(), segDt, stepsPerSeg, (t, y) => u0, p);
            return Y[Y.Length - 1];
        }
    }
}