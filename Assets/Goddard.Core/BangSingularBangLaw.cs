using System;

namespace Goddard.Core
{
    /// <summary>
    /// Структура оптимального управления: релейный → особый → релейный
    /// (bang–singular–bang). Параметры поиска — моменты переключения (s1, s2).
    /// После s2 тяга нулевая: в постановке COPS это выгорание топлива.
    /// </summary>
    public sealed class BangSingularBangLaw
    {
        public double S1 { get; }
        public double S2 { get; }

        private readonly SimParams p;

        public BangSingularBangLaw(double s1, double s2, SimParams p)
        {
            S1 = s1; S2 = s2; this.p = p;
        }

        /// <summary>
        /// Управление как функция времени и состояния (дуга — обратная связь).
        /// </summary>
        public double U(double t, double[] y)
        {
            if (t < S1) return p.UMax;
            if (t < S2)
            {
                double us = SingularControl.U(y[0], y[1], y[2], p);
                return Math.Min(Math.Max(us, 0.0), p.UMax);   // дуга только в допустимых пределах
            }
            return 0.0;
        }
    }
}