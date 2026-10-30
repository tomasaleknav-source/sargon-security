package lt.sargon.android;

import android.content.Context;
import android.net.Uri;

import com.google.common.base.Charsets;
import com.google.common.hash.BloomFilter;
import com.google.common.hash.Funnels;

import org.bouncycastle.jce.provider.BouncyCastleProvider;
import org.bouncycastle.openpgp.PGPCompressedData;
import org.bouncycastle.openpgp.PGPObjectFactory;
import org.bouncycastle.openpgp.PGPPublicKey;
import org.bouncycastle.openpgp.PGPPublicKeyRingCollection;
import org.bouncycastle.openpgp.PGPSignature;
import org.bouncycastle.openpgp.PGPSignatureList;
import org.bouncycastle.openpgp.PGPUtil;
import org.bouncycastle.openpgp.operator.jcajce.JcaKeyFingerprintCalculator;
import org.bouncycastle.openpgp.operator.jcajce.JcaPGPContentVerifierBuilderProvider;

import java.io.BufferedInputStream;
import java.io.BufferedOutputStream;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.file.Files;
import java.nio.file.StandardCopyOption;
import java.security.MessageDigest;
import java.security.Security;
import java.util.Date;
import java.util.Locale;

public final class SignatureScanner {
    private static final String DATABASE_URL = "https://maintainteam.github.io/HypatiaDatabases/";
    private static final String DATABASE_NAME = "hypatia-sha256-bloom.bin";
    private static final String DATABASE_SIGNATURE = DATABASE_NAME + ".sig";
    private static final String KEY_NAME = "gpg.key";
    private static final String EXPECTED_KEY_FINGERPRINT = "B26EFC8E5B0C292AF882A1415298C0C0C3E73288";
    private static final long UPDATE_AFTER_MS = 24L * 60L * 60L * 1000L;
    private static final long MAX_DATABASE_BYTES = 128L * 1024L * 1024L;
    private static final Object LOCK = new Object();

    static {
        if (Security.getProvider("BC") == null) {
            Security.addProvider(new BouncyCastleProvider());
        }
    }

    private SignatureScanner() { }

    public static final class ScanResult {
        public final String sha256;
        public final boolean possibleMatch;
        public final boolean usedOldDatabase;
        public final long databaseUpdatedAt;

        ScanResult(String sha256, boolean possibleMatch, boolean usedOldDatabase, long databaseUpdatedAt) {
            this.sha256 = sha256;
            this.possibleMatch = possibleMatch;
            this.usedOldDatabase = usedOldDatabase;
            this.databaseUpdatedAt = databaseUpdatedAt;
        }
    }

    public static ScanResult scanSelectedFile(Context context, Uri uri) throws Exception {
        File directory = databaseDirectory(context);
        if (!directory.exists() && !directory.mkdirs()) {
            throw new IllegalStateException("Nepavyko paruošti parašų saugyklos.");
        }
        File database = new File(directory, DATABASE_NAME);
        File signature = new File(directory, DATABASE_SIGNATURE);
        File keyFile = new File(directory, KEY_NAME);

        synchronized (LOCK) {
            boolean databaseWasOld = isOld(database);
            if (!database.isFile() || !signature.isFile() || !keyFile.isFile() || databaseWasOld) {
                try {
                    updateDatabaseLocked(directory);
                    databaseWasOld = false;
                } catch (Exception updateFailure) {
                    if (!isVerified(database, signature, keyFile)) {
                        throw updateFailure;
                    }
                    databaseWasOld = isOld(database);
                }
            } else if (!isVerified(database, signature, keyFile)) {
                updateDatabaseLocked(directory);
                databaseWasOld = false;
            }

            if (!isVerified(database, signature, keyFile)) {
                throw new IllegalStateException("Nepavyko patvirtinti virusų duomenų bazės parašo.");
            }

            String hash;
            try (InputStream input = context.getContentResolver().openInputStream(uri)) {
                if (input == null) throw new IllegalStateException("Nepavyko atverti pasirinkto failo.");
                hash = sha256(input);
            }

            BloomFilter<String> filter;
            try (InputStream input = new BufferedInputStream(new FileInputStream(database))) {
                filter = BloomFilter.readFrom(input, Funnels.stringFunnel(Charsets.US_ASCII));
            }
            return new ScanResult(hash, filter.mightContain(hash), databaseWasOld, database.lastModified());
        }
    }

    public static String updateDatabase(Context context) throws Exception {
        File directory = databaseDirectory(context);
        if (!directory.exists() && !directory.mkdirs()) {
            throw new IllegalStateException("Nepavyko paruošti parašų saugyklos.");
        }
        synchronized (LOCK) {
            updateDatabaseLocked(directory);
            File database = new File(directory, DATABASE_NAME);
            if (!isVerified(database, new File(directory, DATABASE_SIGNATURE), new File(directory, KEY_NAME))) {
                throw new IllegalStateException("Atsisiųstos duomenų bazės parašas neteisingas.");
            }
            return "Duomenų bazė atnaujinta: " + new Date(database.lastModified());
        }
    }

    private static File databaseDirectory(Context context) {
        return new File(context.getFilesDir(), "sargon-signatures");
    }

    private static boolean isOld(File database) {
        return !database.isFile() || System.currentTimeMillis() - database.lastModified() > UPDATE_AFTER_MS;
    }

