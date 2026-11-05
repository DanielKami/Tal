// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MonoGameDocument partial — screenshot / export.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;

namespace Tal.GUI
{
    public partial class MonoGameDocument
    {
        #region Constants

        /// <summary>
        /// Maksymalny size pojedynczego wymiaru (ochrona before limitami GPU / VRAM).
        /// </summary>
        private const int MAX_SCREENSHOT_DIMENSION = 8192;

        public GameWindow GetGameWindow() => _gameWindow;

        #endregion

        #region Public API

        /// <summary>
        /// Saves screenshot w N-krotnym magnification.
        /// Everything (3D geometry, labels, colour scale) scales proportionally,
        /// bo viewport render targetu = viewport * N, a camera uses aspect z viewportu.
        /// </summary>
        /// <param name="filePath">Destination PNG path.</param>
        /// <param name="scale">Multiplier (1.0 = original, 2.0 = 2×, 3.0 = 3×, etc.).</param>
        /// <param name="fullFrame">
        /// true  = scene 3D + labels + skala colours,
        /// false = 3D scene only (without sprites).
        /// </param>
        /// <returns>true if the save succeeded.</returns>
        public bool SaveScreenshotScaled(string filePath, float scale, bool fullFrame = true, bool supersample = false)
        {
            var device = GetGraphicsDevice();
            if (device == null || device.IsDisposed)
            {
                Debug.WriteLine("[Screenshot] GraphicsDevice not available");
                return false;
            }

            int baseWidth = device.PresentationParameters.BackBufferWidth;
            int baseHeight = device.PresentationParameters.BackBufferHeight;

            if (baseWidth <= 0 || baseHeight <= 0)
            {
                Debug.WriteLine($"[Screenshot] Invalid base size: {baseWidth}x{baseHeight}");
                return false;
            }

            if (scale < 1.0f) scale = 1.0f;

            int targetWidth = (int)MathF.Round(baseWidth * scale);
            int targetHeight = (int)MathF.Round(baseHeight * scale);

            if (targetWidth > MAX_SCREENSHOT_DIMENSION) targetWidth = MAX_SCREENSHOT_DIMENSION;
            if (targetHeight > MAX_SCREENSHOT_DIMENSION) targetHeight = MAX_SCREENSHOT_DIMENSION;

            return SaveScreenshotInternal(filePath, targetWidth, targetHeight, fullFrame, supersample);
        }

        /// <summary>
        /// Saves screenshot w exact rozmiarze (without multiplier).
        /// </summary>
        public bool SaveScreenshotAtSize(string filePath, int width, int height, bool fullFrame = true)
        {
            return SaveScreenshotInternal(filePath, width, height, fullFrame);
        }

        /// <summary>
        /// Saves screenshot w aktualnym rozmiarze viewportu (1×).
        /// </summary>
        public bool SaveScreenshot(string filePath, bool fullFrame = true)
        {
            return SaveScreenshotInternal(filePath, 0, 0, fullFrame);
        }

        #endregion

        #region Internal

