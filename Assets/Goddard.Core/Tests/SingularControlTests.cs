using NUnit.Framework;

namespace Goddard.Core.Tests
{
    /// <summary>
    /// Контрольные значения u_sing из раздатки (таблица самопроверки).
    /// Выведены символьно (sympy) и сверены с формулой до 1e-9.
    /// </summary>
    [TestFixture]
    public class SingularControlTests
    {
        [TestCase(1.0010, 0.050, 0.90, 1.7999)]
        [TestCase(1.0030, 0.100, 0.85, 3.1942)]
        [TestCase(1.0044, 0.154, 0.60, 4.0733)]
        [TestCase(1.0100, 0.100, 0.80, 2.1643)]
        [TestCase(1.0005, 0.020, 0.95, 1.0978)]
        public void SingularControl_MatchesHandoutValues(double h, double v, double m, double expected)
        {
            double u = SingularControl.U(h, v, m, new SimParams());
            Assert.That(u, Is.EqualTo(expected).Within(1e-3));
        }

        [Test]
        public void SingularControl_AtCopsBurnout_ExceedsThrustLimit()
        {
            // Свойство постановки COPS: в точке выгорания дуга недопустима (u_sing > T_max)
            // — поэтому оптимум почти не отличается от full thrust.
            var p = new SimParams();
            double u = SingularControl.U(1.0044, 0.154, 0.6, p);
            Assert.That(u, Is.GreaterThan(p.UMax));
        }
    }
}