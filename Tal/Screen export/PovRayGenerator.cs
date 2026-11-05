// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Screen capture / exafterrt: AftervRayGenerator.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// AftervRayGenerator — eksafterrtuje current stan molecule do skryptu POV-Ray.
//
// ⭐ ELIPSOIDY ADP:
// Odtwarzamy transformation z Draw.DrawEllipsoidAtom:
// atomMatrix = Scale(1) * Transformation * Un * Scale(p) * Translation(aftersition)
//
// ⭐ DYSKI ELIPSOID:
// - Grubość STAŁA w Å: diskThickness / 3 (compensation via len_axis)
// - Radius scales with the ellipsoid
// - Kolejność: diskScale * shapeMatrix, then * rModel
//
// ⭐ WODÓR:
// - ShowHydrogenIsoSpheres = false → mała izotroafterwa kula
// - ShowHydrogenIsoSpheres = true → full ellipsoidsa (izosfera)
//
// ⭐ CAVITY:
// - Per-vertex colors (texture_list) TYLKO for CavityColorMap
//
// ⭐ POLYHEDRALS (style 4):
// - Atomy jak Balls & Sticks + przezroczyste faces afterliedrów
//
// ⭐ PŁASZCZYZNY (hkl + mean plane):
// - Corners są w RAW Cartesian (without MoleculeShift)
// - Przesuwamy o -MoleculeShift, so that trafić w przestrzeń atoms
// - mesh2 (quad 2 triangles, dwustronny przez inside_vector)
// - Krawędzie jako cienkie cylindry
//
// ⭐ TŁO:
// - Screen-aligned quad DALEKO ZA sceną (quadDist=500, halfSize=400)
// - Gradient color1 (top) / color2 (bottom)
//
// ⭐ KAMERA (KLUCZOWE):
// - XNA fov = PEŁNY angle PIONOWY.
// - POV-Ray `angle` = PEŁNY angle POZIOMY.
// - `right <-1.0, 0, 0>` NA STAŁE — POV-Ray sam scalee obraz
// z rozdzielczości renderu (+W / +H).
// ⚠️ NIE setiać right = aspect! To ściska obraz.
// - Przeliczenie: hFov = 2*atan(tan(vFov/2) * aspect)
// - CAMERA_FOV_STRETCH = 1.0f (samo przeliczenie wystarcza).
// - Rozdzielczość renderu (+W +H) MUSI have aspect = viewafterrt TAL,
// inaczej POV-Ray zdeformuje obraz.
//
// ⭐ KONTRAST / NASYCENIE (dostrojone — mniej przepaleń):
// - _globalBrightness 1.00 (światło -10%)
// - _lightingContrast 0.80 (kontrast -25%)
// - _emissionStrength 0.18 (emisja -20%)
// - _phongBoost 2.7 (refleksy -15%)
//
// ⭐ GUARDY:
// - IsValidSegment — chroni before "degenerate cylinder" w POV-Ray
// - All cylindry checksją length > 1e-4 before zapisem

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Globalization;
using System.Text;
using Tal.Cavity;
using Tal.Core.PowderDiffraction;
using Tal.DrawMethods;
using Tal.GUI;

namespace Tal
{
    internal class PovRayGenerator
    {
        #region Stałe skalowania modeli

        private const float BALL_MODEL_BASE_RADIUS = RuntimeIcosphere.BASE_RADIUS;
        private const float ROD_BASE_RADIUS = 1.0f;

        private const float DISK_THICKNESS_DIVISOR = 1.0f;

 // ⭐ Minimalna length segmentu cylindra
        private const float MIN_SEGMENT_LENGTH = 1e-4f;

        #endregion

        #region Stałe kontrastu / jasności / soczystości

 // ⭐ Dostrojone — mniej światła, mniej kontrastu, mniej emisji, mniej phong.
        private const float _globalBrightness = 1.00f;
        private const float _lightingContrast = 0.80f;
        private const float _phongBoost = 2.70f;
        private const float _saturationBoost = 2.30f;
        private const float _emissionStrength = 0.18f;

        #endregion

        #region Stałe kamery

 // ⭐ 1.0 = without dodatkowego stretchu (samo przeliczenie przez aspect).
        private const float CAMERA_FOV_STRETCH = 1.0f;

 // ⭐ Wysokość renderu w komentarzu (+H). Szerokość liczona z aspectu.
        private const int RENDER_HINT_HEIGHT = 1080;

 // ⭐ right w POV-Ray MUSI be stały = 1.0.
 // POV-Ray sam scalee obraz z rozdzielczości renderu (+W / +H).
 // Setienie right = aspect ściska obraz levelo.
        private const float CAMERA_RIGHT_FIXED = 1.33f;

        #endregion

        #region Formatowanie

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private static string F(float v) => v.ToString("0.######", Inv);
        private static string F(double v) => v.ToString("0.######", Inv);
        private static string V3(Vector3 v) => $"<{F(v.X)}, {F(v.Y)}, {F(v.Z)}>";

        #endregion

        #region Pola

        private readonly Molecule _molecule;
        private readonly StringBuilder _sb = new StringBuilder(1 << 22);

        private readonly Vector3 _bgTop;
        private readonly Vector3 _bgBottom;

        private readonly bool _light1Enabled;
        private readonly Vector3 _light1Direction;
        private readonly float _light1Diffuse;

        private readonly bool _light2Enabled;
        private readonly Vector3 _light2Direction;
        private readonly float _light2Diffuse;

        private readonly float _rawAmbient;
        private readonly float _rawDiffuse;
        private readonly float _rawSpecular;
        private readonly float _rawSpecularPower;

        private readonly Matrix _worldMatrix;

        private readonly float _globalTransmit;

        private readonly float _contrastAmbientFactor;
        private readonly float _contrastDiffuseFactor;
        private readonly float _contrastPhongFactor;
        private readonly float _contrastPhongSizeFactor;

        private readonly int _cavityDrawMode;

        private readonly float _diskThickness;
        private readonly float _diskBaseRadius;
        private readonly bool _disksEnabled;

        #endregion

        #region Konstruktor

        public PovRayGenerator(Molecule molecule)
        {
            _molecule = molecule ?? throw new ArgumentNullException(nameof(molecule));

            var f = Flags.Instance;

            _bgTop = ColorToVector3(f.color1);
            _bgBottom = ColorToVector3(f.color2);

            _light1Enabled = f.Enabled;
            _light1Direction = new Vector3(f.DirectionX, f.DirectionY, f.DirectionZ);
            _light1Diffuse = f.DiffuseIntensity;

            _light2Enabled = f.Light2Enabled;
            _light2Direction = new Vector3(
                f.Light2DirectionX,
                f.Light2DirectionY,
                f.Light2DirectionZ);
            _light2Diffuse = f.Light2DiffuseIntensity;

            _rawAmbient = f.AmbientIntensity;
            _rawDiffuse = f.DiffuseIntensity;
            _rawSpecular = f.SpecularIntensity;
            _rawSpecularPower = f.SpecularPower;

            _contrastAmbientFactor = Math.Clamp(1.2f - _lightingContrast * 0.7f, 0.15f, 1.0f);
            _contrastDiffuseFactor = 0.7f + 0.5f * Math.Clamp(_lightingContrast, 0f, 2f);
            _contrastPhongFactor = Math.Max(0.3f, 0.7f + 0.8f * (_lightingContrast - 0.5f));
            _contrastPhongSizeFactor = Math.Max(0.5f, 1.3f - 0.5f * (_lightingContrast - 0.5f));

            _globalTransmit = Math.Clamp(1.0f - f.transparency / 10.0f, 0f, 1f);

            var activeDoc = MainForm.Instance?.GetActiveDocument();
            var renderer = activeDoc?.GetCavityRenderer();
            _cavityDrawMode = renderer != null
                ? (int)renderer.CurrentDrawMode
                : (int)CavityRenderer.DrawMode.MarchingCubesMesh;

            _diskThickness = f.EllipsoidDiskThickness / DISK_THICKNESS_DIVISOR;
            _diskBaseRadius = f.EllipsoidBaseRadius;
            _disksEnabled = f.EllipsoidDisksEnabled;

            var cam = MainForm.Instance?.GetActiveCamera();
            if (cam != null)
            {
                Matrix scale = Matrix.CreateScale(cam.OneOverZoom);
                Matrix rotate = cam.Rotate;
                Matrix translate = cam.Translate;
                _worldMatrix = scale * rotate * translate;
            }
            else
            {
                _worldMatrix = Matrix.Identity;
            }

            System.Diagnostics.Debug.WriteLine(
                $"[PovRay] DiskThickness (raw)={f.EllipsoidDiskThickness:F4}, " +
                $"DiskThickness (used)={_diskThickness:F4}, " +
                $"BaseRadius={_diskBaseRadius:F3}");

            if (cam != null)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[PovRay] === CAMERA DEBUG ===");
                System.Diagnostics.Debug.WriteLine(
                    $"[PovRay] g_cameraPos={cam.g_cameraPos}");
                System.Diagnostics.Debug.WriteLine(
                    $"[PovRay] g_targetPos={cam.g_targetPos}");
                System.Diagnostics.Debug.WriteLine(
                    $"[PovRay] PerspectiveAngle={cam.PerspectiveAngle}");
                System.Diagnostics.Debug.WriteLine(
                    $"[PovRay] zoom={cam.zoom}");
                System.Diagnostics.Debug.WriteLine(
                    $"[PovRay] OneOverZoom={cam.OneOverZoom}");
                System.Diagnostics.Debug.WriteLine(
                    $"[PovRay] aspect={GetAspectRatio()}");
            }
        }

