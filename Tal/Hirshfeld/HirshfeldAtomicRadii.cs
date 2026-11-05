// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hirshfeld surface module: HirshfeldAtomicRadii.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System.Collections.Generic;

namespace Tal.Hirshfeld
{
 /// <summary>
 /// Tabela sigm gaussowskich i efektywnych Z for atoms promoleculesowych.
 /// Source: Spackman / Stewart (fit to experimental electron densities).
 ///
 /// Sigma values are isotropic Gaussian widths:
 /// rho(r) = Z_eff / (sqrt(2π) σ)^3 · exp(-|r-r_i|² / (2σ²))
 ///
 /// For the anisotropic variant (with ADP) sigma is the base for electron smearing:
 /// a_k = sqrt(U_k_ADP² + σ_e²)
 /// </summary>
    public static class HirshfeldAtomicRadii
    {
 /// <summary>Default sigma for an unknown element (Å).</summary>
        public const float DefaultSigma = 0.45f;

        private static readonly Dictionary<int, float> _sigma = new Dictionary<int, float>
        {
 // Period 1
            {  1, 0.35f },  // H
            {  2, 0.30f },  // He

 // Period 2
            {  3, 0.55f },  // Li
            {  4, 0.50f },  // Be
            {  5, 0.45f },  // B
            {  6, 0.42f },  // C
            {  7, 0.40f },  // N
            {  8, 0.38f },  // O
            {  9, 0.36f },  // F
            { 10, 0.34f },  // Ne

 // Period 3
            { 11, 0.62f },  // Na
            { 12, 0.55f },  // Mg
            { 13, 0.50f },  // Al
            { 14, 0.48f },  // Si
            { 15, 0.47f },  // P
            { 16, 0.46f },  // S
            { 17, 0.45f },  // Cl
            { 18, 0.44f },  // Ar

 // Period 4
            { 19, 0.65f },  // K
            { 20, 0.60f },  // Ca
            { 21, 0.55f },  // Sc
            { 22, 0.52f },  // Ti
            { 23, 0.50f },  // V
            { 24, 0.49f },  // Cr
            { 25, 0.48f },  // Mn
            { 26, 0.47f },  // Fe
            { 27, 0.46f },  // Co
            { 28, 0.45f },  // Ni
            { 29, 0.44f },  // Cu
            { 30, 0.44f },  // Zn
            { 31, 0.48f },  // Ga
            { 32, 0.48f },  // Ge
            { 33, 0.47f },  // As
            { 34, 0.47f },  // Se
            { 35, 0.46f },  // Br
            { 36, 0.45f },  // Kr

 // Period 5
            { 37, 0.70f },  // Rb
            { 38, 0.65f },  // Sr
            { 39, 0.60f },  // Y
            { 40, 0.58f },  // Zr
            { 41, 0.55f },  // Nb
            { 42, 0.54f },  // Mo
            { 43, 0.52f },  // Tc
            { 44, 0.51f },  // Ru
            { 45, 0.50f },  // Rh
            { 46, 0.50f },  // Pd
            { 47, 0.48f },  // Ag
            { 48, 0.48f },  // Cd
            { 49, 0.50f },  // In
            { 50, 0.50f },  // Sn
            { 51, 0.50f },  // Sb
            { 52, 0.50f },  // Te
            { 53, 0.50f },  // I
            { 54, 0.48f },  // Xe

 // Period 6
            { 55, 0.72f },  // Cs
            { 56, 0.68f },  // Ba
            { 57, 0.62f },  // La
            { 58, 0.60f },  // Ce
            { 59, 0.58f },  // Pr
            { 60, 0.56f },  // Nd
            { 61, 0.55f },  // Pm
            { 62, 0.55f },  // Sm
            { 63, 0.55f },  // Eu
            { 64, 0.54f },  // Gd
            { 65, 0.54f },  // Tb
            { 66, 0.54f },  // Dy
            { 67, 0.54f },  // Ho
            { 68, 0.54f },  // Er
            { 69, 0.54f },  // Tm
            { 70, 0.54f },  // Yb
            { 71, 0.54f },  // Lu
            { 72, 0.52f },  // Hf
            { 73, 0.52f },  // Ta
            { 74, 0.52f },  // W
            { 75, 0.51f },  // Re
            { 76, 0.51f },  // Os
            { 77, 0.51f },  // Ir
            { 78, 0.50f },  // Pt
            { 79, 0.50f },  // Au
            { 80, 0.50f },  // Hg
            { 81, 0.52f },  // Tl
            { 82, 0.52f },  // Pb
            { 83, 0.52f },  // Bi
            { 84, 0.52f },  // Po
            { 85, 0.52f },  // At
            { 86, 0.50f },  // Rn

 // Period 7
            { 87, 0.72f },  // Fr
            { 88, 0.68f },  // Ra
            { 89, 0.62f },  // Ac
            { 90, 0.60f },  // Th
            { 91, 0.58f },  // Pa
            { 92, 0.58f },  // U
            { 93, 0.56f },  // Np
            { 94, 0.56f },  // Pu
            { 95, 0.55f },  // Am
        };

 /// <summary>
 /// Returns Gaussian width σ (Å) for the given element.
 /// </summary>
        public static float GetSigma(int atomicNumber)
        {
            return _sigma.TryGetValue(atomicNumber, out float s) ? s : DefaultSigma;
        }

 /// <summary>
 /// Efektywna electron count for density promoleculesowej.
 /// Na start: after prostu atomic number (wystarcza do wizualizacji).
 /// Can be improved later: Z_eff = Z - (number of core electrons)
 /// or use literature values (Spackman / Stewart / Clementi).
 /// </summary>
        public static float ZEffective(int atomicNumber)
        {
            if (atomicNumber <= 0) return 1.0f;
            return atomicNumber;
        }
    }
}