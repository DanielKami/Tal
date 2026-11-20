// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Distance / angle / torsion measurement state.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// FIXED: error propagation, single-digit crystallographic rounding

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Tal.Core.PowderDiffraction;

namespace Tal
{
 /// <summary>
 /// Provides measurement functionality for molecular structures.
 /// Supafterrts distance, angle, and torsion angle measurements with
 /// crystallographic error propagation.
 /// </summary>
    public class Measure
    {
        #region Nested Types

 /// <summary>
 /// Stores custom offset values for measurement labels.
 /// </summary>
        public class MeasurementLabelOffset
        {
            public float OffsetX { get; set; } = 0f;
            public float OffsetY { get; set; } = 0f;
            public bool IsCustom { get; set; } = false;
        }

 /// <summary>
 /// Represents a single measurement (distance, angle, torsion, or atom-plane).
 /// </summary>
        public struct Measurement
        {
 /// <summary>Measurement type: 1=distance, 2=angle, 3=torsion, 4=atom→plane.</summary>
            public int type;

            public int Atom1;
            public int Atom2;
            public int Atom3;
            public int Atom4;

            public float value;
            public float error;

            public string label;

            public Vector3 position1;
            public Vector3 position2;
            public Vector3 position3;
            public Vector3 position4;

            public bool show;

            public Vector3 planeNormal;
            public float planeD;
            public Guid planeId;
            public Vector3 projectionPoint;
        }

        #endregion

        #region Pending Selection (for multi-click measurements)

        public int PendingAtom1 { get; set; } = -1;
        public CrystalPlane PendingPlane1 { get; set; } = null;
        public Vector3 PendingPosition1 { get; set; } = Vector3.Zero;

        public void ResetPendingSelection()
        {
            PendingAtom1 = -1;
            PendingPlane1 = null;
            PendingPosition1 = Vector3.Zero;
        }

        #endregion

        #region Private Fields

        private Molecule _molecule;

 /// <summary>
 /// Fallback cell error fraction, used ONLY when atomic Coordinate_error
 /// is unavailable (all zero). In normal crystallographic data the
 /// atomic esds do NOT contain the cell-error contribution; it is propagated separately (see CellContribution).
 /// </summary>
        private float _cellErrorFraction = 0f;

        #endregion

        #region Public Fields

        public List<Measurement> measurement = new List<Measurement>();
        public string Message = "";

        public Dictionary<int, MeasurementLabelOffset> MeasurementLabelOffsets
            = new Dictionary<int, MeasurementLabelOffset>();

        #endregion

        #region Debug Properties

        public static System.Drawing.Point LastClickPosition { get; set; } = new System.Drawing.Point(-1, -1);
        public static bool ShowDebugCross { get; set; } = false;

        #endregion

        #region Constructor

        public Measure(Molecule molecule)
        {
            _molecule = molecule;
        }

        #endregion

        #region Cell Error Update

        private void UpdateCellError()
        {
            if (_molecule == null)
            {
                _cellErrorFraction = 0.002f;
                return;
            }

            float relErr = 0f;
            int count = 0;

            if (_molecule.a > 0 && _molecule.a_e > 0) { relErr += _molecule.a_e / _molecule.a; count++; }
            if (_molecule.b > 0 && _molecule.b_e > 0) { relErr += _molecule.b_e / _molecule.b; count++; }
            if (_molecule.c > 0 && _molecule.c_e > 0) { relErr += _molecule.c_e / _molecule.c; count++; }

            if (_molecule.alpha > 0 && _molecule.alpha_e > 0) { relErr += _molecule.alpha_e / _molecule.alpha; count++; }
            if (_molecule.beta > 0 && _molecule.beta_e > 0) { relErr += _molecule.beta_e / _molecule.beta; count++; }
            if (_molecule.gamma > 0 && _molecule.gamma_e > 0) { relErr += _molecule.gamma_e / _molecule.gamma; count++; }

            _cellErrorFraction = (count > 0) ? relErr / count : 0.002f;
            _cellErrorFraction = Math.Clamp(_cellErrorFraction, 0.0001f, 0.02f);

            System.Diagnostics.Debug.WriteLine(
                $"[Measure] Cell error fraction: {_cellErrorFraction:F4} ({_cellErrorFraction * 100:F2}%)");
        }

        #endregion

        #region Crystallographic Formatting

 /// <summary>
 /// IUCr-compliant rounding with a SINGLE significant digit of the error.
 /// Examples:
 /// value=1.234567, error=0.0034 -> 1.235(3)
 /// value=1.234567, error=0.012 -> 1.23(1)
 /// value=1.234567, error=0.12 -> 1.2(1)
 /// value=1.234567, error=0.9 -> 1(9)
 /// </summary>
        public static string FormatCrystallographic(double value, float error)
        {
            if (float.IsNaN(error) || float.IsInfinity(error) || error <= 0f)
                return value.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);

            int decimals, errDigit;
            NormalizeError(error, out decimals, out errDigit);

