package lt.sargon.android;

import android.app.Activity;
import android.app.KeyguardManager;
import android.content.Intent;
import android.content.res.ColorStateList;
import android.graphics.Color;
import android.graphics.Typeface;
import android.graphics.drawable.GradientDrawable;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.provider.Settings;
import android.database.Cursor;
import android.provider.OpenableColumns;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.ScrollView;
import android.widget.TextView;

import java.io.InputStream;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

public final class MainActivity extends Activity {
    private static final int PICK_FILE = 520;
    private static final int BG = 0xff081421;
    private static final int CARD = 0xff112236;
    private static final int CARD_LIGHT = 0xff172c42;
    private static final int TEAL = 0xff53e0c2;
    private static final int MUTED = 0xffaebdcd;
    private static final int WHITE = 0xfff4f8fc;
    private static final int BLUE = 0xff1674a4;
    private final ExecutorService worker = Executors.newSingleThreadExecutor();
    private TextView result;
    private EditText expectedHash;

    @Override public void onCreate(Bundle state) {
        super.onCreate(state);
        getWindow().setStatusBarColor(BG);
        getWindow().setNavigationBarColor(BG);
        getWindow().getDecorView().setSystemUiVisibility(0);

        LinearLayout page = new LinearLayout(this);
        page.setOrientation(LinearLayout.VERTICAL);
        page.setPadding(dp(20), dp(18), dp(20), dp(28));
        page.setBackgroundColor(BG);

        ScrollView scroll = new ScrollView(this);
        scroll.setFillViewport(true);
        scroll.setBackgroundColor(BG);
        scroll.addView(page);

        LinearLayout brandRow = new LinearLayout(this);
        brandRow.setGravity(Gravity.CENTER_VERTICAL);
        TextView mark = label("S", 20, true, BG);
        mark.setGravity(Gravity.CENTER);
        mark.setBackground(round(TEAL, 16));
        brandRow.addView(mark, new LinearLayout.LayoutParams(dp(42), dp(42)));
        LinearLayout brandText = new LinearLayout(this);
        brandText.setOrientation(LinearLayout.VERTICAL);
        brandText.setPadding(dp(11), 0, 0, 0);
        brandText.addView(label("SARGON", 17, true, WHITE));
        brandText.addView(label("SECURITY", 10, true, TEAL));
        brandRow.addView(brandText, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1));
        TextView preview = label("ANDROID • 0.52", 10, true, TEAL);
        preview.setGravity(Gravity.CENTER);
        preview.setPadding(dp(10), dp(8), dp(10), dp(8));
        preview.setBackground(round(0xff123a3e, 18));
        brandRow.addView(preview);
        page.addView(brandRow);

        TextView heading = label("Apsaugos skydelis", 28, true, WHITE);
        heading.setPadding(0, dp(25), 0, dp(3));
        page.addView(heading);
        page.addView(label("Telefono saugos informacija ir failo kontrolė", 14, false, MUTED));

