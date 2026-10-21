"""Export changed project sources for review; never applies them to the full game."""
import argparse
import hashlib
import json
from pathlib import Path
import zipfile

ROOT = Path(__file__).resolve().parents[1]
BASELINE = ROOT / "Handoff/Baseline.json"
WATCH = ("Assets", "Packages", "ProjectSettings", "Tools", "CharacterAssets", "Handoff", "References")


def snapshot():
    result = {}
    for folder in WATCH:
        for path in sorted((ROOT / folder).rglob("*")):
            relative = path.relative_to(ROOT).as_posix()
            if (path.is_file() and "__pycache__" not in path.parts
                    and relative != "Handoff/Baseline.json" and not relative.startswith("Handoff/Validation/")):
                result[relative] = hashlib.sha256(path.read_bytes()).hexdigest()
    return result


def export(output, baseline):
    initial = baseline["delivery_sha256"]
    if not initial:
        raise RuntimeError("The delivery baseline has not been finalized.")
    current = snapshot()
    changes = []
    included = set()
    for path in sorted(initial.keys() | current.keys()):
        if initial.get(path) == current.get(path):
            continue
        status = "deleted" if path not in current else "added" if path not in initial else "modified"
        original = baseline["original_sha256"].get(path)
        adapter = path.startswith(("Assets/Tests/", "Assets/Scripts/LaneWorld.PracticeVisualOnly", "Assets/Editor/PracticeVisualWorkshop"))
        changes.append({"path": path, "status": status, "delivery_sha256": initial.get(path),
                        "new_sha256": current.get(path), "original_sha256": original,
                        "merge_required": adapter or (path in initial and original != initial.get(path)),
                        "note": "Review manually against the full project before applying."})
        if status != "deleted":
            included.add(path)
            # Keep stable GUIDs, including when only an asset's contents changed.
            meta = path + ".meta"
            if meta in current:
                included.add(meta)
            for parent in (ROOT / path).parents:
                if parent == ROOT:
                    break
                folder_meta = parent.relative_to(ROOT).as_posix() + ".meta"
                if folder_meta in current:
                    included.add(folder_meta)
    if "Handoff/ARTIST_NOTES.md" in current:
        included.add("Handoff/ARTIST_NOTES.md")
    report = {"schema_version": 1, "source_commit": baseline["source_commit"],
              "changes": changes, "files_in_archive": sorted(included),
              "instructions": "Files/ preserves project paths. Review merge_required files; do not replace the full project wholesale. Deleted entries are proposals, not automatic deletions."}
    output.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(output, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
        archive.writestr("VisualChangeReport.json", json.dumps(report, indent=2, ensure_ascii=False) + "\n")
        for path in sorted(included):
            archive.write(ROOT / path, "Files/" + path)
    print(json.dumps({"archive": str(output), "changes": len(changes), "files": len(included)}, indent=2))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, default=ROOT / "Deliverables/VisualChanges.zip")
    parser.add_argument("--finalize-baseline", action="store_true",
                        help="Delivery preparation only. Never reset this after the artist begins.")
    args = parser.parse_args()
    baseline = json.loads(BASELINE.read_text(encoding="utf-8"))
    if args.finalize_baseline:
        if baseline["delivery_sha256"]:
            raise RuntimeError("Baseline already finalized; refusing to discard the artist's change history.")
        baseline["delivery_sha256"] = snapshot()
        baseline["handoff_modified_paths"] = sorted(path for path, sha in baseline["delivery_sha256"].items()
                                                   if baseline["original_sha256"].get(path) != sha)
        BASELINE.write_text(json.dumps(baseline, indent=2) + "\n", encoding="utf-8")
        print("Delivery baseline finalized.")
    else:
        export(args.output.resolve(), baseline)


if __name__ == "__main__":
    main()