        #endregion

        #region Aspect ratio

 /// <summary>
 /// ⭐ Returns current aspect viewafterrtu TAL. Used do:
 /// - komentarza renderu (+W / +H) — so that POV-Ray nie rozciągał obrazu
 /// - przeliczenia fov pionew → levely
 /// ⚠️ NIE used w right (right jest stały = 1.0)!
 /// </summary>
        private float GetAspectRatio()
        {
            var camera = MainForm.Instance?.GetActiveCamera();
            var gd = camera?.GraphicsDevice;

            if (gd != null && gd.Viewport.Height > 0)
            {
                return (float)gd.Viewport.Width / gd.Viewport.Height;
            }

            return 16f / 9f;
        }

        #endregion

        #region Public API

        public void Save(string filePath)
        {
            if (_molecule == null || _molecule.NumberOfAtoms == 0)
                throw new InvalidOperationException("No molecule loaded.");

            _sb.Clear();
            WriteHeader();
            WriteCamera();
            WriteLights();
            WriteBackground();

            _sb.AppendLine("union {");
            WriteAtoms();
            if (Flags.Instance.ShowHBonds) WriteHydrogenBonds();
            if (Flags.Instance.Draw_cell) WriteUnitCell();
            if (Flags.Instance.Presentation == 4) WritePolyhedrals();
            WriteCrystalPlanes();
            WriteWorldMatrixInline();
            _sb.AppendLine("}");

            WriteFooter();
            File.WriteAllText(filePath, _sb.ToString(), Encoding.UTF8);

            System.Diagnostics.Debug.WriteLine($"[PovRay] Saved {_sb.Length} chars");
        }

        public void SaveWithCavities(string filePath)
        {
            if (_molecule == null || _molecule.NumberOfAtoms == 0)
                throw new InvalidOperationException("No molecule loaded.");

            _sb.Clear();
            WriteHeader();
            WriteCamera();
            WriteLights();
            WriteBackground();

            _sb.AppendLine("union {");
            WriteAtoms();
            if (Flags.Instance.ShowHBonds) WriteHydrogenBonds();
            if (Flags.Instance.Draw_cell) WriteUnitCell();
            if (Flags.Instance.Presentation == 4) WritePolyhedrals();
            WriteCrystalPlanes();
            WriteCavities();
            WriteWorldMatrixInline();
            _sb.AppendLine("}");

            WriteFooter();
            File.WriteAllText(filePath, _sb.ToString(), Encoding.UTF8);

            System.Diagnostics.Debug.WriteLine($"[PovRay] Saved with cavities {_sb.Length} chars");
        }

        #endregion

        #region Header / Footer

        private void WriteHeader()
        {
 // ⭐ Rozdzielczość renderu LICZONA Z ASPECTU viewafterrtu.
 // POV-Ray must renderować w tym samym aspekcie co right/up sceny,
 // inaczej deformuje obraz.
            float aspect = GetAspectRatio();
            int renderH = RENDER_HINT_HEIGHT;
            int renderW = (int)MathF.Round(renderH * aspect);

            _sb.AppendLine("// ============================================================");
            _sb.AppendLine("//  POV-Ray scene generated by TAL");
            _sb.AppendLine($"//  Author: Daniel M. Kaminski, {DateTime.Now:yyyy-MM-dd HH:mm}");
            _sb.AppendLine($"//  Molecule: {_molecule.Name}");
            _sb.AppendLine($"//  Atoms: {_molecule.NumberOfAtoms}");
            _sb.AppendLine($"//  Cell: a={F(_molecule.a)} b={F(_molecule.b)} c={F(_molecule.c)}");
            _sb.AppendLine($"//  Style: {Flags.Instance.Presentation}");
            _sb.AppendLine($"//  Gamma 2.2 | Sat {F(_saturationBoost)} | Phong {F(_phongBoost)}");
            _sb.AppendLine($"//  Contrast {F(_lightingContrast)} | Brightness {F(_globalBrightness)} | Emission {F(_emissionStrength)}");
            _sb.AppendLine($"//  FOV stretch: ×{F(CAMERA_FOV_STRETCH)}");
            _sb.AppendLine($"//  Aspect: {F(aspect)} (viewport)");
            _sb.AppendLine($"//  Disk thickness: {F(_diskThickness)} Å (flags/{F(DISK_THICKNESS_DIVISOR)})");
            _sb.AppendLine($"//  Render: povray +W{renderW} +H{renderH} +A0.1 +AM2 +R4 +FN16 scene.pov");
            _sb.AppendLine("// ============================================================");
            _sb.AppendLine();
            _sb.AppendLine("#version 3.7;");
            _sb.AppendLine("global_settings {");
            _sb.AppendLine("    assumed_gamma 2.2");
            _sb.AppendLine("    max_trace_level 10");
            _sb.AppendLine("}");
            _sb.AppendLine();
        }

        private void WriteFooter()
        {
            _sb.AppendLine();
            _sb.AppendLine("// --- end of scene ---");
        }

        #endregion

        #region World matrix

        private void WriteMatrix(Matrix m, string indent = "        ", float scale3x3 = 1f)
        {
            _sb.AppendLine($"{indent}matrix <");
            _sb.AppendLine($"{indent}    {F(m.M11 * scale3x3)}, {F(m.M12 * scale3x3)}, {F(m.M13 * scale3x3)},");
            _sb.AppendLine($"{indent}    {F(m.M21 * scale3x3)}, {F(m.M22 * scale3x3)}, {F(m.M23 * scale3x3)},");
            _sb.AppendLine($"{indent}    {F(m.M31 * scale3x3)}, {F(m.M32 * scale3x3)}, {F(m.M33 * scale3x3)},");
            _sb.AppendLine($"{indent}    {F(m.M41)}, {F(m.M42)}, {F(m.M43)}");
            _sb.AppendLine($"{indent}>");
        }

        private void WriteWorldMatrixInline()
        {
            _sb.AppendLine("    // ---- World transform (Scale * Rotate * Translate) ----");
            WriteMatrix(_worldMatrix, "    ", 1f);
        }

        #endregion

        #region Camera

 /// <summary>
 /// ⭐ XNA fov = PEŁNY angle PIONOWY (TAL: Matrix.CreatePerspectiveFieldOfView).
 /// ⭐ POV-Ray `angle` = PEŁNY angle POZIOMY.
 ///
 /// Afterprawne przeliczenie (nieliniowe przez tangens):
 /// halfV = radians(fovVertical) / 2
 /// halfH = atan(tan(halfV) * aspect)
 /// angle = degrees(2 * halfH)
 ///
 /// ⭐ KEY: `right <-1.0, 0, 0>` NA STAŁE (nie aspect!).
 /// POV-Ray sam scalee obraz z rozdzielczości renderu (+W / +H).
 /// Setienie right = aspect ściska obraz levelo.
 /// </summary>
        private void WriteCamera()
        {
            var camera = MainForm.Instance?.GetActiveCamera();

            Vector3 povPos, povTarget;
            float fovVerticalDeg;
            float aspectRatio = GetAspectRatio();

            if (camera != null)
            {
                povPos = camera.g_cameraPos;
                povTarget = camera.g_targetPos;

                fovVerticalDeg = camera.PerspectiveAngle;
                if (fovVerticalDeg < 0.1f) fovVerticalDeg = 0.1f;
            }
            else
            {
                povPos = new Vector3(0, 0, 80);
                povTarget = Vector3.Zero;
                fovVerticalDeg = 45f;
            }

 // ⭐ Przeliczenie pionew (XNA) → levely (POV-Ray) przez tangens
            float halfVRad = MathHelper.ToRadians(fovVerticalDeg) * 0.5f;
            float halfHRad = MathF.Atan(MathF.Tan(halfVRad) * aspectRatio);
            float hFovDeg = MathHelper.ToDegrees(halfHRad * 2f);

 // ⭐ Stretch PO przeliczeniu
            float povRayAngleDeg = hFovDeg * CAMERA_FOV_STRETCH;

 // Sanity clamp
            if (povRayAngleDeg > 170f) povRayAngleDeg = 170f;
            if (povRayAngleDeg < 0.1f) povRayAngleDeg = 0.1f;

            _sb.AppendLine("// ------------------------------------------------------------");
            _sb.AppendLine("//  Camera — POV-Ray native orientation");
            _sb.AppendLine($"//  position:      {V3(povPos)}");
            _sb.AppendLine($"//  target:        {V3(povTarget)}");
            _sb.AppendLine($"//  fov vertical:  {F(fovVerticalDeg)}° (TAL/XNA)");
            _sb.AppendLine($"//  aspect:        {F(aspectRatio)} (viewport)");
            _sb.AppendLine($"//  fov horizontal:{F(hFovDeg)}° (przeliczone)");
            _sb.AppendLine($"//  stretch:       ×{F(CAMERA_FOV_STRETCH)} → angle {F(povRayAngleDeg)}°");
            _sb.AppendLine("//  right = 1.0 — POV-Ray scales itself from resolution +W +H");
            _sb.AppendLine("// ------------------------------------------------------------");
            _sb.AppendLine("camera {");
            _sb.AppendLine($"    location {V3(povPos)}");
            _sb.AppendLine($"    look_at  {V3(povTarget)}");
            _sb.AppendLine("    up <0, 1, 0>");
            _sb.AppendLine("    right <-image_width/image_height, 0, 0>");   
            _sb.AppendLine($"    angle {F(povRayAngleDeg)}");
            _sb.AppendLine("}");
            _sb.AppendLine();
        }

