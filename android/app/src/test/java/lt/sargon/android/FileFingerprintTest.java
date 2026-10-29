package lt.sargon.android;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertFalse;
import static org.junit.Assert.assertTrue;

import java.io.ByteArrayInputStream;
import org.junit.Test;

public class FileFingerprintTest {
    @Test public void computesKnownSha256() throws Exception {
        String hash = FileFingerprint.sha256(new ByteArrayInputStream("abc".getBytes("UTF-8")));
        assertEquals("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", hash);
    }
    @Test public void comparesOnlyWellFormedExpectedHashes() {
        String expected = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";
        assertTrue(FileFingerprint.matches("  " + expected.toUpperCase() + "  ", expected));
        assertFalse(FileFingerprint.matches("not-a-hash", expected));
    }
}
