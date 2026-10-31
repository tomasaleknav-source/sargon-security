import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from scanner import classify_scan, infected_file_paths, quarantine_file, restore_latest


class ScanResultTests(unittest.TestCase):
    def test_zero_means_no_detection(self):
        self.assertEqual(classify_scan(0, "/tmp/file: OK"), "clean")

    def test_one_means_possible_detection(self):
        self.assertEqual(classify_scan(1, "/tmp/file: Eicar-Test-Signature FOUND"), "threat")

    def test_found_marker_is_not_clean_even_with_unexpected_exit(self):
        self.assertEqual(classify_scan(2, "/tmp/file: Malware FOUND"), "threat")

    def test_other_exit_code_means_incomplete(self):
        self.assertEqual(classify_scan(2, "Permission denied"), "error")

    def test_detected_paths_are_parsed(self):
        parsed = infected_file_paths("/tmp/a file: Eicar-Test FOUND\n/tmp/clean: OK")
        self.assertEqual(parsed, [Path("/tmp/a file")])


class QuarantineTests(unittest.TestCase):
    def test_quarantine_and_restore_last_file(self):
        with TemporaryDirectory() as temporary:
            root = Path(temporary)
            source = root / "suspected.bin"
            quarantine = root / "quarantine"
            source.write_bytes(b"sample")
            source.chmod(0o640)

            stored = quarantine_file(source, quarantine)

            self.assertFalse(source.exists())
            self.assertEqual(stored.read_bytes(), b"sample")
            self.assertEqual(stored.stat().st_mode & 0o777, 0o600)
            restored = restore_latest(quarantine)

            self.assertEqual(restored, source)
            self.assertEqual(source.read_bytes(), b"sample")
            self.assertEqual(source.stat().st_mode & 0o777, 0o640)
            self.assertFalse(stored.exists())

    def test_restore_does_not_overwrite_existing_file(self):
        with TemporaryDirectory() as temporary:
            root = Path(temporary)
            source = root / "suspected.bin"
            quarantine = root / "quarantine"
            source.write_bytes(b"sample")
            quarantine_file(source, quarantine)
            source.write_bytes(b"keep this")

            with self.assertRaises(FileExistsError):
                restore_latest(quarantine)
            self.assertEqual(source.read_bytes(), b"keep this")


if __name__ == "__main__":
    unittest.main()
