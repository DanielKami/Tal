// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Molecule partial — atom label generation.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// Molecule partial — offsety etykiet atoms.
// Used when the user drags labels in "Move Labels" mode.

namespace Tal
{
    public partial class Molecule
    {
        #region Internal Helpers

 /// <summary>
 /// Allocates LabelOffsets array (size = NumberOfAtoms) and fills with defaults.
 /// Called after 3D atoms are generated.
 /// </summary>
        private void InitializeLabelOffsets()
        {
            if (atom == null || NumberOfAtoms == 0) return;

            LabelOffsets = new AtomLabelOffset[NumberOfAtoms];
            for (int i = 0; i < NumberOfAtoms; i++)
            {
                LabelOffsets[i] = new AtomLabelOffset
                {
                    OffsetX = 0f,
                    OffsetY = -0.04f,
                    IsCustom = false
                };
            }
        }

        #endregion

        #region Public API

 /// <summary>
 /// Sets a custom label offset for the given atom.
 /// Automatically allocates LabelOffsets if needed.
 /// </summary>
        public void SetLabelOffset(int atomIndex, float offsetX, float offsetY)
        {
            if (atomIndex < 0 || atomIndex >= NumberOfAtoms) return;
            if (LabelOffsets == null || LabelOffsets.Length != NumberOfAtoms)
            {
                InitializeLabelOffsets();
            }

            LabelOffsets[atomIndex].OffsetX = offsetX;
            LabelOffsets[atomIndex].OffsetY = offsetY;
            LabelOffsets[atomIndex].IsCustom = true;
        }

 /// <summary>
 /// Resets a single atom's label to its default offset (0, -0.04).
 /// </summary>
        public void ResetLabelOffset(int atomIndex)
        {
            if (atomIndex < 0 || atomIndex >= NumberOfAtoms) return;
            if (LabelOffsets == null || LabelOffsets.Length != NumberOfAtoms) return;

            LabelOffsets[atomIndex].OffsetX = 0f;
            LabelOffsets[atomIndex].OffsetY = -0.04f;
            LabelOffsets[atomIndex].IsCustom = false;
        }

 /// <summary>
 /// Resets all label offsets to their default values (0, -0.04).
 /// </summary>
        public void ResetAllLabelOffsets()
        {
            if (LabelOffsets == null) return;
            for (int i = 0; i < LabelOffsets.Length; i++)
            {
                LabelOffsets[i].OffsetX = 0f;
                LabelOffsets[i].OffsetY = -0.04f;
                LabelOffsets[i].IsCustom = false;
            }
        }

        public void EnsureAxisLabelOffsets()
        {
            if (AxisLabelOffsets != null && AxisLabelOffsets.Length == 3) return;
            AxisLabelOffsets = new AtomLabelOffset[3];
            for (int i = 0; i < 3; i++)
            {
                AxisLabelOffsets[i] = new AtomLabelOffset
                {
                    OffsetX = 0f,
                    OffsetY = 0f,
                    IsCustom = false
                };
            }
        }

        public void SetAxisLabelOffset(int axisIndex, float offsetX, float offsetY)
        {
            if (axisIndex < 0 || axisIndex > 2) return;
            EnsureAxisLabelOffsets();
            AxisLabelOffsets[axisIndex].OffsetX = offsetX;
            AxisLabelOffsets[axisIndex].OffsetY = offsetY;
            AxisLabelOffsets[axisIndex].IsCustom = true;
        }

        public (float offsetX, float offsetY) GetAxisLabelOffset(int axisIndex)
        {
            EnsureAxisLabelOffsets();
            if (axisIndex < 0 || axisIndex > 2) return (0f, 0f);
            return (AxisLabelOffsets[axisIndex].OffsetX, AxisLabelOffsets[axisIndex].OffsetY);
        }

        public void ResetAxisLabelOffsets()
        {
            EnsureAxisLabelOffsets();
            for (int i = 0; i < 3; i++)
            {
                AxisLabelOffsets[i].OffsetX = 0f;
                AxisLabelOffsets[i].OffsetY = 0f;
                AxisLabelOffsets[i].IsCustom = false;
            }
        }


 /// <summary>
 /// Gets the label offset for the given atom.
 /// Returns defaults (0, -0.04) if the array hasn't been allocated yet.
 /// </summary>
        public (float offsetX, float offsetY) GetLabelOffset(int atomIndex)
        {
            if (atomIndex < 0 || atomIndex >= NumberOfAtoms) return (0f, -0.04f);
            if (LabelOffsets == null || LabelOffsets.Length != NumberOfAtoms)
            {
                InitializeLabelOffsets();
            }

            return (LabelOffsets[atomIndex].OffsetX, LabelOffsets[atomIndex].OffsetY);
        }

 /// <summary>
 /// Returns the raw LabelOffsets array (used by Draw for rendering).
 /// </summary>
        public AtomLabelOffset[] GetAllLabelOffsets()
        {
            return LabelOffsets;
        }

        #endregion
    }
}