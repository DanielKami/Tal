// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Molecule partial — atom data and core fields.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// Molecule partial — loading input data:
// - AsignXYZ: z StructureData (CIF)
// - AsignXYZFromRes: z ResData (RES/INS)
// - MapSfacTypeToAtomicNumber: fallback for RES without elementu
// - SymbolToAtomicNumber: konversion symbol → atomic number

using Microsoft.Xna.Framework;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Tal.Calculate;
using Tal.Parsers;

namespace Tal
{
    public partial class Molecule
    {
        #region CIF Loading

 /// <summary>
 /// Loads atom data from a StructureData object (CIF).
 /// Sets cell parameters, space group, symmetry operations,
 /// and builds AsymmetricPart. Then calls Presentation().
 /// </summary>
        public void AsignXYZ(StructureData structure)
        {
            Name = structure.Name;
            a = structure.a;
            b = structure.b;
            c = structure.c;
            alpha = MathHelperTal.ToRadians(structure.alpha);
            beta = MathHelperTal.ToRadians(structure.beta);
            gamma = MathHelperTal.ToRadians(structure.gamma);

            a_e = structure.a_Error;
            b_e = structure.b_Error;
            c_e = structure.c_Error;
            alpha_e = MathHelperTal.ToRadians(structure.alpha_Error);
            beta_e = MathHelperTal.ToRadians(structure.beta_Error);
            gamma_e = MathHelperTal.ToRadians(structure.gamma_Error);

            SetSpaceGroupInfo(structure.SpaceGroupName, structure.SpaceGroup);

            Flags.Instance.EnabledSymmetryOps = null; // all ops on for new structure
            foreach (var op in structure.SymmetryOperations)
            {
                if (!string.IsNullOrEmpty(op))
                    AddSymmetryOperationFromFile(op);
            }

            Matrix Amatrix = FractionaltoCarttesianMatrix(0, 0, 0);
            Matrix Nmatrix = TransformUStatToU();

            foreach (var structureAtom in structure.Atoms)
            {
                Vector3 fracErr = new Vector3(structureAtom.X_Error, structureAtom.Y_Error, structureAtom.Z_Error) * 0.001f;
                Vector3 cartErr = Vector3.Transform(fracErr, Amatrix);

                var simpleAtom = new AtomSimple
                {
                    name = structureAtom.Name,
                    AtomicNumber = structureAtom.AtomicNumber,
                    x = structureAtom.X,
                    y = structureAtom.Y,
                    z = structureAtom.Z,
                    x_e = cartErr.X,
                    y_e = cartErr.Y,
                    z_e = cartErr.Z,
                    occupancy = structureAtom.Occupancy,
                    iso = structureAtom.iso,
                    isAnisotropic = structureAtom.AdpType == "Uani",
                    disorder_group = structureAtom.DisorderGroup,
                    U11 = structureAtom.U11,
                    U22 = structureAtom.U22,
                    U33 = structureAtom.U33,
                    U12 = structureAtom.U12,
                    U13 = structureAtom.U13,
                    U23 = structureAtom.U23,
                    Transformation = Matrix.Identity
                };

                float isoValue = simpleAtom.iso;
                if (isoValue < 0.05f) isoValue = 0.05f;

                if (simpleAtom.isAnisotropic && simpleAtom.U11 > 0)
                {
                    simpleAtom.Ue.M11 = simpleAtom.U11;
                    simpleAtom.Ue.M22 = simpleAtom.U22;
                    simpleAtom.Ue.M33 = simpleAtom.U33;
                    simpleAtom.Ue.M23 = simpleAtom.Ue.M32 = simpleAtom.U23;
                    simpleAtom.Ue.M13 = simpleAtom.Ue.M31 = simpleAtom.U13;
                    simpleAtom.Ue.M12 = simpleAtom.Ue.M21 = simpleAtom.U12;

                    simpleAtom.Ue = Amatrix * Nmatrix * simpleAtom.Ue * Matrix.Transpose(Nmatrix) * Matrix.Transpose(Amatrix);
                    simpleAtom.Un = Eigen.main(ref simpleAtom.Ue);

                    if (simpleAtom.Ue.M11 < 0.001f) simpleAtom.Ue.M11 += 0.001f;
                    if (simpleAtom.Ue.M22 < 0.001f) simpleAtom.Ue.M22 += 0.001f;
                    if (simpleAtom.Ue.M33 < 0.001f) simpleAtom.Ue.M33 += 0.001f;
                    if (simpleAtom.Un.M11 < 0.001f) simpleAtom.Un.M11 += 0.001f;
                    if (simpleAtom.Un.M22 < 0.001f) simpleAtom.Un.M22 += 0.001f;
                    if (simpleAtom.Un.M33 < 0.001f) simpleAtom.Un.M33 += 0.001f;
                }
                else
                {
                    simpleAtom.Ue.M11 = simpleAtom.Ue.M22 = simpleAtom.Ue.M33 = isoValue;
                    simpleAtom.Ue.M12 = simpleAtom.Ue.M13 = simpleAtom.Ue.M23 = 0;
                    simpleAtom.Ue.M21 = simpleAtom.Ue.M31 = simpleAtom.Ue.M32 = 0;

                    simpleAtom.Un.M11 = simpleAtom.Un.M22 = simpleAtom.Un.M33 = (float)Math.Sqrt(isoValue);
                    simpleAtom.Un.M12 = simpleAtom.Un.M13 = simpleAtom.Un.M23 = 0;
                    simpleAtom.Un.M21 = simpleAtom.Un.M31 = simpleAtom.Un.M32 = 0;

                    if (simpleAtom.Ue.M11 < 0.001f) simpleAtom.Ue.M11 += 0.001f;
                    if (simpleAtom.Ue.M22 < 0.001f) simpleAtom.Ue.M22 += 0.001f;
                    if (simpleAtom.Ue.M33 < 0.001f) simpleAtom.Ue.M33 += 0.001f;
                    if (simpleAtom.Un.M11 < 0.001f) simpleAtom.Un.M11 += 0.001f;
                    if (simpleAtom.Un.M22 < 0.001f) simpleAtom.Un.M22 += 0.001f;
                    if (simpleAtom.Un.M33 < 0.001f) simpleAtom.Un.M33 += 0.001f;
                }

                simpleAtom.Ue.M44 = 1;
                simpleAtom.Un.M44 = 1;

                AsymmetricPart.Add(simpleAtom);
            }

            Debug.WriteLine($"[Molecule] AsymmetricPart.Count: {AsymmetricPart.Count}");

            if (AsymmetricPart.Count > 0)
            {
                var first = AsymmetricPart[0];
                Debug.WriteLine($"[Molecule] First atom: {first.name} err=({first.x_e:F6}, {first.y_e:F6}, {first.z_e:F6})");
                Debug.WriteLine($"[Molecule] Ue.M11={first.Ue.M11:F6}, Un.M11={first.Un.M11:F6}, isAnisotropic={first.isAnisotropic}");
            }

            Presentation(Flags.Instance.GenerateEquvalents, Flags.Instance.Presentation);

 // ⭐ Copy refinement / experiment metadata from StructureData
            CopyMetadataFromStructure(structure);
        }

