// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Afterwder diffraction module: AfterwderDiffractionSettings.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Tal.Core.PowderDiffraction;

namespace Tal.Forms
{
 /// <summary>
 /// Setienia for symulacji powder diffraction.
 /// </summary>
    public class PowderDiffractionSettings
    {
        #region Defaults — jedno źródło prawdy

        public const double DefaultMinTwoTheta = 5.0;
        public const double DefaultMaxTwoTheta = 90.0;
        public const XRaySource DefaultSource = XRaySource.CuKα;
        public const double DefaultFWHM = 0.05;
        public const PeakBroadening.ProfileType DefaultProfileType = PeakBroadening.ProfileType.PseudoVoigt;
        public const double DefaultPseudoVoigtEta = 0.5;
        public const double DefaultDebyeWallerB = 0.5;
        public const bool DefaultShowHKL = true;
        public const bool DefaultShowLabels = true;
        public const bool DefaultShowBackground = true;
        public const bool DefaultNormalizeIntensities = true;
        public const XAxisMode DefaultXAxisMode = XAxisMode.TwoTheta;
        public const bool DefaultUseLPCorrection = true;
        public const bool DefaultUseDebyeWaller = true;
        public const double DefaultMinIntensityFraction = 0.01;
        public const int DefaultMaxLabelsToShow = 25;
        public const int DefaultProfileResolution = 2;

        #endregion

        #region Zakres 2θ

 /// <summary>Minimalny angle 2θ w stopniach.</summary>
        public double MinTwoTheta { get; set; } = DefaultMinTwoTheta;

 /// <summary>Maksymalny angle 2θ w stopniach.</summary>
        public double MaxTwoTheta { get; set; } = DefaultMaxTwoTheta;

        #endregion

        #region Źródło promieniowania

 /// <summary>Wybrane source promieniowania X.</summary>
        public XRaySource Source { get; set; } = DefaultSource;

 /// <summary>
 /// Length fali w Ångströmach.
 /// 
 /// ⚠️ Setter is intentionally empty: wavelength is always computed
 /// z currentgo <see cref="Source"/> przez getter. Afterzwala to zachować
 /// kompatybilność z kodem, which robi <c>settings.Wavelength = ...</c>
 /// (np. w event handlerach), nie allowsjąc przypadkowo rozjechać
 /// values z wybranym źródłem.
 /// </summary>
        public double Wavelength
        {
            get => XRaySourceHelper.GetWavelength(Source);
            set { /* no-op: value is derived from Source */ }
        }

        #endregion

        #region Parametry pików

 /// <summary>Szerokość afterłówkowa peaku (FWHM) w jednostkach osi X.</summary>
        public double FWHM { get; set; } = DefaultFWHM;

 /// <summary>Typ profilu peaku.</summary>
        public PeakBroadening.ProfileType ProfileType { get; set; } = DefaultProfileType;

 /// <summary>Parametr mieszania for Pseudo-Voigt (0 = Gaussian, 1 = Lorentzian).</summary>
        public double PseudoVoigtEta { get; set; } = DefaultPseudoVoigtEta;

        #endregion

        #region Debye-Waller

 /// <summary>Czynnik Debye-Waller B w Å².</summary>
        public double DebyeWallerB { get; set; } = DefaultDebyeWallerB;

        #endregion

        #region Opcje wyświetlania

 /// <summary>Czy afterkazywać oznaczenia HKL after najechaniu na peak.</summary>
        public bool ShowHKL { get; set; } = DefaultShowHKL;

 /// <summary>Czy afterkazywać etykiety przy peakach.</summary>
        public bool ShowLabels { get; set; } = DefaultShowLabels;

 /// <summary>Czy afterkazywać wypełnienie afterd krzywą.</summary>
        public bool ShowBackground { get; set; } = DefaultShowBackground;

 /// <summary>Czy normalizować intensity do zakresu 0-1.</summary>
        public bool NormalizeIntensities { get; set; } = DefaultNormalizeIntensities;

 /// <summary>Tryb wyświetlania osi X.</summary>
        public XAxisMode XAxisMode { get; set; } = DefaultXAxisMode;

        #endregion

        #region Zaawansowane opcje obliczeniowe

 /// <summary>Czy uwzględniać afterprawkę Lorentza-afterlaryzacji.</summary>
        public bool UseLPCorrection { get; set; } = DefaultUseLPCorrection;

 /// <summary>Czy uwzględniać czynnik Debye-Waller.</summary>
        public bool UseDebyeWaller { get; set; } = DefaultUseDebyeWaller;

 /// <summary>Minimalna intensity peaku do display (jako ułamek maksimum).</summary>
        public double MinIntensityFraction { get; set; } = DefaultMinIntensityFraction;

 /// <summary>Maksymalna liczba peaks do display etykiet.</summary>
        public int MaxLabelsToShow { get; set; } = DefaultMaxLabelsToShow;

 /// <summary>Rozdzielczość profilu (liczba points na pixel).</summary>
        public int ProfileResolution { get; set; } = DefaultProfileResolution;