    private static void updateDatabaseLocked(File directory) throws Exception {
        File keyTemp = new File(directory, KEY_NAME + ".new");
        File databaseTemp = new File(directory, DATABASE_NAME + ".new");
        File signatureTemp = new File(directory, DATABASE_SIGNATURE + ".new");
        try {
            download(KEY_NAME, keyTemp, 128L * 1024L);
            loadTrustedKey(keyTemp);
            download(DATABASE_NAME, databaseTemp, MAX_DATABASE_BYTES);
            download(DATABASE_SIGNATURE, signatureTemp, 128L * 1024L);
            if (!verify(databaseTemp, signatureTemp, keyTemp)) {
                throw new SecurityException("Duomenų bazės parašas nepatvirtintas.");
            }
            try (InputStream input = new BufferedInputStream(new FileInputStream(databaseTemp))) {
                BloomFilter.readFrom(input, Funnels.stringFunnel(Charsets.US_ASCII));
            }
            replace(keyTemp, new File(directory, KEY_NAME));
            replace(signatureTemp, new File(directory, DATABASE_SIGNATURE));
            replace(databaseTemp, new File(directory, DATABASE_NAME));
        } finally {
            keyTemp.delete();
            databaseTemp.delete();
            signatureTemp.delete();
        }
    }

    private static void download(String name, File output, long maximumBytes) throws Exception {
        HttpURLConnection connection = (HttpURLConnection) new URL(DATABASE_URL + name).openConnection();
        connection.setConnectTimeout(20000);
        connection.setReadTimeout(90000);
        connection.setInstanceFollowRedirects(true);
        connection.setRequestProperty("User-Agent", "SARGON-Android/0.53.0");
        try {
            int status = connection.getResponseCode();
            if (status < 200 || status >= 300) throw new IllegalStateException("Duomenų bazės serveris grąžino klaidą " + status + ".");
            long length = connection.getContentLengthLong();
            if (length > maximumBytes) throw new IllegalStateException("Duomenų bazės failas viršija leidžiamą dydį.");
            try (InputStream input = new BufferedInputStream(connection.getInputStream());
                 BufferedOutputStream outputStream = new BufferedOutputStream(new FileOutputStream(output))) {
                byte[] buffer = new byte[65536];
                long total = 0;
                int read;
                while ((read = input.read(buffer)) != -1) {
                    total += read;
                    if (total > maximumBytes) throw new IllegalStateException("Atsisiųstas failas viršija leidžiamą dydį.");
                    outputStream.write(buffer, 0, read);
                }
                if (total == 0) throw new IllegalStateException("Serveris grąžino tuščią failą.");
            }
        } finally {
            connection.disconnect();
        }
    }

    private static void replace(File source, File target) throws Exception {
        Files.move(source.toPath(), target.toPath(), StandardCopyOption.REPLACE_EXISTING);
    }

    private static boolean isVerified(File database, File signature, File keyFile) {
        if (!database.isFile() || !signature.isFile() || !keyFile.isFile()) return false;
        try {
            return verify(database, signature, keyFile);
        } catch (Exception ignored) {
            return false;
        }
    }

    private static boolean verify(File database, File signatureFile, File keyFile) throws Exception {
        PGPPublicKey key = loadTrustedKey(keyFile);
        try (InputStream signatureInput = new BufferedInputStream(new FileInputStream(signatureFile))) {
            InputStream decoded = PGPUtil.getDecoderStream(signatureInput);
            PGPObjectFactory factory = new PGPObjectFactory(decoded, new JcaKeyFingerprintCalculator());
            Object object = factory.nextObject();
            if (object instanceof PGPCompressedData) {
                factory = new PGPObjectFactory(((PGPCompressedData) object).getDataStream(), new JcaKeyFingerprintCalculator());
                object = factory.nextObject();
            }
            if (!(object instanceof PGPSignatureList)) throw new SecurityException("Nesuprantamas duomenų bazės parašas.");
            PGPSignatureList signatures = (PGPSignatureList) object;
            if (signatures.size() == 0) throw new SecurityException("Duomenų bazės parašo sąrašas tuščias.");
            PGPSignature pgpSignature = signatures.get(0);
            if (pgpSignature.getKeyID() != key.getKeyID()) throw new SecurityException("Duomenų bazės parašą pateikė nepatikimas raktas.");
            pgpSignature.init(new JcaPGPContentVerifierBuilderProvider().setProvider("BC"), key);
            try (InputStream databaseInput = new BufferedInputStream(new FileInputStream(database))) {
                byte[] buffer = new byte[65536];
                int read;
                while ((read = databaseInput.read(buffer)) != -1) pgpSignature.update(buffer, 0, read);
            }
            return pgpSignature.verify();
        }
    }

    private static PGPPublicKey loadTrustedKey(File keyFile) throws Exception {
        try (InputStream keyInput = PGPUtil.getDecoderStream(new BufferedInputStream(new FileInputStream(keyFile)))) {
            PGPPublicKeyRingCollection keys = new PGPPublicKeyRingCollection(keyInput, new JcaKeyFingerprintCalculator());
            PGPPublicKey key = keys.getPublicKey(0x5298C0C0C3E73288L);
            if (key == null || !EXPECTED_KEY_FINGERPRINT.equals(toHex(key.getFingerprint()))) {
                throw new SecurityException("Duomenų bazės leidėjo raktas neatitinka SARGON patikimo rakto.");
            }
            return key;
        }
    }

    private static String toHex(byte[] bytes) {
        StringBuilder out = new StringBuilder(bytes.length * 2);
        for (byte b : bytes) out.append(String.format(Locale.US, "%02X", b & 0xff));
        return out.toString();
    }

    static String sha256(InputStream input) throws Exception {
        MessageDigest digest = MessageDigest.getInstance("SHA-256");
        byte[] buffer = new byte[65536];
        int read;
        while ((read = input.read(buffer)) != -1) digest.update(buffer, 0, read);
        StringBuilder out = new StringBuilder(64);
        for (byte b : digest.digest()) out.append(String.format(Locale.US, "%02x", b & 0xff));
        return out.toString();
    }
}