        #endregion

        #region RES/INS Loading

 /// <summary>
 /// Loads atom data from a RES/INS file.
 ///
 /// KEY: Atomic number determined from ELEMENT SYMBOL (resAtom.element,
 /// setionego przez ResParser na afterdstawie maafterwania SFAC), a NIE z
 /// sztywnej tabeli MapSfacTypeToAtomicNumber. Thanks to this plik z
 /// non-standard SFAC order (e.g. "C H O" instead of "C H N O")
 /// mapuje correctly.
 /// </summary>
        public void AsignXYZFromRes(ResData resData)
        {
            Matrix Amatrix = FractionaltoCarttesianMatrix(0, 0, 0);
            Matrix Nmatrix = TransformUStatToU();

            AsymmetricPart.Clear();

            for (int i = 0; i < resData.atom.Count; i++)
            {
                AtomSimple atom = new AtomSimple();
                var resAtom = resData.atom[i];

 // ═══════════════════════════════════════════════════════════
 // ⭐ Atomic number z SYMBOLU ELEMENTU (nie z SFAC type!)
 // ═══════════════════════════════════════════════════════════
                int atomicNumber = 6;   // C — ultimate fallback

                if (!string.IsNullOrEmpty(resAtom.element))
                {
                    atomicNumber = SymbolToAtomicNumber(resAtom.element);
                    if (atomicNumber <= 0)
                        atomicNumber = MapSfacTypeToAtomicNumber(resAtom.type);
                }
                else
                {
 // Fallback for oldch RES without SFAC
                    atomicNumber = MapSfacTypeToAtomicNumber(resAtom.type);
                }

                Debug.WriteLine($"[AsignXYZFromRes] atom='{resAtom.name}' " +
                                $"sfacType={resAtom.type} element='{resAtom.element}' " +
                                $"→ atomicNumber={atomicNumber}");

                float isoValue = resAtom.iso;
                if (isoValue < 0.001f) isoValue = 0.05f;

                atom.name = resAtom.name;
                atom.AtomicNumber = atomicNumber;
                atom.x = resAtom.x;
                atom.y = resAtom.y;
                atom.z = resAtom.z;

 // Order: first copy fractional errors from RES,
 // then convert na Cartesian.
                atom.x_e = resAtom.x_e;
                atom.y_e = resAtom.y_e;
                atom.z_e = resAtom.z_e;

                Vector3 fracErr = new Vector3(atom.x_e, atom.y_e, atom.z_e);
                Vector3 cartErr = Vector3.Transform(fracErr, Amatrix);

                atom.x_e = cartErr.X;
                atom.y_e = cartErr.Y;
                atom.z_e = cartErr.Z;

                atom.iso = isoValue;
                CalculateError(ref atom.x_e, ref atom.y_e, ref atom.z_e);

                atom.occupancy = resAtom.occ;
                atom.Transformation = Matrix.Identity;
                atom.disorder_group = resAtom.part_number;
                atom.isAnisotropic = resAtom.has_aniso;

                if (resAtom.has_aniso)
                {
                    atom.Ue.M11 = resAtom.u11;
                    atom.Ue.M22 = resAtom.u22;
                    atom.Ue.M33 = resAtom.u33;
                    atom.Ue.M23 = atom.Ue.M32 = resAtom.u23;
                    atom.Ue.M13 = atom.Ue.M31 = resAtom.u13;
                    atom.Ue.M12 = atom.Ue.M21 = resAtom.u12;

                    atom.Ue = Amatrix * Nmatrix * atom.Ue * Matrix.Transpose(Nmatrix) * Matrix.Transpose(Amatrix);
                    atom.Un = Eigen.main(ref atom.Ue);

                    if (atom.Ue.M11 < 0.01f) atom.Ue.M11 += 0.01f;
                    if (atom.Ue.M22 < 0.01f) atom.Ue.M22 += 0.01f;
                    if (atom.Ue.M33 < 0.01f) atom.Ue.M33 += 0.01f;
                    if (atom.Un.M11 < 0.01f) atom.Un.M11 += 0.01f;
                    if (atom.Un.M22 < 0.01f) atom.Un.M22 += 0.01f;
                    if (atom.Un.M33 < 0.01f) atom.Un.M33 += 0.01f;
                }
                else
                {
                    atom.Ue.M11 = atom.Ue.M22 = atom.Ue.M33 = isoValue;
                    atom.Ue.M12 = atom.Ue.M13 = atom.Ue.M23 = 0;
                    atom.Ue.M21 = atom.Ue.M31 = atom.Ue.M32 = 0;

                    atom.Un.M11 = atom.Un.M22 = atom.Un.M33 = (float)Math.Sqrt(isoValue);
                    atom.Un.M12 = atom.Un.M13 = atom.Un.M23 = 0;
                    atom.Un.M21 = atom.Un.M31 = atom.Un.M32 = 0;
                }

                atom.Ue.M44 = 1;
                atom.Un.M44 = 1;

                AsymmetricPart.Add(atom);
            }

 // ⭐ Copy refinement / experiment metadata from ResData
            CopyMetadataFromRes(resData);
        }

