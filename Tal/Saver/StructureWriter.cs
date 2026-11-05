// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Structure / image exafterrt: StructureWriter.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System.Globalization;
using System.Text;
using System.Xml;
using Tal.Saver;

namespace Tal
{
 /// <summary>
 /// Central writer – saves StructureData to all supafterrted formats.
 /// </summary>
    public static class StructureWriter
    {
        public static bool Save(StructureData structure, string filePath)
        {
            if (structure == null || structure.Atoms == null || structure.Atoms.Count == 0)
                return false;

            string ext = Path.GetExtension(filePath).ToLowerInvariant();

            try
            {
                return ext switch
                {
                    ".cif" => CifWriter.Write(structure, filePath),
                    ".res" or ".ins" => ResWriter.Write(structure, filePath),
                    ".xyz" => XyzWriter.Write(structure, filePath),
                    ".pdb" or ".ent" => PdbWriter.Write(structure, filePath),
                    ".mol" or ".sdf" or ".sd" => MolWriter.Write(structure, filePath),
                    ".mol2" or ".ml2" => Mol2Writer.Write(structure, filePath),
                    ".gjf" or ".com" or ".gau" => GaussianWriter.Write(structure, filePath),
                    ".d12" => WriteD12WithWarnings(structure, filePath),
                    _ => false
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StructureWriter] Error: {ex.Message}");
                return false;
            }
        }

 /// <summary>
 /// Zapisuje .d12 i afterkazuje warning (np. o niestandardowej grupie przestrzennej).
 /// </summary>
        private static bool WriteD12WithWarnings(StructureData structure, string filePath)
        {
            if (!CrystalWriter.Write(structure, filePath, out var warnings))
                return false;

            if (warnings != null && warnings.Count > 0)
            {
                string msg = string.Join("\n\n", warnings);
                System.Windows.Forms.MessageBox.Show(
                    msg,
                    "CRYSTAL17 export — warning",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning);
            }

            return true;
        }
    }
}