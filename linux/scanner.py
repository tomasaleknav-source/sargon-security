"""Small, explicit wrapper around the locally installed ClamAV scanner."""

from dataclasses import dataclass
import json
import os
from pathlib import Path
import shutil
import subprocess
import uuid


@dataclass
class ScanResult:
    state: str
    output: str
    returncode: int


def clamscan_path() -> str | None:
    return shutil.which("clamscan")


def classify_scan(returncode: int, output: str) -> str:
    if returncode == 1 or " FOUND" in output:
        return "threat"
    if returncode == 0:
        return "clean"
    return "error"


def infected_file_paths(output: str) -> list[Path]:
    found: list[Path] = []
    for line in output.splitlines():
        line = line.strip()
        if not line.endswith(" FOUND"):
            continue
        left = line[:-6].rsplit(": ", 1)[0]
        path = Path(left)
        if path not in found:
            found.append(path)
    return found


def _manifest_path(quarantine_dir: Path) -> Path:
    return quarantine_dir / "manifest.json"


def _save_manifest(path: Path, records: list[dict]) -> None:
    temp = path.with_suffix(".tmp")
    temp.write_text(json.dumps(records, ensure_ascii=False, indent=2), encoding="utf-8")
    os.chmod(temp, 0o600)
    temp.replace(path)


def quarantine_file(source: str | Path, quarantine_dir: str | Path | None = None) -> Path:
    source = Path(source).expanduser()
    if source.is_symlink() or not source.is_file():
        raise ValueError("Karantinuoti galima tik esamą įprastą failą, ne nuorodą ar aplanką.")
    source = source.resolve(strict=True)
    home = Path.home()
    destination_dir = Path(quarantine_dir) if quarantine_dir else home / ".local" / "share" / "sargon" / "quarantine"
    destination_dir.mkdir(parents=True, exist_ok=True, mode=0o700)
    os.chmod(destination_dir, 0o700)
    if source.parent == destination_dir.resolve() or destination_dir.resolve() in source.parents:
        raise ValueError("Failas jau yra SARGON karantine.")

    manifest = _manifest_path(destination_dir)
    try:
        records = json.loads(manifest.read_text(encoding="utf-8")) if manifest.exists() else []
    except (OSError, json.JSONDecodeError):
        raise RuntimeError("Karantino registras sugadintas; failas nepajudintas.")

    quarantined = destination_dir / f"{uuid.uuid4().hex}_{source.name}"
    # Persist the intended move first. If the record cannot be written, leave
    # the source untouched; if the move fails, roll the record back.
    records.append({"stored": quarantined.name, "original": str(source), "mode": source.stat().st_mode & 0o777})
    _save_manifest(manifest, records)
    try:
        shutil.move(str(source), str(quarantined))
        os.chmod(quarantined, 0o600)
    except Exception:
        _save_manifest(manifest, records[:-1])
        raise
    return quarantined


def restore_latest(quarantine_dir: str | Path | None = None) -> Path:
    destination_dir = Path(quarantine_dir) if quarantine_dir else Path.home() / ".local" / "share" / "sargon" / "quarantine"
    manifest = _manifest_path(destination_dir)
    if not manifest.is_file():
        raise FileNotFoundError("Karantine nėra atkuriamų failų.")
    try:
        records = json.loads(manifest.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        raise RuntimeError("Karantino registras neįskaitomas.")
    if not records:
        raise FileNotFoundError("Karantine nėra atkuriamų failų.")

    record = records[-1]
    stored = destination_dir / record["stored"]
    original = Path(record["original"])
    if not stored.is_file():
        raise FileNotFoundError("Karantinuoto failo turinys nerastas.")
    if original.exists() or original.is_symlink():
        raise FileExistsError(f"Atkūrimas sustabdytas, nes pradinis kelias jau užimtas: {original}")
    if not original.parent.is_dir():
        raise FileNotFoundError("Pradinis aplankas neberastas; atkūrimas sustabdytas.")

    shutil.move(str(stored), str(original))
    try:
        os.chmod(original, int(record.get("mode", 0o600)))
        _save_manifest(manifest, records[:-1])
    except Exception:
        # Avoid losing track of a restored file if registry update fails.
        raise RuntimeError(f"Failas atkurtas į {original}, tačiau karantino registras neatnaujintas.")
    return original


def scan_path(path: str | Path, timeout: int = 21600) -> ScanResult:
    scanner = clamscan_path()
    if scanner is None:
        return ScanResult("error", "ClamAV nerastas. Įdiekite ClamAV ir bandykite dar kartą.", 127)

    target = Path(path).expanduser().resolve()
    if not target.exists():
        return ScanResult("error", f"Nurodytas kelias neegzistuoja: {target}", 2)

    command = [scanner, "--infected", "--no-summary", "--stdout"]
    if target.is_dir():
        command.append("--recursive")
    command.append(str(target))

    try:
        completed = subprocess.run(
            command,
            capture_output=True,
            text=True,
            errors="replace",
            timeout=timeout,
            check=False,
        )
    except subprocess.TimeoutExpired:
        return ScanResult("error", "Patikra viršijo 6 valandų ribą ir buvo sustabdyta.", 124)
    except OSError as exc:
        return ScanResult("error", f"Nepavyko paleisti ClamAV: {exc}", 2)

    output = (completed.stdout + completed.stderr).strip()
    if not output:
        output = "ClamAV nepateikė išsamių eilučių."
    return ScanResult(classify_scan(completed.returncode, output), output, completed.returncode)
