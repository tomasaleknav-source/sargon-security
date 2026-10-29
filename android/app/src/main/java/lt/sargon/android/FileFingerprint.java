package lt.sargon.android;

import java.io.IOException;
import java.io.InputStream;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;

/** Computes a file fingerprint only; it does not detect malware. */
public final class FileFingerprint {
    private FileFingerprint() { }

    public static String sha256(InputStream input) throws IOException {
        if (input == null) throw new IOException("Nepavyko atverti pasirinkto failo.");
        try (InputStream stream = input) {
            final MessageDigest digest;
            try {
                digest = MessageDigest.getInstance("SHA-256");
            } catch (NoSuchAlgorithmException impossible) {
                throw new IllegalStateException("SHA-256 šiame telefone nepasiekiamas.", impossible);
            }
            byte[] buffer = new byte[64 * 1024];
            int count;
            while ((count = stream.read(buffer)) != -1) {
                if (count > 0) digest.update(buffer, 0, count);
            }
            byte[] bytes = digest.digest();
            StringBuilder out = new StringBuilder(bytes.length * 2);
            for (byte value : bytes) out.append(String.format(java.util.Locale.ROOT, "%02x", value & 0xff));
            return out.toString();
        }
    }

    public static boolean matches(String expected, String actual) {
        if (expected == null || actual == null) return false;
        String normalized = expected.replaceAll("\\s", "").toLowerCase(java.util.Locale.ROOT);
        return normalized.matches("[0-9a-f]{64}") && normalized.equals(actual.toLowerCase(java.util.Locale.ROOT));
    }
}
