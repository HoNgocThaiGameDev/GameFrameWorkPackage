"""Compile Firebase adapters and game composition against extracted SDK DLLs; never install or build a player."""
import argparse
import json
from pathlib import Path
import re
import subprocess
import sys


ROOT = Path(__file__).resolve().parents[2]


def compiler_path():
    output = subprocess.check_output(["dotnet", "--list-sdks"], text=True)
    candidates = []
    for version, folder in re.findall(r"^(\S+) \[([^\]]+)\]", output, re.M):
        candidate = Path(folder) / version / "Roslyn/bincore/csc.dll"
        if candidate.exists():
            candidates.append(candidate)
    if not candidates:
        raise RuntimeError("Install a .NET SDK or pass --csc pointing to csc.dll.")
    return candidates[-1]


def unity_response(assembly):
    candidates = list((ROOT / "Library/Bee/artifacts").glob(f"*/{assembly}.rsp"))
    # Player responses do not describe the currently imported Editor sources.
    candidates = [p for p in candidates if "-define:UNITY_EDITOR\n" in p.read_text(encoding="utf-8-sig")]
    if not candidates:
        raise RuntimeError(f"Open Unity and let script compilation finish first: missing Editor response for {assembly}.")
    return max(candidates, key=lambda p: p.stat().st_mtime)


def compile_assembly(assembly, sdk, folder, defines, csc, project_references=()):
    lines = unity_response(assembly).read_text(encoding="utf-8-sig").splitlines()
    filtered = []
    for line in lines:
        lower = line.lower()
        if lower.startswith(("-out:", "-refout:", "/out:", "/refout:", "-analyzer:", "/analyzer:", "/additionalfile:", "-additionalfile:")):
            continue
        if lower.startswith(("-r:", "/reference:", "-reference:")):
            if re.search(r"[\\/]Firebase\.[^\\/]+\.dll", line, re.I):
                continue
            reference_name = Path(line.split(":", 1)[1].strip('"')).name.removesuffix(".dll").removesuffix(".ref")
            if any(reference_name == dll.stem for dll in project_references):
                continue
        if line.startswith("-define:"):
            names = [s for s in line[len("-define:"):].split(";") if s not in ("FIREBASE", "FIREBASE_ANALYTICS")]
            if not names:
                continue
            line = "-define:" + ";".join(names)
        filtered.append(line)
    target = folder / f"{assembly}.dll"
    filtered.append(f'-out:"{target}"')
    if defines:
        filtered.append("-define:" + ";".join(defines))
    for dll in (*sdk, *project_references):
        filtered.append(f'-r:"{dll}"')
    response = folder / f"{assembly}.rsp"
    response.write_text("\n".join(filtered) + "\n", encoding="utf-8")
    result = subprocess.run(["dotnet", str(csc), "@" + str(response)], cwd=ROOT, capture_output=True, text=True)
    log = result.stdout + result.stderr
    (folder / f"{assembly}.log").write_text(log, encoding="utf-8")
    errors = [line for line in log.splitlines() if re.search(r"\berror\s+(?:CS|[A-Z]+)\d+", line)]
    warnings = len(re.findall(r"\bwarning CS\d+", log))
    print(f"  {assembly}: {'PASS' if result.returncode == 0 else 'FAIL'}, {len(errors)} errors, {warnings} warnings")
    for error in errors[:12]:
        print("    " + error)
    return {"assembly": assembly, "passed": result.returncode == 0, "errors": errors, "warnings": warnings}, target


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--sdk-dir", action="append", required=True, help="Repeat to check multiple extracted SDK versions.")
    parser.add_argument("--csc", type=Path, help="Path to Roslyn csc.dll; .NET SDK compiler is discovered by default.")
    args = parser.parse_args()
    csc = args.csc or compiler_path()
    results = []
    for directory in args.sdk_dir:
        sdk_root = Path(directory).resolve()
        sdk = sorted(sdk_root.glob("Firebase.*.dll"))
        required = {"Firebase.App.dll", "Firebase.Auth.dll", "Firebase.Firestore.dll", "Firebase.Analytics.dll"}
        if not required.issubset({p.name for p in sdk}):
            raise RuntimeError(f"{sdk_root}: expected App/Auth/Firestore/Analytics DLLs from the same SDK release.")
        print(f"SDK {sdk_root.name}")
        for mode, defines in (("all", ["FIREBASE", "FIREBASE_ANALYTICS"]), ("analytics-only", ["FIREBASE_ANALYTICS"]), ("cloud-only", ["FIREBASE"])):
            folder = ROOT / "Library/FirebaseCompatibilityChecks" / sdk_root.name / mode
            folder.mkdir(parents=True, exist_ok=True)
            print(f" {mode}")
            mode_sdk = [dll for dll in sdk if not (
                mode == "analytics-only" and dll.name in ("Firebase.Auth.dll", "Firebase.Firestore.dll") or
                mode == "cloud-only" and dll.name == "Firebase.Analytics.dll")]
            adapter, dll = compile_assembly("NebulaSoft.Firebase", mode_sdk, folder, defines, csc)
            results.append({"sdk": str(sdk_root), "mode": mode, **adapter})
            if adapter["passed"]:
                # Deliberately omit every Firebase SDK reference from game and Editor.
                # Keep the same feature defines so conditional code cannot evade this check.
                game, game_dll = compile_assembly("Assembly-CSharp", [], folder, defines, csc, (dll,))
                results.append({"sdk": str(sdk_root), "mode": mode, **game})
                if game["passed"]:
                    editor, _ = compile_assembly("Assembly-CSharp-Editor", [], folder, defines, csc, (dll, game_dll))
                    results.append({"sdk": str(sdk_root), "mode": mode, **editor})
    report = ROOT / "Library/FirebaseCompatibilityChecks/result.json"
    report.write_text(json.dumps(results, indent=2), encoding="utf-8")
    print(f"Report: {report}")
    return 0 if all(item["passed"] for item in results) else 1


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (RuntimeError, OSError, subprocess.SubprocessError) as error:
        print(error, file=sys.stderr)
        sys.exit(1)
