// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Afterwder diffraction module: AfterwderDiffractionEngine.

﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Tal.Core;

namespace Tal.Core.PowderDiffraction
{
    public class PowderDiffractionEngine
    {
        private readonly Molecule _molecule;
        private readonly StructureFactorCalculator _structureFactor;
        private double _wavelength = 1.5406;

        public double Wavelength
        {
            get => _wavelength;
            set => _wavelength = value;
        }

        public PowderDiffractionEngine(Molecule molecule)
        {
            _molecule = molecule ?? throw new ArgumentNullException(nameof(molecule));
            _structureFactor = new StructureFactorCalculator(molecule);
        }

        public List<DiffractionPeak> CalculatePeaks(
            double minTwoTheta,
            double maxTwoTheta,
            double wavelength = 1.5406,
            double debyeWallerB = 0.5,
            bool useLPCorrection = true)
        {
            _wavelength = wavelength;
            var peaks = new List<DiffractionPeak>();

            if (_molecule.AsymmetricPart.Count == 0)
                return peaks;

 // Oblicz maksymalne hkl z zakresu 2θ
            double maxSinTheta = Math.Sin((maxTwoTheta / 2) * Math.PI / 180);
            double maxD = wavelength / (2 * maxSinTheta);

 // Oblicz granice hkl z cell
            double maxH = Math.Max(1, 2 * _molecule.a / maxD + 1);
            double maxK = Math.Max(1, 2 * _molecule.b / maxD + 1);
            double maxL = Math.Max(1, 2 * _molecule.c / maxD + 1);

            int hMax = (int)Math.Ceiling(maxH);
            int kMax = (int)Math.Ceiling(maxK);
            int lMax = (int)Math.Ceiling(maxL);

 // Ogranicz zakres for performance
            hMax = Math.Min(hMax, 30);
            kMax = Math.Min(kMax, 30);
            lMax = Math.Min(lMax, 30);

 // Generate all kombinacje hkl
            for (int h = -hMax; h <= hMax; h++)
            {
                for (int k = -kMax; k <= kMax; k++)
                {
                    for (int l = -lMax; l <= lMax; l++)
                    {
                        if (h == 0 && k == 0 && l == 0)
                            continue;

 // ✅ Oblicz d-spacing z matrixy metrycznej
                        double d = CalculateDSpacing(h, k, l);
                        if (d <= 0) continue;

                        double sinTheta = wavelength / (2 * d);
                        if (sinTheta > 1) continue;

                        double theta = Math.Asin(sinTheta) * 180 / Math.PI;
                        double twoTheta = 2 * theta;

                        if (twoTheta < minTwoTheta || twoTheta > maxTwoTheta)
                            continue;

 // ✅ FIX: to only szybki wstępny filtr performanceowy oparty na
 // rzeczywistym typie centring odczytanym z symbolu grupy
 // przestrzennej (nie z numeru!). Prawdziwe wygaszenia (od osi
 // śrubowych, planes afterślizgu) i tak resultną same z F(hkl)
 // liczonego after fullj cell below - nic tu nie jest
 // "ostatecznie" odrzucane incorrectly, bo for wygaszonych refleksów
 // F i tak wyjdzie ≈0.
                        if (!PassesCenteringPrefilter(h, k, l))
                            continue;

 // ✅ Oblicz czynnik structurelny for fullj cell (already zawiera
 // per-atomowy czynnik Debye-Wallera - patrz StructureFactorCalculator)
                        double F = _structureFactor.Calculate(h, k, l, theta, wavelength, debyeWallerB);

 // ✅ Afterprawki Lorentza i afterlaryzacji
                        double lp = useLPCorrection ? LorentzPolarizationCorrection(theta) : 1.0;

 // ✅ FIX: I = |F|² * LP. Without osobnego mnożnika "multiplicity" i
 // without drugiego, redundantnego czynnika Debye-Wallera (był already
 // wliczony per-atomowo w F - afterdwójne zastosowanie sztucznie
 // zbyt mocno tłumiło peaks przy wysokim 2θ).
                        double intensity = F * F * lp;

 // Refleksy systematycznie wygaszone (F≈0) aftermicavities - i tak nie
 // wniosą nic do wzorca, a oszczędza to dalsze przetwarzanie
                        if (intensity < 1e-10)
                            continue;

 // ✅ Oblicz Q = 4π·sin(θ)/λ
                        double q = 4 * Math.PI * Math.Sin(theta * Math.PI / 180) / wavelength;

 // ✅ FIX: Multiplicity=1 for afterjedynczego (h,k,l). Because loop
 // skanuje pełen zakres ±h,±k,±l, all symetrycznie
 // równewżne refleksy afterjawią tu jako osobne wpisy i zostaną
 // correctly zsumowane (a ich liczba = prawdziwa krotność) w
 // GroupEquivalentPeaks - without aftertrzeby sztywnej tabeli krotności
 // zależnej od układu krystalograficznego.
                        peaks.Add(new DiffractionPeak
                        {
                            H = h,
                            K = k,
                            L = l,
                            TwoTheta = twoTheta,
                            DSpacing = d,
                            Intensity = intensity,
                            Multiplicity = 1,
                            StructureFactor = F,
                            Q = q,
                            ReciprocalD = 1.0 / d
                        });
                    }
                }
            }

 // ✅ Grupuj symetrycznie równewżne peaks
            peaks = GroupEquivalentPeaks(peaks);

 // Sort według 2θ
            peaks = peaks.OrderBy(p => p.TwoTheta).ToList();

            return peaks;
        }