        #endregion

        #region Lights

        private void WriteLights()
        {
            _sb.AppendLine("// ------------------------------------------------------------");
            _sb.AppendLine("//  Lights");
            _sb.AppendLine("// ------------------------------------------------------------");

            Vector3 l0 = SafeNormalize(_light1Direction, new Vector3(0, 0, -1));
            float diff0 = _light1Diffuse * _globalBrightness;

            if (_light1Enabled)
            {
                _sb.AppendLine("light_source {");
                _sb.AppendLine("    <0, 0, 0>");
                _sb.AppendLine($"    color rgb <{F(diff0)}, {F(diff0)}, {F(diff0)}>");
                _sb.AppendLine("    parallel");
                _sb.AppendLine($"    point_at {V3(l0)}");
                _sb.AppendLine("}");
                _sb.AppendLine();
            }

            if (_light2Enabled)
            {
                Vector3 l1 = SafeNormalize(_light2Direction, new Vector3(1, 0, 0));
                float diff1 = _light2Diffuse * _globalBrightness;

                _sb.AppendLine("light_source {");
                _sb.AppendLine("    <0, 0, 0>");
                _sb.AppendLine($"    color rgb <{F(diff1)}, {F(diff1)}, {F(diff1)}>");
                _sb.AppendLine("    parallel");
                _sb.AppendLine($"    point_at {V3(l1)}");
                _sb.AppendLine("}");
                _sb.AppendLine();
            }

            float ambient1 = _rawAmbient * _contrastAmbientFactor * _globalBrightness;
            float ambient2 = _light2Enabled
                ? Flags.Instance.Light2AmbientIntensity * _contrastAmbientFactor * _globalBrightness
                : 0f;
            float totalAmbient = ambient1 + ambient2 * 0.5f;

            _sb.AppendLine($"#declare GlobalAmbient = {F(totalAmbient)};");
            _sb.AppendLine();
        }

        #endregion

        #region Background — gradient quad FAR behind scene

        private void WriteBackground()
        {
            _sb.AppendLine("// ------------------------------------------------------------");
            _sb.AppendLine("//  Background — gradient quad FAR behind scene");
            _sb.AppendLine("//  (color1 = top, color2 = bottom)");
            _sb.AppendLine("// ------------------------------------------------------------");

            var camera = MainForm.Instance?.GetActiveCamera();

            if (camera == null)
            {
                WriteBackgroundSkySphere();
                return;
            }

            Vector3 camPos = camera.g_cameraPos;
            Vector3 camTarget = camera.g_targetPos;

            Vector3 forward = camTarget - camPos;
            float dist = forward.Length();
            if (dist < 1e-3f)
            {
                WriteBackgroundSkySphere();
                return;
            }
            forward /= dist;

            const float quadDist = 500f;
            Vector3 quadCenter = camPos + forward * quadDist;

            const float halfSize = 400f;

            Vector3 worldUp = Vector3.UnitY;
            Vector3 right = Vector3.Cross(forward, worldUp);
            if (right.LengthSquared() < 1e-6f)
            {
                worldUp = Vector3.UnitZ;
                right = Vector3.Cross(forward, worldUp);
            }
            right = Vector3.Normalize(right);
            Vector3 up = Vector3.Normalize(Vector3.Cross(right, forward));

            Vector3 bl = quadCenter - right * halfSize - up * halfSize;
            Vector3 br = quadCenter + right * halfSize - up * halfSize;
            Vector3 tr = quadCenter + right * halfSize + up * halfSize;
            Vector3 tl = quadCenter - right * halfSize + up * halfSize;

            _sb.AppendLine("mesh2 {");
            _sb.AppendLine("    vertex_vectors {");
            _sb.AppendLine("        4,");
            _sb.AppendLine($"        {V3(bl)},");
            _sb.AppendLine($"        {V3(br)},");
            _sb.AppendLine($"        {V3(tr)},");
            _sb.AppendLine($"        {V3(tl)}");
            _sb.AppendLine("    }");
            _sb.AppendLine("    uv_vectors {");
            _sb.AppendLine("        4,");
            _sb.AppendLine("        <0, 0>, <1, 0>, <1, 1>, <0, 1>");
            _sb.AppendLine("    }");
            _sb.AppendLine("    face_indices {");
            _sb.AppendLine("        2,");
            _sb.AppendLine("        <0, 1, 2>, <0, 2, 3>");
            _sb.AppendLine("    }");
            _sb.AppendLine("    uv_mapping");
            _sb.AppendLine("    texture {");
            _sb.AppendLine("        pigment {");
            _sb.AppendLine("            gradient y");
            _sb.AppendLine("            color_map {");
            _sb.AppendLine($"                [0.0 color rgb {V3(_bgBottom)}]");
            _sb.AppendLine($"                [1.0 color rgb {V3(_bgTop)}]");
            _sb.AppendLine("            }");
            _sb.AppendLine("        }");
            _sb.AppendLine("        finish { ambient 1 diffuse 0 }");
            _sb.AppendLine("    }");
            _sb.AppendLine("    no_shadow");
            _sb.AppendLine("}");
            _sb.AppendLine();
        }

        private void WriteBackgroundSkySphere()
        {
            _sb.AppendLine("sky_sphere {");
            _sb.AppendLine("    pigment {");
            _sb.AppendLine("        gradient y");
            _sb.AppendLine("        color_map {");
            _sb.AppendLine($"            [0.0 color rgb {V3(_bgBottom)}]");
            _sb.AppendLine($"            [1.0 color rgb {V3(_bgTop)}]");
            _sb.AppendLine("        }");
            _sb.AppendLine("    }");
            _sb.AppendLine("}");
            _sb.AppendLine();
        }

        #endregion

        #region Finish helpers

        private float GetAmbient()
            => _rawAmbient * _contrastAmbientFactor * _globalBrightness;

        private float GetDiffuse()
            => _rawDiffuse * _contrastDiffuseFactor * _globalBrightness;

        private float GetPhong()
            => _rawSpecular * _contrastPhongFactor * _phongBoost;

        private float GetPhongSize()
            => _rawSpecularPower * _contrastPhongSizeFactor;

        private static Vector3 Saturate(Vector3 color, float factor)
        {
            float lum = 0.2126f * color.X + 0.7152f * color.Y + 0.0722f * color.Z;
            Vector3 result = new Vector3(
                lum + (color.X - lum) * factor,
                lum + (color.Y - lum) * factor,
                lum + (color.Z - lum) * factor);

            return new Vector3(
                Math.Clamp(result.X, 0f, 1f),
                Math.Clamp(result.Y, 0f, 1f),
                Math.Clamp(result.Z, 0f, 1f));
        }

        private void WriteAtomFinish(Vector3 atomColor)
        {
            Vector3 emission = atomColor * _emissionStrength;

            _sb.AppendLine("    finish {");
            _sb.AppendLine($"        ambient {F(GetAmbient())}");
            _sb.AppendLine($"        diffuse {F(GetDiffuse())}");
            _sb.AppendLine($"        phong {F(GetPhong())}");
            _sb.AppendLine($"        phong_size {F(Math.Max(1f, GetPhongSize()))}");
            _sb.AppendLine("        specular 0.6");
            _sb.AppendLine("        roughness 0.008");
            _sb.AppendLine("        brilliance 0.6");
            _sb.AppendLine("        reflection {");
            _sb.AppendLine("            0.10");
            _sb.AppendLine("            fresnel on");
            _sb.AppendLine("            metallic 0.1");
            _sb.AppendLine("        }");
            _sb.AppendLine($"        emission rgb {V3(emission)}");
            _sb.AppendLine("    }");
        }

