// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Afterwder diffraction module: Complex.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

namespace Tal.Utils
{
 /// <summary>
 /// Simple complex-number structure — used in calculations
 /// czynnika structurelnego F(hkl) i innych operacji krystalograficznych.
 /// 
 /// Świadomie NIE we use <see cref="System.Numerics.Complex"/> z BCL,
 /// bo:
 /// - aftertrzebujemy only kilku operacji (nie całego API BCL),
 /// - unikamy konfliktu nazw w plikach używających <c>using System.Numerics;</c>,
 /// - structure jest lżejsza i bardziej przewidywalna w kontekście obliczeń.
 /// </summary>
    public struct Complex
    {
        #region Fields

 /// <summary>Część rzeczywista.</summary>
        public double Real { get; set; }

 /// <summary>Część urojona.</summary>
        public double Imaginary { get; set; }

        #endregion

        #region Constructors

 /// <summary>Creates number zesafterloną o afterdanych częściach.</summary>
        public Complex(double real, double imaginary)
        {
            Real = real;
            Imaginary = imaginary;
        }

 /// <summary>Creates number rzeczywistą (część urojona = 0).</summary>
        public Complex(double real) : this(real, 0.0) { }

        #endregion

        #region Static Factory

 /// <summary>Liczba zero (0 + 0i).</summary>
        public static Complex Zero => new Complex(0, 0);

 /// <summary>Liczba jedynka (1 + 0i).</summary>
        public static Complex One => new Complex(1, 0);

 /// <summary>Jednostka urojona (0 + 1i).</summary>
        public static Complex I => new Complex(0, 1);

 /// <summary>Creates number zesafterloną z afterstaci biegunewj: r·(cos θ + i·sin θ).</summary>
        public static Complex FromPolar(double magnitude, double phase)
            => new Complex(magnitude * Math.Cos(phase), magnitude * Math.Sin(phase));

        #endregion

        #region Properties

 /// <summary>Moduł |z| = √(Re² + Im²).</summary>
        public readonly double Magnitude => Math.Sqrt(Real * Real + Imaginary * Imaginary);

 /// <summary>Kwadrat modułu |z|² = Re² + Im².</summary>
        public readonly double MagnitudeSquared => Real * Real + Imaginary * Imaginary;

 /// <summary>Argument (faza) φ = atan2(Im, Re).</summary>
        public readonly double Phase => Math.Atan2(Imaginary, Real);

 /// <summary>Sprzężenie z* = Re - i·Im.</summary>
        public readonly Complex Conjugate => new Complex(Real, -Imaginary);

        #endregion

        #region Arithmetic Operators

        public static Complex operator +(Complex a, Complex b)
            => new Complex(a.Real + b.Real, a.Imaginary + b.Imaginary);

        public static Complex operator -(Complex a, Complex b)
            => new Complex(a.Real - b.Real, a.Imaginary - b.Imaginary);

        public static Complex operator -(Complex a)
            => new Complex(-a.Real, -a.Imaginary);

        public static Complex operator *(Complex a, Complex b)
            => new Complex(
                a.Real * b.Real - a.Imaginary * b.Imaginary,
                a.Real * b.Imaginary + a.Imaginary * b.Real);

        public static Complex operator *(Complex a, double scalar)
            => new Complex(a.Real * scalar, a.Imaginary * scalar);

        public static Complex operator *(double scalar, Complex a)
            => new Complex(a.Real * scalar, a.Imaginary * scalar);

        public static Complex operator /(Complex a, double scalar)
        {
            if (Math.Abs(scalar) < 1e-30)
                throw new DivideByZeroException("Complex division by ~zero scalar.");
            return new Complex(a.Real / scalar, a.Imaginary / scalar);
        }

        public static Complex operator /(Complex a, Complex b)
        {
            double denom = b.MagnitudeSquared;
            if (denom < 1e-30)
                throw new DivideByZeroException("Complex division by ~zero.");
            return new Complex(
                (a.Real * b.Real + a.Imaginary * b.Imaginary) / denom,
                (a.Imaginary * b.Real - a.Real * b.Imaginary) / denom);
        }

        #endregion

        #region Equality

        public readonly bool Equals(Complex other)
            => Real == other.Real && Imaginary == other.Imaginary;

        public override readonly bool Equals(object obj)
            => obj is Complex c && Equals(c);

        public override readonly int GetHashCode()
            => HashCode.Combine(Real, Imaginary);

        public static bool operator ==(Complex a, Complex b) => a.Equals(b);
        public static bool operator !=(Complex a, Complex b) => !a.Equals(b);

        #endregion

        #region Debug

        public override readonly string ToString()
            => Imaginary >= 0
                ? $"{Real:F6} + {Imaginary:F6}i"
                : $"{Real:F6} - {-Imaginary:F6}i";

        #endregion
    }
}