package lt.sargon.android;

import android.app.Activity;
import android.app.KeyguardManager;
import android.content.Intent;
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
    private static final int BG = 0xff07111f;
    private static final int CARD = 0xff101f32;
    private static final int CARD2 = 0xff142942;
    private static final int BLUE = 0xff55b7ff;
    private static final int WHITE = 0xfff5f8fc;
    private static final int MUTED = 0xffaab9c9;
    private final ExecutorService worker = Executors.newSingleThreadExecutor();
    private TextView result;
    private ShieldBadge shieldBadge;
    private EditText expectedHash;

    @Override public void onCreate(Bundle state) {
        super.onCreate(state);
        getWindow().setStatusBarColor(BG);
        getWindow().setNavigationBarColor(BG);
        getWindow().getDecorView().setSystemUiVisibility(0);

        LinearLayout page = new LinearLayout(this);
        page.setOrientation(LinearLayout.VERTICAL);
        page.setPadding(dp(18), dp(14), dp(18), dp(30));
        page.setBackgroundColor(BG);

        ScrollView scroll = new ScrollView(this);
        scroll.setFillViewport(true);
        scroll.addView(page);

        buildTopBar(page);
        TextView title = label("Apsaugos centras", 28, true, WHITE);
        title.setPadding(0, dp(23), 0, dp(3));
        page.addView(title);
        page.addView(label("Telefono būklė ir papildomi įrankiai", 14, false, MUTED));

        buildProtectionCard(page);
        buildDeviceStatus(page);
        buildFileTools(page);
        TextView footer = label("SARGON Android 0.52.5 PREVIEW  •  Pagalbinė programėlė", 11, false, 0xff71849a);
        footer.setGravity(Gravity.CENTER);
        footer.setPadding(0, dp(24), 0, 0);
        page.addView(footer);
        setContentView(scroll);
    }

    private void buildTopBar(LinearLayout page) {
        LinearLayout row = new LinearLayout(this);
        row.setGravity(Gravity.CENTER_VERTICAL);
        TextView logo = label("S", 20, true, BG);
        logo.setGravity(Gravity.CENTER);
        logo.setBackground(round(BLUE, 14));
        row.addView(logo, new LinearLayout.LayoutParams(dp(42), dp(42)));
        LinearLayout name = new LinearLayout(this);
        name.setOrientation(LinearLayout.VERTICAL);
        name.setPadding(dp(10), 0, 0, 0);
        name.addView(label("SARGON", 17, true, WHITE));
        name.addView(label("SECURITY", 10, true, BLUE));
        row.addView(name, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1));
        TextView tag = label("PREVIEW 0.52.5", 10, true, BLUE);
        tag.setGravity(Gravity.CENTER);
        tag.setPadding(dp(11), dp(8), dp(11), dp(8));
        tag.setBackground(round(0xff123653, 20));
        row.addView(tag);
        page.addView(row);
    }

    private void buildProtectionCard(LinearLayout page) {
        LinearLayout card = new LinearLayout(this);
        card.setOrientation(LinearLayout.VERTICAL);
        card.setPadding(dp(18), dp(18), dp(18), dp(18));
        card.setBackground(gradient(0xff1b65a0, 0xff102b49, 20));

        LinearLayout top = new LinearLayout(this);
        top.setGravity(Gravity.CENTER_VERTICAL);
        shieldBadge = new ShieldBadge();
        ShieldBadge badge = shieldBadge;
        top.addView(badge, new LinearLayout.LayoutParams(dp(64), dp(72)));
        LinearLayout copy = new LinearLayout(this);
        copy.setOrientation(LinearLayout.VERTICAL);
        copy.setPadding(dp(14), 0, 0, 0);
        copy.addView(label("Telefono apsauga", 19, true, WHITE));
        copy.addView(label("Patikrinkite Android saugos įrankius", 12, false, 0xffc6d7e3));
        top.addView(copy, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1));
        card.addView(top);

        TextView pill = label("BŪSENA NEPATVIRTINTA", 11, true, 0xffffca74);
        pill.setPadding(dp(10), dp(7), dp(10), dp(7));
        pill.setBackground(round(0xff3a3020, 18));
        card.addView(pill, topMargin(dp(14)));

        TextView details = label("Ši programėlė negali nuskenuoti viso telefono. Pagrindinę apsaugą tikrinkite per Android ir Google Play Protect.", 13, false, 0xffd2e0e9);
        details.setLineSpacing(dp(2), 1f);
        card.addView(details, topMargin(dp(10)));

        TextView action = button("ATVERTI SAUGOS NUSTATYMUS", BLUE, BG, v -> openSecuritySettings());
        card.addView(action, topMargin(dp(15)));
        page.addView(card, topMargin(dp(20)));
    }

    private void buildDeviceStatus(LinearLayout page) {
        section(page, "Įrenginio būklė", "SISTEMOS DUOMENYS");
        LinearLayout grid = new LinearLayout(this);
        grid.setOrientation(LinearLayout.HORIZONTAL);

        KeyguardManager keyguard = (KeyguardManager) getSystemService(KEYGUARD_SERVICE);
        boolean secure = keyguard != null && keyguard.isDeviceSecure();
        String patch = Build.VERSION.SECURITY_PATCH == null || Build.VERSION.SECURITY_PATCH.isEmpty()
                ? "Nenurodyta" : Build.VERSION.SECURITY_PATCH;

        LinearLayout lock = miniCard("EKRANO UŽRAKTAS", secure ? "Įjungtas" : "Neįjungtas", secure ? 0xff39d98a : 0xffff5964, "01");
        LinearLayout.LayoutParams half = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MATCH_PARENT, 1);
        half.rightMargin = dp(6);
        grid.addView(lock, half);

        LinearLayout update = miniCard("SAUGOS PATAISA", patch, WHITE, "02");
        LinearLayout.LayoutParams half2 = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MATCH_PARENT, 1);
        half2.leftMargin = dp(6);
        grid.addView(update, half2);
        page.addView(grid);
        TextView android = label("Android " + Build.VERSION.RELEASE + "  •  API " + Build.VERSION.SDK_INT, 12, false, MUTED);
        android.setPadding(dp(3), dp(9), 0, 0);
        page.addView(android);
    }

    private void buildFileTools(LinearLayout page) {
        section(page, "Failo kontrolė", "SHA-256");
        LinearLayout card = new LinearLayout(this);
        card.setOrientation(LinearLayout.VERTICAL);
        card.setPadding(dp(16), dp(16), dp(16), dp(16));
        card.setBackground(round(CARD, 17));

        LinearLayout top = new LinearLayout(this);
        top.setGravity(Gravity.CENTER_VERTICAL);
        TextView icon = label("#", 22, true, BLUE);
        icon.setGravity(Gravity.CENTER);
        icon.setBackground(round(CARD2, 13));
        top.addView(icon, new LinearLayout.LayoutParams(dp(44), dp(44)));
        LinearLayout copy = new LinearLayout(this);
        copy.setOrientation(LinearLayout.VERTICAL);
        copy.setPadding(dp(12), 0, 0, 0);
        copy.addView(label("Failo kontrolinis kodas", 16, true, WHITE));
        copy.addView(label("Pasirinkite failą iš telefono", 12, false, MUTED));
        top.addView(copy, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1));
        card.addView(top);

        expectedHash = new EditText(this);
        expectedHash.setSingleLine(true);
        expectedHash.setHint("Palyginimo kodas (nebūtina)");
        expectedHash.setTextSize(13);
        expectedHash.setInputType(android.text.InputType.TYPE_CLASS_TEXT | android.text.InputType.TYPE_TEXT_FLAG_NO_SUGGESTIONS);
        expectedHash.setTextColor(WHITE);
        expectedHash.setHintTextColor(0xff92a5b8);
        expectedHash.setPadding(dp(12), 0, dp(12), 0);
        expectedHash.setBackground(round(CARD2, 11));
        card.addView(expectedHash, topMargin(dp(14)));

        result = label("Pasirinkus failą, čia bus parodytas SHA-256 kodas.", 12, false, 0xffd5e1ea);
        result.setTypeface(Typeface.MONOSPACE);
        result.setPadding(dp(12), dp(12), dp(12), dp(12));
        result.setBackground(round(0xff091522, 11));
        card.addView(result, topMargin(dp(10)));

        TextView pick = button("PASIRINKTI FAILĄ", 0xff1c5272, WHITE, v -> chooseFile());
        card.addView(pick, topMargin(dp(12)));
        page.addView(card);

        TextView note = label("Kontrolinis kodas nėra virusų aptikimas. Failo turinys lieka telefone; kodą lyginkite tik su patikimo leidėjo pateiktu kodu.", 12, false, 0xffffc46b);
        note.setLineSpacing(dp(2), 1f);
        note.setPadding(dp(3), dp(12), dp(3), 0);
        page.addView(note);
    }

    private LinearLayout miniCard(String title, String value, int valueColor, String index) {
        LinearLayout card = new LinearLayout(this);
        card.setOrientation(LinearLayout.VERTICAL);
        card.setPadding(dp(13), dp(13), dp(13), dp(13));
        card.setBackground(round(CARD, 15));
        TextView small = label(index + "  /  " + title, 9, true, MUTED);
        card.addView(small);
        TextView data = label(value, 15, true, valueColor);
        data.setPadding(0, dp(9), 0, 0);
        card.addView(data);
        return card;
    }

    private void section(LinearLayout page, String title, String eyebrow) {
        LinearLayout row = new LinearLayout(this);
        row.setGravity(Gravity.CENTER_VERTICAL);
        row.setPadding(0, dp(22), 0, dp(9));
        TextView heading = label(title, 19, true, WHITE);
        row.addView(heading, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1));
        TextView tag = label(eyebrow, 9, true, BLUE);
        row.addView(tag);
        page.addView(row);
    }

    private TextView button(String text, int background, int foreground, View.OnClickListener click) {
        TextView view = label(text, 13, true, foreground);
        view.setGravity(Gravity.CENTER);
        view.setPadding(dp(14), dp(8), dp(14), dp(8));
        view.setMinHeight(dp(51));
        view.setBackground(round(background, 12));
        view.setOnClickListener(click);
        return view;
    }

    private TextView label(String text, int size, boolean bold, int color) {
        TextView view = new TextView(this);
        view.setText(text);
        view.setTextSize(size);
        view.setTextColor(color);
        if (bold) view.setTypeface(Typeface.DEFAULT, Typeface.BOLD);
        return view;
    }

    private GradientDrawable round(int color, int radius) {
        GradientDrawable d = new GradientDrawable();
        d.setColor(color);
        d.setCornerRadius(dp(radius));
        return d;
    }

    private GradientDrawable gradient(int top, int bottom, int radius) {
        GradientDrawable d = new GradientDrawable(GradientDrawable.Orientation.TL_BR, new int[]{top, bottom});
        d.setCornerRadius(dp(radius));
        return d;
    }

    private LinearLayout.LayoutParams topMargin(int margin) {
        LinearLayout.LayoutParams lp = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT);
        lp.topMargin = margin;
        return lp;
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
        shieldBadge.setState(ShieldBadge.CHECKING);
        String name = displayName(uri);
        String expected = expectedHash.getText().toString();
        result.setTextColor(BLUE);
        result.setText("SKAIČIUOJAMA…\n" + name);
        worker.execute(() -> {
            String message;
            int messageColor;
            int visualState;
            try {
                InputStream stream = getContentResolver().openInputStream(uri);
                String hash = FileFingerprint.sha256(stream);
                if (expected.trim().isEmpty()) {
                    message = "FAILAS: " + name + "\n\nSHA-256:\n" + hash + "\n\nTai nėra virusų patikros rezultatas.";
                    messageColor = BLUE;
                    visualState = ShieldBadge.IDLE;
                } else if (FileFingerprint.matches(expected, hash)) {
                    message = "KODAI SUTAMPA\n" + name + "\n\nSHA-256:\n" + hash + "\n\nSutapimas nepatvirtina failo saugumo.";
                    messageColor = 0xff39d98a;
                    visualState = ShieldBadge.MATCH;
                } else {
                    message = "KODAI NESUTAMPA\n" + name + "\n\nSHA-256:\n" + hash + "\n\nPatikrinkite kontrolinį kodą patikimame šaltinyje.";
                    messageColor = 0xffff5964;
                    visualState = ShieldBadge.MISMATCH;
                }
            } catch (Exception ex) {
                message = "Failo kontrolinio kodo apskaičiuoti nepavyko. Failas nebuvo patikrintas.";
                messageColor = 0xffff5964;
                visualState = ShieldBadge.MISMATCH;
            }
            final String text = message;
            final int color = messageColor;
            final int animationState = visualState;
            runOnUiThread(() -> {
                result.setTextColor(color);
                result.setText(text);
                shieldBadge.setState(animationState);
            });
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

    private int dp(int value) { return (int) (value * getResources().getDisplayMetrics().density + 0.5f); }

    private final class ShieldBadge extends View {
        static final int IDLE = 0;
        static final int CHECKING = 1;
        static final int MATCH = 2;
        static final int MISMATCH = 3;
        private final android.graphics.Paint paint = new android.graphics.Paint(android.graphics.Paint.ANTI_ALIAS_FLAG);
        private android.animation.ValueAnimator pulse;
        private float phase;
        private int state = IDLE;

        ShieldBadge() { super(MainActivity.this); }

        void setState(int next) {
            state = next;
            if (pulse != null) {
                pulse.setDuration(state == CHECKING ? 620 : 1180);
            }
            invalidate();
        }

        @Override protected void onAttachedToWindow() {
            super.onAttachedToWindow();
            if (pulse == null) {
                pulse = android.animation.ValueAnimator.ofFloat(0f, 1f);
                pulse.setDuration(1180);
                pulse.setRepeatMode(android.animation.ValueAnimator.REVERSE);
                pulse.setRepeatCount(android.animation.ValueAnimator.INFINITE);
                pulse.setInterpolator(new android.view.animation.AccelerateDecelerateInterpolator());
                pulse.addUpdateListener(a -> {
                    phase = (float) a.getAnimatedValue();
                    invalidate();
                });
            }
            if (!pulse.isStarted()) pulse.start();
        }

        @Override protected void onDetachedFromWindow() {
            if (pulse != null) pulse.cancel();
            super.onDetachedFromWindow();
        }

        @Override protected void onDraw(android.graphics.Canvas canvas) {
            super.onDraw(canvas);
            float w = getWidth(), h = getHeight();
            int accent = state == MATCH ? 0xff39d98a : state == MISMATCH ? 0xffff5964 : 0xff55b7ff;
            float cx = w * .5f, cy = h * .52f;
            float baseRadius = Math.min(w, h) * .43f;

            // Clearly visible blue breathing halo and moving ring accents.
            paint.setStyle(android.graphics.Paint.Style.FILL);
            paint.setColor(accent);
            paint.setAlpha(35 + (int) (75 * (1f - phase)));
            canvas.drawCircle(cx, cy, baseRadius * (0.86f + 0.18f * phase), paint);

            paint.setStyle(android.graphics.Paint.Style.STROKE);
            paint.setStrokeWidth(dp(2));
            paint.setAlpha(185);
            canvas.drawCircle(cx, cy, baseRadius * (0.80f + 0.08f * phase), paint);

            android.graphics.RectF orbit = new android.graphics.RectF(
                    cx - baseRadius, cy - baseRadius, cx + baseRadius, cy + baseRadius);
            paint.setStrokeWidth(dp(4));
            paint.setStrokeCap(android.graphics.Paint.Cap.ROUND);
            paint.setAlpha(255);
            float angle = phase * 360f;
            canvas.drawArc(orbit, angle, 92f, false, paint);
            canvas.drawArc(orbit, angle + 180f, 46f, false, paint);

            double radians = Math.toRadians(angle);
            float dotX = cx + (float) Math.cos(radians) * baseRadius;
            float dotY = cy + (float) Math.sin(radians) * baseRadius;
            paint.setStyle(android.graphics.Paint.Style.FILL);
            canvas.drawCircle(dotX, dotY, dp(4), paint);

            canvas.save();
            float shieldScale = 0.94f + 0.12f * phase;
            canvas.scale(shieldScale, shieldScale, cx, cy);
            paint.setAlpha(255);

            android.graphics.Path shield = new android.graphics.Path();
            shield.moveTo(w * .5f, h * .05f);
            shield.lineTo(w * .91f, h * .20f);
            shield.lineTo(w * .85f, h * .62f);
            shield.quadTo(w * .79f, h * .82f, w * .5f, h * .96f);
            shield.quadTo(w * .21f, h * .82f, w * .15f, h * .62f);
            shield.lineTo(w * .09f, h * .20f);
            shield.close();
            paint.setColor(accent);
            canvas.drawPath(shield, paint);

            paint.setColor(Color.WHITE);
            paint.setStrokeWidth(dp(4));
            paint.setStrokeCap(android.graphics.Paint.Cap.ROUND);
            paint.setStrokeJoin(android.graphics.Paint.Join.ROUND);
            paint.setStyle(android.graphics.Paint.Style.STROKE);
            android.graphics.Path mark = new android.graphics.Path();
            if (state == MISMATCH) {
                mark.moveTo(w * .5f, h * .33f);
                mark.lineTo(w * .5f, h * .56f);
                canvas.drawPath(mark, paint);
                paint.setStyle(android.graphics.Paint.Style.FILL);
                canvas.drawCircle(w * .5f, h * .69f, dp(2), paint);
            } else {
                mark.moveTo(w * .32f, h * .51f);
                mark.lineTo(w * .45f, h * .64f);
                mark.lineTo(w * .70f, h * .38f);
                canvas.drawPath(mark, paint);
            }
            paint.setStyle(android.graphics.Paint.Style.FILL);
            canvas.restore();
        }
    }

    @Override protected void onDestroy() {
        worker.shutdownNow();
        super.onDestroy();
    }
}
