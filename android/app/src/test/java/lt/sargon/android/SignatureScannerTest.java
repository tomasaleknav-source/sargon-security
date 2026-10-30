package lt.sargon.android;

import static org.junit.Assert.assertEquals;

import java.io.ByteArrayInputStream;
import org.junit.Test;

public class SignatureScannerTest {
    @Test public void computesSha256ForSelectedFileBytes() throws Exception {
        String hash = SignatureScanner.sha256(new ByteArrayInputStream("abc".getBytes("UTF-8")));
        assertEquals("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", hash);
    }
}
