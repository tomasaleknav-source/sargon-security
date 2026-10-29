# SARGON Android 0.52-android — bandomasis leidimas

Tai atskiras Android pagalbininkas, ne Windows SARGON perkėlimas ir ne antivirusinė programa.

## Ką daro

- Parodo Android versiją, sistemos saugos naujinimo datą ir ar įjungtas ekrano užraktas.
- Atveria telefono saugos nustatymus, kad naudotojas galėtų pats patikrinti Google Play Protect.
- Naudotojui pasirinkus failą, apskaičiuoja SHA-256 kodą ir gali palyginti jį su naudotojo įvestu patikimu kodu.
- Failo turinys nesiunčiamas į tinklą. Programėlė neprašo interneto ar plačios prieigos prie telefono failų.

## Ko nedaro

- Neskaito Play Protect būsenos API būdu, nes tam reikia Google Play platinimo ir serverio konfigūracijos.
- Neaptinka virusų ir netikrina viso telefono ar kitų programėlių.
- Nevaldo Windows Defender ir neturi ClamAV variklio.
- SHA-256 kodo sutapimas patvirtina tik failo vientisumą pagal pateiktą kontrolinį kodą, o ne tai, kad failas saugus.

Tikro Android antivirusinio tikrinimo variklis ir nepriklausomi Android įrenginių bandymai dar neįtraukti. Kol kas pagrindinei apsaugai palikite Google Play Protect įjungtą.

## Surinkimas

Atverkite `android` katalogą Android Studio arba naudokite CI darbo eigą `Android preview build`. CI sukuria bandymui skirtą `app-debug.apk`; jis nėra Google Play leidimas ir nėra pasirašytas viešam platinimui.