        private void WriteCavityFinish(Vector3 cavityColor)
        {
            Vector3 emission = cavityColor * _emissionStrength * 0.6f;

            _sb.AppendLine("        finish {");
            _sb.AppendLine($"            ambient {F(GetAmbient() * 1.5f)}");
            _sb.AppendLine($"            diffuse {F(GetDiffuse() * 0.9f)}");
            _sb.AppendLine($"            phong {F(GetPhong() * 0.7f)}");
            _sb.AppendLine($"            phong_size {F(Math.Max(1f, GetPhongSize()))}");
            _sb.AppendLine("            brilliance 0.6");
            _sb.AppendLine("            reflection {");
            _sb.AppendLine("                0.08");
            _sb.AppendLine("                fresnel on");
            _sb.AppendLine("            }");
            _sb.AppendLine($"            emission rgb {V3(emission)}");
            _sb.AppendLine("        }");
        }

        private void WriteLineFinish(Vector3 color)
        {
            _sb.AppendLine("        finish {");
            _sb.AppendLine($"            ambient {F(GetAmbient())}");
            _sb.AppendLine($"            diffuse {F(GetDiffuse())}");
            _sb.AppendLine($"            phong {F(GetPhong() * 0.4f)}");
            _sb.AppendLine($"            phong_size {F(Math.Max(1f, GetPhongSize()))}");
            _sb.AppendLine("        }");
        }

        private void WritePolyhedronFinish(Vector3 color)
        {
            Vector3 emission = color * _emissionStrength * 0.3f;

            _sb.AppendLine("        finish {");
            _sb.AppendLine($"            ambient {F(GetAmbient() * 0.7f)}");
            _sb.AppendLine($"            diffuse {F(GetDiffuse() * 1.0f)}");
            _sb.AppendLine($"            phong {F(GetPhong() * 1.5f)}");
            _sb.AppendLine($"            phong_size {F(Math.Max(1f, GetPhongSize() * 1.2f))}");
            _sb.AppendLine("            specular 0.3");
            _sb.AppendLine("            roughness 0.02");
            _sb.AppendLine("            brilliance 1.2");
            _sb.AppendLine("            reflection {");
            _sb.AppendLine("                0.25");
            _sb.AppendLine("                fresnel on");
            _sb.AppendLine("                metallic 0.0");
            _sb.AppendLine("            }");
            _sb.AppendLine($"            emission rgb {V3(emission)}");
            _sb.AppendLine("        }");
        }

        private void WriteCrystalPlaneFinish(Vector3 color)
        {
            Vector3 emission = color * _emissionStrength * 0.4f;

            _sb.AppendLine("        finish {");
            _sb.AppendLine($"            ambient {F(GetAmbient() * 1.3f)}");
            _sb.AppendLine($"            diffuse {F(GetDiffuse() * 0.8f)}");
            _sb.AppendLine($"            phong {F(GetPhong() * 0.5f)}");
            _sb.AppendLine($"            phong_size {F(Math.Max(1f, GetPhongSize()))}");
            _sb.AppendLine("            brilliance 1.0");
            _sb.AppendLine("            reflection {");
            _sb.AppendLine("                0.05");
            _sb.AppendLine("                fresnel on");
            _sb.AppendLine("            }");
            _sb.AppendLine($"            emission rgb {V3(emission)}");
            _sb.AppendLine("        }");
        }

        #endregion

        #region Segment guard

        private static bool IsValidSegment(Vector3 a, Vector3 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            float dz = a.Z - b.Z;
            return (dx * dx + dy * dy + dz * dz) > MIN_SEGMENT_LENGTH * MIN_SEGMENT_LENGTH;
        }

        private static bool IsFiniteVector(Vector3 v)
        {
            return !float.IsNaN(v.X) && !float.IsInfinity(v.X)
                && !float.IsNaN(v.Y) && !float.IsInfinity(v.Y)
                && !float.IsNaN(v.Z) && !float.IsInfinity(v.Z);
        }

        #endregion

        #region Atoms

        private void WriteAtoms()
        {
            _sb.AppendLine("// ------------------------------------------------------------");
            _sb.AppendLine("//  Atoms");
            _sb.AppendLine("// ------------------------------------------------------------");

            var mol = _molecule;
            int n = mol.NumberOfAtoms;

            // atom[] is the current 3D presentation (asymmetric / selected symmetry ops / packing).
            // Selective symmetry (EnabledSymmetryOps) is already applied when Presentation() rebuilds atom[].
            bool showH = Flags.Instance.ShowHydrogens;

            for (int i = 0; i < n; i++)
            {
                ref var atom = ref mol.atom[i];
                if (!atom.show) continue;
                if (atom.style == 10) continue;
                if (!showH && atom.AtomicNumber == 1) continue;

                if (!IsFiniteVector(atom.Coordinate))
                    continue;

                Vector3 color = GetAtomColorVec(i);
                string colStr = $"{F(color.X)}, {F(color.Y)}, {F(color.Z)}";

                float alpha = Math.Clamp(atom.occupancy, 0.05f, 1.0f);
                float transmit = 1.0f - alpha;

                Vector3 pos = atom.Coordinate;

                switch (atom.style)
                {
                    case 0:
                        WriteAtomSphere(i, pos, colStr, color, transmit,
                            GetVdwRadiusForPov(atom.AtomicNumber) / RuntimeIcosphere.BASE_RADIUS);
                        break;
                    case 1:
                        WriteCovalentBonds(i, transmit);
                        break;
                    case 2:
                        WriteCovalentBonds(i, transmit);
                        WriteAtomSphere(i, pos, colStr, color, transmit,
                            GetBallRadius(atom.AtomicNumber));
                        break;
                    case 3:
                        WriteCovalentBonds(i, transmit);
                        WriteAtomEllipsoid(i, pos, colStr, color, transmit);
                        break;
                    case 4:
                        WriteCovalentBonds(i, transmit);
                        WriteAtomSphere(i, pos, colStr, color, transmit,
                            GetBallRadius(atom.AtomicNumber));
                        break;
                    default:
                        WriteAtomSphere(i, pos, colStr, color, transmit,
                            GetBallRadius(atom.AtomicNumber));
                        break;
                }
            }

            _sb.AppendLine();
        }

        private void WriteAtomSphere(int index, Vector3 pos, string colStr, Vector3 color,
                                       float transmit, float radius)
        {
            float povRadius = radius * BALL_MODEL_BASE_RADIUS;
            if (povRadius <= 1e-4f) povRadius = 0.01f;
            if (!IsFiniteVector(pos)) return;

            _sb.AppendLine($"    // Atom {index}: {_molecule.atom[index].name}");
            _sb.AppendLine("    sphere {");
            _sb.AppendLine($"        {V3(pos)}, {F(povRadius)}");
            _sb.AppendLine("        pigment {");
            _sb.AppendLine($"            color rgbt <{colStr}, {F(transmit)}>");
            _sb.AppendLine("        }");
            WriteAtomFinish(color);
            _sb.AppendLine("    }");
        }

        private void WriteAtomEllipsoid(int index, Vector3 pos, string colStr, Vector3 color,
                                          float transmit)
        {
            ref var atom = ref _molecule.atom[index];

            bool isHydrogen = atom.AtomicNumber == 1;
            bool showHydrogenAsSphere = isHydrogen && !Flags.Instance.ShowHydrogenIsoSpheres;

            if (showHydrogenAsSphere)
            {
                float baseRadius = IsMetal(atom.AtomicNumber)
                    ? Flags.Instance.BallRadiusMetal
                    : Flags.Instance.BallRadiusNonMetal;
                float ballRadius = baseRadius * Flags.Instance.HydrogenBallScale;
                WriteAtomSphere(index, pos, colStr, color, transmit, ballRadius);
                return;
            }

            if (atom.Un == Matrix.Identity)
            {
                WriteAtomSphere(index, pos, colStr, color, transmit, GetBallRadius(atom.AtomicNumber));
                return;
            }

            float probability = Flags.Instance.ATP_Probability;

            Matrix atomMatrixXna = Matrix.Identity;
            atomMatrixXna *= atom.Transformation;
            atomMatrixXna *= atom.Un;
            atomMatrixXna *= Matrix.CreateScale(probability, probability, probability);
            atomMatrixXna *= Matrix.CreateTranslation(pos);

            float rModel = BALL_MODEL_BASE_RADIUS;

            _sb.AppendLine($"    // Atom {index}: {atom.name} (ellipsoid ADP, prob={F(probability)})");
            _sb.AppendLine("    sphere {");
            _sb.AppendLine("        <0, 0, 0>, 1");
            WriteMatrix(atomMatrixXna, "        ", rModel);
            _sb.AppendLine("        pigment {");
            _sb.AppendLine($"            color rgbt <{colStr}, {F(transmit)}>");
            _sb.AppendLine("        }");
            WriteAtomFinish(color);
            _sb.AppendLine("    }");

            if (_disksEnabled && !isHydrogen)
            {
                WriteEllipsoidDisks(index, pos, probability, atom.Transformation, atom.Un);
            }
        }

