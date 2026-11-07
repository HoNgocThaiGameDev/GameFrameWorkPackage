"""Render editable PlantUML sources locally; never uploads project source."""
from __future__ import annotations

import argparse
import hashlib
import json
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DIAGRAMS = ROOT / "docs/diagrams/uml"


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--jar", type=Path, required=True,
                        help="Path to the local PlantUML executable JAR")
    parser.add_argument("--java", default="java")
    parser.add_argument("--png", action="store_true", help="Also export preview PNGs")
    args = parser.parse_args()
    if not args.jar.is_file():
        raise FileNotFoundError(args.jar)
    sources = sorted(DIAGRAMS.glob("*.puml"))
    base = [args.java, "-Djava.awt.headless=true", "-jar", str(args.jar.resolve()),
            "-charset", "UTF-8", "-failfast2"]
    subprocess.run(base + ["-tsvg"] + [str(p) for p in sources], check=True, cwd=DIAGRAMS)
    if args.png:
        subprocess.run(base + ["-tpng"] + [str(p) for p in sources], check=True, cwd=DIAGRAMS)
    # Check for error-diagram output and unresolved source markers.
    for source in sources:
        svg = source.with_suffix(".svg")
        text = svg.read_text(encoding="utf-8") if svg.is_file() else ""
        if not text or any(msg in text for msg in ["Syntax Error", "This position is ignored"]):
            raise RuntimeError("Invalid render: " + str(source))
    print(json.dumps({"diagrams": len(sources), "jar": str(args.jar),
                      "jar_sha256": hashlib.sha256(args.jar.read_bytes()).hexdigest()}))


if __name__ == "__main__":
    main()
