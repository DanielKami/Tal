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
 /// per-atom aftersitional error already contains the cell-error contribution.
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

            float error = CalculateDistanceError(pos1, pos2,
                _molecule.atom[atom1].Coordinate_error,
                _molecule.atom[atom2].Coordinate_error);

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

            float error = CalculateAngleError(v1, v2, v3,
                _molecule.atom[atom1].Coordinate_error,
                _molecule.atom[atom2].Coordinate_error,
                _molecule.atom[atom3].Coordinate_error);

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

            float error = CalculateTorsionError(V1, V2, V3, V4,
                _molecule.atom[atom1].Coordinate_error,
                _molecule.atom[atom2].Coordinate_error,
                _molecule.atom[atom3].Coordinate_error,
                _molecule.atom[atom4].Coordinate_error);

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
            float posErr = CalculateVectorError(_molecule.atom[atomIdx].Coordinate_error);
            float error = posErr > 0f ? posErr : distance * _cellErrorFraction;

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

        private float CalculateDistanceError(Vector3 pos1, Vector3 pos2,
            Vector3 err1, Vector3 err2)
        {
            Vector3 diff = pos1 - pos2;
            float d = diff.Length();
            if (d < 1e-4f) return 0.001f;

            Vector3 e1 = EffectivePositionalError(pos1, err1, pos1);
            Vector3 e2 = EffectivePositionalError(pos2, err2, pos2);

            float dx = diff.X / d, dy = diff.Y / d, dz = diff.Z / d;

            float errorSq =
                dx * dx * e1.X * e1.X + dy * dy * e1.Y * e1.Y + dz * dz * e1.Z * e1.Z +
                dx * dx * e2.X * e2.X + dy * dy * e2.Y * e2.Y + dz * dz * e2.Z * e2.Z;

            return (float)Math.Sqrt(Math.Max(errorSq, 1e-12));
        }

        private float CalculateAngleError(Vector3 pos1, Vector3 pos2, Vector3 pos3,
            Vector3 err1, Vector3 err2, Vector3 err3)
        {
            Vector3 a = pos1 - pos2;
            Vector3 b = pos3 - pos2;
            float lenA = a.Length();
            float lenB = b.Length();

            if (lenA < 1e-4f || lenB < 1e-4f)
                return 0.5f;

            Vector3 aHat = a / lenA;
            Vector3 bHat = b / lenB;

            double cosTheta = Math.Clamp(Vector3.Dot(aHat, bHat), -1.0, 1.0);
            double theta = Math.Acos(cosTheta);
            double sinTheta = Math.Sin(theta);

            Vector3 e1 = EffectivePositionalError(pos1, err1, pos1);
            Vector3 e2 = EffectivePositionalError(pos2, err2, pos2);
            Vector3 e3 = EffectivePositionalError(pos3, err3, pos3);

            if (Math.Abs(sinTheta) < 0.01)
            {
                float avgErr = (CalculateVectorError(e1) +
                                CalculateVectorError(e2) +
                                CalculateVectorError(e3)) / 3.0f;
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
                grad1.X * grad1.X * e1.X * e1.X + grad1.Y * grad1.Y * e1.Y * e1.Y + grad1.Z * grad1.Z * e1.Z * e1.Z +
                grad2.X * grad2.X * e2.X * e2.X + grad2.Y * grad2.Y * e2.Y * e2.Y + grad2.Z * grad2.Z * e2.Z * e2.Z +
                grad3.X * grad3.X * e3.X * e3.X + grad3.Y * grad3.Y * e3.Y * e3.Y + grad3.Z * grad3.Z * e3.Z * e3.Z;

            double errDeg = Math.Sqrt(Math.Max(errRadSq, 0.0)) * (180.0 / Math.PI);
            return (float)Math.Clamp(errDeg, 0.001, 10.0);
        }

        private float CalculateTorsionError(Vector3 pos1, Vector3 pos2, Vector3 pos3, Vector3 pos4,
            Vector3 err1, Vector3 err2, Vector3 err3, Vector3 err4)
        {
            const float EPS = 1e-4f;
            Vector3[] positions = { pos1, pos2, pos3, pos4 };
            Vector3[] errs = { err1, err2, err3, err4 };

            Vector3[] eff = new Vector3[4];
            for (int i = 0; i < 4; i++)
                eff[i] = EffectivePositionalError(positions[i], errs[i], positions[i]);

            if (!IsTorsionWellDefined(pos1, pos2, pos3, pos4))
                return 0.5f;

            double errRadSq = 0.0;

            for (int atom = 0; atom < 4; atom++)
            {
                for (int coord = 0; coord < 3; coord++)
                {
                    float e = (coord == 0) ? eff[atom].X
                            : (coord == 1) ? eff[atom].Y
                                           : eff[atom].Z;
                    if (e < 1e-6f) continue;

                    Vector3[] plus = (Vector3[])positions.Clone();
                    Vector3[] minus = (Vector3[])positions.Clone();
                    if (coord == 0) { plus[atom].X += EPS; minus[atom].X -= EPS; }
                    if (coord == 1) { plus[atom].Y += EPS; minus[atom].Y -= EPS; }
                    if (coord == 2) { plus[atom].Z += EPS; minus[atom].Z -= EPS; }

                    double tPlus = CalculateTorsionAngleRad(plus[0], plus[1], plus[2], plus[3]);
                    double tMinus = CalculateTorsionAngleRad(minus[0], minus[1], minus[2], minus[3]);

                    double diff = tPlus - tMinus;
                    while (diff > Math.PI) diff -= 2.0 * Math.PI;
                    while (diff < -Math.PI) diff += 2.0 * Math.PI;

                    double deriv = diff / (2.0 * EPS);
                    if (Math.Abs(deriv) > 1e3) deriv = Math.Sign(deriv) * 1e3;

                    errRadSq += deriv * deriv * e * e;
                }
            }

            double errDeg = Math.Sqrt(Math.Max(errRadSq, 0.0)) * (180.0 / Math.PI);
            return (float)Math.Clamp(errDeg, 0.001, 10.0);
        }

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