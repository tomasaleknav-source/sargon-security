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

from scanner import clamscan_path, infected_file_paths, quarantine_file, restore_latest, scan_path


VERSION = "0.2.0-preview"
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
        self.last_infected_paths: list[Path] = []
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

        recovery = tk.Frame(outer, bg=BG)
        recovery.pack(fill="x", pady=(8, 0))
        self.quarantine_button = self._button(recovery, "Karantinuoti aptiktus failus", self._quarantine, color="#9a3c47")
        self.quarantine_button.configure(state="disabled")
        self.quarantine_button.pack(side="left", fill="x", expand=True, padx=(0, 6))
        self.restore_button = self._button(recovery, "Atkurti paskutinį failą", self._restore_quarantined, color="#294663")
        self.restore_button.pack(side="left", fill="x", expand=True, padx=(6, 0))

        self.auto_button = self._button(outer, "Įjungti automatinius bazės atnaujinimus", self._enable_auto_updates, color="#15547d")
        self.auto_button.pack(fill="x", pady=(8, 0))

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
            "Tai ClamAV pagrindu veikianti peržiūros versija: ji tikrina pasirinktus failus, bet neteikia visos sistemos "
            "realaus laiko apsaugos. Aptiktus failus galite perkelti į karantiną. Švarus rezultatas nėra saugumo garantija."
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
            text = f"ClamAV: {version or 'įdiegtas'}\n{text}\n{self._auto_update_status()}\n{realtime_status()}"
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
        for button in (self.scan_file_button, self.scan_folder_button, self.update_button,
                       self.restore_button, self.auto_button):
            button.configure(state=state)
        self.quarantine_button.configure(state="disabled" if value or not self.last_infected_paths else "normal")

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
        self.last_infected_paths = []
        if result.state == "clean":
            title, color = "ATITIKMENŲ NERASTA — tai negarantuoja, kad failai saugūs.", GREEN
            self.quarantine_button.configure(state="disabled")
        elif result.state == "threat":
            title, color = "GALIMA GRĖSMĖ — nebandykite atidaryti rastų failų.", RED
            self.last_infected_paths = infected_file_paths(result.output)
            self.quarantine_button.configure(state="normal" if self.last_infected_paths else "disabled")
        else:
            title, color = "PATIKRA NEBAIGTA — rezultatu nepasikliaukite.", AMBER
            self.quarantine_button.configure(state="disabled")
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

    def _quarantine(self) -> None:
        if not self.last_infected_paths:
            messagebox.showinfo("SARGON", "Šiuo metu nėra aptiktų failų karantinui.")
            return
        if not messagebox.askyesno(
            "SARGON karantinas",
            f"Perkelti {len(self.last_infected_paths)} aptiktą failą(-us) į SARGON karantiną? Failai bus atskirti nuo pradinių vietų, bet automatiškai neištrinami.",
        ):
            return
        moved, errors = [], []
        for path in self.last_infected_paths:
            try:
                moved.append(str(quarantine_file(path)))
            except Exception as exc:
                errors.append(f"{path}: {exc}")
        self.last_infected_paths = []
        self.quarantine_button.configure(state="disabled")
        text = "KARANTINO REZULTATAS\n" + ("\nPerkelta:\n" + "\n".join(moved) if moved else "\nNė vienas failas neperkeltas.")
        if errors:
            text += "\n\nNepavyko perkelti:\n" + "\n".join(errors)
        self._set_result(text, AMBER if errors else RED)

    def _restore_quarantined(self) -> None:
        try:
            manifest = Path.home() / ".local" / "share" / "sargon" / "quarantine" / "manifest.json"
            import json
            records = json.loads(manifest.read_text(encoding="utf-8"))
            if not records:
                messagebox.showinfo("SARGON karantinas", "Karantine nėra atkuriamų failų.")
                return
            original = records[-1]["original"]
        except FileNotFoundError:
            messagebox.showinfo("SARGON karantinas", "Karantine nėra atkuriamų failų.")
            return
        except Exception as exc:
            messagebox.showerror("SARGON karantinas", f"Karantino registro nepavyko perskaityti: {exc}")
            return
        if not messagebox.askyesno(
            "SARGON karantinas",
            f"Atkurti paskutinį karantinuotą failą į pradinę vietą?\n\n{original}\n\nDėmesio: šis failas anksčiau buvo aptiktas kaip galima grėsmė.",
        ):
            return
        try:
            restored = restore_latest()
            self._set_result(f"FAILAS ATKURTAS\n\n{restored}\n\nPrieš atidarydami dar kartą patikrinkite jį.", AMBER)
        except Exception as exc:
            messagebox.showerror("SARGON karantinas", f"Atkurti nepavyko: {exc}")

    def _find_update_unit(self) -> str | None:
        systemctl = shutil.which("systemctl")
        if not systemctl:
            return None
        try:
            listing = subprocess.run([systemctl, "list-unit-files", "--type=service", "--no-legend"],
                                     capture_output=True, text=True, timeout=8, check=False).stdout
        except (OSError, subprocess.TimeoutExpired):
            return None
        for candidate in ("clamav-freshclam.service", "freshclam.service"):
            if candidate in listing:
                return candidate
        return None

    def _auto_update_status(self) -> str:
        unit = self._find_update_unit()
        if not unit:
            return "Automatinio atnaujinimo tarnyba nerasta"
        try:
            result = subprocess.run(["systemctl", "is-active", unit], capture_output=True,
                                    text=True, timeout=4, check=False)
            return "Automatiniai bazės atnaujinimai: įjungti" if result.returncode == 0 else "Automatiniai bazės atnaujinimai: išjungti"
        except (OSError, subprocess.TimeoutExpired):
            return "Automatinio atnaujinimo būsena nenustatyta"

    def _enable_auto_updates(self) -> None:
        unit = self._find_update_unit()
        systemctl = shutil.which("systemctl")
        if not unit or not systemctl:
            messagebox.showerror("SARGON", "Šioje sistemoje nerasta ClamAV freshclam systemd tarnyba. Galite atnaujinti bazę rankiniu mygtuku.")
            return
        if os.geteuid() == 0:
            command = [systemctl, "enable", "--now", unit]
        elif shutil.which("pkexec"):
            command = [shutil.which("pkexec"), systemctl, "enable", "--now", unit]
        else:
            messagebox.showerror("SARGON", "Automatiniam atnaujinimui reikia administratoriaus teisių ir PolicyKit (pkexec).")
            return
        self._set_busy(True)
        self._set_result("Įjungiama ClamAV automatinio atnaujinimo tarnyba…", BLUE)

        def work() -> None:
            try:
                result = subprocess.run(command, capture_output=True, text=True, errors="replace", timeout=120, check=False)
                detail = (result.stdout + result.stderr).strip() or f"systemctl grąžino kodą {result.returncode}."
                state = "clean" if result.returncode == 0 else "error"
            except (OSError, subprocess.TimeoutExpired) as exc:
                detail, state = str(exc), "error"
            self.after(0, lambda: self._finish_auto_updates(state, detail))

        threading.Thread(target=work, daemon=True).start()

    def _finish_auto_updates(self, state: str, detail: str) -> None:
        self._set_busy(False)
        if state == "clean":
            self._set_result("AUTOMATINIAI ATNAUJINIMAI ĮJUNGTI\n\n" + detail, GREEN)
        else:
            self._set_result("AUTOMATINIŲ ATNAUJINIMŲ ĮJUNGTI NEPAVYKO\n\n" + detail, AMBER)
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
