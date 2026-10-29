package lt.sargon.android;

import android.app.Activity;
import android.app.KeyguardManager;
import android.content.Intent;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.provider.Settings;
import android.database.Cursor;
import android.provider.OpenableColumns;
import android.view.Gravity;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.ScrollView;
import android.widget.TextView;
import android.graphics.Color;
import android.graphics.Typeface;
import android.text.InputType;

import java.io.InputStream;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

public final class MainActivity extends Activity {
    private static final int PICK_FILE = 520;
    private final ExecutorService worker = Executors.newSingleThreadExecutor();
    private TextView result;
    private EditText expectedHash;

    @Override public void onCreate(Bundle state) {
        super.onCreate(state);
        getWindow().setStatusBarColor(Color.rgb(11, 21, 36));
        getWindow().setNavigationBarColor(Color.rgb(11, 21, 36));
        getWindow().getDecorView().setSystemUiVisibility(0);

        int pad = dp(20);
        LinearLayout page = new LinearLayout(this);
        page.setOrientation(LinearLayout.VERTICAL);
        page.setPadding(pad, pad, pad, pad);
        page.setBackgroundColor(Color.rgb(11, 21, 36));
        ScrollView scroll = new ScrollView(this);
        scroll.setFillViewport(true);
        scroll.addView(page);

        TextView title = label("◈  SARGON Security", 27, true, Color.rgb(104, 221, 195));
        page.addView(title);
        page.addView(label("Android leidimas • 0.52-android", 16, true, Color.WHITE));
        page.addView(label("Telefono saugos pagalbininkas", 15, false, 0xffdce8f2));

        section(page, "Telefono būklė");
        KeyguardManager keyguard = (KeyguardManager) getSystemService(KEYGUARD_SERVICE);
        boolean secure = keyguard != null && keyguard.isDeviceSecure();
        String patch = Build.VERSION.SECURITY_PATCH == null || Build.VERSION.SECURITY_PATCH.isEmpty()
                ? "nenurodyta" : Build.VERSION.SECURITY_PATCH;
        page.addView(label("Android: " + Build.VERSION.RELEASE + " (API " + Build.VERSION.SDK_INT + ")", 16, false, Color.WHITE));
        page.addView(label("Saugos naujinimo data: " + patch, 16, false, Color.WHITE));
        page.addView(label("Ekrano užraktas: " + (secure ? "įjungtas" : "neįjungtas"), 16, false,
                secure ? 0xff68ddc3 : 0xffffc46b));
        page.addView(label("„Play Protect“ būseną patikrinkite „Google Play“ programoje. Ši programėlė jos pati nenuskaito.", 14, false, 0xffc8d5e2));
        page.addView(button("Atverti telefono saugos nustatymus", v -> openSecuritySettings()));

        section(page, "Failo kontrolinis kodas");
        page.addView(label("Pasirinkite failą ir apskaičiuokite jo SHA-256 kodą. Failo turinys lieka telefone. Tai nėra virusų patikra.", 14, false, 0xffc8d5e2));
        expectedHash = new EditText(this);
        expectedHash.setSingleLine(true);
        expectedHash.setHint("Tikėtinas SHA-256 kodas (nebūtina)");
        expectedHash.setInputType(InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_FLAG_NO_SUGGESTIONS);
        expectedHash.setTextColor(Color.WHITE);
        expectedHash.setHintTextColor(0xffaebdca);
        expectedHash.setPadding(dp(12), dp(8), dp(12), dp(8));
        expectedHash.setBackgroundColor(0xff172438);
        page.addView(expectedHash, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, dp(52)));
        page.addView(button("Pasirinkti failą", v -> chooseFile()));
        result = label("Failas dar nepasirinktas.", 14, false, Color.WHITE);
        result.setPadding(dp(12), dp(12), dp(12), dp(12));
        result.setBackgroundColor(0xff172438);
        page.addView(result);

