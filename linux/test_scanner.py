import unittest

from scanner import classify_scan


class ScanResultTests(unittest.TestCase):
    def test_zero_means_no_detection(self):
        self.assertEqual(classify_scan(0, "/tmp/file: OK"), "clean")

    def test_one_means_possible_detection(self):
        self.assertEqual(classify_scan(1, "/tmp/file: Eicar-Test-Signature FOUND"), "threat")

    def test_found_marker_is_not_clean_even_with_unexpected_exit(self):
        self.assertEqual(classify_scan(2, "/tmp/file: Malware FOUND"), "threat")

    def test_other_exit_code_means_incomplete(self):
        self.assertEqual(classify_scan(2, "Permission denied"), "error")


if __name__ == "__main__":
    unittest.main()
