// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// 3D drawing / rendering module: Camera.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using System.Windows.Forms;
using Tal;

namespace Tal.DrawMethods
{
 /// <summary>
 /// Provides 3D camera functionality for the TAL application.
 /// Handles perspective projection, rotation, translation, and zoom operations
 /// for viewing molecular structures and cavities.
 ///
 /// ⭐ v2: Optymalizacja #4 — UpdateQuadRotation uses inline
 /// Matrix.CreateRotationX/Y/Z zamiast Matrix.CreateFromAxisAngle
 /// (CameraRotateAroundX/Y/Z are always UnitX/Y/Z).
 /// </summary>
    public class Camera
    {
        #region Public Fields

        public const float NEAR_PLANE = 1f;
        public const float FAR_PLANE = 80f;

        /// <summary>Live near clip distance (Z-clipping mouse adjusts this).</summary>
        public float NearPlaneDistance = NEAR_PLANE;

        /// <summary>Live far clip distance (Z-clipping + Shift adjusts this).</summary>
        public float FarPlaneDistance = FAR_PLANE;

        private const float ZCLIP_MOUSE_SENSITIVITY = 0.0012f; // ~4× slower than previous
        public const float DEFAULT_PERSPECTIVE_ANGLE = 0.1f;
        public const float DEFAULT_PERSPECTIVE_ANGLE_PERSPECTIVE = 30f;
        public const float CAMERA_DISTANCE = 20.0f;
        public const float ZOOM = 1000f;

 /// <summary>Indicates whether perspective projection is enabled.</summary>
        public bool perspective;

 /// <summary>The view matrix for the camera.</summary>
        public Matrix View;

 /// <summary>The perspective projection matrix.</summary>
        public Matrix Projection;

 /// <summary>The orthographic projection matrix for 2D overlays.</summary>
        public Matrix Projection2;

 /// <summary>The rotation matrix for the camera.</summary>
        public Matrix Rotate;

 /// <summary>The scale matrix for zoom operations.</summary>
        public Matrix Scale;

 /// <summary>The translate matrix for shift.</summary>
        public Matrix Translate;

 /// <summary>The current zoom level.</summary>
        public float zoom;

 /// <summary>The inverse of the zoom level (1/zoom).</summary>
        public float OneOverZoom;

 /// <summary>Flag indicating zoom operation is active.</summary>
        public bool flag_zooming;

 /// <summary>Flag indicating movement operation is active.</summary>
        public bool flag_moving;

 /// <summary>Flag indicating rotation operation is active.</summary>
        public bool flag_rotating = true;

 /// <summary>Flag indicating selection mode is active.</summary>
        public bool flag_select = true;

 /// <summary>Flag indicating lasso selection mode is active.</summary>
        public bool flag_lasso;

 /// <summary>Flag indicating an item has been selected.</summary>
        public bool flag_selected;

 /// <summary>Flag indicating a double tap has occurred.</summary>
        public bool doubleTap;

 /// <summary>Flag indicating rotation has ended.</summary>
        public bool flag_end_rotation;

 /// <summary>Flag indicating mouse release for lasso selection.</summary>
        public bool flag_release;

 /// <summary>The camera aftersition in world space.</summary>
        public Vector3 g_cameraPos;

 /// <summary>The target aftersition the camera is looking at.</summary>
        public Vector3 g_targetPos;

 /// <summary>The system translation offset.</summary>
        public Vector3 g_system_translate;

 /// <summary>The current mouse aftersition.</summary>
        public Vector2 ptMouseCurrent;

 /// <summary>The previous mouse aftersition.</summary>
        public Vector2 ptMousePrevious;

 /// <summary>The mouse aftersition for double tap detection.</summary>
        public Vector2 ptMouseDoubleTap;

 /// <summary>The current mouse aftersition.</summary>
        public Vector2 ptMousePosition;

 /// <summary>The mouse aftersition for zoom drag operations.</summary>
        public Vector2 ptMouseZoomDrag;

 /// <summary>Temafterrary mouse aftersition storage.</summary>
        public Vector2 ptTempPosition;

 /// <summary>The mouse aftersition for selection operations.</summary>
        public Vector2 ptMouseSelect;

        #endregion

        #region Private Fields

        private float g_yow;
        private float g_pitch;
        private float g_arcX, g_arcY;