        #endregion

        #region SFAC Fallback

 /// <summary>
 /// WARNING: This method assumes STANDARD SHELX order:
 /// 1=C, 2=H, 3=N, 4=O, 5=S, 6=P, 7=F, 8=Cl, 9=Br, 10=I
 ///
 /// If the RES file has a different order (e.g. "C H O"), this method will give WRONG results.
 ///
 /// Preferuj resAtom.element (z ResParser) → SymbolToAtomicNumber.
 /// Use this method ONLY as a last-resort fallback when element is empty.
 /// </summary>
        private int MapSfacTypeToAtomicNumber(int sfacType)
        {
            switch (sfacType)
            {
                case 1: return 6;    // C
                case 2: return 1;    // H
                case 3: return 7;    // N
                case 4: return 8;    // O
                case 5: return 16;   // S
                case 6: return 15;   // P
                case 7: return 9;    // F
                case 8: return 17;   // Cl
                case 9: return 35;   // Br
                case 10: return 53;  // I
                default:
                    if (sfacType > 0 && sfacType <= 118)
                        return sfacType;
                    return 6;
            }
        }

        #endregion

        #region Symbol → Atomic Number

 /// <summary>
 /// Konversion symbolu elementu (np. "O", "Cl", "C1") na atomic number.
 /// Ignoruje cyfry, spacje, underscores w symbolu.
 /// </summary>
        public static int SymbolToAtomicNumber(string symbol)
        {
            if (string.IsNullOrEmpty(symbol)) return 0;

 // Remove digits, spaces, underscores, hyphens
            string clean = Regex.Replace(symbol, @"[\d\s\-_]", "").Trim();

            if (string.IsNullOrEmpty(clean)) return 0;

 // First try the central VdwParameters table
            int z = VdwParameters.GetAtomicNumber(clean);
            if (z > 0) return z;

 // Fallback — pierwsze 2 znaki (np. "Clx" → "Cl")
            if (clean.Length >= 2)
            {
                z = VdwParameters.GetAtomicNumber(clean.Substring(0, 2));
                if (z > 0) return z;
            }

 // Fallback — pierwszy znak (np. "Cx" → "C")
            if (clean.Length >= 1)
            {
                z = VdwParameters.GetAtomicNumber(clean.Substring(0, 1));
                if (z > 0) return z;
            }

            return 0;
        }

        #endregion
    }
}