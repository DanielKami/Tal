// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Afterwder diffraction module: DiffractionPeak.

﻿namespace Tal.Core.PowderDiffraction
{
    public class DiffractionPeak
    {
        public int H { get; set; }
        public int K { get; set; }
        public int L { get; set; }

        public double TwoTheta { get; set; }      // 2θ w stopniach
        public double DSpacing { get; set; }      // d w Å
        public double Q { get; set; }             // Q = 4π·sin(θ)/λ w Å⁻¹
        public double ReciprocalD { get; set; }   // 1/d w Å⁻¹

        public double Intensity { get; set; }
        public int Multiplicity { get; set; }
        public double StructureFactor { get; set; }

        public string HKL => $"({H}{K}{L})";

        public override string ToString()
        {
            return $"hkl={HKL}, 2θ={TwoTheta:F2}°, d={DSpacing:F3}Å, I={Intensity:F0}";
        }
    }
}