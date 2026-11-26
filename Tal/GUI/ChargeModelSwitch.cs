// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Shared switch of the partial-charge model (SQE / Gasteiger) used by the Atom Color and Cavity Settings windows.

using System;
using System.Linq;
using System.Windows.Forms;

namespace Tal.Forms
{
    /// <summary>
    /// Changes <see cref="Flags.ChargeModel"/>, recalculates the charges of the active molecule,
    /// redraws the scene and notifies the windows that show the model (so their lists stay in sync
    /// and the cavity property map is recomputed).
    /// </summary>
    public static class ChargeModelSwitch
    {
        /// <summary>Raised on the UI thread after the charges were recalculated. Argument: new model (0 = SQE, 1 = Gasteiger).</summary>
        public static event Action<int> Changed;

        /// <summary>Display names in the order of the model index.</summary>
        public static readonly string[] Names = { "SQE (split-charge)", "Gasteiger (PEOE)" };

        public static void Change(int model)
        {
            model = model == 1 ? 1 : 0;
            Flags.Instance.ChargeModel = model;
            Flags.Instance.MarkDirty();

            var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            var molecule = mainForm?.GetActiveMolecule();
            if (molecule != null && molecule.NumberOfAtoms > 0)
            {
                molecule.RecalculateCharges();   // also sets the Charge colour range
            }

            Flags.Instance.VisualRevision++;
            mainForm?.RefreshAllDocuments();

            Changed?.Invoke(model);
        }
    }
}
