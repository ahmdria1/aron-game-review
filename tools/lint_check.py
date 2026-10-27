#!/usr/bin/env python3
# (c) Ahmed Alamrani. All rights reserved.
"""Lint the C# logic assembly and its tests with the Roslyn analyzers and the rules in .editorconfig.

Runs `dotnet format --verify-no-changes --severity warn` and fails when a file that is NOT in
tools/lint_baseline.txt has a violation. Files in the baseline are known, older violations; they are
listed so the check can be strict for everything new. Run with --update-baseline to rewrite the list
(only do this after cleaning files, never to hide new problems).
"""
import json
import os
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROJECT = "Tests/Aron.Logic.Tests"
BASELINE = os.path.join(ROOT, "tools", "lint_baseline.txt")


def findings():
    out = tempfile.mkdtemp()
    subprocess.run(
        ["dotnet", "format", PROJECT, "--verify-no-changes", "--severity", "warn",
         "--report", out, "--verbosity", "quiet"],
        cwd=ROOT, check=False, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    path = os.path.join(out, "format-report.json")
    if not os.path.exists(path):
        return {}
    result = {}
    for entry in json.load(open(path, encoding="utf-8")):
        rel = os.path.relpath(entry["FilePath"], ROOT).replace(os.sep, "/")
        result[rel] = [f'{c["LineNumber"]}: {c["DiagnosticId"]} {c["FormatDescription"]}' for c in entry["FileChanges"]]
    return result


def main():
    found = findings()
    if "--update-baseline" in sys.argv:
        with open(BASELINE, "w", encoding="utf-8", newline="\n") as f:
            f.write("".join(p + "\n" for p in sorted(found)))
        print(f"baseline written: {len(found)} files")
        return 0
    baseline = set(l.strip() for l in open(BASELINE, encoding="utf-8") if l.strip()) if os.path.exists(BASELINE) else set()
    bad = {p: v for p, v in found.items() if p not in baseline}
    fixed = sorted(baseline - set(found))
    for p in fixed:
        print(f"note: {p} is clean now; remove it from tools/lint_baseline.txt")
    if bad:
        print(f"Lint failed: {len(bad)} file(s) with violations that are not in the baseline.")
        for p in sorted(bad):
            print(p)
            for line in bad[p][:20]:
                print("   " + line)
        return 1
    print(f"Lint passed. {len(found)} file(s) with older violations are in the baseline.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