        private Vector3 CameraRotateAroundX;
        private Vector3 CameraRotateAroundY;
        private Vector3 CameraRotateAroundZ;

        private readonly float ROTATION_STEP = 0.01f;
        private readonly float ROTATION_STEP_Z = 0.004f;
        // Pan sensitivity (right/middle drag). Was 1 — ~1000× too strong with default zoom.
        private readonly float SHIFT_STEP = 0.001f;

        private float m_fCameraYawAngle;
        private float m_fCameraPitchAngle;

        private const float TRANSLATION_STEP = 1f;
        private const float ROTATION_STEP_DEG = MathHelperTal.Pi / 180f;
        private const float ROTATION_90_STEP = MathHelperTal.PiOver2;

        #endregion

        #region Public Properties

 /// <summary>Afterzycja camera w world space (alias g_cameraAfters).</summary>
        public Vector3 Position
        {
            get => g_cameraPos;
            set => g_cameraPos = value;
        }

 /// <summary>Gets or sets the graphics device for aspect ratio calculations.</summary>
        public GraphicsDevice GraphicsDevice { get; set; }

 /// <summary>Gets or sets the perspective angle in degrees.</summary>
        public float PerspectiveAngle = NEAR_PLANE;

 /// <summary>
 /// ⭐ Licznik changes stanu camera.
 /// Increments ONLY when the camera actually changed (rotation, translation,
 /// zoom, perspective, reset, set view).
 /// </summary>
        public int ChangeCounter { get; private set; } = 0;

 /// <summary>
 /// ⭐ Registers a camera state change.
 /// </summary>
        public void MarkChanged()
        {
            ChangeCounter++;
        }

 // ⭐ DIAG — inspect private fields for logs
        public float Debug_Yow => g_yow;
        public float Debug_Pitch => g_pitch;
        public float Debug_ArcX => g_arcX;
        public float Debug_ArcY => g_arcY;

 /// <summary>
 /// ⭐ True when the mouse changed rotation state, but ApplyEffects
 /// has not yet applied that delta to Rotate.
 /// </summary>
        public bool HasPendingRotation =>
            Math.Abs(g_yow) > 1e-6f ||
            Math.Abs(g_pitch) > 1e-6f ||
            Math.Abs(g_arcX) > 1e-6f ||
            Math.Abs(g_arcY) > 1e-6f;

 /// <summary>
 /// ⭐ True when camera jest during activego ruchu.
 /// </summary>
        public bool IsActivelyMoving =>
            flag_rotating || flag_moving || flag_zooming;

        #endregion

        #region Constructor

        public Camera()
        {
            Initialize();
            ApplyEffects();
        }

        #endregion

        #region Initialization Methods

        public void Initialize()
        {
            UpdatePerspective();

            g_cameraPos = new Vector3(0, 0, CAMERA_DISTANCE);
            g_targetPos = new Vector3(0, 0, 0);

            m_fCameraYawAngle = 0.0f;
            m_fCameraPitchAngle = 0.0f;

            Rotate = Matrix.Identity;
            Scale = Matrix.Identity;

            g_system_translate = new Vector3(0, 0, 0);

            flag_rotating = false;
            flag_zooming = false;
            flag_moving = false;
            flag_end_rotation = false;
            flag_release = false;

            ptMouseDoubleTap = Vector2.Zero;
            ptMousePosition = Vector2.Zero;
            ptMouseZoomDrag = Vector2.Zero;
            ptTempPosition = Vector2.Zero;

            g_yow = 0;
            g_pitch = 0;
            g_arcX = 0;
            g_arcY = 0;

            PerspectiveAngle = Camera.DEFAULT_PERSPECTIVE_ANGLE;
            UpdatePerspective();

 // ⭐ Reset camera = change stanu
            MarkChanged();
        }

        public void Reset()
        {
            Initialize();
            ApplyEffects();
            MarkChanged();
        }

        public void ResetView()
        {
            Rotate = Matrix.Identity;
            g_system_translate = Vector3.Zero;
            zoom = ZOOM;
            OneOverZoom = 1.0f / zoom;
            ApplyEffects();
            MarkChanged();
        }

        #endregion

        #region Projection Updates