        private void WriteEllipsoidDisks(int index, Vector3 pos, float probability,
                                          Matrix transformation, Matrix un)
        {
            float diskThickness = _diskThickness;
            float baseRadius = _diskBaseRadius;

            Matrix shapeMatrix = transformation;
            shapeMatrix *= un;
            shapeMatrix *= Matrix.CreateScale(probability, probability, probability);

            Vector3 axisX = new Vector3(shapeMatrix.M11, shapeMatrix.M12, shapeMatrix.M13);
            Vector3 axisY = new Vector3(shapeMatrix.M21, shapeMatrix.M22, shapeMatrix.M23);
            Vector3 axisZ = new Vector3(shapeMatrix.M31, shapeMatrix.M32, shapeMatrix.M33);

            float lenX = axisX.Length();
            float lenY = axisY.Length();
            float lenZ = axisZ.Length();

            float rModel = BALL_MODEL_BASE_RADIUS;

            for (int elips = 0; elips < 3; elips++)
            {
                float sx, sy, sz;

                if (elips == 0)
                {
                    sx = (lenX > 1e-6f) ? diskThickness / (lenX * rModel) : baseRadius;
                    sy = baseRadius;
                    sz = baseRadius;
                }
                else if (elips == 1)
                {
                    sx = baseRadius;
                    sy = (lenY > 1e-6f) ? diskThickness / (lenY * rModel) : baseRadius;
                    sz = baseRadius;
                }
                else
                {
                    sx = baseRadius;
                    sy = baseRadius;
                    sz = (lenZ > 1e-6f) ? diskThickness / (lenZ * rModel) : baseRadius;
                }

                Matrix diskScale = Matrix.CreateScale(sx, sy, sz);
                Matrix diskMatrix = diskScale * shapeMatrix;

                diskMatrix.M11 *= rModel; diskMatrix.M12 *= rModel; diskMatrix.M13 *= rModel;
                diskMatrix.M21 *= rModel; diskMatrix.M22 *= rModel; diskMatrix.M23 *= rModel;
                diskMatrix.M31 *= rModel; diskMatrix.M32 *= rModel; diskMatrix.M33 *= rModel;

                diskMatrix.M41 = pos.X;
                diskMatrix.M42 = pos.Y;
                diskMatrix.M43 = pos.Z;

                _sb.AppendLine($"    // Atom {index} disk {elips} " +
                               $"(thickness={F(diskThickness)} Å, radius={F(baseRadius)})");
                _sb.AppendLine("    sphere {");
                _sb.AppendLine("        <0, 0, 0>, 1");
                _sb.AppendLine("        matrix <");
                _sb.AppendLine($"            {F(diskMatrix.M11)}, {F(diskMatrix.M12)}, {F(diskMatrix.M13)},");
                _sb.AppendLine($"            {F(diskMatrix.M21)}, {F(diskMatrix.M22)}, {F(diskMatrix.M23)},");
                _sb.AppendLine($"            {F(diskMatrix.M31)}, {F(diskMatrix.M32)}, {F(diskMatrix.M33)},");
                _sb.AppendLine($"            {F(diskMatrix.M41)}, {F(diskMatrix.M42)}, {F(diskMatrix.M43)}");
                _sb.AppendLine("        >");
                _sb.AppendLine("        pigment { color rgbt <0, 0, 0, 0> }");
                _sb.AppendLine("        finish { ambient 0 diffuse 0 phong 0 }");
                _sb.AppendLine("    }");
            }
        }

        private void WriteCovalentBonds(int index, float transmit)
        {
            ref var atom = ref _molecule.atom[index];
            if (atom.NrBondedAtoms == 0) return;

            Vector3 p1 = atom.Coordinate;

            float povStickRadius = Flags.Instance.StickRadius * ROD_BASE_RADIUS;
            if (povStickRadius <= 1e-5f) povStickRadius = 0.005f;

            for (int j = 0; j < atom.NrBondedAtoms; j++)
            {
                int otherIdx = atom.BondedAtom[j];
                if (otherIdx < 0 || otherIdx >= _molecule.NumberOfAtoms) continue;

                ref var other = ref _molecule.atom[otherIdx];
                if (!other.show || other.style == 10) continue;
                if (!Flags.Instance.ShowHydrogens && other.AtomicNumber == 1) continue;
                if (otherIdx < index) continue;

                Vector3 p2 = other.Coordinate;

                if (!IsValidSegment(p1, p2)) continue;

                Vector3 mid = (p1 + p2) * 0.5f;

                if (!IsValidSegment(p1, mid)) continue;
                if (!IsValidSegment(mid, p2)) continue;

                Vector3 colorA = GetAtomColorVec(index);
                Vector3 colorB = GetAtomColorVec(otherIdx);
                string colA = $"{F(colorA.X)}, {F(colorA.Y)}, {F(colorA.Z)}";
                string colB = $"{F(colorB.X)}, {F(colorB.Y)}, {F(colorB.Z)}";

                _sb.AppendLine($"    // Bond {index}({atom.name})-{otherIdx}({other.name})");

                WriteCylinderSegment(p1, mid, povStickRadius, colA, colorA, transmit);
                WriteCylinderSegment(mid, p2, povStickRadius, colB, colorB, transmit);
            }
        }

        private void WriteCylinderSegment(Vector3 a, Vector3 b, float radius,
                                            string colStr, Vector3 color, float transmit)
        {
            if (!IsValidSegment(a, b)) return;

            _sb.AppendLine("    cylinder {");
            _sb.AppendLine($"        {V3(a)}, {V3(b)}, {F(radius)}");
            _sb.AppendLine("        pigment {");
            _sb.AppendLine($"            color rgbt <{colStr}, {F(transmit)}>");
            _sb.AppendLine("        }");
            WriteAtomFinish(color);
            _sb.AppendLine("    }");
        }

        #endregion

        #region Hydrogen bonds

        private void WriteHydrogenBonds()
        {
            var mol = _molecule;
            if (mol == null) return;

            _sb.AppendLine("// ------------------------------------------------------------");
            _sb.AppendLine("//  Hydrogen bonds");
            _sb.AppendLine("// ------------------------------------------------------------");

            float hbRadius = Flags.Instance.HBondThickness * ROD_BASE_RADIUS;
            if (hbRadius <= 1e-5f) hbRadius = 0.005f;

            float dashLen = Math.Max(Flags.Instance.HBondDashLength, 0.01f);
            float gapLen = Math.Max(Flags.Instance.HBondGapLength, 0.01f);
            bool dashed = Flags.Instance.HBondDashed;

            for (int i = 0; i < mol.NumberOfAtoms; i++)
            {
                ref var atom = ref mol.atom[i];
                if (!atom.show || !atom.UseForHBonds) continue;
                if (atom.H_NrBondedAtoms == 0) continue;
                if (!IsFiniteVector(atom.Coordinate)) continue;

                Vector3 donorPos = atom.Coordinate;

                for (int j = 0; j < atom.H_NrBondedAtoms; j++)
                {
                    int acceptorIdx = atom.H_BondedAtom[j];
                    if (acceptorIdx < 0 || acceptorIdx >= mol.NumberOfAtoms) continue;
                    if (!mol.atom[acceptorIdx].show || !mol.atom[acceptorIdx].UseForHBonds) continue;
                    if (acceptorIdx < i) continue;

                    Vector3 acceptorPos = mol.atom[acceptorIdx].Coordinate;

                    if (!IsValidSegment(donorPos, acceptorPos)) continue;

                    Vector3 greenCol = Flags.Instance.HBondColor;
                    string col = $"{F(greenCol.X)}, {F(greenCol.Y)}, {F(greenCol.Z)}";

                    if (dashed)
                        WriteDashedCylinder(donorPos, acceptorPos, hbRadius, dashLen, gapLen, col, greenCol);
                    else
                        WriteSolidCylinderGreen(donorPos, acceptorPos, hbRadius, col, greenCol);
                }
            }

            _sb.AppendLine();
        }

        private void WriteSolidCylinderGreen(Vector3 a, Vector3 b, float radius,
                                                string colStr, Vector3 color)
        {
            if (!IsValidSegment(a, b)) return;

            _sb.AppendLine("    cylinder {");
            _sb.AppendLine($"        {V3(a)}, {V3(b)}, {F(radius)}");
            _sb.AppendLine($"        pigment {{ color rgb <{colStr}> }}");
            WriteLineFinish(color);
            _sb.AppendLine("    }");
        }

