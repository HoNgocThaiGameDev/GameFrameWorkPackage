"""Read-only inventory and GUID triage. Unresolved package GUIDs are NOT missing-script findings."""
from __future__ import annotations

import argparse
from collections import Counter, defaultdict
import hashlib
import json
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[2]
SOURCE_ROOTS = ("Assets/NebulaSoft Core", "Assets/Project Files/Game", "Assets/Tests")


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig", errors="replace")


def relative(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def scan() -> dict:
    sources, counts, assemblies = [], {}, []
    for name in SOURCE_ROOTS:
        files = sorted((ROOT / name).rglob("*.cs"))
        counts[name] = {"files": len(files), "lines": sum(len(read(p).splitlines()) for p in files)}
        for path in files:
            ancestor = next((folder for folder in [path.parent, *path.parents] if folder.is_relative_to(ROOT / "Assets") and list(folder.glob("*.asmdef"))), None)
            sources.append({"path": relative(path), "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
                            "assembly_folder": relative(ancestor) if ancestor else "predefined assembly"})
        for path in sorted((ROOT / name).rglob("*.asmdef")):
            assemblies.append({"path": relative(path), **json.loads(read(path))})

    guid_map = defaultdict(list)
    orphan_metas = []
    # Library/PackageCache may be absent. Only actual local metadata is resolved.
    meta_roots = [ROOT / "Assets", ROOT / "Packages"]
    cache = ROOT / "Library/PackageCache"
    if cache.exists():
        meta_roots.append(cache)
    for base in meta_roots:
        for path in base.rglob("*.meta"):
            match = re.search(r"^guid: ([0-9a-f]{32})$", read(path), re.M)
            if match:
                guid_map[match[1]].append(relative(path.with_suffix("")))
            if not path.with_suffix("").exists():
                orphan_metas.append(relative(path))

    unresolved = []
    confirmed_local_stale = []
    asset_count = 0
    for path in (ROOT / "Assets").rglob("*"):
        if path.suffix not in (".unity", ".prefab", ".asset"):
            continue
        if any(part in path.parts for part in ("Plugins", "ThirdParty", "GoogleMobileAds", "ExternalDependencyManager", "TextMesh Pro")):
            continue
        asset_count += 1
        for block in re.split(r"(?=^--- !u!)", read(path), flags=re.M):
            match = re.search(r"m_Script:.*guid: ([0-9a-f]{32})", block)
            if not match or match[1] in guid_map:
                continue
            name = re.search(r"^  m_Name: (.*)$", block, re.M)
            entry = {"path": relative(path), "guid": match[1], "name": name[1] if name else "",
                     "classification": "unresolved; may belong to a UPM package"}
            if entry["name"] in ("NebulaSoft.AnalyticsHelperInitModule", "Level Skin Database"):
                entry["classification"] = "stale first-party script reference; active usage must be checked separately"
                confirmed_local_stale.append(entry)
            unresolved.append(entry)

    status = subprocess.run(["git", "status", "--porcelain", "-z"], cwd=ROOT, capture_output=True, check=True).stdout
    status_counts = Counter(entry[:2].decode("utf-8", errors="replace") for entry in status.split(b"\0") if entry)
    tests = list((ROOT / "Assets/Tests").rglob("*.cs"))
    return {
        "audit_date": "2026-11-07",
        "scope": "Working tree; automated inventory is not a full semantic or Unity build verification",
        "unity_version": read(ROOT / "ProjectSettings/ProjectVersion.txt").splitlines()[0],
        "source_counts": counts,
        "test_attributes": dict(Counter(tag for path in tests for tag in re.findall(r"\[(Test|TestCase|UnityTest)(?:\]|\()", read(path)))),
        "working_tree_status_counts_at_scan": dict(status_counts),
        "first_party_assemblies": assemblies,
        "assets_examined_for_script_guids": asset_count,
        "package_cache_available": cache.exists(),
        "duplicate_local_guids": {key: values for key, values in guid_map.items() if len(values) > 1},
        "orphan_meta_files": orphan_metas,
        "stale_first_party_script_references": confirmed_local_stale,
        "unresolved_script_references": unresolved,
        "source_fingerprints": sources,
    }


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    result = scan()
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(json.dumps({key: value for key, value in result.items() if key in (
        "source_counts", "test_attributes", "assets_examined_for_script_guids", "package_cache_available",
        "duplicate_local_guids", "stale_first_party_script_references")}, indent=2, ensure_ascii=False))