        public void UpdatePerspective()
        {
            zoom = ZOOM * 0.1f / PerspectiveAngle;
            if (zoom < 0.1f) zoom = 0.1f;
            if (zoom > 1000f) zoom = 1000f;

            OneOverZoom = 1.0f / zoom;
            Scale = Matrix.CreateScale(OneOverZoom, OneOverZoom, OneOverZoom);

 // ⚠️ We do NOT call MarkChanged() here — UpdatePerspective is called
 // in Initialize() and via ApplyEffects() (every frame).
        }

        #endregion

        #region Camera Transformations

        public void ApplyEffects()
        {
            OneOverZoom = 1.0f / zoom;
            Scale = Matrix.CreateScale(OneOverZoom, OneOverZoom, OneOverZoom);

            float aspect = 1f;
            if (GraphicsDevice != null)
                aspect = GraphicsDevice.Viewport.AspectRatio;

            float fov = MathHelperTal.ToRadians(PerspectiveAngle);
            if (fov < 0.0001f) fov = 0.0001f;

            // Clamp: near must stay < far
            if (NearPlaneDistance < 0.01f) NearPlaneDistance = 0.01f;
            if (FarPlaneDistance < NearPlaneDistance + 0.1f)
                FarPlaneDistance = NearPlaneDistance + 0.1f;
            if (FarPlaneDistance > 500f) FarPlaneDistance = 500f;

            float farPlane = FarPlaneDistance;
            float nearPlane = NearPlaneDistance;

            Projection = Matrix.CreatePerspectiveFieldOfView(
                fov,
                aspect,
                nearPlane,
                farPlane
            );

            Projection2 = Matrix.CreateOrthographicOffCenter(
                0, GraphicsDevice?.Viewport.Width ?? 20,
                GraphicsDevice?.Viewport.Height ?? 20, 0,
                0.0f, 100f
            );

            Vector3 LookUp = new Vector3(0.0f, 1.0f, 0.0f);
            if (g_cameraPos.X == 0 && g_cameraPos.Z == 0)
                LookUp = new Vector3(0.0f, 0.0f, 1.0f);

            View = Matrix.CreateLookAt(g_cameraPos, g_targetPos, LookUp);

            Vector3 corrected_translate = g_system_translate;
            if (PerspectiveAngle < 2)
                corrected_translate /= 100f;
            Translate = Matrix.CreateTranslation(corrected_translate);

            UpdateCameraOrientationPitchYaw();
            UpdateQuadRotation();

 // ⚠️ We do NOT call MarkChanged() here — ApplyEffects is called every frame.
        }

        public Matrix GetScaleRotate()
        {
            return Matrix.CreateScale(OneOverZoom, OneOverZoom, OneOverZoom) * Rotate;
        }

        #endregion

        #region Rotation Methods

        public void RotateX(float angle)
        {
            Rotate *= Matrix.CreateRotationX(angle);
            ApplyEffects();
            MarkChanged();
        }

        public void RotateY(float angle)
        {
            Rotate *= Matrix.CreateRotationY(angle);
            ApplyEffects();
            MarkChanged();
        }

        public void RotateZ(float angle)
        {
            Rotate *= Matrix.CreateRotationZ(angle);
            ApplyEffects();
            MarkChanged();
        }

        #endregion

        #region Translation Methods

        public void MoveLeft()
        {
            g_system_translate.X -= TRANSLATION_STEP;
            ApplyEffects();
            MarkChanged();
        }

        public void MoveRight()
        {
            g_system_translate.X += TRANSLATION_STEP;
            ApplyEffects();
            MarkChanged();
        }

        public void MoveUp()
        {
            g_system_translate.Y += TRANSLATION_STEP;
            MarkChanged();
            ApplyEffects();
        }

        #endregion

        #region Zoom Methods

        public void ZoomIn()
        {
            zoom *= 0.9f;
            OneOverZoom = 1.0f / zoom;
            ApplyEffects();
            MarkChanged();
        }

        public void ZoomOut()
        {
            zoom *= 1.1f;
            OneOverZoom = 1.0f / zoom;
            ApplyEffects();
            MarkChanged();
        }

        public void Scroll(MouseEventArgs e)
        {
            if (e.Delta < 0)
                zoom *= 1.1f;
            else
                zoom *= 0.9f;
            OneOverZoom = 1.0f / zoom;

            MarkChanged();
        }