        private void WriteDashedCylinder(Vector3 a, Vector3 b, float radius,
                                          float dashLen, float gapLen, string colStr, Vector3 color)
        {
            Vector3 dir = b - a;
            float len = dir.Length();

            if (len < 1e-3f) return;
            dir /= len;

            dashLen = Math.Max(dashLen, 0.01f);
            gapLen = Math.Max(gapLen, 0.01f);

            float pattern = dashLen + gapLen;
            if (pattern <= 1e-4f) pattern = 0.1f;

            int segments = (int)(len / pattern);
            float remaining = len - segments * pattern;

            for (int k = 0; k < segments; k++)
            {
                float t0 = (k * pattern) / len;
                float t1 = (k * pattern + dashLen) / len;
                Vector3 p0 = a + dir * (t0 * len);
                Vector3 p1 = a + dir * (t1 * len);

                if (!IsValidSegment(p0, p1)) continue;

                WriteSolidCylinderGreen(p0, p1, radius, colStr, color);
            }

            if (remaining > 1e-3f)
            {
                float t0 = (segments * pattern) / len;
                float t1 = Math.Min(1.0f, (segments * pattern + dashLen) / len);
                if (t1 > t0 + 1e-4f)
                {
                    Vector3 p0 = a + dir * (t0 * len);
                    Vector3 p1 = a + dir * (t1 * len);

                    if (IsValidSegment(p0, p1))
                        WriteSolidCylinderGreen(p0, p1, radius, colStr, color);
                }
            }
        }

        #endregion

        #region Unit cell

        private void WriteUnitCell()
        {
            var mol = _molecule;
            if (mol.UnitCellFrame == null || mol.UnitCellFrame.Count == 0) return;

            _sb.AppendLine("// ------------------------------------------------------------");
            _sb.AppendLine("//  Unit cell");
            _sb.AppendLine("// ------------------------------------------------------------");

            float cellRadius = Flags.Instance.StickRadius * 0.8f * ROD_BASE_RADIUS;
            if (cellRadius <= 1e-5f) cellRadius = 0.005f;

            for (int i = 0; i < mol.UnitCellFrame.Count; i++)
            {
                var edge = mol.UnitCellFrame[i];

                if (edge.distance < 1e-3f) continue;

                float cz = MathF.Cos(edge.AngleZ);
                float sz = MathF.Sin(edge.AngleZ);
                float cxy = MathF.Cos(edge.AngleXY);
                float sxy = MathF.Sin(edge.AngleXY);

                Vector3 dirXna = new Vector3(cz * cxy, cz * sxy, -sz);

                Vector3 startXna = edge.position;
                Vector3 endXna = edge.position + dirXna * edge.distance;

                if (!IsFiniteVector(startXna) || !IsFiniteVector(endXna)) continue;
                if (!IsValidSegment(startXna, endXna)) continue;

                Vector3 edgeColor = Saturate(edge.color, _saturationBoost);
                string colStr = $"{F(edgeColor.X)}, {F(edgeColor.Y)}, {F(edgeColor.Z)}";

                _sb.AppendLine("    cylinder {");
                _sb.AppendLine($"        {V3(startXna)}, {V3(endXna)}, {F(cellRadius)}");
                _sb.AppendLine($"        pigment {{ color rgb <{colStr}> }}");
                WriteLineFinish(edgeColor);
                _sb.AppendLine("    }");
            }

            _sb.AppendLine();
        }

        #endregion

        #region Crystal Planes

        private void WriteCrystalPlanes()
        {
            var planes = _molecule.Planes;
            if (planes == null || planes.Count == 0) return;

            var visible = new List<CrystalPlane>();
            foreach (var p in planes)
            {
                if (p == null || !p.Visible) continue;
                if (p.Corners == null || p.Corners.Length < 4) continue;
                visible.Add(p);
            }

            if (visible.Count == 0) return;

            _sb.AppendLine("// ------------------------------------------------------------");
            _sb.AppendLine($"//  Crystal planes ({visible.Count})");
            _sb.AppendLine("// ------------------------------------------------------------");

            Vector3 negShift = -_molecule.MoleculeShift;

            foreach (var plane in visible)
            {
                Vector3 c0 = plane.Corners[0] + negShift;
                Vector3 c1 = plane.Corners[1] + negShift;
                Vector3 c2 = plane.Corners[2] + negShift;
                Vector3 c3 = plane.Corners[3] + negShift;

                if (!IsFiniteVector(c0) || !IsFiniteVector(c1) ||
                    !IsFiniteVector(c2) || !IsFiniteVector(c3))
                    continue;

                Vector3 rawCol = new Vector3(
                    plane.Color.R / 255f,
                    plane.Color.G / 255f,
                    plane.Color.B / 255f);
                Vector3 col = Saturate(rawCol, _saturationBoost);
                string colStr = $"{F(col.X)}, {F(col.Y)}, {F(col.Z)}";

                float alpha = plane.Transparent ? 0.35f : 0.85f;
                float transmit = 1.0f - alpha;

                _sb.AppendLine($"    // Plane: {plane.Name} ({plane.GetDescription()})");

                _sb.AppendLine("    mesh2 {");
                _sb.AppendLine("        vertex_vectors {");
                _sb.AppendLine("            4,");
                _sb.AppendLine($"            {V3(c0)},");
                _sb.AppendLine($"            {V3(c1)},");
                _sb.AppendLine($"            {V3(c2)},");
                _sb.AppendLine($"            {V3(c3)}");
                _sb.AppendLine("        }");
                _sb.AppendLine("        face_indices {");
                _sb.AppendLine("            2,");
                _sb.AppendLine("            <0, 1, 2>,");
                _sb.AppendLine("            <0, 2, 3>");
                _sb.AppendLine("        }");
                _sb.AppendLine("        inside_vector <0, 0, 1>");
                _sb.AppendLine("        pigment {");
                _sb.AppendLine($"            color rgbt <{colStr}, {F(transmit)}>");
                _sb.AppendLine("        }");
                WriteCrystalPlaneFinish(col);
                _sb.AppendLine("    }");

                Vector3 edgeRaw = rawCol * 0.7f;
                Vector3 edgeCol = Saturate(edgeRaw, _saturationBoost);
                string edgeColStr = $"{F(edgeCol.X)}, {F(edgeCol.Y)}, {F(edgeCol.Z)}";

                const float edgeRadius = 0.025f;

                WritePlaneEdge(c0, c1, edgeRadius, edgeColStr, edgeCol);
                WritePlaneEdge(c1, c2, edgeRadius, edgeColStr, edgeCol);
                WritePlaneEdge(c2, c3, edgeRadius, edgeColStr, edgeCol);
                WritePlaneEdge(c3, c0, edgeRadius, edgeColStr, edgeCol);
            }

            _sb.AppendLine();
        }

        private void WritePlaneEdge(Vector3 a, Vector3 b, float radius,
                                     string colStr, Vector3 color)
        {
            if (!IsValidSegment(a, b)) return;

            _sb.AppendLine("    cylinder {");
            _sb.AppendLine($"        {V3(a)}, {V3(b)}, {F(radius)}");
            _sb.AppendLine($"        pigment {{ color rgb <{colStr}> }}");
            WriteLineFinish(color);
            _sb.AppendLine("    }");
        }

        #endregion

        #region Polyhedrals

        private void WritePolyhedrals()
        {
            var polyhedrals = Polihedrals.VertexesList;
            if (polyhedrals == null || polyhedrals.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[PovRay.Polyhedrals] No polyhedra in VertexesList — skipping");
                return;
            }

            _sb.AppendLine("// ------------------------------------------------------------");
            _sb.AppendLine("//  Coordination polyhedra (transparent)");
            _sb.AppendLine($"//  Count: {polyhedrals.Count}");
            _sb.AppendLine("// ------------------------------------------------------------");

            float polyTransmit = Math.Clamp(1.0f - Flags.Instance.PolyhedronAlpha, 0f, 1f);

            for (int p = 0; p < polyhedrals.Count; p++)
            {
                var vs = polyhedrals[p];

                if (vs.vertices_n == null || vs.vertices_n.Length == 0)
                    continue;

                Vector3 color = vs.color;

                string typeName = vs.Type.ToString();
                int ligandCount = vs.LigandCount;

                WritePolyhedronMesh(vs.vertices_n,
                                    vs.position,
                                    color, polyTransmit,
                                    p, "front", typeName, ligandCount);

                if (vs.vertices_s != null && vs.vertices_s.Length > 0)
                {
                    WritePolyhedronMesh(vs.vertices_s,
                                        vs.position,
                                        color * 0.7f, polyTransmit,
                                        p, "back", typeName, ligandCount);
                }
            }

            _sb.AppendLine();
        }

