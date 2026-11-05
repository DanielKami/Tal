// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MainForm partial — helpers for revealing and applying symmetry operations.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using Tal.DrawMethods;

namespace Tal
{
    public partial class MainForm
    {
        #region Reveal Symmetry Helpers

        private (Vector3 translation, int symOpIndex, float distance) FindBestTranslationForAtom(
            Molecule molecule,
            int atomIndex,
            int rangeX = 5,
            int rangeY = 5,
            int rangeZ = 15,             // larger range along Z
            float maxDistanceThreshold = 5.0f)
        {
            if (molecule == null || atomIndex < 0 || atomIndex >= molecule.NumberOfAtoms)
                return (Vector3.Zero, -1, float.MaxValue);

            // ═══════════════════════════════════════════════════════════
            // 1. Reference point — position of the clicked atom
            // ═══════════════════════════════════════════════════════════
            Vector3 clickedPoint = molecule.atom[atomIndex].Coordinate;
            Vector3 clickedRaw = clickedPoint + molecule.MoleculeShift;

            // ═══════════════════════════════════════════════════════════
            // 2. Symmetry operations
            // ═══════════════════════════════════════════════════════════
            var symOps = new List<SymmetryElements.ReadSymm>();
            if (molecule.SymmetryOperations.Count == 0)
            {
                var identity = new SymmetryElements.ReadSymm();
                identity.sign1 = new int[] { 1, 1, 1 };
                identity.sign2 = new int[] { 0, 0, 0 };
                identity.pos1 = new int[] { 0, 1, 2 };
                identity.pos2 = new int[] { 0, 0, 0 };
                identity.shift = new float[] { 0, 0, 0 };
                symOps.Add(identity);
            }
            else
            {
                symOps.AddRange(molecule.SymmetryOperations);
            }

            Matrix U = Matrix.Transpose(molecule.FractionaltoCarttesianMatrix(0, 0, 0));

            // ═══════════════════════════════════════════════════════════
            // 3. AsymmetricPart centre (once) — for a quick test
            // ═══════════════════════════════════════════════════════════
            Vector3 asymPartCenterFrac = Vector3.Zero;
            foreach (var asymAtom in molecule.AsymmetricPart)
            {
                asymPartCenterFrac += new Vector3(asymAtom.x, asymAtom.y, asymAtom.z);
            }
            asymPartCenterFrac /= molecule.AsymmetricPart.Count;

            // ═══════════════════════════════════════════════════════════
            // 4. Loop over symops
            // ═══════════════════════════════════════════════════════════
            float bestDistance = float.MaxValue;
            Vector3 bestTranslation = Vector3.Zero;
            int bestSymOpIndex = -1;

            for (int opIdx = 0; opIdx < symOps.Count; opIdx++)
            {
                var symOp = symOps[opIdx];

                float cx = asymPartCenterFrac.X;
                float cy = asymPartCenterFrac.Y;
                float cz = asymPartCenterFrac.Z;

                float[] cxArr = new float[] { cx, cy, cz };

                float centerX = cxArr[symOp.pos1[0]] * symOp.sign1[0]
                              + cxArr[symOp.pos2[0]] * symOp.sign2[0]
                              + symOp.shift[0];
                float centerY = cxArr[symOp.pos1[1]] * symOp.sign1[1]
                              + cxArr[symOp.pos2[1]] * symOp.sign2[1]
                              + symOp.shift[1];
                float centerZ = cxArr[symOp.pos1[2]] * symOp.sign1[2]
                              + cxArr[symOp.pos2[2]] * symOp.sign2[2]
                              + symOp.shift[2];

                // ⭐ Loop over cells — separate ranges for X, Y, Z
                for (int tx = -rangeX; tx <= rangeX; tx++)
                {
                    for (int ty = -rangeY; ty <= rangeY; ty++)
                    {
                        for (int tz = -rangeZ; tz <= rangeZ; tz++)
                        {
                            if (opIdx == 0 && tx == 0 && ty == 0 && tz == 0)
                                continue;

                            Vector3 candidateCenterFrac = new Vector3(
                                centerX + tx,
                                centerY + ty,
                                centerZ + tz);

                            Vector3 candidateCenterRaw = Vector3.Transform(candidateCenterFrac, U);

                            float dist = Vector3.Distance(candidateCenterRaw, clickedRaw);

                            if (dist < bestDistance && dist < maxDistanceThreshold)
                            {
                                bestDistance = dist;
                                bestTranslation = new Vector3(tx, ty, tz);
                                bestSymOpIndex = opIdx;
                            }
                        }
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine(
                $"[FindBestTranslation] atomIdx={atomIndex}, " +
                $"clickedRaw=({clickedRaw.X:F2},{clickedRaw.Y:F2},{clickedRaw.Z:F2}), " +
                $"range=({rangeX},{rangeY},{rangeZ}), threshold={maxDistanceThreshold:F2}, " +
                $"→ dist={bestDistance:F3}, symOp={bestSymOpIndex}, " +
                $"trans=({bestTranslation.X},{bestTranslation.Y},{bestTranslation.Z})");

            return (bestTranslation, bestSymOpIndex, bestDistance);
        }


        private (Vector3 translation, int symOpIndex, float screenDistSq)
    FindRevealMoleculeAtScreenPosition(
        Molecule molecule,
        System.Drawing.Point mousePos,
        int viewportWidth,
        int viewportHeight,
        Camera camera,
        int rangeX = 5,
        int rangeY = 5,
        int rangeZ = 10,
        int pixelTolerance = 60)   // promień w pikselach
        {
            if (molecule == null || camera == null || molecule.AsymmetricPart.Count == 0)
                return (Vector3.Zero, -1, float.MaxValue);

            // Symmetry operations
            var symOps = new List<SymmetryElements.ReadSymm>();
            if (molecule.SymmetryOperations.Count == 0)
            {
                var identity = new SymmetryElements.ReadSymm();
                identity.sign1 = new int[] { 1, 1, 1 };
                identity.sign2 = new int[] { 0, 0, 0 };
                identity.pos1 = new int[] { 0, 1, 2 };
                identity.pos2 = new int[] { 0, 0, 0 };
                identity.shift = new float[] { 0, 0, 0 };
                symOps.Add(identity);
            }
            else
            {
                symOps.AddRange(molecule.SymmetryOperations);
            }

            Matrix U = Matrix.Transpose(molecule.FractionaltoCarttesianMatrix(0, 0, 0));

            // AsymmetricPart centre in fractional coordinates
            Vector3 asymPartCenterFrac = Vector3.Zero;
            foreach (var asymAtom in molecule.AsymmetricPart)
            {
                asymPartCenterFrac += new Vector3(asymAtom.x, asymAtom.y, asymAtom.z);
            }
            asymPartCenterFrac /= molecule.AsymmetricPart.Count;

            // Macierz full transform (world → screen)
            Matrix fullTransform = camera.Scale * camera.Rotate * camera.View
                                  * camera.Translate * camera.Projection;

            int halfWidth = viewportWidth / 2;
            int halfHeight = viewportHeight / 2;

            float toleranceSq = pixelTolerance * pixelTolerance;

            // ═══════════════════════════════════════════════════════════
            // Loop over all candidates — for each, project the centre
            // ═══════════════════════════════════════════════════════════
            float bestDistSq = float.MaxValue;
            Vector3 bestTranslation = Vector3.Zero;
            int bestSymOpIndex = -1;

            float[] cxArr = new float[]
            {
        asymPartCenterFrac.X,
        asymPartCenterFrac.Y,
        asymPartCenterFrac.Z
            };

            for (int opIdx = 0; opIdx < symOps.Count; opIdx++)
            {
                var symOp = symOps[opIdx];

                float refX = cxArr[symOp.pos1[0]] * symOp.sign1[0]
                           + cxArr[symOp.pos2[0]] * symOp.sign2[0]
                           + symOp.shift[0];
                float refY = cxArr[symOp.pos1[1]] * symOp.sign1[1]
                           + cxArr[symOp.pos2[1]] * symOp.sign2[1]
                           + symOp.shift[1];
                float refZ = cxArr[symOp.pos1[2]] * symOp.sign1[2]
                           + cxArr[symOp.pos2[2]] * symOp.sign2[2]
                           + symOp.shift[2];

                for (int tx = -rangeX; tx <= rangeX; tx++)
                {
                    for (int ty = -rangeY; ty <= rangeY; ty++)
                    {
                        for (int tz = -rangeZ; tz <= rangeZ; tz++)
                        {
                            if (opIdx == 0 && tx == 0 && ty == 0 && tz == 0)
                                continue;

                            Vector3 candidateFrac = new Vector3(
                                refX + tx, refY + ty, refZ + tz);

                            Vector3 candidateRaw = Vector3.Transform(candidateFrac, U);

                            // ⭐ Konwersja raw → shifted (bo scene 3D rysuje shifted)
                            Vector3 candidateShifted = candidateRaw - molecule.MoleculeShift;

                            // ⭐ Projection of molecule centre onto the screen
                            Matrix world = Matrix.CreateTranslation(candidateShifted);
                            Matrix wvp = world * fullTransform;

                            float w = wvp.M44;
                            if (w <= 0) continue;   // behind camera

                            float screenX = halfWidth + wvp.M41 / w * halfWidth;
                            float screenY = halfHeight - wvp.M42 / w * halfHeight;

                            float dx = screenX - mousePos.X;
                            float dy = screenY - mousePos.Y;
                            float distSq = dx * dx + dy * dy;

                            if (distSq < bestDistSq)
                            {
                                bestDistSq = distSq;
                                bestTranslation = new Vector3(tx, ty, tz);
                                bestSymOpIndex = opIdx;
                            }
                        }
                    }
                }
            }

            // ⭐ Check whether the found candidate is within tolerance
            if (bestSymOpIndex < 0 || bestDistSq > toleranceSq)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[FindReveal] no candidate within {pixelTolerance}px " +
                    $"(best={Math.Sqrt(bestDistSq):F1}px)");
                return (Vector3.Zero, -1, bestDistSq);
            }

            System.Diagnostics.Debug.WriteLine(
                $"[FindReveal] FOUND — screenDist={Math.Sqrt(bestDistSq):F1}px, " +
                $"symOp={bestSymOpIndex}, " +
                $"trans=({bestTranslation.X},{bestTranslation.Y},{bestTranslation.Z})");

            return (bestTranslation, bestSymOpIndex, bestDistSq);
        }


        private List<Molecule.AtomSimple> GenerateTranslatedMoleculeWithSymmetry(
            Molecule molecule,
            Vector3 translation,
            int symOpIndex)
        {
            var result = new List<Molecule.AtomSimple>();

            if (molecule == null || molecule.AsymmetricPart.Count == 0)
                return result;

            var symOps = new List<SymmetryElements.ReadSymm>();
            if (molecule.SymmetryOperations.Count == 0)
            {
                var identity = new SymmetryElements.ReadSymm();
                identity.sign1 = new int[] { 1, 1, 1 };
                identity.sign2 = new int[] { 0, 0, 0 };
                identity.pos1 = new int[] { 0, 1, 2 };
                identity.pos2 = new int[] { 0, 0, 0 };
                identity.shift = new float[] { 0, 0, 0 };
                symOps.Add(identity);
            }
            else
            {
                symOps.AddRange(molecule.SymmetryOperations);
            }

            if (symOpIndex < 0 || symOpIndex >= symOps.Count)
                symOpIndex = 0;

            var symOp = symOps[symOpIndex];

            foreach (var asymAtom in molecule.AsymmetricPart)
            {
                float[] pos = new float[] { asymAtom.x, asymAtom.y, asymAtom.z };

                float newX = pos[symOp.pos1[0]] * symOp.sign1[0] + pos[symOp.pos2[0]] * symOp.sign2[0] + symOp.shift[0];
                float newY = pos[symOp.pos1[1]] * symOp.sign1[1] + pos[symOp.pos2[1]] * symOp.sign2[1] + symOp.shift[1];
                float newZ = pos[symOp.pos1[2]] * symOp.sign1[2] + pos[symOp.pos2[2]] * symOp.sign2[2] + symOp.shift[2];

                newX += translation.X;
                newY += translation.Y;
                newZ += translation.Z;

                var newAtom = asymAtom;
                newAtom.x = newX;
                newAtom.y = newY;
                newAtom.z = newZ;
                newAtom.Transformation = Matrix.Identity;

                result.Add(newAtom);
            }

            return result;
        }

        private bool CheckForCollisions(Molecule molecule, List<Molecule.AtomSimple> newAtoms, float minDistance = 0.5f)
        {
            if (molecule == null || molecule.NumberOfAtoms == 0 || newAtoms == null || newAtoms.Count == 0)
                return false;

            Matrix U = Matrix.Transpose(molecule.FractionaltoCarttesianMatrix(0, 0, 0));

            // ⭐ MoleculeShift — bring everything into NON-shifted Cartesian,
            //    bo:
            //      - newAtoms are NON-shifted (fractional → Cartesian without subtracting shift)
            //      - existing molecule.atom[] atoms are SHIFTED (Coordinate = rawCart - MoleculeShift)
            //    Without this we compare different coordinate systems and collisions are false.
            Vector3 shift = molecule.MoleculeShift;

            foreach (var newAtom in newAtoms)
            {
                // New atom in NON-shifted Cartesian
                Vector3 newPos = Vector3.Transform(new Vector3(newAtom.x, newAtom.y, newAtom.z), U);

                for (int i = 0; i < molecule.NumberOfAtoms; i++)
                {
                    // ⭐ Existing atom also in NON-shifted space (add shift back)
                    Vector3 existingPos = molecule.atom[i].Coordinate + shift;

                    float distance = Vector3.Distance(newPos, existingPos);

                    if (distance < minDistance)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[CheckForCollisions] KOLIZJA: nowy ({newAtom.name}) vs " +
                            $"existing ({molecule.atom[i].name}), distance = {distance:F4} Å " +
                            $"(< {minDistance})");
                        return true;
                    }
                }
            }

            return false;
        }
        #endregion
    }
}
