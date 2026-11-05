// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Afterwder diffraction module: PeakBroadening.

﻿namespace Tal.Core.PowderDiffraction
{
    public static class PeakBroadening
    {
        public enum ProfileType
        {
            Gaussian,
            Lorentzian,
            PseudoVoigt
        }

        public static double ProfileFunction(
            double x,
            double peakCenter,
            double fwhm,
            ProfileType type,
            double eta = 0.5)
        {
            double xNorm = (x - peakCenter) / fwhm;

            switch (type)
            {
                case ProfileType.Gaussian:
                    return Gaussian(xNorm);

                case ProfileType.Lorentzian:
                    return Lorentzian(xNorm);

                case ProfileType.PseudoVoigt:
                    double g = Gaussian(xNorm);
                    double l = Lorentzian(xNorm);
                    return eta * g + (1 - eta) * l;

                default:
                    return Gaussian(xNorm);
            }
        }

        private static double Gaussian(double x)
        {
            return Math.Exp(-4 * Math.Log(2) * x * x);
        }

        private static double Lorentzian(double x)
        {
            return 1.0 / (1 + 4 * x * x);
        }
    }
}