        private void WritePolyhedronMesh(
            VertexPositionNormalTexture[] vertices,
            Vector3 center,
            Vector3 color,
            float transmit,
            int polyIndex,
            string faceName,
            string typeName,
            int ligandCount)
        {
            int vertexCount = vertices.Length;
            int triangleCount = vertexCount / 3;

            if (triangleCount == 0) return;

            Vector3 satColor = Saturate(color, _saturationBoost);
            string colStr = $"{F(satColor.X)}, {F(satColor.Y)}, {F(satColor.Z)}";

            _sb.AppendLine($"    // Polyhedron #{polyIndex} ({typeName}, {ligandCount} ligands) — {faceName}");
            _sb.AppendLine("    mesh2 {");

            _sb.AppendLine("        vertex_vectors {");
            _sb.AppendLine($"            {vertexCount},");
            for (int i = 0; i < vertexCount; i++)
            {
                Vector3 pos = vertices[i].Position + center;
                string sep = (i == vertexCount - 1) ? "" : ",";
                _sb.AppendLine($"            <{F(pos.X)}, {F(pos.Y)}, {F(pos.Z)}>{sep}");
            }
            _sb.AppendLine("        }");

            _sb.AppendLine("        normal_vectors {");
            _sb.AppendLine($"            {vertexCount},");
            for (int i = 0; i < vertexCount; i++)
            {
                Vector3 n = vertices[i].Normal;
                string sep = (i == vertexCount - 1) ? "" : ",";
                _sb.AppendLine($"            <{F(n.X)}, {F(n.Y)}, {F(n.Z)}>{sep}");
            }
            _sb.AppendLine("        }");

            _sb.AppendLine("        face_indices {");
            _sb.AppendLine($"            {triangleCount},");
            for (int t = 0; t < triangleCount; t++)
            {
                int i0 = t * 3;
                int i1 = t * 3 + 1;
                int i2 = t * 3 + 2;
                string sep = (t == triangleCount - 1) ? "" : ",";
                _sb.AppendLine($"            <{i0}, {i1}, {i2}>{sep}");
            }
            _sb.AppendLine("        }");

            _sb.AppendLine("        inside_vector <0, 1, 0>");

            _sb.AppendLine("        pigment {");
            _sb.AppendLine($"            color rgbt <{colStr}, {F(transmit)}>");
            _sb.AppendLine("        }");

            WritePolyhedronFinish(satColor);

            _sb.AppendLine("    }");
            _sb.AppendLine();
        }

        #endregion

        #region Cavities

        private void WriteCavities()
        {
            var mainForm = MainForm.Instance;
            if (mainForm == null) return;

            var doc = mainForm.GetActiveDocument();
            if (doc == null) return;

            var renderer = doc.GetCavityRenderer();
            if (renderer == null) return;

            // Match on-screen visibility:
            // - Clear Cavities → HasCavities == false
            // - Draw mode "Hidden" (radio / CurrentDrawMode) → no mesh on screen
            // Previously POV always dumped mesh2 whenever meshes still lived in
            // the renderer, even after the user turned cavities off.
            if (!renderer.HasCavities)
            {
                System.Diagnostics.Debug.WriteLine("[PovRay.Cav] Skip — no cavities on renderer");
                return;
            }

            // Use LIVE renderer mode only. Flags.CavityDrawMode defaults to 0 (Hidden)
            // and often lags behind the UI — that incorrectly skipped all cavity meshes.
            if (renderer.CurrentDrawMode == CavityRenderer.DrawMode.Hidden)
            {
                System.Diagnostics.Debug.WriteLine("[PovRay.Cav] Skip — CurrentDrawMode is Hidden");
                return;
            }

            // Same set as on-screen: Visible on Cavity + RenderCavity (checkboxes)
            var renderCavities = renderer.GetVisibleRenderCavitiesForExport();
            if (renderCavities == null || renderCavities.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[PovRay.Cav] Skip — no visible cavities for export");
                return;
            }

            var candidates = new List<CavityRenderer.RenderCavity>();

            foreach (var rc in renderCavities)
            {
                bool hasMesh = rc.MarchingCubesGenerated
                    && rc.MarchingCubesMesh != null
                    && rc.MarchingCubesMesh.IsValid
                    && rc.MarchingCubesMesh.TriangleCount > 0;

                if (!hasMesh
                    && rc.BaseMarchingCubesMesh != null
                    && rc.BaseMarchingCubesMesh.IsValid
                    && rc.BaseMarchingCubesMesh.TriangleCount > 0)
                {
                    hasMesh = true;
                }

                if (hasMesh)
                    candidates.Add(rc);
            }

            if (candidates.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[PovRay.Cav] Skip — 0 meshes among {renderCavities.Count} visible cavities");
                return;
            }

            System.Diagnostics.Debug.WriteLine(
                $"[PovRay.Cav] Exporting {candidates.Count} visible cavities " +
                $"(ids: {string.Join(",", candidates.Select(c => c.CavityId))}), drawMode={_cavityDrawMode}");

            _sb.AppendLine("    // ------------------------------------------------------------");
            _sb.AppendLine("    //  Cavities (mesh2)");
            _sb.AppendLine("    // ------------------------------------------------------------");

            foreach (var rc in candidates)
            {
                WriteSingleCavity(rc);
            }

            _sb.AppendLine();
        }

        private Vector3[] GetCavityNormals(CavityRenderer.RenderCavity rc)
        {
            var mesh = rc.MarchingCubesMesh;
            if (mesh == null || !mesh.IsValid) return Array.Empty<Vector3>();

            int vCount = mesh.Vertices.Count;

            if (rc.CachedNormals != null && rc.CachedNormals.Length == vCount)
            {
                var copy = new Vector3[vCount];
                for (int i = 0; i < vCount; i++)
                    copy[i] = -rc.CachedNormals[i];
                return copy;
            }

            var verts = mesh.Vertices;
            var inds = mesh.Indices;
            var normals = new Vector3[vCount];

            for (int i = 0; i < inds.Count; i += 3)
            {
                int i0 = inds[i];
                int i1 = inds[i + 1];
                int i2 = inds[i + 2];

                if (i0 >= vCount || i1 >= vCount || i2 >= vCount) continue;

                Vector3 v0 = verts[i0];
                Vector3 v1 = verts[i1];
                Vector3 v2 = verts[i2];

                Vector3 n = Vector3.Cross(v1 - v0, v2 - v0);
                float lenSq = n.LengthSquared();
                if (lenSq > 1e-6f)
                {
                    n /= MathF.Sqrt(lenSq);
                    normals[i0] += n;
                    normals[i1] += n;
                    normals[i2] += n;
                }
            }

            for (int i = 0; i < vCount; i++)
            {
                float lenSq = normals[i].LengthSquared();
                normals[i] = lenSq > 1e-6f
                    ? Vector3.Normalize(normals[i])
                    : Vector3.Up;
            }

            return normals;
        }

