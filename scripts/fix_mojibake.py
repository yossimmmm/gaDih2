from pathlib import Path
from ftfy import fix_text

ROOT = Path(__file__).resolve().parents[1]
TARGETS = [
    ROOT / "TriviaGame",
    ROOT / "TriviaDBL",
    ROOT / "ModelsTrivia",
    ROOT / "TriviaGame.Mobile",
    ROOT / "docs",
]
EXTS = {".razor", ".cs", ".js", ".xaml", ".md", ".json"}

changed = 0
for base in TARGETS:
    for p in base.rglob("*"):
        if not p.is_file():
            continue
        if p.suffix.lower() not in EXTS:
            continue
        if "\\bin\\" in str(p) or "\\obj\\" in str(p):
            continue
        try:
            src = p.read_text(encoding="utf-8")
        except UnicodeDecodeError:
            continue
        fixed = fix_text(src)
        if fixed != src:
            p.write_text(fixed, encoding="utf-8")
            changed += 1
            print(p)

print(f"changed_files={changed}")
