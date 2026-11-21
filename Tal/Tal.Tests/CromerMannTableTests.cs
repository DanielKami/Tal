using Tal.Core.PowderDiffraction;
using Xunit;

namespace Tal.Tests
{
    public class CromerMannTableTests
    {
        public static IEnumerable<object[]> AllElements()
        {
            for (int z = 1; z <= CromerMannTable.MaxSupportedZ; z++)
                yield return new object[] { z };
        }

        [Theory]
        [MemberData(nameof(AllElements))]
        public void EveryElementHToCfHasCoefficients(int z)
        {
            Assert.True(CromerMannTable.Has(z), $"{CromerMannTable.Symbol(z)} has no coefficients");
        }

        /// <summary>f0(s = 0) = Σa_i + c must equal Z. Catches misprints in a_i and c
        /// (e.g. Li a1 = 1.282 instead of 1.1282, an element copied from its neighbour).</summary>
        [Theory]
        [MemberData(nameof(AllElements))]
        public void SumRuleGivesAtomicNumber(int z)
        {
            double f0 = CromerMannTable.FormFactor(z, 0.0);
            Assert.InRange(f0, z - 0.08, z + 0.08);   // tabulated fits deviate by up to 0.06 (Tl)
        }

        /// <summary>The scattering factor decreases monotonically with sin(theta)/lambda and stays positive.</summary>
        [Theory]
        [MemberData(nameof(AllElements))]
        public void FormFactorDecreasesWithS(int z)
        {
            double previous = CromerMannTable.FormFactor(z, 0.0);
            for (double s = 0.05; s <= 2.0; s += 0.05)
            {
                double f = CromerMannTable.FormFactor(z, s * s);
                Assert.True(f <= previous + 1e-6, $"{CromerMannTable.Symbol(z)}: f0 rises at s = {s:F2}");
                Assert.True(f > -0.3, $"{CromerMannTable.Symbol(z)}: f0 negative at s = {s:F2}");
                previous = f;
            }
        }

        /// <summary>Elements that were silently zero in the old table must now contribute.</summary>
        [Theory]
        [InlineData(43)]  // Tc
        [InlineData(54)]  // Xe
        [InlineData(65)]  // Tb
        [InlineData(69)]  // Tm
        [InlineData(70)]  // Yb
        [InlineData(71)]  // Lu
        [InlineData(72)]  // Hf
        [InlineData(84)]  // Po
        [InlineData(92)]  // U
        [InlineData(98)]  // Cf
        public void FormerlyEmptyElementsContribute(int z)
        {
            Assert.True(CromerMannTable.FormFactor(z, 0.25) > 0.4 * z);
        }

        /// <summary>Promethium used to carry the coefficients of samarium.</summary>
        [Fact]
        public void PromethiumIsNotSamarium()
        {
            Assert.InRange(CromerMannTable.FormFactor(61, 0.0), 60.9, 61.1);
            Assert.InRange(CromerMannTable.FormFactor(62, 0.0), 61.9, 62.1);
        }

        /// <summary>No silent fallback: an element without coefficients is refused.</summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        [InlineData(99)]
        [InlineData(118)]
        public void UnsupportedElementsAreRejected(int z)
        {
            Assert.False(CromerMannTable.Has(z));
            Assert.Throws<InvalidOperationException>(() => CromerMannTable.FormFactor(z, 0.1));
        }

        [Fact]
        public void SymbolsAreCorrect()
        {
            Assert.Equal("H", CromerMannTable.Symbol(1));
            Assert.Equal("Tb", CromerMannTable.Symbol(65));
            Assert.Equal("Cf", CromerMannTable.Symbol(98));
            Assert.Equal("Og", CromerMannTable.Symbol(118));
            Assert.Equal("Z=0", CromerMannTable.Symbol(0));
        }
    }
}
