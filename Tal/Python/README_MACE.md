# MACE energy + forces backend for TAL

## Runtime (next to TAL.exe)

| File | Role |
|------|------|
| `evaluate_energy.exe` | PyInstaller worker |
| `mace_medium.pt` / `mace_large.pt` | TorchScript with `forward_lammps` |

## Worker stderr (device check)

The worker prints lines such as:

```
[worker] device=cuda
[worker] device=cpu
```

TAL shows the full stderr in the Debug window. That is the authoritative GPU/CPU signal.

## Build evaluator

```powershell
pyinstaller --onefile --add-data "mace_medium.pt;." `
  --collect-all torch --collect-all mace --collect-all e3nn --collect-all xnn `
  evaluate_energy.py
```

JSON in: `model`, `pos`, `src`, `dst`, `cell_shifts`, `atomic_numbers`, `cell`, optional `molecule_ids`  
JSON out: `energy`, `forces`, `device`, optional `gpu` / `molecules`

Atomic forces → display arrows; Molecule ID grouping in TAL → intermolecular COM arrows.
