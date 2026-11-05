// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Unit cell frame display settings (thickness, length factor, a/b/c labels).

using System;
using System.Drawing;
using System.Windows.Forms;
using Tal.GUI;
using WeifenLuo.WinFormsUI.Docking;

namespace Tal.Forms
{
    public partial class UnitCellDisplayWindow : DockContent
    {
        private bool _updating;

        public UnitCellDisplayWindow()
        {
            InitializeComponent();
            DockWindowConfig.SetupWindow(this, "📦 Unit Cell Frame");
            WireEvents();
            LoadFromFlags();
        }

        private void WireEvents()
        {
            _chkShowCell.CheckedChanged += (s, e) =>
            {
                if (_updating) return;
                Flags.Instance.Draw_cell = _chkShowCell.Checked;
                try { MainForm.Instance?.NotifyUnitCellVisibilityChanged(); } catch { }
                try
                {
                    MainForm.Instance?.GetDisplayOptionsWindow()?.SetShowCellAxes(Flags.Instance.Draw_cell);
                }
                catch { }
                RefreshScene();
            };

            _chkAxisLabels.CheckedChanged += (s, e) =>
            {
                if (_updating) return;
                Flags.Instance.ShowAxisLabels = _chkAxisLabels.Checked;
                RefreshScene();
            };

            _trackThickness.Scroll += (s, e) =>
            {
                if (_updating) return;
                float v = _trackThickness.Value / 1000f;
                Flags.Instance.CellLineThickness = v;
                _lblThicknessVal.Text = v.ToString("F3");
                RefreshScene();
            };

            _trackFactor.Scroll += (s, e) =>
            {
                if (_updating) return;
                float v = _trackFactor.Value / 10000f;
                Flags.Instance.CellLineFactor = v;
                _lblFactorVal.Text = v.ToString("F4");
                RefreshScene();
            };

            _trackFont.Scroll += (s, e) =>
            {
                if (_updating) return;
                // 30–200 → 0.30–2.00
                float v = _trackFont.Value / 100f;
                Flags.Instance.AxisLabelScale = v;
                _lblFontVal.Text = v.ToString("F2");
                RefreshScene();
            };

            _btnDefault.Click += (s, e) =>
            {
                _updating = true;
                try
                {
                    _chkShowCell.Checked = true;
                    _chkAxisLabels.Checked = true;
                    _trackThickness.Value = 15;
                    _trackFactor.Value = 127;
                    _trackFont.Value = 100;
                    _lblThicknessVal.Text = "0.015";
                    _lblFactorVal.Text = "0.0127";
                    _lblFontVal.Text = "1.00";
                }
                finally { _updating = false; }

                Flags.Instance.Draw_cell = true;
                Flags.Instance.ShowAxisLabels = true;
                Flags.Instance.CellLineThickness = 0.015f;
                Flags.Instance.CellLineFactor = 0.0127f;
                Flags.Instance.AxisLabelScale = 1.0f;
                try { MainForm.Instance?.NotifyUnitCellVisibilityChanged(); } catch { }
                try { MainForm.Instance?.GetDisplayOptionsWindow()?.SetShowCellAxes(true); } catch { }
                try { MainForm.Instance?.GetActiveMolecule()?.ResetAxisLabelOffsets(); } catch { }
                RefreshScene();
            };

            _btnResetOffsets.Click += (s, e) =>
            {
                try { MainForm.Instance?.GetActiveMolecule()?.ResetAxisLabelOffsets(); } catch { }
                RefreshScene();
            };

            _btnApply.Click += (s, e) => RefreshScene();
        }

        public void LoadFromFlags()
        {
            _updating = true;
            try
            {
                _chkShowCell.Checked = Flags.Instance.Draw_cell;
                _chkAxisLabels.Checked = Flags.Instance.ShowAxisLabels;

                int th = (int)Math.Round(Flags.Instance.CellLineThickness * 1000f);
                th = Math.Clamp(th, _trackThickness.Minimum, _trackThickness.Maximum);
                _trackThickness.Value = th;
                _lblThicknessVal.Text = Flags.Instance.CellLineThickness.ToString("F3");

                int fac = (int)Math.Round(Flags.Instance.CellLineFactor * 10000f);
                fac = Math.Clamp(fac, _trackFactor.Minimum, _trackFactor.Maximum);
                _trackFactor.Value = fac;
                _lblFactorVal.Text = Flags.Instance.CellLineFactor.ToString("F4");

                int font = (int)Math.Round(Flags.Instance.AxisLabelScale * 100f);
                font = Math.Clamp(font, _trackFont.Minimum, _trackFont.Maximum);
                _trackFont.Value = font;
                _lblFontVal.Text = Flags.Instance.AxisLabelScale.ToString("F2");
            }
            finally { _updating = false; }
        }

        public void UpdateFromFlags() => LoadFromFlags();

        public void SetShowCellSilent(bool on)
        {
            _updating = true;
            try { _chkShowCell.Checked = on; }
            finally { _updating = false; }
        }

        private static void RefreshScene()
        {
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();
            var main = MainForm.Instance;
            var dock = main?.GetDockPanel();
            if (dock == null) return;
            foreach (var doc in dock.Documents)
            {
                if (doc is MonoGameDocument mono)
                    mono.RefreshView();
            }
        }
    }
}