        private void WriteSingleCavity(CavityRenderer.RenderCavity rc)
        {
            var mesh = rc.MarchingCubesMesh;
            if (mesh == null || !mesh.IsValid || mesh.TriangleCount == 0)
                mesh = rc.BaseMarchingCubesMesh;
            if (mesh == null || !mesh.IsValid || mesh.TriangleCount == 0) return;

            var verts = mesh.Vertices;
            var inds = mesh.Indices;

            int vCount = verts.Count;
            int triCount = inds.Count / 3;

            var normals = GetCavityNormals(rc);
            bool useNormals = normals != null && normals.Length == vCount;

            bool useColorMap = (_cavityDrawMode == (int)CavityRenderer.DrawMode.CavityColorMap);

            bool hasVertexColors = useColorMap
                                    && rc.HasVertexColors
                                    && rc.VertexColors4 != null
                                    && rc.VertexColors4.Length == vCount;

            Vector4 baseCol = rc.BaseColor4;
            Vector3 cavCol = new Vector3(baseCol.X, baseCol.Y, baseCol.Z);
            cavCol = Saturate(cavCol, _saturationBoost);
            baseCol = new Vector4(cavCol.X, cavCol.Y, cavCol.Z, baseCol.W);
            string colStr = $"{F(baseCol.X)}, {F(baseCol.Y)}, {F(baseCol.Z)}";

            _sb.AppendLine($"    // Cavity #{rc.CavityId}: V={vCount}, T={triCount}, " +
                           $"drawMode={_cavityDrawMode}, vertexColors={hasVertexColors}");

            _sb.AppendLine("    mesh2 {");

            _sb.AppendLine("        vertex_vectors {");
            _sb.AppendLine($"            {vCount},");
            for (int i = 0; i < vCount; i++)
            {
                var v = verts[i];
                string sep = (i == vCount - 1) ? "" : ",";
                _sb.AppendLine($"            <{F(v.X)}, {F(v.Y)}, {F(v.Z)}>{sep}");
            }
            _sb.AppendLine("        }");

            if (useNormals)
            {
                _sb.AppendLine("        normal_vectors {");
                _sb.AppendLine($"            {vCount},");
                for (int i = 0; i < vCount; i++)
                {
                    var n = normals[i];
                    string sep = (i == vCount - 1) ? "" : ",";
                    _sb.AppendLine($"            <{F(n.X)}, {F(n.Y)}, {F(n.Z)}>{sep}");
                }
                _sb.AppendLine("        }");
            }

            if (hasVertexColors)
            {
                _sb.AppendLine("        texture_list {");
                _sb.AppendLine($"            {vCount},");
                for (int i = 0; i < vCount; i++)
                {
                    var vc = rc.VertexColors4[i];
                    Vector3 cv = new Vector3(vc.X, vc.Y, vc.Z);
                    cv = Saturate(cv, _saturationBoost);

                    string sep = (i == vCount - 1) ? "" : ",";
                    _sb.AppendLine($"            texture {{");
                    _sb.AppendLine($"                pigment {{ color rgbt <{F(cv.X)}, {F(cv.Y)}, {F(cv.Z)}, {F(_globalTransmit)}> }}");
                    _sb.AppendLine($"                finish {{");
                    _sb.AppendLine($"                    ambient {F(GetAmbient() * 1.5f)}");
                    _sb.AppendLine($"                    diffuse {F(GetDiffuse() * 0.9f)}");
                    _sb.AppendLine($"                    phong {F(GetPhong() * 0.7f)}");
                    _sb.AppendLine($"                    phong_size {F(Math.Max(1f, GetPhongSize()))}");
                    _sb.AppendLine("                    brilliance 0.6");
                    _sb.AppendLine($"                    emission rgb {V3(cv * _emissionStrength * 0.6f)}");
                    _sb.AppendLine($"                }}");
                    _sb.AppendLine($"            }}{sep}");
                }
                _sb.AppendLine("        }");
            }

            _sb.AppendLine("        face_indices {");
            _sb.AppendLine($"            {triCount},");
            for (int t = 0; t < triCount; t++)
            {
                int i0 = inds[t * 3];
                int i1 = inds[t * 3 + 1];
                int i2 = inds[t * 3 + 2];
                string sep = (t == triCount - 1) ? "" : ",";

                if (hasVertexColors)
                {
                    _sb.AppendLine($"            <{i0}, {i1}, {i2}>, {i0}, {i1}, {i2}{sep}");
                }
                else
                {
                    _sb.AppendLine($"            <{i0}, {i1}, {i2}>{sep}");
                }
            }
            _sb.AppendLine("        }");

            _sb.AppendLine("        inside_vector <0, 0, 1>");

            if (!hasVertexColors)
            {
                _sb.AppendLine("        pigment {");
                _sb.AppendLine($"            color rgbt <{colStr}, {F(_globalTransmit)}>");
                _sb.AppendLine("        }");

                WriteCavityFinish(cavCol);
            }

            _sb.AppendLine("    }");
            _sb.AppendLine();
        }

        #endregion

        #region Helpers

        private static Vector3 ColorToVector3(Microsoft.Xna.Framework.Color c)
        {
            return new Vector3(c.R / 255f, c.G / 255f, c.B / 255f);
        }

        private static Vector3 SafeNormalize(Vector3 v, Vector3 fallback)
        {
            float lenSq = v.LengthSquared();
            if (lenSq < 1e-8f) return fallback;
            return v / MathF.Sqrt(lenSq);
        }

        private float GetVdwRadiusForPov(int atomicNumber)
        {
            return _molecule.GetVdwRadius(atomicNumber);
        }

        private float GetBallRadius(int atomicNumber)
        {
            bool metal = IsMetal(atomicNumber);
            float r = metal ? Flags.Instance.BallRadiusMetal : Flags.Instance.BallRadiusNonMetal;

            if (atomicNumber == 1)
                r *= Flags.Instance.HydrogenBallScale;

            return r;
        }

        private static bool IsMetal(int atomicNumber)
        {
            return (atomicNumber >= 3 && atomicNumber <= 4) ||
                   (atomicNumber >= 11 && atomicNumber <= 12) ||
                   (atomicNumber >= 19 && atomicNumber <= 31) ||
                   (atomicNumber >= 37 && atomicNumber <= 50) ||
                   (atomicNumber >= 55 && atomicNumber <= 83) ||
                   (atomicNumber >= 87 && atomicNumber <= 112);
        }

        private Vector3 GetAtomColorVec(int atomIndex)
        {
            ref var atom = ref _molecule.atom[atomIndex];
            int mode = Flags.Instance.ColorMode;

            Vector3 raw;

            if (mode == 4)
            {
                if (atom.CustomColor.X >= 0 && atom.CustomColor.Y >= 0 && atom.CustomColor.Z >= 0)
                    raw = atom.CustomColor;
                else
                    raw = GetVdwColor(atom.AtomicNumber);
            }
            else
            {
                switch (mode)
                {
                    case 0: raw = GetVdwColor(atom.AtomicNumber); break;
                    case 1: raw = GetSymmetryColor(atom.SymmetryEquivalentNumberInCell); break;
                    case 2: raw = GetMoleculeColor(atom.IndividualMoleculeInCell); break;
                    case 3:
                        raw = GetSymmetryMoleculeColor(
                                atom.SymmetryEquivalentNumberInCell * 100 + atom.IndividualMoleculeInCell); break;
                    case 5:
                        try
                        {
                            var c = ColorManager.ValueToVector3(atom.charge, ColorManager.PropertyType.Charge);
                            raw = (float.IsNaN(c.X) || float.IsNaN(c.Y) || float.IsNaN(c.Z))
                                ? new Vector3(0.5f) : c;
                        }
                        catch { raw = new Vector3(0.5f); }
                        break;
                    case 6:
                        try
                        {
                            var c = ColorManager.ValueToVector3(atom.Hydrophobicity, ColorManager.PropertyType.Hydrophobicity);
                            raw = (float.IsNaN(c.X) || float.IsNaN(c.Y) || float.IsNaN(c.Z))
                                ? new Vector3(0.5f) : c;
                        }
                        catch { raw = new Vector3(0.5f); }
                        break;
                    default: raw = GetVdwColor(atom.AtomicNumber); break;
                }
            }

            return Saturate(raw, _saturationBoost);
        }

        private static Vector3 GetVdwColor(int atomicNumber)
        {
            if (VdwParameters.color != null &&
                atomicNumber >= 0 &&
                atomicNumber < VdwParameters.color.Length)
            {
                var c = VdwParameters.color[atomicNumber];
                if (c.X < 0.001f && c.Y < 0.001f && c.Z < 0.001f)
                    return new Vector3(0.6f);
                return c;
            }
            return new Vector3(0.6f);
        }

        private static Vector3 GetSymmetryColor(int symNum)
        {
            Vector3[] colors = {
                new Vector3(1f, 0.2f, 0.2f), new Vector3(0.2f, 1f, 0.2f),
                new Vector3(0.2f, 0.2f, 1f), new Vector3(1f, 1f, 0.2f),
                new Vector3(1f, 0.2f, 1f),   new Vector3(0.2f, 1f, 1f),
                new Vector3(1f, 0.6f, 0.2f), new Vector3(0.6f, 0.2f, 1f),
                new Vector3(0.2f, 0.8f, 0.4f), new Vector3(0.8f, 0.2f, 0.4f),
                new Vector3(0.4f, 0.8f, 0.8f), new Vector3(0.8f, 0.6f, 0.2f),
                new Vector3(0.2f, 0.4f, 0.8f), new Vector3(0.8f, 0.4f, 0.8f),
                new Vector3(0.4f, 0.8f, 0.2f), new Vector3(0.2f, 0.6f, 0.6f),
            };
            return colors[Math.Abs(symNum) % colors.Length];
        }

        private static Vector3 GetMoleculeColor(int molNum)
        {
            Vector3[] colors = {
                new Vector3(1f, 0.4f, 0.4f), new Vector3(0.4f, 1f, 0.4f),
                new Vector3(0.4f, 0.4f, 1f), new Vector3(1f, 1f, 0.4f),
                new Vector3(1f, 0.4f, 1f),   new Vector3(0.4f, 1f, 1f),
                new Vector3(1f, 0.6f, 0.2f), new Vector3(0.6f, 0.2f, 1f),
                new Vector3(0.2f, 0.8f, 0.4f), new Vector3(0.8f, 0.2f, 0.4f),
            };
            return colors[Math.Abs(molNum) % colors.Length];
        }

        private static Vector3 GetSymmetryMoleculeColor(int key)
        {
            var rand = new Random(key);
            return new Vector3(
                (float)(rand.NextDouble() * 0.8 + 0.2),
                (float)(rand.NextDouble() * 0.8 + 0.2),
                (float)(rand.NextDouble() * 0.8 + 0.2));
        }

        #endregion
    }
}