        #endregion

        #region View Presets

        public void SetView(string view, Molecule molecule)
        {
            const float STEP = MathHelperTal.Pi / 180f;

            switch (view)
            {
                case "+X": Rotate *= Matrix.CreateRotationX(STEP); break;
                case "-X": Rotate *= Matrix.CreateRotationX(-STEP); break;
                case "+Y": Rotate *= Matrix.CreateRotationY(STEP); break;
                case "-Y": Rotate *= Matrix.CreateRotationY(-STEP); break;
                case "+Z": Rotate *= Matrix.CreateRotationZ(STEP); break;
                case "-Z": Rotate *= Matrix.CreateRotationZ(-STEP); break;

                case "a":
                    Rotate = Matrix.Identity
 * Matrix.CreateRotationY(3.0f * MathHelperTal.Pi / 2.0f)
 * Matrix.CreateRotationY(3.0f * MathHelperTal.Pi);
                    break;

                case "b":
                    if (molecule != null && molecule.UnitCellFrame.Count > 1)
                    {
                        Rotate = Matrix.Identity
 * Matrix.CreateRotationZ(-molecule.UnitCellFrame[1].AngleXY)
 * Matrix.CreateRotationY(molecule.UnitCellFrame[1].AngleZ)
 * Matrix.CreateRotationY(-MathHelperTal.Pi / 2)
 * Matrix.CreateRotationY(3.0f * MathHelperTal.Pi);
                    }
                    else
                    {
                        Rotate = Matrix.Identity * Matrix.CreateRotationX(MathHelperTal.PiOver2);
                    }
                    break;

                case "c":
                    if (molecule != null && molecule.UnitCellFrame.Count > 2)
                    {
                        Rotate = Matrix.Identity
 * Matrix.CreateRotationZ(-molecule.UnitCellFrame[2].AngleXY)
 * Matrix.CreateRotationY(-molecule.UnitCellFrame[2].AngleZ)
 * Matrix.CreateRotationY(-MathHelperTal.Pi / 2)
 * Matrix.CreateRotationY(3.0f * MathHelperTal.Pi);
                    }
                    else
                    {
                        Rotate = Matrix.Identity;
                    }
                    break;

                case "a*":
                    Rotate = Matrix.Identity
 * Matrix.CreateRotationY(-3.0f * MathHelperTal.Pi)
 * Matrix.CreateRotationY(-3.0f * MathHelperTal.Pi / 2.0f);
                    break;

                case "b*":
                    if (molecule != null && molecule.UnitCellFrame.Count > 1)
                    {
                        Rotate = Matrix.Identity
 * Matrix.CreateRotationY(-3.0f * MathHelperTal.Pi)
 * Matrix.CreateRotationY(MathHelperTal.Pi / 2)
 * Matrix.CreateRotationY(-molecule.UnitCellFrame[1].AngleZ)
 * Matrix.CreateRotationZ(molecule.UnitCellFrame[1].AngleXY);
                    }
                    else
                    {
                        Rotate = Matrix.Identity * Matrix.CreateRotationX(-MathHelperTal.PiOver2);
                    }
                    break;

                case "c*":
                    if (molecule != null && molecule.UnitCellFrame.Count > 2)
                    {
                        Rotate = Matrix.Identity
 * Matrix.CreateRotationY(-3.0f * MathHelperTal.Pi)
 * Matrix.CreateRotationY(MathHelperTal.Pi / 2)
 * Matrix.CreateRotationY(molecule.UnitCellFrame[2].AngleZ)
 * Matrix.CreateRotationZ(molecule.UnitCellFrame[2].AngleXY);
                    }
                    else
                    {
                        Rotate = Matrix.Identity;
                    }
                    break;

                default:
                    return;   // nieznany widok — do nothing
            }

            if (view != "+X" && view != "-X" && view != "+Y" && view != "-Y" && view != "+Z" && view != "-Z")
            {
                g_system_translate = Vector3.Zero;
            }

            ApplyEffects();
            MarkChanged();
        }

        #endregion

        #region Mouse Event Handlers

