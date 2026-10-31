"""Small, explicit wrapper around the locally installed ClamAV scanner."""

from dataclasses import dataclass
from pathlib import Path
import shutil
import subprocess


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