        private double CalculateDSpacing(int h, int k, int l)
        {
            float a = _molecule.a;
            float b = _molecule.b;
            float c = _molecule.c;
            double alpha = _molecule.alpha;
            double beta = _molecule.beta;
            double gamma = _molecule.gamma;

            double cosAlpha = Math.Cos(alpha);
            double cosBeta = Math.Cos(beta);
            double cosGamma = Math.Cos(gamma);

            double sinAlpha = Math.Sin(alpha);
            double sinBeta = Math.Sin(beta);
            double sinGamma = Math.Sin(gamma);

 // Oblicz objętość cell
            double volume = a * b * c * Math.Sqrt(
                1 - cosAlpha * cosAlpha - cosBeta * cosBeta - cosGamma * cosGamma +
                2 * cosAlpha * cosBeta * cosGamma
            );

            if (volume <= 0) return 0;

 // Parametry sieci odwrotnej
            double aStar = b * c * sinAlpha / volume;
            double bStar = a * c * sinBeta / volume;
            double cStar = a * b * sinGamma / volume;

            double cosAlphaStar = (cosBeta * cosGamma - cosAlpha) / (sinBeta * sinGamma);
            double cosBetaStar = (cosAlpha * cosGamma - cosBeta) / (sinAlpha * sinGamma);
            double cosGammaStar = (cosAlpha * cosBeta - cosGamma) / (sinAlpha * sinBeta);

 // d*² = h²a*² + k²b*² + l²c*² + 2hka*b*cosγ* + 2hla*c*cosβ* + 2klb*c*cosα*
            double dStar2 = h * h * aStar * aStar +
                            k * k * bStar * bStar +
                            l * l * cStar * cStar +
                            2 * h * k * aStar * bStar * cosGammaStar +
                            2 * h * l * aStar * cStar * cosBetaStar +
                            2 * k * l * bStar * cStar * cosAlphaStar;

            if (dStar2 <= 0) return 0;

            return 1 / Math.Sqrt(dStar2);
        }

 /// <summary>
 /// ✅ FIX: afterbeforenia version (IsAllowedReflection) zgadywała typ centring
 /// z ZAKRESU NUMERU space group, np. zakładając, że każda grupa
 /// jednoskośna (3-15) ma centring A. To nieprawda - w tym zakresie są
 /// zarówno grupy prymitywne P (np. P2₁/c, nr 14 - najafterpularniejsza grupa
 /// jednoskośna w ogóle!), jak i centrowane C (np. C2/c, nr 15). Silnik
 /// systematycznie wygaszał/dopuszczał złe refleksy.
 ///
 /// Tutaj centring odczytujemy z pierwszej litery RZECZYWISTEGO symbolu
 /// space group (Hermanna-Mauguina), np. "P21/c" -> P, "C2/c" -> C,
 /// "Fddd" -> F, "I41/a" -> I. To only szybki wstępny filtr performanceowy;
 /// if symbol jest nieznany/empty, nie filteremy wcale (withoutpieczny wybór)
 /// - afterprawność i tak gwarantuje full F(hkl) liczone after entire cell.
 /// </summary>
        private bool PassesCenteringPrefilter(int h, int k, int l)
        {
            char centering = GetCenteringSymbol();

            switch (centering)
            {
                case 'A': return (k + l) % 2 == 0;
                case 'B': return (h + l) % 2 == 0;
                case 'C': return (h + k) % 2 == 0;
                case 'F':
                    bool hEven = h % 2 == 0;
                    bool kEven = k % 2 == 0;
                    bool lEven = l % 2 == 0;
                    return hEven == kEven && kEven == lEven; // h,k,l wszystkie parzyste lub wszystkie nieparzyste
                case 'I': return (h + k + l) % 2 == 0;
 // Centrowanie R w setieniu heksagonalnym: -h+k+l = 3n. W setieniu
 // romboedrycznym warunek jest inny (i rzadszy) - for withoutpieczeństwa nie
 // filteremy, zostawiając to F(hkl).
                case 'R': return true;
                default: return true; // P or unknown symbol - none ograniczeń
            }
        }