        public void MouseDown(MouseEventArgs e)
        {
            ptMousePrevious.X = e.X;
            ptMousePrevious.Y = e.Y;
            doubleTap = false;
            flag_end_rotation = false;

            System.Diagnostics.Debug.WriteLine($"[Camera.MouseDown] flag_select={flag_select}, flag_lasso={flag_lasso}");
            System.Diagnostics.Debug.WriteLine($"[Camera.MouseDown] Mouse position: ({e.X},{e.Y})");

            if (flag_select)
            {
                ptMouseSelect.X = e.X;
                ptMouseSelect.Y = e.Y;
                flag_selected = false;
            }
        }

        public void MouseMove(MouseEventArgs e, Panel panelViewport)
        {
            doubleTap = false;

            if (flag_select)
            {
                ptMouseSelect.X = 0;
                ptMouseSelect.Y = 0;
                flag_selected = false;
            }

            if (flag_lasso && !flag_release)
            {
                Vector2 currentPos = new Vector2(e.X, e.Y);

                if (Lasso.VertexList.Count > 0)
                {
                    float dist = Vector2.Distance(currentPos, Lasso.VertexList[Lasso.VertexList.Count - 1]);
                    if (dist > Lasso.MIN_POINT_DISTANCE)
                    {
                        Lasso.VertexList.Add(currentPos);
                    }
                }
            }

            ptMouseCurrent.X = e.X;
            ptMouseCurrent.Y = e.Y;

            float dx = ptMousePrevious.X - ptMouseCurrent.X;
            float dy = ptMousePrevious.Y - ptMouseCurrent.Y;
            bool moved = Math.Abs(dx) > 0.001f || Math.Abs(dy) > 0.001f;

            // ── Z-Clipping: mouse Y adjusts near plane; Shift+Y adjusts far ──
            // Active only when the Display Options "Z-Clipping" checkbox is on.
            bool zClip = Flags.Instance.ZClipping;
            bool anyDrag = e.Button == MouseButtons.Left
                        || e.Button == MouseButtons.Right
                        || e.Button == MouseButtons.Middle;

            if (zClip && anyDrag && moved)
            {
                flag_rotating = false;
                flag_moving = false;
                g_yow = 0;
                g_pitch = 0;

                bool shift = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
                // Fine control: small constant step (panel-pixel based)
                // Full drag of ~400 px ≈ 0.5 Å of plane motion at default sensitivity
                float delta = dy * ZCLIP_MOUSE_SENSITIVITY;

                if (shift)
                {
                    FarPlaneDistance = Math.Clamp(
                        FarPlaneDistance + delta,
                        NearPlaneDistance + 0.5f,
                        500f);
                }
                else
                {
                    NearPlaneDistance = Math.Clamp(
                        NearPlaneDistance + delta,
                        0.05f,
                        FarPlaneDistance - 0.5f);
                }

                ptMousePrevious = ptMouseCurrent;
                MarkChanged();
                return;
            }

            // ── Pan: Right button (or Middle) when Z-clipping is OFF ──
            flag_moving = false;
            if (!zClip && (e.Button == MouseButtons.Right || e.Button == MouseButtons.Middle))
            {
                flag_moving = true;
                flag_rotating = false;

                if (moved)
                {
                    g_system_translate.X -= SHIFT_STEP * dx;
                    g_system_translate.Y += SHIFT_STEP * dy;
                    ptMousePrevious = ptMouseCurrent;
                    MarkChanged();
                }
                return;
            }

            // ── Rotate: Left button ──
            if (e.Button == MouseButtons.Left && !flag_lasso)
            {
                flag_rotating = true;
            }
            else
            {
                flag_rotating = false;
            }

            if (!flag_moving && !flag_zooming && flag_rotating)
            {
                float deltaX = dx;
                float deltaY = dy;

                if (moved)
                {
                    g_yow = ROTATION_STEP * deltaX;
                    g_pitch = ROTATION_STEP * deltaY;
                    g_arcX = panelViewport.Width / 2 - ptMouseCurrent.X;
                    g_arcY = panelViewport.Height / 2 - ptMouseCurrent.Y;
                    ptMousePrevious = ptMouseCurrent;
                    MarkChanged();
                }
                else
                {
                    g_yow = 0;
                    g_pitch = 0;
                    g_arcX = 0;
                    g_arcY = 0;
                }
            }
        }

