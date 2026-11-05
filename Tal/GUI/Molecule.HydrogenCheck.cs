// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// GUI helper — hydrogen normalisation status checks for Molecule.

﻿// Molecule.HydrogenCheck.cs
// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// Molecule partial — hydrogen normalisation state check
// before Hirshfeld analysis (and other operations requiring
// poprawnej geometry H).

using System.Collections.Generic;
using System.Diagnostics;
using Tal.Calculate;

namespace Tal
{
    public partial class Molecule
    {
        public enum HydrogenState
        {
            /// <summary>No atoms H w AsymmetricPart.</summary>
            NoHydrogens,

            /// <summary>H are already normalised (all fixy = skipped "already normalized").</summary>
            AlreadyNormalized,

            /// <summary>User manually disabled (clicked Reset in HydrogenNormalizationWindow).</summary>
            DisabledByUser,

            /// <summary>H are in experimental state and require normalisation.</summary>
            NeedsNormalization,

            /// <summary>AsymmetricPart empty / not loaded.</summary>
            NoStructure
        }

        /// <summary>
        /// Sprawdza stan normalizacji BEZ modyfikacji structure.
        ///
        /// ⭐ Wykorzystuje existing flags:
        ///    - HydrogenNormalizationApplied
        ///    - HydrogenNormalizationDisabledByUser
        ///    - HydrogenNormalizer.Check() (non-mutating)
        ///
        /// Returns fixes (do raportu) via out.
        /// </summary>
        public HydrogenState CheckHydrogenState(
            out List<HydrogenNormalizer.HydrogenFix> fixes,
            float cutoffHeavy = 1.3f,
            bool skipBridging = true)
        {
            fixes = null;

            if (AsymmetricPart == null || AsymmetricPart.Count == 0)
                return HydrogenState.NoStructure;

            int hCount = 0;
            for (int i = 0; i < AsymmetricPart.Count; i++)
                if (AsymmetricPart[i].AtomicNumber == 1) hCount++;

            if (hCount == 0)
                return HydrogenState.NoHydrogens;

            // ⭐ ALWAYS count fixes — regardless of the flags
            fixes = HydrogenNormalizer.Check(this, cutoffHeavy, skipBridging);

            int toFix = 0;
            for (int i = 0; i < fixes.Count; i++)
                if (!fixes[i].Skipped) toFix++;

            // Flag DisabledByUser to informacja DODATKOWA — not zmienia stanu geometry.
            // If H are already normalised (toFix == 0) — return AlreadyNormalized
            // regardless of the flags (the flags does not matter).
            if (toFix == 0)
                return HydrogenState.AlreadyNormalized;

            // There are fixes — H require normalisation.
            // But the user may have clicked Reset — we report that via DisabledByUser.
            if (HydrogenNormalizationDisabledByUser)
                return HydrogenState.DisabledByUser;

            return HydrogenState.NeedsNormalization;
        }

        /// <summary>
        /// Normalizuje H i przebudowuje scene 3D.
        /// Sets flags Applied = true, DisabledByUser = false.
        /// </summary>
        public List<HydrogenNormalizer.HydrogenFix> ApplyHydrogenNormalization(
            float cutoffHeavy = 1.3f,
            bool skipBridging = true)
        {
            Debug.WriteLine("[Molecule] ApplyHydrogenNormalization — START");

            var fixes = HydrogenNormalizer.Normalize(this, cutoffHeavy, skipBridging);

            HydrogenNormalizationApplied = true;
            HydrogenNormalizationDisabledByUser = false;

            // ⭐ Przebuduj scene 3D (atom[], MoleculeShift, bonds)
            RebuildFromAsymmetricPart();

            Debug.WriteLine(
                $"[Molecule] ApplyHydrogenNormalization — END " +
                $"(atoms={NumberOfAtoms}, applied={HydrogenNormalizationApplied})");

            return fixes;
        }

        /// <summary>
        /// Przywraca oryginalne pozycje H i przebudowuje scene 3D.
        /// Sets flags Applied = false, DisabledByUser = true.
        /// </summary>
        public bool RevertHydrogenNormalization()
        {
            Debug.WriteLine("[Molecule] RevertHydrogenNormalization — START");

            bool ok = HydrogenNormalizer.ResetToOriginal(this);
            if (ok)
            {
                HydrogenNormalizationApplied = false;
                HydrogenNormalizationDisabledByUser = true;
                RebuildFromAsymmetricPart();
            }

            Debug.WriteLine($"[Molecule] RevertHydrogenNormalization — END (ok={ok})");
            return ok;
        }
    }
}