        #endregion

        #region Metody pomocnicze

 /// <summary>Creates kopię setień.</summary>
        public PowderDiffractionSettings Clone()
        {
            return new PowderDiffractionSettings
            {
                MinTwoTheta = MinTwoTheta,
                MaxTwoTheta = MaxTwoTheta,
                Source = Source,
                FWHM = FWHM,
                ProfileType = ProfileType,
                PseudoVoigtEta = PseudoVoigtEta,
                DebyeWallerB = DebyeWallerB,
                ShowHKL = ShowHKL,
                ShowLabels = ShowLabels,
                ShowBackground = ShowBackground,
                NormalizeIntensities = NormalizeIntensities,
                XAxisMode = XAxisMode,
                UseLPCorrection = UseLPCorrection,
                UseDebyeWaller = UseDebyeWaller,
                MinIntensityFraction = MinIntensityFraction,
                MaxLabelsToShow = MaxLabelsToShow,
                ProfileResolution = ProfileResolution
            };
        }

 /// <summary>Resetuje setienia do defaultch.</summary>
        public void ResetToDefaults()
        {
            MinTwoTheta = DefaultMinTwoTheta;
            MaxTwoTheta = DefaultMaxTwoTheta;
            Source = DefaultSource;
            FWHM = DefaultFWHM;
            ProfileType = DefaultProfileType;
            PseudoVoigtEta = DefaultPseudoVoigtEta;
            DebyeWallerB = DefaultDebyeWallerB;
            ShowHKL = DefaultShowHKL;
            ShowLabels = DefaultShowLabels;
            ShowBackground = DefaultShowBackground;
            NormalizeIntensities = DefaultNormalizeIntensities;
            XAxisMode = DefaultXAxisMode;
            UseLPCorrection = DefaultUseLPCorrection;
            UseDebyeWaller = DefaultUseDebyeWaller;
            MinIntensityFraction = DefaultMinIntensityFraction;
            MaxLabelsToShow = DefaultMaxLabelsToShow;
            ProfileResolution = DefaultProfileResolution;
        }

        #endregion
    }

    #region Enums

 /// <summary>Źródła promieniowania X.</summary>
    public enum XRaySource
    {
        CuKα,
        MoKα,
        CoKα,
        FeKα,
        CrKα,
        AgKα
    }

 /// <summary>Tryby wyświetlania osi X.</summary>
    public enum XAxisMode
    {
        TwoTheta,   // 2θ w stopniach
        Q,          // Q = 4π·sin(θ)/λ w Å⁻¹
        DSpacing,   // d w Å
        Reciprocal  // 1/d w Å⁻¹
    }

    #endregion

    #region Helpers

 /// <summary>Aftermocnicze metody for źródeł promieniowania X.</summary>
    public static class XRaySourceHelper
    {
 /// <summary>Afterbiera length fali for danego source w Ångströmach.</summary>
        public static double GetWavelength(XRaySource source)
        {
            return source switch
            {
                XRaySource.CuKα => 1.5406,
                XRaySource.MoKα => 0.7107,
                XRaySource.CoKα => 1.7889,
                XRaySource.FeKα => 1.9360,
                XRaySource.CrKα => 2.2910,
                XRaySource.AgKα => 0.5609,
                _ => 1.5406
            };
        }

 /// <summary>Afterbiera name wyświetlaną for danego source.</summary>
        public static string GetDisplayName(XRaySource source)
        {
            return source switch
            {
                XRaySource.CuKα => "Cu Kα (1.5406 Å)",
                XRaySource.MoKα => "Mo Kα (0.7107 Å)",
                XRaySource.CoKα => "Co Kα (1.7889 Å)",
                XRaySource.FeKα => "Fe Kα (1.9360 Å)",
                XRaySource.CrKα => "Cr Kα (2.2910 Å)",
                XRaySource.AgKα => "Ag Kα (0.5609 Å)",
                _ => "Cu Kα (1.5406 Å)"
            };
        }

 /// <summary>Afterbiera skróconą name source.</summary>
        public static string GetShortName(XRaySource source)
        {
            return source switch
            {
                XRaySource.CuKα => "Cu",
                XRaySource.MoKα => "Mo",
                XRaySource.CoKα => "Co",
                XRaySource.FeKα => "Fe",
                XRaySource.CrKα => "Cr",
                XRaySource.AgKα => "Ag",
                _ => "Cu"
            };
        }

 /// <summary>Afterbiera energię source w keV.</summary>
        public static double GetEnergy(XRaySource source)
        {
            double wavelength = GetWavelength(source);
            return 12.398 / wavelength; // E = hc/λ w keV
        }

 /// <summary>Afterbiera all source jako list nazw wyświetlanych.</summary>
        public static string[] GetAllDisplayNames()
        {
            return Enum.GetValues(typeof(XRaySource))
                .Cast<XRaySource>()
                .Select(s => GetDisplayName(s))
                .ToArray();
        }
    }

    #endregion
}