        public void MouseUp(MouseEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[Camera.MouseUp] flag_select={flag_select}, flag_lasso={flag_lasso}");
            System.Diagnostics.Debug.WriteLine($"[Camera.MouseUp] ptMouseSelect=({ptMouseSelect.X},{ptMouseSelect.Y}), e=({e.X},{e.Y})");

            if (flag_select)
            {
                if (ptMouseSelect.X == e.X && ptMouseSelect.Y == e.Y)
                {
                    flag_selected = true;
                    doubleTap = false;
                    System.Diagnostics.Debug.WriteLine($"[Camera.MouseUp] ✅ flag_selected=true");
                }
                else
                {
                    flag_selected = false;
                    System.Diagnostics.Debug.WriteLine($"[Camera.MouseUp] ❌ flag_selected=false - mouse moved");
                }
            }

            if (flag_lasso)
            {
                flag_rotating = false;
                g_yow = 0;
                g_pitch = 0;
                g_arcX = 0;
                g_arcY = 0;
                ptMouseSelect.X = -2;
                flag_release = true;
                doubleTap = false;
                System.Diagnostics.Debug.WriteLine($"[Camera.MouseUp] ✅ LASSO - finished, ptMouseSelect.X=-2");
            }

            flag_rotating = false;
            flag_moving = false;
            flag_end_rotation = true;
            ptMouseCurrent = Vector2.Zero;
            g_yow = 0;
            g_pitch = 0;
            g_arcX = 0;
            g_arcY = 0;

            MarkChanged();   // ⭐ end of interaction → change (finalises rotation)
        }

        public void MouseWheel(MouseEventArgs e)
        {
            Scroll(e);
        }

        public void MouseDoubleClick(MouseEventArgs e)
        {
            doubleTap = true;
            Reset();
        }

        #endregion

        #region Internal Update Methods

        private void UpdateCameraOrientationPitchYaw()
        {
            Matrix invView = Matrix.Invert(View);

            Vector3 pZBasis = new Vector3(invView.M31, invView.M32, invView.M33);

            m_fCameraYawAngle = (float)Math.Atan2(pZBasis.X, pZBasis.Z);

            m_fCameraPitchAngle = (float)-Math.Atan2(
                pZBasis.Y,
                Math.Sqrt(pZBasis.Z * pZBasis.Z + pZBasis.X * pZBasis.X));

            CameraRotateAroundX = Vector3.UnitX;
            CameraRotateAroundY = Vector3.UnitY;
            CameraRotateAroundZ = Vector3.UnitZ;
        }

 /// <summary>
 /// ⭐ Updates rotation based on mouse deltas.
 /// KEY: skip when nic nie zmienia — zaafterbiega dryfowi Rotate *= Identity.
 ///
 /// This method is called from ApplyEffects() EVERY FRAME — therefore
 /// Must NOT call MarkChanged().
 ///
 /// ⭐ v2: Inline Matrix.CreateRotationX/Y/Z — szybsze than
 /// CreateFromAxisAngle(UnitX/Y/Z, angle).
 /// </summary>
        private void UpdateQuadRotation()
        {
            float roll_X = g_pitch * g_arcX;
            float roll_Y = g_yow * g_arcY;
            float roll = ROTATION_STEP_Z * (roll_Y - roll_X);

 // ⭐ SKIP: when nothing changes — Rotate stays exactly the same.
            if (Math.Abs(g_yow) < 1e-6f &&
                Math.Abs(g_pitch) < 1e-6f &&
                Math.Abs(roll) < 1e-6f &&
                Math.Abs(g_arcX) < 1e-6f &&
                Math.Abs(g_arcY) < 1e-6f)
            {
                return;
            }

 // ⭐ #4 Inline — CreateFromAxisAngle(UnitY, angle) == CreateRotationY(angle)
 // CameraRotateAroundX/Y/Z are set in UpdateCameraOrientationPitchYaw
 // na Vector3.UnitX/Y/Z (always).
            Matrix xtmp = Matrix.CreateRotationY(-g_yow);
            Matrix ytmp = Matrix.CreateRotationX(-g_pitch);
            Matrix ztmp = Matrix.CreateRotationZ(roll);

            Rotate *= xtmp * ytmp * ztmp;

            g_yow = 0;
            g_pitch = 0;
            g_arcX = 0;
            g_arcY = 0;
        }

        #endregion
    }
}