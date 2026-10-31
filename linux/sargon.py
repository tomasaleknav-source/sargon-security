#!/usr/bin/env python3
"""SARGON Linux preview: a small desktop interface for ClamAV."""

from datetime import datetime, timezone
from pathlib import Path
import os
import shutil
import subprocess
import threading
import tkinter as tk
from tkinter import filedialog, messagebox
import webbrowser

from scanner import clamscan_path, scan_path


VERSION = "0.1.0-preview"
BLUE = "#1677b8"
BG = "#071321"
PANEL = "#102236"
TEXT = "#f4f8fc"
MUTED = "#afc0d1"
GREEN = "#20b26b"
RED = "#d83d4d"
AMBER = "#e8ad43"
LOG_PATH = Path.home() / ".local" / "state" / "sargon" / "scan.log"
DB_PATH = Path("/var/lib/clamav")


def database_status() -> tuple[str, str]:
    try:
        files = [p for p in DB_PATH.iterdir() if p.suffix in (".cvd", ".cld")]
    except OSError:
        files = []
    if not files:
        return "Parašų bazės nerasta", "red"
    latest = max(p.stat().st_mtime for p in files)
    age_days = max(0, (datetime.now(timezone.utc).timestamp() - latest) // 86400)
    stamp = datetime.fromtimestamp(latest).strftime("%Y-%m-%d %H:%M")
    if age_days > 7:
        return f"Baziniai failai seni ({stamp})", "amber"
    return f"Bazė atnaujinta: {stamp}", "green"


def realtime_status() -> str:
    # A running clamd alone does not mean that on-access scanning is enabled.
    try:
        result = subprocess.run(
            ["pgrep", "-x", "clamonacc"],
            capture_output=True,
            text=True,
            timeout=3,
            check=False,
        )
        if result.returncode == 0:
            return "ClamAV on-access procesas veikia; patikrinkite jo stebimus aplankus ir žurnalą."
    except (OSError, subprocess.TimeoutExpired):
        pass
    return "Realaus laiko apsauga neįjungta šiame peržiūros leidime."


class SargonApp(tk.Tk):
    def __init__(self) -> None:
        super().__init__()
        self.title(f"SARGON Security Linux {VERSION}")
        self.geometry("780x720")
        self.minsize(650, 600)
        self.configure(bg=BG)
        self.busy = False
        self._build()
        self._refresh_status()

    def _label(self, parent: tk.Widget, text: str, size: int, color: str = TEXT, bold: bool = False) -> tk.Label:
        return tk.Label(
            parent,
            text=text,
            bg=parent.cget("bg"),
            fg=color,
            font=("DejaVu Sans", size, "bold" if bold else "normal"),
            anchor="w",
            justify="left",
        )

    def _button(self, parent: tk.Widget, text: str, command, color: str = BLUE) -> tk.Button:
        return tk.Button(
            parent,
            text=text,
            command=command,
            bg=color,
            fg="white",
            activebackground="#3192d0",
            activeforeground="white",
            relief="flat",
            bd=0,
            padx=16,
            pady=12,
            font=("DejaVu Sans", 11, "bold"),
            cursor="hand2",
        )

    def _build(self) -> None:
        outer = tk.Frame(self, bg=BG, padx=24, pady=20)
        outer.pack(fill="both", expand=True)

        head = tk.Frame(outer, bg=BG)
        head.pack(fill="x")
        shield = tk.Canvas(head, width=58, height=64, bg=BG, highlightthickness=0)
        shield.pack(side="left", padx=(0, 14))
        shield.create_polygon(29, 4, 52, 13, 49, 38, 29, 59, 9, 38, 6, 13, fill=BLUE, outline="#72ccff", width=2)
        shield.create_line(29, 17, 29, 42, fill="white", width=5, capstyle="round")
        shield.create_line(18, 29, 40, 29, fill="white", width=5, capstyle="round")
        titlebox = tk.Frame(head, bg=BG)
        titlebox.pack(side="left", fill="x", expand=True)
        self._label(titlebox, "SARGON Security", 24, "#71d9c5", True).pack(anchor="w")
        self._label(titlebox, f"Linux peržiūros leidimas  •  {VERSION}", 11, MUTED).pack(anchor="w", pady=(4, 0))

        self.status_card = tk.Frame(outer, bg=PANEL, padx=18, pady=16, highlightthickness=1, highlightbackground="#294663")
        self.status_card.pack(fill="x", pady=(22, 14))
        self.status_title = self._label(self.status_card, "Apsaugos būsena tikrinama…", 16, TEXT, True)
        self.status_title.pack(fill="x")
        self.status_lines = self._label(self.status_card, "", 11, MUTED)
        self.status_lines.pack(fill="x", pady=(8, 0))

        self._label(outer, "Patikra", 17, TEXT, True).pack(anchor="w", pady=(8, 10))
        actions = tk.Frame(outer, bg=BG)
        actions.pack(fill="x")
        self.scan_file_button = self._button(actions, "Tikrinti failą…", self._choose_file)
        self.scan_file_button.pack(side="left", fill="x", expand=True, padx=(0, 6))
        self.scan_folder_button = self._button(actions, "Tikrinti aplanką…", self._choose_folder)
        self.scan_folder_button.pack(side="left", fill="x", expand=True, padx=6)
        self.update_button = self._button(actions, "Atnaujinti bazę", self._update_database, color="#15547d")
        self.update_button.pack(side="left", fill="x", expand=True, padx=(6, 0))

        self._label(outer, "Paskutinio veiksmo rezultatas", 14, TEXT, True).pack(anchor="w", pady=(20, 8))
        self.result_box = tk.Text(
            outer,
            height=12,
            bg="#050d16",
            fg=TEXT,
            insertbackground=TEXT,
            relief="flat",
            padx=12,
            pady=10,
            wrap="word",
            font=("DejaVu Sans Mono", 10),
        )
        self.result_box.pack(fill="both", expand=True)
        self.result_box.insert("1.0", "Pasirinkite failą arba aplanką. SARGON patikrins jį su vietoje įdiegta ClamAV baze.")
        self.result_box.configure(state="disabled")

        note = (
            "Ši versija nėra pilna antivirusinė: ji nenuskaito visos sistemos fone ir pati neužkerta kelio visoms grėsmėms. "
            "Švarus rezultatas nėra saugumo garantija."
        )
        self._label(outer, note, 10, AMBER).pack(anchor="w", pady=(12, 6))
        link = tk.Label(outer, text="ClamAV realaus laiko apsaugos dokumentacija", bg=BG, fg="#78c8ff", cursor="hand2", font=("DejaVu Sans", 10, "underline"))
        link.pack(anchor="w")
        link.bind("<Button-1>", lambda _event: webbrowser.open("https://docs.clamav.net/manual/OnAccess.html"))

    def _refresh_status(self) -> None:
        if clamscan_path() is None:
            text = "ClamAV neįdiegtas. Įdiekite ClamAV, tada paleiskite SARGON iš naujo."
            color = RED
        else:
            text, key = database_status()
            color = {"green": GREEN, "amber": AMBER, "red": RED}[key]
            version = subprocess.run([clamscan_path(), "--version"], capture_output=True, text=True, check=False).stdout.strip()
            text = f"ClamAV: {version or 'įdiegtas'}\n{text}\n{realtime_status()}"
        self.status_title.configure(text="Apsaugos būklė", fg=color)
        self.status_lines.configure(text=text)

    def _set_result(self, message: str, color: str = TEXT) -> None:
        self.result_box.configure(state="normal", fg=color)
        self.result_box.delete("1.0", "end")
        self.result_box.insert("1.0", message[:30000])
        self.result_box.configure(state="disabled")

    def _set_busy(self, value: bool) -> None:
        self.busy = value
        state = "disabled" if value else "normal"
        for button in (self.scan_file_button, self.scan_folder_button, self.update_button):
            button.configure(state=state)

    def _choose_file(self) -> None:
        path = filedialog.askopenfilename(title="Pasirinkite failą patikrai")
        if path:
            self._scan(path)

    def _choose_folder(self) -> None:
        path = filedialog.askdirectory(title="Pasirinkite aplanką patikrai")
        if path:
            self._scan(path)

    def _scan(self, path: str) -> None:
        if self.busy:
            return
        self._set_busy(True)
        self._set_result(f"Tikrinama…\n{path}\n\nPatikra gali užtrukti.", BLUE)

        def work() -> None:
            result = scan_path(path)
            self.after(0, lambda: self._finish_scan(path, result))

        threading.Thread(target=work, daemon=True).start()

    def _finish_scan(self, path, result) -> None:
        self._set_busy(False)
        if result.state == "clean":
            title, color = "ATITIKMENŲ NERASTA — tai negarantuoja, kad failai saugūs.", GREEN
        elif result.state == "threat":
            title, color = "GALIMA GRĖSMĖ — nebandykite atidaryti rastų failų.", RED
        else:
            title, color = "PATIKRA NEBAIGTA — rezultatu nepasikliaukite.", AMBER
        message = f"{title}\n\nKelias: {path}\n\n{result.output}"
        self._set_result(message, color)
        self._write_log(path, result.state, result.output)
        self._refresh_status()

    def _update_database(self) -> None:
        if self.busy:
            return
        freshclam = shutil.which("freshclam")
        if not freshclam:
            messagebox.showerror("SARGON", "Komanda freshclam nerasta. Įdiekite ClamAV atnaujinimo komponentą.")
            return
        if os.geteuid() == 0:
            command = [freshclam]
        elif shutil.which("pkexec"):
            command = [shutil.which("pkexec"), freshclam]
        else:
            messagebox.showerror("SARGON", "Parašų bazei atnaujinti reikia administratoriaus teisių. Įdiekite PolicyKit (pkexec).")
            return
        self._set_busy(True)
        self._set_result("Siunčiamasi oficiali ClamAV parašų bazė…\nPatvirtinkite administratoriaus teises, jei sistema paprašys.", BLUE)

        def work() -> None:
            try:
                result = subprocess.run(command, capture_output=True, text=True, errors="replace", timeout=900, check=False)
                output = (result.stdout + result.stderr).strip() or f"freshclam grąžino kodą {result.returncode}."
                state = "clean" if result.returncode == 0 else "error"
            except (OSError, subprocess.TimeoutExpired) as exc:
                output, state = f"Atnaujinimas nepavyko: {exc}", "error"
            self.after(0, lambda: self._finish_update(state, output))

        threading.Thread(target=work, daemon=True).start()

    def _finish_update(self, state: str, output: str) -> None:
        self._set_busy(False)
        color = GREEN if state == "clean" else AMBER
        title = "Parašų atnaujinimas baigtas." if state == "clean" else "Parašų bazė neatnaujinta."
        self._set_result(f"{title}\n\n{output}", color)
        self._refresh_status()

    @staticmethod
    def _write_log(path: str, state: str, output: str) -> None:
        try:
            LOG_PATH.parent.mkdir(parents=True, exist_ok=True)
            timestamp = datetime.now().astimezone().isoformat(timespec="seconds")
            excerpt = " | ".join(output.splitlines()[:12])[:1500]
            with LOG_PATH.open("a", encoding="utf-8") as handle:
                handle.write(f"{timestamp}\t{state}\t{path}\t{excerpt}\n")
        except OSError:
            pass


if __name__ == "__main__":
    SargonApp().mainloop()