        /// <summary>
        /// Shared screenshot save implementation.
        ///
        /// 1. Render scene do HDR (HalfVector4) — consistent z cavity / additive.
        /// 2. Kompozycja na solidne background do Color RT.
        /// 3. Boost kontrastu / nasycenia (cavity not wychodzi blade).
        /// 4. Wymuszenie alpha = 255 i zapis PNG.
        /// </summary>
        /// <summary>
        /// ⭐ Shared screenshot save implementation with the FULL AA pipeline:
        ///     1. RT scene 3D — SSAA (if supersample) LUB MSAA (if MSAAOnSceneRT > 0)
        ///     2. Downsample → finalSceneRT (if SSAA > 1)
        ///     3. Sprite layer (labels + colour scale) at native resolution
        ///     4. Zapis PNG (Opaque, alpha=255)
        /// </summary>
        private bool SaveScreenshotInternal(
            string filePath,
            int width,
            int height,
            bool fullFrame,
            bool supersample = false)
        {
            GraphicsDevice device = null;
            RenderTarget2D sceneRT = null;         // RT sceny 3D — SSAA × MSAA
            RenderTarget2D finalSceneRT = null;    // finalny — po downsample
            RenderTarget2D pngTarget = null;       // color buffer for PNG
            RenderTargetBinding[] previousTargets = null;
            Viewport previousViewport = default;

            float savedRenderTargetScale = Flags.Instance.RenderTargetScale;
            int savedSSAASamples = Flags.Instance.SSAASamples;

            try
            {
                device = GetGraphicsDevice();
                if (device == null || device.IsDisposed) return false;

                int baseW = device.PresentationParameters.BackBufferWidth;
                int baseH = device.PresentationParameters.BackBufferHeight;

                if (width <= 0 || height <= 0) { width = baseW; height = baseH; }
                if (width <= 0 || height <= 0)
                {
                    Debug.WriteLine($"[Screenshot] Invalid size: {width}x{height}");
                    return false;
                }

                // ⭐ Skala warstwy 2D — proporcjonalnie do size RT
                float scaleW = baseW > 0 ? (float)width / baseW : 1.0f;
                float scaleH = baseH > 0 ? (float)height / baseH : 1.0f;
                Flags.Instance.RenderTargetScale = Math.Max(scaleW, scaleH);

                // ⭐ SSAA inside RT — 2× when supersample, otherwise 1×
                int internalSSAA = supersample ? 2 : 1;

                // ⭐ MSAA — z flags TAL (identycznie jak RenderFrame)
                int requestedMSAA = GetSceneMSAA();
                if (internalSSAA > 1) requestedMSAA = 0;    // SSAA i MSAA razem = bez sensu

                // ⭐ Disable SSAA in TAL during render (avoid double SSAA)
                Flags.Instance.SSAASamples = 1;

                previousTargets = device.GetRenderTargets();
                previousViewport = device.Viewport;

                int rtW = width * internalSSAA;
                int rtH = height * internalSSAA;

                // ═══════════════════════════════════════════════════════════════
                // 1. RT scene 3D — SSAA × MSAA
                // ═══════════════════════════════════════════════════════════════
                SurfaceFormat sceneFmt = requestedMSAA > 0
                    ? SurfaceFormat.Color            // MSAA nie wspiera HalfVector4
                    : SurfaceFormat.HalfVector4;

                sceneRT = new RenderTarget2D(
                    device,
                    rtW, rtH,
                    mipMap: false,
                    preferredFormat: sceneFmt,
                    preferredDepthFormat: DepthFormat.Depth24Stencil8,
                    preferredMultiSampleCount: requestedMSAA,
                    usage: RenderTargetUsage.PreserveContents);

                int actualMSAA = sceneRT.MultiSampleCount;

                Debug.WriteLine(
                    $"[Screenshot] sceneRT: {rtW}×{rtH}, fmt={sceneFmt}, " +
                    $"MSAA={actualMSAA} (requested {requestedMSAA}), SSAA={internalSSAA}×");

                device.SetRenderTarget(sceneRT);
                device.Viewport = new Viewport(0, 0, rtW, rtH);

                device.BlendState = BlendState.Opaque;
                device.DepthStencilState = DepthStencilState.Default;
                device.RasterizerState = RasterizerState.CullCounterClockwise;

                device.Clear(
                    ClearOptions.Target | ClearOptions.DepthBuffer,
                    new Microsoft.Xna.Framework.Color(0, 0, 0, 0),
                    1.0f, 0);

                // Gradient background
                var gameWindow = _gameWindow;
                if (gameWindow != null)
                    gameWindow.DrawBackgroundGradient(device, rtW, rtH);
                else
                    device.Clear(ClearOptions.Target, Flags.Instance.color2, 1.0f, 0);

                // Scena 3D (without sprite layer)
                DrawSceneToRenderTarget();

                // ═══════════════════════════════════════════════════════════════
                // 2. Downsample → finalSceneRT (if SSAA > 1)
                // ═══════════════════════════════════════════════════════════════
                if (internalSSAA > 1)
                {
                    finalSceneRT = new RenderTarget2D(
                        device,
                        width, height,
                        mipMap: false,
                        preferredFormat: SurfaceFormat.HalfVector4,
                        preferredDepthFormat: DepthFormat.None,
                        preferredMultiSampleCount: 0,
                        usage: RenderTargetUsage.DiscardContents);

                    device.SetRenderTarget(finalSceneRT);
                    device.Viewport = new Viewport(0, 0, width, height);

                    device.BlendState = BlendState.Opaque;
                    device.DepthStencilState = DepthStencilState.None;
                    device.RasterizerState = RasterizerState.CullNone;

                    var sb = new SpriteBatch(device);
                    sb.Begin(
                        SpriteSortMode.Deferred,
                        BlendState.Opaque,
                        SamplerState.LinearClamp,       // ⭐ downsample 2×2
                        DepthStencilState.None,
                        RasterizerState.CullNone);
                    sb.Draw(sceneRT,
                        new Microsoft.Xna.Framework.Rectangle(0, 0, width, height),
                        Microsoft.Xna.Framework.Color.White);
                    sb.End();
                    sb.Dispose();
                }
                else
                {
                    // Without SSAA — sceneRT is finalny
                    finalSceneRT = sceneRT;
                }

                // ═══════════════════════════════════════════════════════════════
                // 3. Sprite layer (labels + colour scale) — if fullFrame
                // ═══════════════════════════════════════════════════════════════
                if (fullFrame)
                {
                    device.SetRenderTarget(finalSceneRT);
                    device.Viewport = new Viewport(0, 0, width, height);

                    // ⭐ Important: ApplyEffects after viewport change (Projection2 depends on viewport)
                    var camera = GetCamera();
                    if (camera != null)
                    {
                        camera.GraphicsDevice = device;
                        camera.ApplyEffects();
                    }

                    device.BlendState = BlendState.AlphaBlend;
                    device.DepthStencilState = DepthStencilState.None;
                    device.RasterizerState = RasterizerState.CullNone;

                    _gameWindow?.DrawSpriteLayer();
                }

                // ═══════════════════════════════════════════════════════════════
                // 4. Konwersja HDR → Color for PNG (without re-alpha)
                // ═══════════════════════════════════════════════════════════════
                pngTarget = new RenderTarget2D(
                    device,
                    width, height,
                    mipMap: false,
                    preferredFormat: SurfaceFormat.Color,
                    preferredDepthFormat: DepthFormat.None,
                    preferredMultiSampleCount: 0,
                    usage: RenderTargetUsage.PreserveContents);

                device.SetRenderTarget(pngTarget);
                device.Viewport = new Viewport(0, 0, width, height);

                device.BlendState = BlendState.Opaque;
                device.DepthStencilState = DepthStencilState.None;
                device.RasterizerState = RasterizerState.CullNone;

                // Fallback background for pixels with A=0
                device.Clear(ClearOptions.Target, Flags.Instance.color2, 1.0f, 0);

                if (gameWindow != null)
                    gameWindow.DrawBackgroundGradient(device, width, height);

                var sb2 = new SpriteBatch(device);
                sb2.Begin(
                    SpriteSortMode.Deferred,
                    BlendState.Opaque,
                    SamplerState.LinearClamp,
                    DepthStencilState.None,
                    RasterizerState.CullNone);
                sb2.Draw(finalSceneRT,
                    new Microsoft.Xna.Framework.Rectangle(0, 0, width, height),
                    Microsoft.Xna.Framework.Color.White);
                sb2.End();
                sb2.Dispose();

                ApplyContrastBoost(pngTarget, contrast: 1.12f, brightness: 0.02f, saturation: 1.08f);
                ForceOpaqueAlpha(pngTarget);

                // ═══════════════════════════════════════════════════════════════
                // 5. Zapis PNG
                // ═══════════════════════════════════════════════════════════════
                using (var stream = System.IO.File.Create(filePath))
                {
                    pngTarget.SaveAsPng(stream, width, height);
                }

                Debug.WriteLine(
                    $"[Screenshot] Saved: {filePath} ({width}×{height}), " +
                    $"SSAA={internalSSAA}×, MSAA={actualMSAA}×, fullFrame={fullFrame}");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Screenshot] Error: {ex.Message}");
                Debug.WriteLine($"[Screenshot] Stack: {ex.StackTrace}");

                try
                {
                    if (device != null)
                    {
                        if (previousTargets != null)
                            device.SetRenderTargets(previousTargets);

                        device.Viewport = previousViewport;

                        var camera = GetCamera();
                        if (camera != null)
                        {
                            camera.GraphicsDevice = device;
                            camera.ApplyEffects();
                        }
                    }
                }
                catch { }

                return false;
            }
            finally
            {
                Flags.Instance.RenderTargetScale = savedRenderTargetScale;
                Flags.Instance.SSAASamples = savedSSAASamples;

                // ⭐ Release only owned RTs — finalSceneRT may point at sceneRT
                if (!ReferenceEquals(sceneRT, finalSceneRT))
                {
                    sceneRT?.Dispose();
                    finalSceneRT?.Dispose();
                }
                else
                {
                    sceneRT?.Dispose();
                }

                pngTarget?.Dispose();
            }
        }

