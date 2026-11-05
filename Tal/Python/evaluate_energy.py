# evaluate_energy.py
# Worker MACE dla TAL. Ładuje model wybrany w JSON-ie, zwraca energię i siły.
# Uruchomienie: evaluate_energy.exe < payload.json
# lub: echo '{...}' | evaluate_energy.exe

import os
import sys
import json
import torch

os.environ["PYTORCH_CUDA_ALLOC_CONF"] = "expandable_segments:True"

# Mapowanie nazwy modelu (z TAL) -> plik .pt
MODEL_FILES = {
    "off23-small":  "mace_off23_small.pt",
    "off23-medium": "mace_off23_medium.pt",
    "off23-large":  "mace_off23_large.pt",
    "mp0-medium":   "mace_mp0_medium.pt",
    "mp0-large":    "mace_mp0_large.pt",
}
DEFAULT_MODEL = "off23-medium"

# Cache modeli w pamięci — jeśli worker jest wołany wielokrotnie w jednym procesie,
# nie ładuj tego samego modelu za każdym razem.
_MODEL_CACHE = {}


def resolve_model(model_name):
    """Waliduje nazwę, sprawdza plik, robi fallback na DEFAULT_MODEL.
    Zwraca (model_name, model_file, fallback_used, warning)."""
    warning = None
    fallback = False

    if model_name not in MODEL_FILES:
        warning = f"Nieznany model '{model_name}' — fallback na '{DEFAULT_MODEL}'"
        print(f"[worker] UWAGA: {warning}", file=sys.stderr)
        model_name = DEFAULT_MODEL

    model_file = MODEL_FILES[model_name]

    if not os.path.exists(model_file):
        print(f"[worker] BRAK {model_file} (cwd={os.getcwd()})", file=sys.stderr)
        if model_name != DEFAULT_MODEL:
            fb_file = MODEL_FILES[DEFAULT_MODEL]
            if os.path.exists(fb_file):
                warning = f"Model '{model_name}' niedostępny — użyto '{DEFAULT_MODEL}'"
                print(f"[worker] FALLBACK: {model_file} -> {fb_file}", file=sys.stderr)
                model_file = fb_file
                model_name = DEFAULT_MODEL
                fallback = True
            else:
                raise FileNotFoundError(
                    f"Brak '{model_file}' i fallbacku '{fb_file}' (cwd={os.getcwd()})"
                )
        else:
            raise FileNotFoundError(f"Brak '{model_file}' (cwd={os.getcwd()})")

    return model_name, model_file, fallback, warning


def load_model_cached(model_file, device):
    """Ładuje model TorchScript z cache (jeśli już w pamięci)."""
    key = (model_file, str(device))
    if key in _MODEL_CACHE:
        return _MODEL_CACHE[key]

    print(f"[worker] ładowanie {model_file} na {device}...", file=sys.stderr)
    model = torch.jit.load(model_file, map_location=device)
    model.eval()
    _MODEL_CACHE[key] = model
    return model


def main():
    try:
        data = json.load(sys.stdin)
        requested = data.get("model", DEFAULT_MODEL)
        print(f"[worker] dostałem JSON: model={requested!r}", file=sys.stderr)

        device = "cuda" if torch.cuda.is_available() else "cpu"
        gpu_name = None
        if device == "cuda":
            gpu_name = torch.cuda.get_device_name(0)
        print(f"[worker] device={device}" + (f" GPU={gpu_name}" if gpu_name else ""),
              file=sys.stderr)

        model_name, model_file, fallback, warning = resolve_model(requested)

        model = load_model_cached(model_file, device)
        n_params = sum(p.numel() for p in model.parameters())
        print(
            f"[worker] loaded {model_file} model={model_name} "
            f"params={n_params} fallback={fallback}",
            file=sys.stderr,
        )

        # Tensory wejściowe
        pos = torch.tensor(data["pos"], dtype=torch.float32, device=device)
        edge_index = torch.tensor(
            [data["src"], data["dst"]], dtype=torch.long, device=device
        )
        cell_shifts = torch.tensor(
            data["cell_shifts"], dtype=torch.float32, device=device
        )
        atomic_numbers = torch.tensor(
            data["atomic_numbers"], dtype=torch.long, device=device
        )
        cell = torch.tensor(
            data["cell"], dtype=torch.float32, device=device
        ).reshape(3, 3)

        with torch.no_grad():
            out = model.forward_lammps(
                pos, edge_index, cell_shifts, atomic_numbers, cell
            )
            energy = out["energy"].item()
            forces = out["forces"].tolist()

        response = {
            "energy": energy,
            "forces": forces,
            "model_requested": requested,
            "model_used": model_name,
            "model_file": model_file,
            "model_params": n_params,
            "fallback_used": fallback,
            "device": device,
            "gpu_name": gpu_name,
        }
        if warning:
            response["warning"] = warning

        print(json.dumps(response))

    except Exception as e:
        print(
            json.dumps({"error": str(e), "error_type": type(e).__name__}),
        )
        sys.exit(1)


if __name__ == "__main__":
    main()