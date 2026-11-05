// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Molecule partial — van der Waals radii / surfaces.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// Molecule partial — access do promieni van der Waalsa i jonewch
// (delegacja do VdwParameters).

namespace Tal
{
    public partial class Molecule
    {
        #region VdW Radius

 /// <summary>
 /// Returns the van der Waals radius (Å) for the given atomic number.
 /// </summary>
        public float GetVdwRadius(int atomicNumber)
        {
            return VdwParameters.GetRadius(atomicNumber);
        }

 /// <summary>
 /// Returns the van der Waals radius (Å) inferred from the atom name.
 /// </summary>
        public float GetVdwRadius(string atomName)
        {
            return VdwParameters.GetRadius(atomName);
        }

        #endregion

        #region Ionic Radius

 /// <summary>
 /// Returns the ionic radius (Å) for the given atomic number.
 /// </summary>
        public float GetIonicRadius(int atomicNumber)
        {
            return VdwParameters.GetIonicRadius(atomicNumber);
        }

 /// <summary>
 /// Returns the ionic radius (Å) inferred from the atom name.
 /// </summary>
        public float GetIonicRadius(string atomName)
        {
            return VdwParameters.GetIonicRadius(atomName);
        }

        #endregion

        #region Atomic Number

 /// <summary>
 /// Infers the atomic number from the atom name (e.g. "C1" → 6).
 /// </summary>
        public int GetAtomicNumber(string atomName)
        {
            return VdwParameters.GetAtomicNumber(atomName);
        }

        #endregion
    }
}