            double roundedValue = Math.Round(value, decimals, MidpointRounding.AwayFromZero);
            string fmt = "F" + decimals;
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                 "{0}({1})", roundedValue.ToString(fmt), errDigit);
        }

 /// <summary>
 /// Crystallographic rounding to a SINGLE significant digit of the error.
 /// Rounds <paramref name="value"/> accordingly and returns the error digit.
 /// </summary>
        public static int LErr(ref double value, float error)
        {
            if (float.IsNaN(error) || float.IsInfinity(error) || error <= 0f)
            {
                value = Math.Round(value, 4, MidpointRounding.AwayFromZero);
                return 0;
            }

            int decimals, errDigit;
            NormalizeError(error, out decimals, out errDigit);

            value = Math.Round(value, decimals, MidpointRounding.AwayFromZero);
            return errDigit;
        }

 /// <summary>
 /// Returns only the value formatted to the number of decimal places
 /// implied by the error, without the parenthesised digit.
 /// </summary>
        public static string FormatValueOnly(double value, float error)
        {
            if (float.IsNaN(error) || float.IsInfinity(error) || error <= 0f)
                return value.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);

            int decimals, errDigit;
            NormalizeError(error, out decimals, out errDigit);

            double rounded = Math.Round(value, decimals, MidpointRounding.AwayFromZero);
            return rounded.ToString("F" + decimals, System.Globalization.CultureInfo.InvariantCulture);
        }

 /// <summary>
 /// Reduces the error to a SINGLE significant digit in [1..9] and returns
 /// the corresafternding number of decimal places for the value.
 ///
 /// error = 0.0034 -> decimals=3, digit=3 (value shown as X.XXX)
 /// error = 0.012 -> decimals=2, digit=1 (value shown as X.XX)
 /// error = 0.12 -> decimals=1, digit=1 (value shown as X.X)
 /// error = 0.9 -> decimals=0, digit=9
 /// error = 9.0 -> decimals=0, digit=9
 /// error = 12.0 -> decimals=0, digit=1 (value shown as integer)
 /// </summary>
        private static void NormalizeError(float error, out int decimals, out int errDigit)
        {
            if (error <= 0f) { decimals = 4; errDigit = 0; return; }

            int exp = (int)Math.Floor(Math.Log10(error));
            double scaled = error / Math.Pow(10, exp);   // in [1, 10)
            int digit = (int)Math.Round(scaled, MidpointRounding.AwayFromZero);

            if (digit >= 10)
            {
                digit = 1;
                exp += 1;
            }
            if (digit < 1) digit = 1;

            errDigit = digit;
            decimals = -exp;
            if (decimals < 0) decimals = 0;
        }

        #endregion

        #region Label Offset Management

        public void ClearAllLabelOffsets()
        {
            MeasurementLabelOffsets.Clear();
        }

        public void ClearLabelOffset(int index)
        {
            if (MeasurementLabelOffsets.ContainsKey(index))
                MeasurementLabelOffsets.Remove(index);
        }

        #endregion

        #region Mouse Selection

        public void MouseSelect(Molecule molecule, int Width, int Height,
            Matrix fullTransformMatrix, System.Drawing.Point MousePosition)
        {
            LastClickPosition = MousePosition;
            ShowDebugCross = true;

            const int TOLERANCE = 10;

            for (int i = 0; i < molecule.NumberOfAtoms; ++i)
            {
                if (molecule.atom[i].style == 10) continue;

                Vector3 pos = molecule.atom[i].Coordinate;

                Matrix world = Matrix.CreateTranslation(pos);
                Matrix worldViewProjectionMatrix = world * fullTransformMatrix;

                float w = worldViewProjectionMatrix.M44;
                if (w <= 0) continue;

                int modelX = (int)(Width / 2 * (1.0 + worldViewProjectionMatrix.M41 / w));
                int modelY = (int)(Height / 2 * (1.0 - worldViewProjectionMatrix.M42 / w));

                int dx = Math.Abs(modelX - MousePosition.X);
                int dy = Math.Abs(modelY - MousePosition.Y);

                if (dx < TOLERANCE && dy < TOLERANCE)
                {
                    if (!molecule.atom[i].selected)
                    {
                        molecule.atom[i].selected = true;
                        molecule.NumberSelected++;
                        molecule.atom[i].SelectionOrder = molecule.NumberSelected;
                    }
                    else
                    {
                        molecule.atom[i].selected = false;
                        molecule.NumberSelected--;
                        molecule.atom[i].SelectionOrder = 0;
                    }
                    break;
                }
            }
        }

        #endregion

        #region Measurement Methods

        public bool MeasureDistance()
        {
            if (_molecule.NumberSelected > 2)
            {
                Message = $"Too many selected atoms! ({_molecule.NumberSelected})";
                return false;
            }

            if (_molecule.NumberSelected < 2)
            {
                Message = $"Not enough atoms selected! ({_molecule.NumberSelected})";
                return false;
            }

            int atom1 = -1, atom2 = -1;
            Vector3 pos1 = Vector3.Zero, pos2 = Vector3.Zero;
            string symbol1 = "", symbol2 = "";

            for (int j = 0; j < _molecule.NumberOfAtoms; ++j)
            {
                if (_molecule.atom[j].selected)
                {
                    if (_molecule.atom[j].SelectionOrder == 1)
                    {
                        atom1 = j;
                        pos1 = _molecule.atom[j].Coordinate;
                        symbol1 = _molecule.atom[j].name;
                    }
                    else if (_molecule.atom[j].SelectionOrder == 2)
                    {
                        atom2 = j;
                        pos2 = _molecule.atom[j].Coordinate;
                        symbol2 = _molecule.atom[j].name;
                    }
                }
            }

            if (atom1 < 0 || atom2 < 0)
            {
                Message = "Invalid atom selection";
                return false;
            }

            double distance = Vector3.Distance(pos1, pos2);

            float error = DistanceErrorCore(
                MakePosErr(_molecule.atom[atom1]),
                MakePosErr(_molecule.atom[atom2]));

            int errorDigit = LErr(ref distance, error);
            Message = $"Distance {symbol1}-{symbol2}: " +
                      $"{FormatValueOnly(distance, error)}({errorDigit}) Å";

            Measurement m = new Measurement();
            m.type = 1;
            m.Atom1 = atom1;
            m.Atom2 = atom2;
            m.Atom3 = -1;
            m.Atom4 = -1;
            m.value = (float)distance;
            m.error = error;
            m.label = $"d{measurement.Count + 1}";
            m.position1 = pos1;
            m.position2 = pos2;
            m.position3 = Vector3.Zero;
            m.position4 = Vector3.Zero;
            m.show = true;
            measurement.Add(m);

            return true;
        }

        public bool MeasureAngle()
        {
            if (_molecule.NumberSelected > 3)
            {
                Message = $"Too many selected atoms! ({_molecule.NumberSelected})";
                return false;
            }

            if (_molecule.NumberSelected < 3)
            {
                Message = $"Not enough atoms selected! ({_molecule.NumberSelected})";
                return false;
            }

            int atom1 = -1, atom2 = -1, atom3 = -1;
            Vector3 v1 = Vector3.Zero, v2 = Vector3.Zero, v3 = Vector3.Zero;
            string symbol1 = "", symbol2 = "", symbol3 = "";

            for (int j = 0; j < _molecule.NumberOfAtoms; ++j)
            {
                if (_molecule.atom[j].selected)
                {
                    if (_molecule.atom[j].SelectionOrder == 1)
                    {
                        atom1 = j;
                        v1 = _molecule.atom[j].Coordinate;
                        symbol1 = _molecule.atom[j].name;
                    }
                    else if (_molecule.atom[j].SelectionOrder == 2)
                    {
                        atom2 = j;
                        v2 = _molecule.atom[j].Coordinate;
                        symbol2 = _molecule.atom[j].name;
                    }
                    else if (_molecule.atom[j].SelectionOrder == 3)
                    {
                        atom3 = j;
                        v3 = _molecule.atom[j].Coordinate;
                        symbol3 = _molecule.atom[j].name;
                    }
                }
            }

            Vector3 a = v1 - v2;
            Vector3 b = v3 - v2;

            float lenA = a.Length();
            float lenB = b.Length();

            if (lenA < 0.0001f || lenB < 0.0001f)
            {
                Message = "Atoms are too close!";
                return false;
            }

            double dot = Vector3.Dot(a, b);
            double cosAngle = dot / (lenA * lenB);
            cosAngle = Math.Clamp(cosAngle, -1.0, 1.0);
            double angleRad = Math.Acos(cosAngle);
            double angleDeg = MathHelperTal.ToDegrees((float)angleRad);

            float error = AngleErrorCore(
                MakePosErr(_molecule.atom[atom1]),
                MakePosErr(_molecule.atom[atom2]),
                MakePosErr(_molecule.atom[atom3]));

            int errorDigit = LErr(ref angleDeg, error);
            Message = $"Angle {symbol1}-{symbol2}-{symbol3}: " +
                      $"{FormatValueOnly(angleDeg, error)}({errorDigit})°";

            Measurement m = new Measurement();
            m.type = 2;
            m.Atom1 = atom1;
            m.Atom2 = atom2;
            m.Atom3 = atom3;
            m.Atom4 = -1;
            m.value = (float)angleDeg;
            m.error = error;
            m.label = $"a{measurement.Count + 1}";
            m.position1 = v1;
            m.position2 = v2;
            m.position3 = v3;
            m.position4 = Vector3.Zero;
            m.show = true;
            measurement.Add(m);

            return true;
        }

        public bool MeasureTorsion()
        {
            if (_molecule.NumberSelected > 4)
            {
                Message = $"Too many selected atoms! ({_molecule.NumberSelected})";
                return false;
            }

            if (_molecule.NumberSelected < 4)
            {
                Message = $"Not enough atoms selected! ({_molecule.NumberSelected})";
                return false;
            }

            int atom1 = -1, atom2 = -1, atom3 = -1, atom4 = -1;
            Vector3 V1 = Vector3.Zero, V2 = Vector3.Zero, V3 = Vector3.Zero, V4 = Vector3.Zero;
            string symbol1 = "", symbol2 = "", symbol3 = "", symbol4 = "";

            for (int j = 0; j < _molecule.NumberOfAtoms; ++j)
            {
                if (_molecule.atom[j].selected)
                {
                    if (_molecule.atom[j].SelectionOrder == 1)
                    {
                        atom1 = j;
                        V1 = _molecule.atom[j].Coordinate;
                        symbol1 = _molecule.atom[j].name;
                    }
                    else if (_molecule.atom[j].SelectionOrder == 2)
                    {
                        atom2 = j;
                        V2 = _molecule.atom[j].Coordinate;
                        symbol2 = _molecule.atom[j].name;
                    }
                    else if (_molecule.atom[j].SelectionOrder == 3)
                    {
                        atom3 = j;
                        V3 = _molecule.atom[j].Coordinate;
                        symbol3 = _molecule.atom[j].name;
                    }
                    else if (_molecule.atom[j].SelectionOrder == 4)
                    {
                        atom4 = j;
                        V4 = _molecule.atom[j].Coordinate;
                        symbol4 = _molecule.atom[j].name;
                    }
                }
            }

            double torsionRad = CalculateTorsionAngleRad(V1, V2, V3, V4);
            double torsionDeg = MathHelperTal.ToDegrees((float)torsionRad);

            float error = TorsionErrorCore(new[]
            {
                MakePosErr(_molecule.atom[atom1]),
                MakePosErr(_molecule.atom[atom2]),
                MakePosErr(_molecule.atom[atom3]),
                MakePosErr(_molecule.atom[atom4])
            });

            int errorDigit = LErr(ref torsionDeg, error);
            Message = $"Torsion {symbol1}-{symbol2}-{symbol3}-{symbol4}: " +
                      $"{FormatValueOnly(torsionDeg, error)}({errorDigit})°";

            Measurement m = new Measurement();
            m.type = 3;
            m.Atom1 = atom1;
            m.Atom2 = atom2;
            m.Atom3 = atom3;
            m.Atom4 = atom4;
            m.value = (float)torsionDeg;
            m.error = error;
            m.label = $"t{measurement.Count + 1}";
            m.position1 = V1;
            m.position2 = V2;
            m.position3 = V3;
            m.position4 = V4;
            m.show = true;
            measurement.Add(m);

            return true;
        }

 /// <summary>
 /// Measures the perpendicular distance from the currently selected atom
 /// to the given plane.
 /// </summary>
        public bool MeasureAtomToPlane(CrystalPlane plane, Vector3 moleculeShift)
        {
            if (plane == null)
            {
                Message = "No reference plane selected";
                return false;
            }

            if (_molecule.NumberSelected != 1)
            {
                Message = $"Select exactly ONE atom (currently {_molecule.NumberSelected})";
                return false;
            }

            int atomIdx = -1;
            for (int j = 0; j < _molecule.NumberOfAtoms; j++)
            {
                if (_molecule.atom[j].selected) { atomIdx = j; break; }
            }

            if (atomIdx < 0) { Message = "No atom selected"; return false; }

            return MeasureAtomToPlane(atomIdx, plane, moleculeShift);
        }

 /// <summary>
 /// Measures the perpendicular distance from the given atom to the given plane.
 /// </summary>
        public bool MeasureAtomToPlane(int atomIdx, CrystalPlane plane, Vector3 moleculeShift)
        {
            if (plane == null) { Message = "No reference plane"; return false; }
            if (atomIdx < 0 || atomIdx >= _molecule.NumberOfAtoms)
            {
                Message = "Invalid atom index";
                return false;
            }

            Vector3 shift = moleculeShift;
            Vector3 atomRaw = _molecule.atom[atomIdx].Coordinate + shift;

            Vector3 n = new Vector3((float)plane.A, (float)plane.B, (float)plane.C);
            float nLen = n.Length();
            if (nLen < 1e-6f) { Message = "Plane normal is degenerate"; return false; }
            n /= nLen;
            float d = (float)plane.D / nLen;

            float s = Vector3.Dot(n, atomRaw) + d;
            float distance = Math.Abs(s);
            Vector3 projRaw = atomRaw - s * n;

            Vector3 atomShifted = atomRaw - shift;
            Vector3 projShifted = projRaw - shift;

            UpdateCellError();
            // sigma of the atom position ALONG the plane normal: sqrt(nᵀ C n)
            float posErr;
            Cov3 atomCov = _molecule.atom[atomIdx].Coordinate_cov;
            if (!atomCov.IsZero)
                posErr = (float)Math.Sqrt(atomCov.Quadratic(n));
            else
                posErr = CalculateVectorError(_molecule.atom[atomIdx].Coordinate_error);
            // mean plane: add the position errors of the atoms that define the plane
            // (and treat the measured atom as one variable when it belongs to the plane)
            if (posErr > 0f && plane.Type == CrystalPlaneType.MeanPlane &&
                MakePosErr(_molecule.atom[atomIdx]).Real)
            {
                double pv = MeanPlanePositionVariance(atomIdx, plane, shift, atomRaw, out bool okPlane);
                if (okPlane) posErr = (float)Math.Sqrt(pv);
            }

            float error;
            if (posErr > 0f)
            {
                error = posErr;
                // real refined esds -> add the unit-cell esd contribution in quadrature
                if (MakePosErr(_molecule.atom[atomIdx]).Real)
                {
                    double cellC = PlaneCellContribution(atomRaw, plane, shift);
                    error = (float)Math.Sqrt((double)posErr * posErr + cellC * cellC);
                }
            }
            else
            {
                error = distance * _cellErrorFraction;   // no esds in the file: rough fallback
            }

            double distVal = distance;
            int errDigit = LErr(ref distVal, error);
            Message = $"Distance {_molecule.atom[atomIdx].name} → plane ({plane.H}{plane.K}{plane.L}): " +
                      $"{FormatValueOnly(distVal, error)}({errDigit}) Å";

            var m = new Measurement
            {
                type = 4,
                Atom1 = atomIdx,
                Atom2 = -1,
                Atom3 = -1,
                Atom4 = -1,
                value = distance,
                error = error,
                label = $"p{measurement.Count + 1}",
                position1 = atomShifted,
                position2 = projShifted,
                position3 = Vector3.Zero,
                position4 = Vector3.Zero,
                planeNormal = n,
                planeD = d,
                planeId = plane.Id,
                projectionPoint = projShifted,
                show = true
            };

            measurement.Add(m);
            return true;
        }

        #endregion

        #region Measurement Visibility

        public void HideMeasurement(int index)
        {
            if (index >= 0 && index < measurement.Count)
            {
                var m = measurement[index];
                m.show = false;
                measurement[index] = m;
            }
        }

        public void ShowMeasurement(int index)
        {
            if (index >= 0 && index < measurement.Count)
            {
                var m = measurement[index];
                m.show = true;
                measurement[index] = m;
            }
        }

        public void ToggleMeasurement(int index)
        {
            if (index >= 0 && index < measurement.Count)
            {
                var m = measurement[index];
                m.show = !m.show;
                measurement[index] = m;
            }
        }

        #endregion

        #region Measurement Management

        public void ClearMeasurements()
        {
            measurement.Clear();
            MeasurementLabelOffsets.Clear();
            Message = "";
        }

        public string GetMeasurementSummary()
        {
            if (measurement.Count == 0)
                return "No measurements";

            string summary = $"Measurements: {measurement.Count}\n";
            for (int i = 0; i < measurement.Count; i++)
            {
                var m = measurement[i];
                double val = m.value;
                int err = LErr(ref val, m.error);
                string valStr = FormatValueOnly(val, m.error);

                switch (m.type)
                {
                    case 1:
                        summary += $"  {i + 1}. Distance: {valStr}({err}) Å\n";
                        break;
                    case 2:
                        summary += $"  {i + 1}. Angle: {valStr}({err})°\n";
                        break;
                    case 3:
                        summary += $"  {i + 1}. Torsion: {valStr}({err})°\n";
                        break;
                    case 4:
                        summary += $"  {i + 1}. Atom→Plane: {valStr}({err}) Å\n";
                        break;
                }
            }
            return summary;
        }

        public string GetAllMeasurements()
        {
            if (measurement.Count == 0)
                return "No measurements";

            string result = "";
            for (int i = 0; i < measurement.Count; i++)
            {
                var m = measurement[i];
                double val = m.value;
                int err = LErr(ref val, m.error);
                string valStr = FormatValueOnly(val, m.error);

                switch (m.type)
                {
                    case 1:
                        result += $"{i + 1}. Distance: {valStr}({err}) Å\n";
                        break;
                    case 2:
                        result += $"{i + 1}. Angle: {valStr}({err})°\n";
                        break;
                    case 3:
                        result += $"{i + 1}. Torsion: {valStr}({err})°\n";
                        break;
                    case 4:
                        result += $"{i + 1}. Atom→Plane: {valStr}({err}) Å\n";
                        break;
                }
            }
            return result;
        }

        #endregion

        #region Error Calculation Methods

        // ─────────────────────────────────────────────────────────────────────
        // Error propagation model
        //   * every atom carries a full Cartesian position covariance C (Cov3):
        //     fractional esds are independent, Cartesian ones are correlated for
        //     oblique cells, and C is rotated together with symmetry operations;
        //   * the geometric quantity q (d, theta, tau) is linearised:
        //         var(q) = sum_atoms  g_a^T C_a g_a      g_a = dq/dr_a
        //   * the unit-cell esds (a, b, c, alpha, beta, gamma) are propagated
        //     separately at fixed fractional coordinates and added in quadrature:
        //         var_cell(q) = sum_k ( dq/dp_k * sigma(p_k) )^2
        //     (cell parameters treated as independent, as in SHELXL / PLATON).
        // ─────────────────────────────────────────────────────────────────────

        private struct PosErr
        {
            public Vector3 Pos;
            /// <summary>Position covariance (Å²).</summary>
            public Cov3 Cov;
            /// <summary>True when Cov comes from real refined esds (not the fallback guess).</summary>
            public bool Real;
        }

        private PosErr MakePosErr(in Molecule.Atom a)
        {
            var pe = new PosErr { Pos = a.Coordinate };

            if (!a.Coordinate_cov.IsZero)
            {
                pe.Cov = a.Coordinate_cov;
                pe.Real = true;
            }
            else if (CalculateVectorError(a.Coordinate_error) > 0f)
            {
                pe.Cov = Cov3.FromDiagonal(a.Coordinate_error);
                pe.Real = true;
            }
            else
            {
                pe.Cov = Cov3.FromDiagonal(EffectivePositionalError(a.Coordinate, a.Coordinate_error, a.Coordinate));
                pe.Real = false;
            }
            return pe;
        }

        private Vector3 EffectivePositionalError(Vector3 pos, Vector3 coordErr, Vector3 cellRefPos)
        {
            float mag = CalculateVectorError(coordErr);
            if (mag > 0f)
                return coordErr;

            UpdateCellError();
            float e = _cellErrorFraction * cellRefPos.Length();
            if (e <= 0f) e = 0.001f;
            return new Vector3(e, e, e);
        }

        // ── public API (used by Measure itself and by StructureInfoWindow) ──

        /// <summary>Bond-length esd (Å) between two atoms.</summary>
        public float DistanceError(in Molecule.Atom a, in Molecule.Atom b)
            => DistanceErrorCore(MakePosErr(a), MakePosErr(b));

        /// <summary>Valence-angle esd (degrees) a–b–c, b is the vertex.</summary>
        public float AngleError(in Molecule.Atom a, in Molecule.Atom b, in Molecule.Atom c)
            => AngleErrorCore(MakePosErr(a), MakePosErr(b), MakePosErr(c));

        /// <summary>Torsion-angle esd (degrees) a–b–c–d.</summary>
        public float TorsionError(in Molecule.Atom a, in Molecule.Atom b, in Molecule.Atom c, in Molecule.Atom d)
            => TorsionErrorCore(new[] { MakePosErr(a), MakePosErr(b), MakePosErr(c), MakePosErr(d) });

        // ── cores ──

        private float DistanceErrorCore(PosErr p1, PosErr p2)
        {
            Vector3 diff = p1.Pos - p2.Pos;
            float d = diff.Length();
            if (d < 1e-4f) return 0.001f;

            Vector3 u = diff / d;
            double var = p1.Cov.Quadratic(u) + p2.Cov.Quadratic(u);

            if (p1.Real && p2.Real)
            {
                double c = CellContribution(new[] { p1.Pos, p2.Pos }, DistanceOf, false);
                var += c * c;
            }

            return (float)Math.Sqrt(Math.Max(var, 1e-12));
        }

        private float AngleErrorCore(PosErr p1, PosErr p2, PosErr p3)
        {
            Vector3 a = p1.Pos - p2.Pos;
            Vector3 b = p3.Pos - p2.Pos;
            float lenA = a.Length();
            float lenB = b.Length();

            if (lenA < 1e-4f || lenB < 1e-4f)
                return 0.5f;

            Vector3 aHat = a / lenA;
            Vector3 bHat = b / lenB;

            double cosTheta = Math.Clamp(Vector3.Dot(aHat, bHat), -1.0, 1.0);
            double theta = Math.Acos(cosTheta);
            double sinTheta = Math.Sin(theta);

            if (Math.Abs(sinTheta) < 0.01)
            {
                // (almost) linear: gradient is singular -> rough upper estimate
                float avgErr = (CalculateVectorError(p1.Cov.Sigma) +
                                CalculateVectorError(p2.Cov.Sigma) +
                                CalculateVectorError(p3.Cov.Sigma)) / 3.0f;
                float avgDist = 0.5f * (lenA + lenB);
                float errDeg_ = (avgErr / avgDist) * (180.0f / MathF.PI);
                return Math.Clamp(errDeg_, 0.001f, 10.0f);
            }

            Vector3 gA = -(bHat - aHat * (float)cosTheta) / (lenA * (float)sinTheta);
            Vector3 gB = -(aHat - bHat * (float)cosTheta) / (lenB * (float)sinTheta);

            Vector3 grad1 = gA;
            Vector3 grad2 = -gA - gB;
            Vector3 grad3 = gB;

            const float GRAD_CLAMP = 1e3f;
            grad1 = ClampVector(grad1, GRAD_CLAMP);
            grad2 = ClampVector(grad2, GRAD_CLAMP);
            grad3 = ClampVector(grad3, GRAD_CLAMP);

            double errRadSq =
                p1.Cov.Quadratic(grad1) +
                p2.Cov.Quadratic(grad2) +
                p3.Cov.Quadratic(grad3);

            if (p1.Real && p2.Real && p3.Real)
            {
                double c = CellContribution(new[] { p1.Pos, p2.Pos, p3.Pos }, AngleOf, false);
                errRadSq += c * c;
            }

            double errDeg = Math.Sqrt(Math.Max(errRadSq, 0.0)) * (180.0 / Math.PI);
            return (float)Math.Clamp(errDeg, 0.001, 10.0);
        }

        private float TorsionErrorCore(PosErr[] p)
        {
            if (!IsTorsionWellDefined(p[0].Pos, p[1].Pos, p[2].Pos, p[3].Pos))
                return 0.5f;

            const double EPS = 1e-4;
            double[][] x = new double[4][];
            for (int i = 0; i < 4; i++)
                x[i] = new double[] { p[i].Pos.X, p[i].Pos.Y, p[i].Pos.Z };

            double errRadSq = 0.0;

            for (int atom = 0; atom < 4; atom++)
            {
                double[] g = new double[3];
                for (int coord = 0; coord < 3; coord++)
                {
                    double saved = x[atom][coord];
                    x[atom][coord] = saved + EPS;
                    double tPlus = TorsionOf(x);
                    x[atom][coord] = saved - EPS;
                    double tMinus = TorsionOf(x);
                    x[atom][coord] = saved;

                    double diff = WrapAngle(tPlus - tMinus);
                    double deriv = diff / (2.0 * EPS);
                    if (Math.Abs(deriv) > 1e3) deriv = Math.Sign(deriv) * 1e3;
                    g[coord] = deriv;
                }
                errRadSq += p[atom].Cov.Quadratic(new Vector3((float)g[0], (float)g[1], (float)g[2]));
            }

            if (p[0].Real && p[1].Real && p[2].Real && p[3].Real)
            {
                double c = CellContribution(new[] { p[0].Pos, p[1].Pos, p[2].Pos, p[3].Pos }, TorsionOf, true);
                errRadSq += c * c;
            }

            double errDeg = Math.Sqrt(Math.Max(errRadSq, 0.0)) * (180.0 / Math.PI);
            return (float)Math.Clamp(errDeg, 0.001, 10.0);
        }

        // ── unit-cell contribution ──

        /// <summary>Unit-cell parameters (Å, rad) and their esds; false when there are no cell esds.</summary>
        private bool TryGetCell(out double[] p, out double[] s)
        {
            p = null; s = null;
            if (_molecule == null) return false;

            p = new[] { (double)_molecule.a, _molecule.b, _molecule.c, _molecule.alpha, _molecule.beta, _molecule.gamma };
            s = new[] { (double)_molecule.a_e, _molecule.b_e, _molecule.c_e, _molecule.alpha_e, _molecule.beta_e, _molecule.gamma_e };

            if (p[0] <= 0 || p[1] <= 0 || p[2] <= 0) return false;

            for (int k = 0; k < 6; k++)
                if (s[k] > 0 && !double.IsNaN(s[k]) && !double.IsInfinity(s[k]))
                    return true;
            return false;
        }

        /// <summary>
        /// Contribution of the unit-cell esds to any quantity q(cell parameters) with the
        /// fractional coordinates held fixed: sqrt( sum_k ( (q(p_k+s_k) - q(p_k-s_k)) / 2 )^2 ).
        /// Same units as q. Returns 0 when the structure has no cell esds.
        /// </summary>
        private double CellContributionParams(Func<double[], double> q, bool wrapAngle)
        {
            if (!TryGetCell(out double[] p, out double[] s)) return 0.0;

            double sum = 0.0;
            for (int k = 0; k < 6; k++)
            {
                if (!(s[k] > 0) || double.IsInfinity(s[k]) || double.IsNaN(s[k])) continue;

                double[] pp = (double[])p.Clone();
                pp[k] = p[k] + s[k];
                double qPlus = q(pp);
                pp[k] = p[k] - s[k];
                double qMinus = q(pp);

                double d = qPlus - qMinus;
                if (wrapAngle) d = WrapAngle(d);
                d *= 0.5;
                sum += d * d;
            }
            return Math.Sqrt(sum);
        }

        /// <summary>
        /// Cell contribution for a quantity of atomic positions (distance, angle, torsion).
        /// Fractional vectors relative to the first atom are held fixed (translation cancels).
        /// </summary>
        private double CellContribution(Vector3[] pos, Func<double[][], double> q, bool wrapAngle)
        {
            if (!TryGetCell(out double[] p, out _)) return 0.0;
            if (!TryInvert3(CellMatrix(p), out double[,] inv)) return 0.0;

            double[][] frac = new double[pos.Length][];
            for (int i = 0; i < pos.Length; i++)
            {
                Vector3 r = pos[i] - pos[0];
                frac[i] = MulVec(inv, r.X, r.Y, r.Z);
            }

            return CellContributionParams(pp => q(ToCartesian(CellMatrix(pp), frac)), wrapAngle);
        }

        /// <summary>
        /// Least-squares mean plane through the points: centroid c and unit normal
        /// (eigenvector of the smallest eigenvalue of the scatter matrix). Same fit as MeanPlaneCalculator.
        /// </summary>
        private static bool TryFitMeanPlane(double[][] pts, out double[] c, out double[] nrm)
        {
            int n = pts.Length;
            c = new double[3];
            nrm = null;
            if (n < 3) return false;

            for (int i = 0; i < n; i++)
                for (int a = 0; a < 3; a++) c[a] += pts[i][a] / n;

            double[,] cov = new double[3, 3];
            for (int i = 0; i < n; i++)
                for (int a = 0; a < 3; a++)
                    for (int b = 0; b < 3; b++)
                        cov[a, b] += (pts[i][a] - c[a]) * (pts[i][b] - c[b]) / n;

            if (!MeanPlaneCalculator.SymmetricEigen3x3(cov, out double[] ev, out double[,] vec))
                return false;

            int min = 0;
            if (ev[1] < ev[min]) min = 1;
            if (ev[2] < ev[min]) min = 2;

            double[] v = { vec[0, min], vec[1, min], vec[2, min] };
            double len = Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);
            if (len < 1e-12) return false;

            nrm = new[] { v[0] / len, v[1] / len, v[2] / len };
            return true;
        }

        /// <summary>
        /// Variance (Å²) of the signed distance atom → mean plane caused by the position errors of
        /// BOTH the measured atom and every atom that defines the plane:
        ///     var = sum_k g_kᵀ C_k g_k ,  g_k = ∂d/∂r_k  (numerical, the plane is refitted).
        /// When the measured atom is itself one of the plane atoms its position is the same variable
        /// (direct term and plane term are differentiated together). Plane atoms without refined
        /// esds contribute nothing.
        /// </summary>
        private double MeanPlanePositionVariance(int atomIdx, CrystalPlane plane, Vector3 shift,
                                                 Vector3 atomRaw, out bool ok)
        {
            ok = false;
            if (plane == null || plane.AtomIndices == null) return 0.0;

            var members = new List<int>();
            foreach (int idx in plane.AtomIndices)
                if (idx >= 0 && idx < _molecule.NumberOfAtoms) members.Add(idx);
            if (members.Count < 3) return 0.0;

            double[][] pts = new double[members.Count][];
            int memberOfAtom = -1;
            for (int i = 0; i < members.Count; i++)
            {
                Vector3 r = _molecule.atom[members[i]].Coordinate + shift;
                pts[i] = new double[] { r.X, r.Y, r.Z };
                if (members[i] == atomIdx) memberOfAtom = i;
            }

            // measured atom: the same array as the plane atom when it is a member
            double[] a = memberOfAtom >= 0 ? pts[memberOfAtom] : new double[] { atomRaw.X, atomRaw.Y, atomRaw.Z };

            if (!TryFitMeanPlane(pts, out double[] c0, out double[] nRef)) return 0.0;

            bool failed = false;
            double SignedDistance()
            {
                if (!TryFitMeanPlane(pts, out double[] c, out double[] nn)) { failed = true; return 0.0; }
                if (nn[0] * nRef[0] + nn[1] * nRef[1] + nn[2] * nRef[2] < 0)
                    for (int k = 0; k < 3; k++) nn[k] = -nn[k];
                return nn[0] * (a[0] - c[0]) + nn[1] * (a[1] - c[1]) + nn[2] * (a[2] - c[2]);
            }

            const double EPS = 1e-4;

            double Gradient(double[] target, Cov3 cov)
            {
                double[] g = new double[3];
                for (int k = 0; k < 3; k++)
                {
                    double saved = target[k];
                    target[k] = saved + EPS;
                    double dp = SignedDistance();
                    target[k] = saved - EPS;
                    double dm = SignedDistance();
                    target[k] = saved;
                    g[k] = (dp - dm) / (2.0 * EPS);
                }
                return cov.Quadratic(new Vector3((float)g[0], (float)g[1], (float)g[2]));
            }

            double var = 0.0;
            for (int i = 0; i < members.Count; i++)
            {
                PosErr pe = MakePosErr(_molecule.atom[members[i]]);
                if (!pe.Real) continue;
                var += Gradient(pts[i], pe.Cov);
            }

            if (memberOfAtom < 0)
            {
                PosErr pa = MakePosErr(_molecule.atom[atomIdx]);
                if (pa.Real) var += Gradient(a, pa.Cov);
            }

            if (failed || !(var > 0.0) || double.IsNaN(var)) return 0.0;
            ok = true;
            return var;
        }

        /// <summary>
        /// Cell contribution to the atom → plane distance (Å).
        ///  * HKL plane  h·x + k·y + l·z = 1 is fixed in fractional space, so only the length of the
        ///    reciprocal vector |g| = |M⁻ᵀ(h,k,l)| changes with the cell: d = |h·x+k·y+l·z−1| / |g|.
        ///  * Mean plane: all plane atoms and the measured atom keep their fractional coordinates,
        ///    the plane is refitted (centroid + smallest-variance direction) for each cell variant.
        /// atomRaw = atom position WITHOUT molecule shift (absolute Cartesian).
        /// </summary>
        private double PlaneCellContribution(Vector3 atomRaw, CrystalPlane plane, Vector3 shift)
        {
            if (plane == null) return 0.0;
            if (!TryGetCell(out double[] p0, out _)) return 0.0;
            if (!TryInvert3(CellMatrix(p0), out double[,] inv0)) return 0.0;

            double[] fAtom = MulVec(inv0, atomRaw.X, atomRaw.Y, atomRaw.Z);

            if (plane.Type == CrystalPlaneType.HKL)
            {
                if (plane.H == 0 && plane.K == 0 && plane.L == 0) return 0.0;
                double h = plane.H, k = plane.K, l = plane.L;

                return CellContributionParams(pp =>
                {
                    if (!TryInvert3(CellMatrix(pp), out double[,] im)) return 0.0;
                    double gx = im[0, 0] * h + im[1, 0] * k + im[2, 0] * l;
                    double gy = im[0, 1] * h + im[1, 1] * k + im[2, 1] * l;
                    double gz = im[0, 2] * h + im[1, 2] * k + im[2, 2] * l;
                    double gl = Math.Sqrt(gx * gx + gy * gy + gz * gz);
                    if (gl < 1e-12) return 0.0;
                    return Math.Abs(h * fAtom[0] + k * fAtom[1] + l * fAtom[2] - 1.0) / gl;
                }, false);
            }

            if (plane.Type == CrystalPlaneType.MeanPlane && plane.AtomIndices != null)
            {
                var fr = new List<double[]>();
                foreach (int idx in plane.AtomIndices)
                {
                    if (idx < 0 || idx >= _molecule.NumberOfAtoms) continue;
                    Vector3 r = _molecule.atom[idx].Coordinate + shift;
                    fr.Add(MulVec(inv0, r.X, r.Y, r.Z));
                }
                if (fr.Count < 3) return 0.0;

                return CellContributionParams(pp =>
                {
                    double[,] mp = CellMatrix(pp);
                    double[][] pts = new double[fr.Count][];
                    for (int i = 0; i < pts.Length; i++)
                        pts[i] = MulVec(mp, fr[i][0], fr[i][1], fr[i][2]);

                    if (!TryFitMeanPlane(pts, out double[] c, out double[] nrm)) return 0.0;

                    double[] ap = MulVec(mp, fAtom[0], fAtom[1], fAtom[2]);
                    return Math.Abs(nrm[0] * (ap[0] - c[0]) + nrm[1] * (ap[1] - c[1]) + nrm[2] * (ap[2] - c[2]));
                }, false);
            }

            return 0.0;
        }

        /// <summary>
        /// Column convention x_cart = M · x_frac, identical to FractionaltoCarttesianMatrix
        /// (a along x, b in the xy plane). Angles in radians.
        /// </summary>
        private static double[,] CellMatrix(double[] p)
        {
            double a = p[0], b = p[1], c = p[2];
            double cosA = Math.Cos(p[3]), cosB = Math.Cos(p[4]), cosG = Math.Cos(p[5]);
            double sinB = Math.Sin(p[4]), sinG = Math.Sin(p[5]);

            double cosAStar = (cosB * cosG - cosA) / (sinB * sinG);
            double sinAStar = Math.Sqrt(Math.Max(1.0 - cosAStar * cosAStar, 0.0));

            return new double[,]
            {
                { a, b * cosG, c * cosB },
                { 0, b * sinG, -c * sinB * cosAStar },
                { 0, 0,        c * sinB * sinAStar }
            };
        }

        private static bool TryInvert3(double[,] m, out double[,] inv)
        {
            double a = m[0, 0], b = m[0, 1], c = m[0, 2];
            double d = m[1, 0], e = m[1, 1], f = m[1, 2];
            double g = m[2, 0], h = m[2, 1], i = m[2, 2];

            double det = a * (e * i - f * h) - b * (d * i - f * g) + c * (d * h - e * g);
            inv = new double[3, 3];
            if (Math.Abs(det) < 1e-12) return false;

            inv[0, 0] = (e * i - f * h) / det; inv[0, 1] = (c * h - b * i) / det; inv[0, 2] = (b * f - c * e) / det;
            inv[1, 0] = (f * g - d * i) / det; inv[1, 1] = (a * i - c * g) / det; inv[1, 2] = (c * d - a * f) / det;
            inv[2, 0] = (d * h - e * g) / det; inv[2, 1] = (b * g - a * h) / det; inv[2, 2] = (a * e - b * d) / det;
            return true;
        }

        private static double[] MulVec(double[,] m, double x, double y, double z)
        {
            return new[]
            {
                m[0, 0] * x + m[0, 1] * y + m[0, 2] * z,
                m[1, 0] * x + m[1, 1] * y + m[1, 2] * z,
                m[2, 0] * x + m[2, 1] * y + m[2, 2] * z
            };
        }

        private static double[][] ToCartesian(double[,] m, double[][] frac)
        {
            double[][] r = new double[frac.Length][];
            for (int i = 0; i < frac.Length; i++)
                r[i] = MulVec(m, frac[i][0], frac[i][1], frac[i][2]);
            return r;
        }

        private static double WrapAngle(double d)
        {
            while (d > Math.PI) d -= 2.0 * Math.PI;
            while (d < -Math.PI) d += 2.0 * Math.PI;
            return d;
        }

        // ── quantities evaluated on double[][] positions ──

        private static double DistanceOf(double[][] q)
        {
            double dx = q[1][0] - q[0][0], dy = q[1][1] - q[0][1], dz = q[1][2] - q[0][2];
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>Angle (rad) at q[1] between q[0] and q[2].</summary>
        private static double AngleOf(double[][] q)
        {
            double[] a = { q[0][0] - q[1][0], q[0][1] - q[1][1], q[0][2] - q[1][2] };
            double[] b = { q[2][0] - q[1][0], q[2][1] - q[1][1], q[2][2] - q[1][2] };
            double la = Math.Sqrt(a[0] * a[0] + a[1] * a[1] + a[2] * a[2]);
            double lb = Math.Sqrt(b[0] * b[0] + b[1] * b[1] + b[2] * b[2]);
            double cos = (a[0] * b[0] + a[1] * b[1] + a[2] * b[2]) / (la * lb);
            return Math.Acos(Math.Clamp(cos, -1.0, 1.0));
        }

        /// <summary>Torsion angle (rad) of q[0]-q[1]-q[2]-q[3], same sign convention as CalculateTorsionAngleRad.</summary>
        private static double TorsionOf(double[][] q)
        {
            double[] b1 = Sub(q[1], q[0]);
            double[] b2 = Sub(q[2], q[1]);
            double[] b3 = Sub(q[3], q[2]);

            double[] n1 = Cross(b1, b2);
            double[] n2 = Cross(b2, b3);
            double l1 = Math.Sqrt(Dot(n1, n1));
            double l2 = Math.Sqrt(Dot(n2, n2));
            if (l1 < 1e-9 || l2 < 1e-9) return 0.0;

            for (int i = 0; i < 3; i++) { n1[i] /= l1; n2[i] /= l2; }

            double angle = Math.Acos(Math.Clamp(Dot(n1, n2), -1.0, 1.0));
            double lb2 = Math.Sqrt(Dot(b2, b2));
            double[] c = Cross(n1, n2);
            if (Dot(c, b2) / lb2 < 0) angle = -angle;
            return angle;
        }

        private static double[] Sub(double[] a, double[] b) => new[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] };
        private static double Dot(double[] a, double[] b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
        private static double[] Cross(double[] a, double[] b) => new[]
        {
            a[1] * b[2] - a[2] * b[1],
            a[2] * b[0] - a[0] * b[2],
            a[0] * b[1] - a[1] * b[0]
        };

        // ── displayed torsion value (float, unchanged) ──

        private double CalculateTorsionAngleRad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
        {
            Vector3 b1 = v2 - v1;
            Vector3 b2 = v3 - v2;
            Vector3 b3 = v4 - v3;

            Vector3 n1 = Vector3.Cross(b1, b2);
            Vector3 n2 = Vector3.Cross(b2, b3);

            float len1 = n1.Length();
            float len2 = n2.Length();
            if (len1 < 1e-6f || len2 < 1e-6f)
                return 0.0;

            n1 /= len1;
            n2 /= len2;

            double dot = Math.Clamp(Vector3.Dot(n1, n2), -1.0, 1.0);
            double angle = Math.Acos(dot);

            Vector3 b2hat = b2 / b2.Length();
            if (Vector3.Dot(Vector3.Cross(n1, n2), b2hat) < 0)
                angle = -angle;

            return angle;
        }

        private bool IsTorsionWellDefined(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
        {
            Vector3 b1 = v2 - v1;
            Vector3 b2 = v3 - v2;
            Vector3 b3 = v4 - v3;
            if (b2.Length() < 1e-4f) return false;
            if (Vector3.Cross(b1, b2).Length() < 1e-6f) return false;
            if (Vector3.Cross(b2, b3).Length() < 1e-6f) return false;
            return true;
        }

        private static Vector3 ClampVector(Vector3 v, float max)
        {
            float len = v.Length();
            if (len > max) return v * (max / len);
            return v;
        }

        private static float CalculateVectorError(Vector3 vec)
        {
            if (float.IsNaN(vec.X) || float.IsNaN(vec.Y) || float.IsNaN(vec.Z))
                return 0f;
            float s = vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z;
            return (float)Math.Sqrt(Math.Max(s, 0f));
        }

        #endregion
    }
}