        /// <summary>
        /// ⭐ Odczytuje MSAA scene 3D z flags TAL (identycznie jak GameWindow).
        /// </summary>
        private static int GetSceneMSAA()
        {
            int msaa = Flags.Instance.MSAAOnSceneRT;
            if (msaa != 0 && msaa != 2 && msaa != 4 && msaa != 8)
                msaa = 4;
            return msaa;
        }

        /// <summary>
        /// Boosts contrast, brightness and saturation of Colour RT pixels (CPU).
        /// contrast: 1.0 = no change, 1.3–1.5 = sharper capture.
        /// brightness: RGB offset after contrast (e.g. 0.03–0.06).
        /// saturation: 1.0 = without zmian, 1.1–1.3 = mniej „szare” colours.
        /// </summary>
        private static void ApplyContrastBoost(
            RenderTarget2D rt,
            float contrast = 1.35f,
            float brightness = 0.04f,
            float saturation = 1.15f)
        {
            if (rt == null || rt.IsDisposed) return;

            int w = rt.Width;
            int h = rt.Height;
            if (w <= 0 || h <= 0) return;

            var data = new Microsoft.Xna.Framework.Color[w * h];
            rt.GetData(data);

            for (int i = 0; i < data.Length; i++)
            {
                float r = data[i].R / 255f;
                float g = data[i].G / 255f;
                float b = data[i].B / 255f;

                // contrast around 0.5 + brightness
                r = (r - 0.5f) * contrast + 0.5f + brightness;
                g = (g - 0.5f) * contrast + 0.5f + brightness;
                b = (b - 0.5f) * contrast + 0.5f + brightness;

                // saturation relative to luminance
                float y = 0.2126f * r + 0.7152f * g + 0.0722f * b;
                r = y + (r - y) * saturation;
                g = y + (g - y) * saturation;
                b = y + (b - y) * saturation;

                r = Math.Clamp(r, 0f, 1f);
                g = Math.Clamp(g, 0f, 1f);
                b = Math.Clamp(b, 0f, 1f);

                data[i].R = (byte)Math.Round(r * 255f);
                data[i].G = (byte)Math.Round(g * 255f);
                data[i].B = (byte)Math.Round(b * 255f);
            }

            rt.SetData(data);
        }

        /// <summary>
        /// Sets alpha all pikseli na 255 — PNG without transparency.
        /// </summary>
        private static void ForceOpaqueAlpha(RenderTarget2D rt)
        {
            if (rt == null || rt.IsDisposed) return;

            int w = rt.Width;
            int h = rt.Height;
            if (w <= 0 || h <= 0) return;

            var data = new Microsoft.Xna.Framework.Color[w * h];
            rt.GetData(data);

            for (int i = 0; i < data.Length; i++)
                data[i].A = 255;

            rt.SetData(data);
        }

        #endregion
    }
}