        LinearLayout hero = card();
        hero.setOrientation(LinearLayout.HORIZONTAL);
        hero.setGravity(Gravity.CENTER_VERTICAL);
        hero.setPadding(dp(16), dp(16), dp(16), dp(16));
        hero.setBackground(round(0xff12334a, 18));
        TextView shield = label("✓", 27, true, BG);
        shield.setGravity(Gravity.CENTER);
        shield.setBackground(round(TEAL, 40));
        hero.addView(shield, new LinearLayout.LayoutParams(dp(54), dp(54)));
        LinearLayout heroText = new LinearLayout(this);
        heroText.setOrientation(LinearLayout.VERTICAL);
        heroText.setPadding(dp(14), 0, 0, 0);
        heroText.addView(label("Pagalbininkas paruoštas", 17, true, WHITE));
        TextView disclaimer = label("Tai nėra viso telefono antivirusinė patikra.", 12, false, 0xffc2d3df);
        heroText.addView(disclaimer);
        hero.addView(heroText, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1));
        LinearLayout.LayoutParams heroLp = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT);
        heroLp.topMargin = dp(20);
        page.addView(hero, heroLp);

        section(page, "Telefono būklė", "ANDROID SISTEMA");
        LinearLayout statusCard = card();
        statusCard.setOrientation(LinearLayout.VERTICAL);
        KeyguardManager keyguard = (KeyguardManager) getSystemService(KEYGUARD_SERVICE);
        boolean secure = keyguard != null && keyguard.isDeviceSecure();
        String patch = Build.VERSION.SECURITY_PATCH == null || Build.VERSION.SECURITY_PATCH.isEmpty()
                ? "Nenurodyta" : Build.VERSION.SECURITY_PATCH;
        addInfoRow(statusCard, "Android versija", Build.VERSION.RELEASE + "  •  API " + Build.VERSION.SDK_INT, TEAL);
        addDivider(statusCard);
        addInfoRow(statusCard, "Saugos pataisos data", patch, WHITE);
        addDivider(statusCard);
        addInfoRow(statusCard, "Ekrano užraktas", secure ? "Įjungtas" : "Neįjungtas",
                secure ? TEAL : 0xffffbd66);
        page.addView(statusCard);

        LinearLayout playCard = card();
        playCard.setOrientation(LinearLayout.VERTICAL);
        TextView playTitle = label("Google Play Protect", 17, true, WHITE);
        playCard.addView(playTitle);
        TextView playDesc = label("SARGON negali nuskaityti „Play Protect“ būsenos. Patikrinkite ją „Google Play“ programoje.", 13, false, MUTED);
        playDesc.setPadding(0, dp(5), 0, dp(8));
        playCard.addView(playDesc);
        page.addView(playCard, topMargin(dp(12)));
        page.addView(actionButton("Atverti Android saugos nustatymus", BLUE, v -> openSecuritySettings()), topMargin(dp(10)));

        section(page, "Failo kontrolė", "SHA-256");
        LinearLayout fileCard = card();
        fileCard.setOrientation(LinearLayout.VERTICAL);
        fileCard.addView(label("Pasirinkite vieną failą ir apskaičiuokite jo kontrolinį kodą.", 13, false, MUTED));
        expectedHash = new EditText(this);
        expectedHash.setSingleLine(true);
        expectedHash.setHint("Palyginimo kodas (nebūtina)");
        expectedHash.setTextSize(13);
        expectedHash.setInputType(android.text.InputType.TYPE_CLASS_TEXT | android.text.InputType.TYPE_TEXT_FLAG_NO_SUGGESTIONS);
        expectedHash.setTextColor(WHITE);
        expectedHash.setHintTextColor(0xff94a7b9);
        expectedHash.setPadding(dp(12), 0, dp(12), 0);
        expectedHash.setBackground(round(CARD_LIGHT, 10));
        LinearLayout.LayoutParams inputLp = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, dp(50));
        inputLp.topMargin = dp(12);
        fileCard.addView(expectedHash, inputLp);
        result = label("Pasirinktas failas ir jo SHA-256 kodas bus rodomi čia.", 12, false, 0xffd5e1ea);
        result.setTypeface(Typeface.MONOSPACE);
        result.setPadding(dp(12), dp(12), dp(12), dp(12));
        result.setBackground(round(0xff0b1928, 10));
        fileCard.addView(result, topMargin(dp(10)));
        page.addView(fileCard);
        page.addView(actionButton("Pasirinkti failą", TEAL, v -> chooseFile()), topMargin(dp(10)));

        TextView note = label("Failas lieka telefone. SHA-256 kodas nėra virusų aptikimas; jis padeda tik palyginti failą su patikimo šaltinio kodu.", 12, false, 0xffffc46b);
        note.setPadding(dp(2), dp(13), dp(2), 0);
        page.addView(note);

        TextView footer = label("SARGON Android • papildomas saugos įrankis", 11, false, 0xff71869a);
        footer.setGravity(Gravity.CENTER);
        footer.setPadding(0, dp(24), 0, 0);
        page.addView(footer);
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
        String expected = expectedHash.getText().toString();
        result.setText("Skaičiuojamas SHA-256…\n" + name);
        worker.execute(() -> {
            String message;
            try {
                InputStream stream = getContentResolver().openInputStream(uri);
                String hash = FileFingerprint.sha256(stream);
                if (expected.trim().isEmpty()) {
                    message = "FAILAS: " + name + "\n\nSHA-256:\n" + hash + "\n\nTai nėra virusų patikros rezultatas.";
                } else if (FileFingerprint.matches(expected, hash)) {
                    message = "FAILAS: " + name + "\n\nSHA-256 SUTAMPA\n" + hash + "\n\nTai nepatvirtina failo saugumo.";
                } else {
                    message = "FAILAS: " + name + "\n\nKODAI NESUTAMPA\n" + hash + "\n\nPatikrinkite kodą patikimame šaltinyje.";
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
        return "Pasirinktas failas";
    }

    private void openSecuritySettings() {
        try { startActivity(new Intent(Settings.ACTION_SECURITY_SETTINGS)); }
        catch (Exception ex) { startActivity(new Intent(Settings.ACTION_SETTINGS)); }
    }

    private void section(LinearLayout page, String title, String eyebrow) {
        LinearLayout row = new LinearLayout(this);
        row.setGravity(Gravity.BOTTOM);
        row.setPadding(0, dp(23), 0, dp(9));
        TextView h = label(title, 19, true, WHITE);
        row.addView(h, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1));
        TextView e = label(eyebrow, 9, true, TEAL);
        e.setGravity(Gravity.CENTER);
        row.addView(e);
        page.addView(row);
    }

    private LinearLayout card() {
        LinearLayout box = new LinearLayout(this);
        box.setOrientation(LinearLayout.VERTICAL);
        box.setPadding(dp(15), dp(14), dp(15), dp(14));
        box.setBackground(round(CARD, 15));
        box.setElevation(dp(2));
        return box;
    }

    private void addInfoRow(LinearLayout parent, String name, String value, int valueColor) {
        LinearLayout row = new LinearLayout(this);
        row.setGravity(Gravity.CENTER_VERTICAL);
        TextView left = label(name, 13, false, MUTED);
        TextView right = label(value, 13, true, valueColor);
        right.setGravity(Gravity.RIGHT | Gravity.CENTER_VERTICAL);
        row.addView(left, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1));
        row.addView(right);
        parent.addView(row);
    }

    private void addDivider(LinearLayout parent) {
        View line = new View(this);
        line.setBackgroundColor(0xff263b50);
        LinearLayout.LayoutParams lp = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, dp(1));
        lp.topMargin = dp(7);
        lp.bottomMargin = dp(7);
        parent.addView(line, lp);
    }

    private TextView label(String text, int size, boolean bold, int color) {
        TextView view = new TextView(this);
        view.setText(text);
        view.setTextSize(size);
        view.setTextColor(color);
        if (bold) view.setTypeface(Typeface.DEFAULT, Typeface.BOLD);
        return view;
    }

    private TextView actionButton(String text, int color, View.OnClickListener click) {
        TextView button = label(text, 14, true, BG);
        button.setGravity(Gravity.CENTER);
        button.setPadding(dp(14), dp(7), dp(14), dp(7));
        button.setMinHeight(dp(50));
        button.setBackground(round(color, 12));
        button.setOnClickListener(click);
        return button;
    }

    private GradientDrawable round(int color, int radius) {
        GradientDrawable bg = new GradientDrawable();
        bg.setColor(color);
        bg.setCornerRadius(dp(radius));
        return bg;
    }

    private LinearLayout.LayoutParams topMargin(int margin) {
        LinearLayout.LayoutParams lp = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT);
        lp.topMargin = margin;
        return lp;
    }

    private int dp(int value) { return (int) (value * getResources().getDisplayMetrics().density + 0.5f); }

    @Override protected void onDestroy() {
        worker.shutdownNow();
        super.onDestroy();
    }
}