        section(page, "Svarbu");
        page.addView(label("SARGON Android nepakeičia „Google Play Protect“ ir neskenuoja viso telefono. Nenaudokite SHA-256 kodo kaip įrodymo, kad failas saugus, nebent lyginate jį su patikimu leidėjo šaltiniu.", 14, false, 0xffffc46b));
        setContentView(scroll);
    }

    private void chooseFile() {
        Intent intent = new Intent(Intent.ACTION_OPEN_DOCUMENT);
        intent.addCategory(Intent.CATEGORY_OPENABLE);
        intent.setType("*/*");
        startActivityForResult(intent, PICK_FILE);
    }

    @Override protected void onActivityResult(int request, int resultCode, Intent data) {
        super.onActivityResult(request, resultCode, data);
        if (request != PICK_FILE || resultCode != RESULT_OK || data == null || data.getData() == null) return;
        Uri uri = data.getData();
        String name = displayName(uri);
        result.setText("Skaičiuojama: " + name + "…");
        worker.execute(() -> {
            String message;
            try {
                InputStream stream = getContentResolver().openInputStream(uri);
                String hash = FileFingerprint.sha256(stream);
                String expected = expectedHash.getText().toString();
                if (expected.trim().isEmpty()) {
                    message = "Failas: " + name + "\nSHA-256: " + hash + "\n\nTai kontrolinis kodas, ne virusų patikros rezultatas.";
                } else if (FileFingerprint.matches(expected, hash)) {
                    message = "Failas: " + name + "\nSHA-256 sutampa su įvestu kodu.\n\nTai patvirtina tik kodo sutapimą, ne failo saugumą.";
                } else {
                    message = "Failas: " + name + "\nSHA-256 nesutampa su įvestu kodu.\n\nNeatidarykite failo, kol nepatikrinote kodo patikimame šaltinyje.";
                }
            } catch (Exception ex) {
                message = "Failo kodo apskaičiuoti nepavyko. Failas nebuvo patikrintas.";
            }
            final String text = message;
            runOnUiThread(() -> result.setText(text));
        });
    }

    private String displayName(Uri uri) {
        try (Cursor cursor = getContentResolver().query(uri, null, null, null, null)) {
            if (cursor != null && cursor.moveToFirst()) {
                int column = cursor.getColumnIndex(OpenableColumns.DISPLAY_NAME);
                if (column >= 0) return cursor.getString(column);
            }
        } catch (Exception ignored) { }
        return "pasirinktas failas";
    }

    private void openSecuritySettings() {
        try { startActivity(new Intent(Settings.ACTION_SECURITY_SETTINGS)); }
        catch (Exception ex) { startActivity(new Intent(Settings.ACTION_SETTINGS)); }
    }

    private void section(LinearLayout page, String text) {
        TextView heading = label(text, 20, true, Color.rgb(104, 221, 195));
        heading.setPadding(0, dp(22), 0, dp(8));
        page.addView(heading);
    }

    private TextView label(String text, int size, boolean bold, int color) {
        TextView view = new TextView(this);
        view.setText(text);
        view.setTextSize(size);
        view.setTextColor(color);
        if (bold) view.setTypeface(Typeface.DEFAULT, Typeface.BOLD);
        view.setPadding(0, dp(5), 0, dp(5));
        return view;
    }

    private Button button(String text, android.view.View.OnClickListener click) {
        Button button = new Button(this);
        button.setText(text);
        button.setAllCaps(false);
        button.setTextColor(Color.WHITE);
        button.setBackgroundTintList(android.content.res.ColorStateList.valueOf(0xff12618a));
        button.setOnClickListener(click);
        LinearLayout.LayoutParams lp = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, dp(52));
        lp.topMargin = dp(8);
        lp.bottomMargin = dp(4);
        button.setLayoutParams(lp);
        return button;
    }

    private int dp(int value) { return (int) (value * getResources().getDisplayMetrics().density + 0.5f); }

    @Override protected void onDestroy() {
        worker.shutdownNow();
        super.onDestroy();
    }
}
