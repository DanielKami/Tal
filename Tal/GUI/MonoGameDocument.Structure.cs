// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MonoGameDocument partial — structure load and molecule binding.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Tal.Cavity;
using Tal.DrawMethods;

namespace Tal.GUI
{
    public partial class MonoGameDocument
    {
        #region Structure Identification

        /// <summary>
        /// Sets the structure identification information.
        /// </summary>
        /// <param name="structureName">The structure name.</param>
        /// <param name="sourceFileName">The source file name.</param>
        public void SetStructureInfo(string structureName, string sourceFileName)
        {
            _structureName = structureName ?? "";
            _sourceFileName = sourceFileName ?? "";
        }

        /// <summary>
        /// Sets the structure identification information including full path.
        /// ⭐ NEW VERSION — also sets the full file path, which allows
        ///    unambiguously distinguish structures with the same name from different files.
        /// </summary>
        public void SetStructureInfo(string structureName, string sourceFileName, string sourceFilePath)
        {
            _structureName = structureName ?? "";
            _sourceFileName = sourceFileName ?? "";
            _sourceFilePath = sourceFilePath ?? "";
        }

        /// <summary>
        /// Checks if this document matches the given structure.
        /// ⭐ Compares FULL file PATH + structure name — so that
        ///    two different files with the same internal name (data_XXX) will NOT be
        ///    pomylone.
        /// </summary>
        public bool MatchesStructure(StructureData structure)
        {
            if (structure == null) return false;

            // 1) If we have a full path — it is the final decision
            if (!string.IsNullOrEmpty(_sourceFilePath) &&
                !string.IsNullOrEmpty(structure.FilePath))
            {
                bool sameFile = string.Equals(
                    _sourceFilePath,
                    structure.FilePath,
                    StringComparison.OrdinalIgnoreCase);

                bool sameName = string.Equals(
                    _structureName,
                    structure.Name,
                    StringComparison.Ordinal);

                return sameFile && sameName;
            }

            // 2) Fallback — if no path, use the old signature
            return MatchesStructure(structure.Name, structure.SourceFileName);
        }

        /// <summary>
        /// Old version — backward compatibility.
        /// Uses nazwy structure + nazwy file (without path).
        /// ⚠️ May give false hits when two different files share the same
        ///    the structure name inside the CIF.
        /// </summary>
        public bool MatchesStructure(string structureName, string sourceFileName)
        {
            if (string.IsNullOrEmpty(structureName)) return false;

            // Compare _structureName (set by SetStructureInfo)
            // with the given name — exact match, not Contains()
            if (string.Equals(_structureName, structureName, StringComparison.Ordinal))
            {
                // Additionally require file-name match if both are known
                if (string.IsNullOrEmpty(_sourceFileName) ||
                    string.IsNullOrEmpty(sourceFileName))
                {
                    return true;   // no file info — assume it is the same
                }

                return string.Equals(
                    _sourceFileName,
                    sourceFileName,
                    StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        /// <summary>Gets the structure name.</summary>
        public string GetStructureName() => _structureName;

        /// <summary>Gets the source file name.</summary>
        public string GetSourceFileName() => _sourceFileName;

        /// <summary>Gets the source file full path.</summary>
        public string GetSourceFilePath() => _sourceFilePath;

        /// <summary>Gets structure information as a string.</summary>
        public string GetStructureInfo()
        {
            if (_molecule == null) return "No structure loaded";
            return $"Structure: {_structureName}, Atoms: {_molecule.NumberOfAtoms}, " +
                   $"Source: {_sourceFileName}, Path: {_sourceFilePath}";
        }

        /// <summary>Checks if a structure is loaded.</summary>
        public bool HasStructure() => _molecule != null && _molecule.NumberOfAtoms > 0;

        #endregion

        #region Molecule Loading

        /// <summary>
        /// Loads a structure from StructureData.
        /// ⭐ Saves FULL PATH file — used do unambiguous identification.
        /// </summary>
        /// <param name="structure">The structure data to load.</param>
        public void LoadFromStructure(StructureData structure)
        {
            if (structure == null || !structure.IsLoaded)
            {
                System.Diagnostics.Trace.WriteLine("[MonoGameDocument] No structure to load");
                return;
            }

            System.Diagnostics.Trace.WriteLine(
                $"[MonoGameDocument] Loading structure: {structure.Name} " +
                $"(file: {structure.SourceFileName}, path: {structure.FilePath})");

            // ⭐ Remember identifying metadata BEFORE loading the molecule
            _structureName = structure.Name ?? "";
            _sourceFileName = structure.SourceFileName ?? "";
            _sourceFilePath = structure.FilePath ?? "";

            _molecule = new Molecule();
            _molecule.AsignXYZ(structure);

            try
            {
                // Subscribe to molecule shift changes
                _molecule.MoleculeShiftChanged += OnMoleculeShiftChanged;

                // Create Measure instance for this document
                Measure = new Measure(_molecule);
                System.Diagnostics.Trace.WriteLine($"[MonoGameDocument] Created Measure for {Text}, hash={Measure.GetHashCode()}");

                if (_gameWindow != null && !_gameWindow.IsDisposed)
                {
                    _gameWindow.LoadMolecule(_molecule);
                    _gameWindow.SetMeasure(Measure);
                    _gameWindow.RefreshView();
                }

                // ⭐ CavityDetector and CavityHelpers need no GPU — create immediately
                _cavityDetector = new CavityDetector(_molecule);
                _cavityHelpers = new CavityHelpers(_molecule);

                // ⭐ CavityRenderer wymaga GraphicsDevice — we create leniwie (lazy init)
                _cavityRenderer = null;
                _cavityRendererNeedsInit = true;

                System.Diagnostics.Trace.WriteLine(
                    $"[MonoGameDocument] Molecule loaded: {_molecule.NumberOfAtoms} atoms " +
                    $"(CavityRenderer deferred - lazy init)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[MonoGameDocument] Load error: {ex.Message}");
                System.Diagnostics.Trace.WriteLine($"[MonoGameDocument] Stack: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Loads a molecule directly.
        /// </summary>
        /// <param name="molecule">The molecule to load.</param>
        public void LoadMolecule(Molecule molecule)
        {
            _molecule = molecule;
            _gameWindow?.LoadMolecule(molecule);

            // ⭐ Zsynchronizuj polyhedrals z Draw
            if (_gameWindow != null && !_gameWindow.IsDisposed)
            {
                var polyhedrals = (Flags.Instance.Presentation == 4)
                    ? Polihedrals.VertexesList
                    : new List<Polihedrals.VertexStruct>();

                _gameWindow.SetPolyhedrals(polyhedrals);
            }
        }

        #endregion
    }
}