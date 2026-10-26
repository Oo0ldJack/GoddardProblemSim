using System;

namespace Goddard.Core
{
    /// <summary>
    /// Особое управление u_sing(h, v, m) — явный вид из условия σ̈ = 0 (см. раздатку,
    /// вывод через скобки Пуассона). Формула получена в нормировке COPS
    /// (m0 = h0 = g0 = 1) и сверена с символьным вычислением до 1e-9.
    /// Структура: компенсация drag + гравитация/градиент атмосферы − поправка на выгорание.
    /// </summary>
    public static class SingularControl
    {
        public static double U(double h, double v, double m, SimParams p)
        {
            // Дуга существует на восходящем участке; при v < 0 физический смысл теряется.
            v = Math.Max(v, 0.0);

            double c = p.C, hc = p.Hc, vc = p.Vc;
            double den = 2 * c * c + 4 * c * v + v * v;

            double termDrag = Drag.D(h, v, p);   // первое слагаемое == D(h,v) при m0 = 1
            double termGrav = m * c * (c * h * h * hc * v * v
                                       + h * h * hc * v * v * v
                                       + 2 * c + 2 * v)
                              / (h * h * den);
            double termMass = 4 * c * c * m * m * Math.Exp(hc * (h - p.H0) / p.H0)
                              / (h * h * h * vc * den);
            return termDrag + termGrav - termMass;
        }
    }
}