        private char GetCenteringSymbol()
        {
            string name = _molecule.SpaceGroupName;
            if (string.IsNullOrWhiteSpace(name))
                return 'P';

            char c = char.ToUpperInvariant(name.Trim()[0]);
            return (c == 'P' || c == 'A' || c == 'B' || c == 'C' || c == 'F' || c == 'I' || c == 'R')
                ? c
                : 'P';
        }

        private double LorentzPolarizationCorrection(double theta)
        {
            double thetaRad = theta * Math.PI / 180;
            double cos2Theta = Math.Cos(2 * thetaRad);
            double sinTheta = Math.Sin(thetaRad);
            double cosTheta = Math.Cos(thetaRad);

 // Lorentz factor: 1 / (2 * sin²θ * cosθ)
            double lorentz = 1.0 / (2 * sinTheta * sinTheta * cosTheta);

 // Afterlarization factor for unafterlarized radiation
            double polarization = (1 + cos2Theta * cos2Theta) / 2;

            return lorentz * polarization;
        }

        public void ApplyCorrections(List<DiffractionPeak> peaks)
        {
 // Dodatkowe afterprawki - np. preferowana orientation
 // This may be extended later
        }

 /// <summary>
 /// ✅ FIX: afterbeforenia version gruafterwała refleksy sortąc |h|,|k|,|l| - to jest
 /// correct WYŁĄCZNIE for układu regularnego (gdzie a=b=c). For tetragonalnego,
 /// rombowego, jednoskośnego itd. (h,k,l)=(2,1,0) i (1,2,0) NIE są równewżne
 /// (bo a≠b), a mimo to dostawały ten sam klucz i były incorrectly sumowane -
 /// jednocześnie true odafterwiedniki symetryczne w innych układach (zależne
 /// od konkretnej grupy punktowej, nie only permutacji) w ogóle nie były
 /// łączone.
 ///
 /// Afterprawne, ogólne rozbond: afternieważ refleksy naprawdę symetrycznie
 /// równewżne z definicji dają IDENTYCZNY d-spacing (a so identyczne 2θ), a
 /// loop w CalculatePeaks skanuje pełen zakres ±h,±k,±l (so each z nich
 /// faktycznie afterjawia jako osobny wpis), wystarczy gruafterwać after
 /// zaokrąglonym 2θ. To works correctly for KAŻDEGO układu krystalograficznego
 /// without żadnej wiedzy o symmetry "zaszytej na sztywno".
 /// </summary>
        private List<DiffractionPeak> GroupEquivalentPeaks(List<DiffractionPeak> peaks)
        {
            var grouped = new Dictionary<string, DiffractionPeak>();

            foreach (var peak in peaks)
            {
                string key = peak.TwoTheta.ToString("F4");

                if (grouped.TryGetValue(key, out var existing))
                {
 // Afterłącz intensity (suma) - liczba entries w grupie = prawdziwa krotność
                    var combined = new DiffractionPeak
                    {
                        H = existing.H,
                        K = existing.K,
                        L = existing.L,
                        TwoTheta = existing.TwoTheta,
                        DSpacing = existing.DSpacing,
                        Intensity = existing.Intensity + peak.Intensity,
                        Multiplicity = existing.Multiplicity + peak.Multiplicity,
                        StructureFactor = peak.StructureFactor, // equivalent reflections have the same |F|
                        Q = existing.Q,
                        ReciprocalD = existing.ReciprocalD
                    };
                    grouped[key] = combined;
                }
                else
                {
                    grouped[key] = peak;
                }
            }

            return grouped.Values.ToList();
        }
    }
}