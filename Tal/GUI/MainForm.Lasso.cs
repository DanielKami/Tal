// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MainForm partial — lasso selection mode.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;

namespace Tal
{
    public partial class MainForm
    {
        #region Lasso

        public void CloseLasso(int viewportWidth, int viewportHeight)
        {
            if (!_isLassoActive || Lasso.VertexList.Count < 3)
            {
                Lasso.VertexList.Clear();
                var camera_ = GetActiveCamera();
                if (camera_ != null)
                {
                    camera_.flag_lasso = false;
                }
                return;
            }

            var molecule = GetActiveMolecule();
            var camera = GetActiveCamera();
            var doc = GetActiveDocument();

            if (molecule == null || camera == null || doc == null)
            {
                Lasso.VertexList.Clear();
                camera.flag_lasso = false;
                return;
            }

            Matrix fullTransformMatrix = camera.Scale * camera.Rotate * camera.View *
                                         camera.Translate * camera.Projection;

            // ⭐ Check whether atoms are already selected
            bool hasExistingSelection = false;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (molecule.atom[i].selected)
                {
                    hasExistingSelection = true;
                    break;
                }
            }

            // ⭐ Rule:
            //   - are selected → append (add new ones to existing)
            //   - no selected → replace (zacznij od new)
            bool append = hasExistingSelection;

            Lasso.CloseAndFillGaps();
            Lasso.MouseLasso(molecule, viewportWidth, viewportHeight, fullTransformMatrix, append);

            SetStatus(
                append
                    ? $"Lasso added to selection — {molecule.NumberSelected} atoms total"
                    : $"Lasso selected {molecule.NumberSelected} atoms",
                $"Structures: {StructureStorage.Instance.StructureCount}", "🔲");

            Lasso.VertexList.Clear();
            camera.flag_lasso = false;
            camera.flag_release = false;
            camera.MarkChanged();
            doc.RefreshView();
        }

        #endregion
    }
}
