"""Rebuild lane-unit GLBs from preserved Riot sources; then run Unity's BakeOfficialLaneUnits.
Usage: python Tools/Import-OfficialLaneUnits.py --converter /path/lol2gltf.exe --dotnet-root /path/dotnet
No network or game-install dependency. SHA-256 validates every archived input before conversion.
"""
from pathlib import Path
import argparse
import hashlib
import json
import os
import subprocess
import tempfile
import shutil


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--converter", required=True, type=Path)
    parser.add_argument("--dotnet-root", type=Path)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1] / "Assets/SourceModels/LaneUnitsOfficial"
    env = os.environ.copy()
    if args.dotnet_root:
        env["DOTNET_ROOT"] = str(args.dotnet_root.resolve())
    for folder in sorted(p for p in root.iterdir() if p.is_dir()):
        manifest = json.loads((folder / "Sources.json").read_text(encoding="utf-8-sig"))
        raw = folder / "Raw"
        for entry in manifest["files"]:
            actual = hashlib.sha256((raw / entry["file"]).read_bytes()).hexdigest()
            if actual != entry["sha256"]:
                raise ValueError(f"Source hash mismatch: {folder.name}/{entry['file']}")
        model = next(raw / e["file"] for e in manifest["files"] if e["file"].endswith(".skn"))
        skeleton = next(raw / e["file"] for e in manifest["files"] if e["file"].endswith(".skl"))
        texture = next(raw / e["file"] for e in manifest["files"] if e["file"].endswith(".tex"))
        # Isolate selected clips so abandoned/raw reference animations cannot enter the GLB.
        with tempfile.TemporaryDirectory(prefix="midlane-minion-") as scratch:
            for entry in manifest["files"]:
                if entry["file"].endswith(".anm"):
                    shutil.copyfile(raw / entry["file"], Path(scratch) / Path(entry["file"]).name)
            subprocess.run([str(args.converter.resolve()), "skn2gltf", "-m", str(model), "-s", str(skeleton),
                            "-a", scratch, "-g", str(folder / "Model.glb"),
                            "--materials", *manifest["materials"],
                            "--textures", *([str(texture)] * len(manifest["materials"]))], check=True, env=env)
        print(f"Converted {folder.name}")


if __name__ == "__main__":
    main()
