// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Cavity detection / analysis module: CellParameters.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using SysVector3 = System.Numerics.Vector3;
using System;

/// <summary>
/// Static class storing crystallographic cell parameters and providing utility methods.
/// Handles both orthogonal and triclinic cells with proper voxel volume calculation.
/// </summary>
public static class CellParameters
{
    #region Fields

    public static SysVector3 Origin { get; private set; }
    public static SysVector3 VecA { get; private set; }
    public static SysVector3 VecB { get; private set; }
    public static SysVector3 VecC { get; private set; }
    public static float StepX { get; private set; }  // Fractional step
    public static float StepY { get; private set; }  // Fractional step
    public static float StepZ { get; private set; }  // Fractional step
    public static float VoxelSize { get; private set; }  // In Angstroms (constant)
    public static bool IsInitialized { get; private set; } = false;

    private static float _cachedVoxelVolume = 0f;
    private static bool _voxelVolumeCached = false;

    #endregion

    #region Set and Clear

 /// <summary>
 /// Sets the cell parameters.
 /// </summary>
 /// <param name="origin">Origin of the cell.</param>
 /// <param name="vecA">Unit cell vector A.</param>
 /// <param name="vecB">Unit cell vector B.</param>
 /// <param name="vecC">Unit cell vector C.</param>
 /// <param name="stepX">Fractional step in X direction.</param>
 /// <param name="stepY">Fractional step in Y direction.</param>
 /// <param name="stepZ">Fractional step in Z direction.</param>
 /// <param name="voxelSize">Voxel size in Angstroms.</param>
    public static void Set(
        SysVector3 origin,
        SysVector3 vecA,
        SysVector3 vecB,
        SysVector3 vecC,
        float stepX,
        float stepY,
        float stepZ,
        float voxelSize)
    {
        Origin = origin;
        VecA = vecA;
        VecB = vecB;
        VecC = vecC;
        StepX = stepX;
        StepY = stepY;
        StepZ = stepZ;
        VoxelSize = voxelSize;
        IsInitialized = true;
        _voxelVolumeCached = false;

        System.Diagnostics.Debug.WriteLine($"[CellParameters] Set: Origin={origin}");
        System.Diagnostics.Debug.WriteLine($"[CellParameters] VecA={vecA}, VecB={vecB}, VecC={vecC}");
        System.Diagnostics.Debug.WriteLine($"[CellParameters] Steps (frac): X={stepX:F6}, Y={stepY:F6}, Z={stepZ:F6}");
        System.Diagnostics.Debug.WriteLine($"[CellParameters] VoxelSize: {voxelSize:F4} Å");
    }

 /// <summary>
 /// Clears all cell parameters.
 /// </summary>
    public static void Clear()
    {
        Origin = SysVector3.Zero;
        VecA = SysVector3.Zero;
        VecB = SysVector3.Zero;
        VecC = SysVector3.Zero;
        StepX = 0;
        StepY = 0;
        StepZ = 0;
        VoxelSize = 0;
        IsInitialized = false;
        _voxelVolumeCached = false;
        _cachedVoxelVolume = 0f;
    }

    #endregion

    #region Step Helpers

 /// <summary>
 /// Returns the step size in Angstroms along vector A.
 /// </summary>
    public static float GetStepX_Angstroms() => StepX * VecA.Length();

 /// <summary>
 /// Returns the step size in Angstroms along vector B.
 /// </summary>
    public static float GetStepY_Angstroms() => StepY * VecB.Length();

 /// <summary>
 /// Returns the step size in Angstroms along vector C.
 /// </summary>
    public static float GetStepZ_Angstroms() => StepZ * VecC.Length();

    #endregion

    #region Volume Calculation

 /// <summary>
 /// Returns the volume of a single voxel in Å³ for TRICLINIC systems.
 /// Calculates: V = |(stepA × stepB) · stepC|
 /// where stepA = StepX * VecA, stepB = StepY * VecB, stepC = StepZ * VecC
 /// </summary>
    public static float GetVoxelVolume()
    {
        if (!IsInitialized) return 0f;

        if (_voxelVolumeCached)
            return _cachedVoxelVolume;

 // Calculate for triclinic system
        SysVector3 stepA = StepX * VecA;
        SysVector3 stepB = StepY * VecB;
        SysVector3 stepC = StepZ * VecC;

 // V = |(stepA × stepB) · stepC|
        SysVector3 cross = SysVector3.Cross(stepA, stepB);
        float volume = Math.Abs(SysVector3.Dot(cross, stepC));

        _cachedVoxelVolume = volume;
        _voxelVolumeCached = true;

        System.Diagnostics.Debug.WriteLine($"[CellParameters] Voxel volume: {volume:F6} Å³ (triclinic)");

        return volume;
    }

    #endregion

    #region Cell Properties

 /// <summary>
 /// Checks if the cell is non-orthogonal (triclinic).
 /// </summary>
    public static bool IsNonOrthogonal()
    {
        if (!IsInitialized) return false;

        float dotAB = Math.Abs(SysVector3.Dot(VecA, VecB));
        float dotAC = Math.Abs(SysVector3.Dot(VecA, VecC));
        float dotBC = Math.Abs(SysVector3.Dot(VecB, VecC));

        const float eps = 0.001f;
        return dotAB > eps || dotAC > eps || dotBC > eps;
    }

    #endregion

    #region Coordinate Conversion

 /// <summary>
 /// Converts fractional coordinates to Cartesian coordinates.
 /// </summary>
    public static SysVector3 FracToCartesian(float fx, float fy, float fz)
    {
        if (!IsInitialized) return SysVector3.Zero;
        return Origin + fx * VecA + fy * VecB + fz * VecC;
    }

    #endregion
}