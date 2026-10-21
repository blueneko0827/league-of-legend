"""Collect the locally available base Irelia/dummy assets without changing Assets/.

Run with --verify to check the saved collection without the external sources.
Raw formats and Unity .meta files are preserved byte for byte outside Assets/.
"""

import argparse
from collections import Counter
import hashlib
import json
import os
from pathlib import Path
import shutil


PROJECT = Path(__file__).resolve().parents[1]
LIBRARY = PROJECT / "CharacterAssets"


def digest(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def copy_file(source, target):
    # Skin metadata names plus the archive hierarchy can exceed MAX_PATH.
    if os.name == "nt":
        shutil.copy2("\\\\?\\" + str(source.resolve()), "\\\\?\\" + str(target.resolve()))
    else:
        shutil.copy2(source, target)


def files(root):
    if not root.exists():
        raise FileNotFoundError(root)
    return sorted(p for p in root.rglob("*") if p.is_file())


def select_irelia_more(rel):
    """Keep base skin, shared resources and unresolved extraction names only."""
    name = rel.as_posix().lower()
    if len(rel.parts) == 1:
        return "unresolved"  # Hash-only extraction names cannot identify a skin.
    for prefix in (
        "assets/characters/irelia/skins/base/",
        "assets/characters/ireliablades/skins/base/",
        "assets/sounds/wwise2016/sfx/characters/irelia/skins/base/",
        "assets/shared/", "assets/particles/",
        "assets/characters/irelia/hud/icons2d/",
    ):
        if name.startswith(prefix):
            return "base_or_shared"
    if name in (
        "assets/characters/irelia/hud/irelia_circle_0.tex",
        "assets/characters/irelia/hud/irelia_square_0.tex",
    ):
        return "base_or_shared"
    for champion in ("irelia", "ireliablades"):
        prefix = f"data/characters/{champion}/"
        if name.startswith(prefix):
            tail = name[len(prefix):]
            if tail in (f"{champion}.bin", "skins/root.bin", "skins/skin0.bin", "animations/skin0.bin"):
                return "base_or_shared"
            if "/" not in tail and "skins_skin0_" in tail:
                return "base_or_shared"
    return None


class Collection:
    def __init__(self, folder, roots):
        self.folder = folder
        self.roots = roots
        self.entries = {}

    def add(self, source, destination, root_name, relative, category):
        sha = digest(source)
        # A few Riot shared-definition filenames exceed Windows' path limit
        # once nested in a project. Keep their exact original path in sources.
        if (len(str(self.folder / destination)) >= 240
                or destination.name.startswith(("irelia_multi_skins_", "ireliablades_multi_skins_"))):
            path_hash = hashlib.sha256(destination.as_posix().encode()).hexdigest()[:24]
            destination = Path("Original/LongNames") / (path_hash + destination.suffix)
        dest = destination.as_posix()
        provenance = {"root": root_name, "path": relative.as_posix()}
        if dest in self.entries:
            entry = self.entries[dest]
            if entry["sha256"] != sha:
                raise ValueError(f"Different source versions at {dest}; preserve them separately")
            entry["sources"].append(provenance)
            return
        target = self.folder / destination
        target.parent.mkdir(parents=True, exist_ok=True)
        if not target.exists() or digest(target) != sha:
            copy_file(source, target)
        if digest(target) != sha:
            raise ValueError(f"Copy verification failed: {target}")
        self.entries[dest] = {
            "path": dest, "bytes": source.stat().st_size, "sha256": sha,
            "category": category, "sources": [provenance],
        }

    def unity_tree(self, relative):
        for source in files(PROJECT / relative):
            path = source.relative_to(PROJECT)
            self.add(source, Path("Unity") / path, "project", path, "unity_snapshot")

    def unity_file(self, relative):
        for path in (relative, Path(str(relative) + ".meta")):
            if (PROJECT / path).is_file():
                self.add(PROJECT / path, Path("Unity") / path, "project", path, "unity_snapshot")

    def save(self, **extra):
        entries = sorted(self.entries.values(), key=lambda entry: entry["path"])
        summary = {
            "files": len(entries), "bytes": sum(e["bytes"] for e in entries),
            "by_extension": dict(sorted(Counter(Path(e["path"]).suffix for e in entries).items())),
            "by_category": dict(sorted(Counter(e["category"] for e in entries).items())),
        }
        manifest = {"schema_version": 1, "source_roots": self.roots, "summary": summary,
                    "files": entries, **extra}
        (self.folder / "manifest.json").write_text(
            json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
        return summary


def verify(folder):
    manifest = json.loads((folder / "manifest.json").read_text(encoding="utf-8"))
    for entry in manifest["files"]:
        path = folder / entry["path"]
        if not path.is_file() or path.stat().st_size != entry["bytes"] or digest(path) != entry["sha256"]:
            raise ValueError(f"Missing or modified collection file: {path}")
    expected = {entry["path"] for entry in manifest["files"]} | {"README.md", "manifest.json"}
    extra = {p.relative_to(folder).as_posix() for p in files(folder)} - expected
    if extra:
        raise ValueError(f"Unlisted files: {sorted(extra)}")
    return manifest["summary"]


def collect(args):
    raw = args.irelia_source.resolve()
    irelia = Collection(LIBRARY / "Irelia/Base", {"irelia": str(raw), "project": "."})
    base = raw / "base"
    for source in files(base):
        rel = source.relative_to(base)
        irelia.add(source, Path("Original/assets/characters/irelia/skins/base") / rel,
                   "irelia", source.relative_to(raw), "base_original")
    excluded = []
    for source in files(raw / "IreliaMore"):
        rel = source.relative_to(raw / "IreliaMore")
        selected = select_irelia_more(rel)
        if selected:
            dest = Path("Original/Unresolved" if selected == "unresolved" else "Original") / rel
            irelia.add(source, dest, "irelia", source.relative_to(raw),
                       "unresolved_original" if selected == "unresolved" else "base_or_shared_original")
        else:
            excluded.append(source.relative_to(raw).as_posix())
    for source in files(raw / "irelia voice"):
        rel = source.relative_to(raw / "irelia voice")
        irelia.add(source, Path("Original") / rel, "irelia", source.relative_to(raw), "voice_original")
    # Keep this separate: it may be a distinct version of the nested voice bank.
    bank = raw / "irelia_base_vo_audio.bnk"
    if bank.is_file():
        irelia.add(bank, Path("Original/AdditionalBanks") / bank.name,
                   "irelia", bank.relative_to(raw), "voice_original")
    for relative in (
        "Assets/Resources/Models/IreliaOfficial", "Assets/Resources/Audio/Irelia",
        "Assets/Resources/Audio/IreliaVoice", "Assets/SourceModels/IreliaOfficial",
        "Assets/SourceAudio/IreliaOfficial",
    ):
        irelia.unity_tree(Path(relative))
    for stem in ("Portrait", "Passive", "Q", "W", "E", "R"):
        irelia.unity_file(Path(f"Assets/Resources/HudIcons/{stem}.png"))
    for shader in sorted((PROJECT / "Assets/Resources/Shaders").glob("Irelia*.shader")):
        irelia.unity_file(shader.relative_to(PROJECT))
    irelia_summary = irelia.save(
        excluded_other_skins_or_modes=excluded,
        notes=["Unresolved contains hash-named files with no verified skin attribution.",
               "Shared dependencies may have names from other champions.",
               "The Unity directory is a snapshot, not a second Unity project."])

    dummy = Collection(LIBRARY / "TrainingDummy/Base", {"downloads": str(args.dummy_source.resolve()), "project": "."})
    for filename in ("target_dummyIdle.glb", "target_dummyHit.glb"):
        source = args.dummy_source / filename
        project_source = PROJECT / "Assets/SourceModels/TrainingDummy" / filename
        if digest(source) != digest(project_source):
            raise ValueError(f"External and active dummy models differ: {filename}")
        dummy.add(source, Path("Original") / filename, "downloads", Path(filename), "base_original")
    for relative in ("Assets/SourceModels/TrainingDummy", "Assets/Resources/Models/TrainingDummy"):
        dummy.unity_tree(Path(relative))
    dummy_summary = dummy.save(notes=["Only Idle and Hit source clips were found; no separate dummy audio/VFX bank was located."])
    return {"Irelia/Base": irelia_summary, "TrainingDummy/Base": dummy_summary}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--irelia-source", type=Path,
                        default=Path.home() / "Downloads/LOL Data/ireliaLOL")
    parser.add_argument("--dummy-source", type=Path, default=Path.home() / "Downloads")
    parser.add_argument("--verify", action="store_true")
    args = parser.parse_args()
    if args.verify:
        result = {name: verify(LIBRARY / name) for name in ("Irelia/Base", "TrainingDummy/Base")}
    else:
        result = collect(args)
    print(json.dumps(result, indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main()
