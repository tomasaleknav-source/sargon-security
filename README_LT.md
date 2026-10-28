# SARGON Security 0.50 — Windows bandomasis šaltinis

**Tai šaltinio kandidatas, ne paruoštas diegiklis.** Šiame pakete nėra sukompiliuoto 0.50 `.exe`; GitHub Actions Windows CI patvirtino programos ir diegiklio surinkimą bei host logikos testus, tačiau diegimas ir native Windows priėmimo bandymai dar neatlikti. Nemokamas pagrindinis McAfee antivirusinės pakaitalas yra Windows integruotas Microsoft Defender. Šis SARGON leidimas jį valdo ir tikrina jo būseną; SARGON pats nėra antiviruso variklis. ClamAV yra tik papildoma failų patikra.

Greitam Windows bandymui ZIP faile taip pat yra `Paleisti_SARGON_Defender.cmd` ir `SARGON_Defender_Valdymas.ps1`. Išskleiskite paketą į aplanką ir dukart spustelėkite `.cmd` paleidimo failą. Šis atskiras valdymo langas nenaudoja kompiliuoto C# kliento; Windows Defender lieka tikruoju varikliu.

## Perėjimas nuo McAfee prie nemokamo Defender

SARGON lange spauskite **„Pereiti nuo McAfee prie Defender“**. Įrankis parodys dabartinę Defender būseną ir gali atverti Windows programų sąrašą. Ten McAfee pašalinkite tik tada, kai jau nusprendėte pereiti; šis įrankis pats McAfee netrina ir apsaugos neišjungia. Jei prašoma, paleiskite kompiuterį iš naujo. Grįžę į SARGON spauskite **„Atnaujinti būseną“** ir Windows saugoje patikrinkite, kad Microsoft Defender būtų aktyvus teikėjas, antivirusinė ir apsauga realiuoju laiku būtų įjungtos. Kol to nepatvirtinote, laikykite perėjimą neužbaigtu.

## Kas atnaujinta 0.49 versijoje

- Pridėtas pagrindinis Microsoft Defender valdymo skyrius: skaityti jo aktyvumo ir apsaugos bazės būseną, prašyti bazių atnaujinimo, paleisti greitąją arba viso kompiuterio patikrą ir peržiūrėti iki 30 naujausių Defender grėsmių įrašų. Patikras ir veiksmus vykdo Microsoft Defender; SARGON parodo gautą būseną ir nuorodas į Windows saugumo istoriją.
- Defender būsenos bei radinių istorijos langai nėra nepriklausomas aptikimo patvirtinimas. SARGON nenustato ir nekeičia Defender apsaugos politikos; patikrinkite aktyvų teikėją Windows saugumo programoje.

- Pridėtas audito perspėjimas: kai stebėjimo įvykiuose per 15 sekundžių pasikeičia bent 100 skirtingų kelių, programa rodo signalą iki 120 sekundžių, įrašo jį į ataskaitą ir parodo pranešimą. Kelių skaičiavimas turi ribą, kad įvykių srautas negalėtų be galo auginti atminties naudojimo.
- Signalas **nėra viruso aptikimas**: SARGON nenustato, kuris procesas keitė failus, nepatvirtina išpirkos reikalaujančios programos ir automatiškai failų neblokuoja, nešalina bei nekarantinuoja. Perspėjimą gali sukelti ir teisėta masinė failų operacija.
- Nauji perspėjimo laukai tikrinami paslaugos būsenos pranešime; būsenos lange ir patikrų suvestinėje rodomas aktyvus signalas.
- Suderinti PC, paslaugos, diegiklio ir įrankių versijos slenksčiai į 0.49.

## Paveldėtos galimybės ir ribos

- Paslauga periodiškai skenuoja pasirinktus vietinius diskus su ClamAV ir po failo pakeitimo atlieka papildomą patikrą. `FileSystemWatcher` veikia po įrašymo, neužblokuoja vykdymo ir neapima laiko, kai paslauga sustabdyta. Eilės perpildymas bei stebėjimo klaidos žymimos kaip aprėpties spragos.
- ClamAV duomenų bazės parengtis reikalauja neutralaus failo runtime patikros. Tai patvirtina tik variklio veikimą, ne aptikimo efektyvumą.
- Radiniai pateikiami peržiūrai; prieš karantiną jie tikrinami iš naujo. Paslauga originalų automatiškai netrina. Ankstesnė karantino kopija, atkūrimas į naują kelią ir pasirašytų atnaujinimų patikra lieka bandomi komponentai; automatinis grąžinimas po dalinio diegimo neįgyvendintas.
- Nėra AMSI tiekėjo, branduolio minifilter tvarkyklės, failų atidarymo / vykdymo blokavimo, atminties, įkrovos ar procesų elgsenos apsaugos ir Windows Security Center antivirusinio tiekėjo registracijos.
- Automatinio atnaujinimo leidėjo raktas ir pasirašyti vieši leidimai dar nesukonfigūruoti. SmartScreen perspėjimo pašalinimas neįmanomas vien sukūrus naują diegiklį; tam reikia Authenticode pasirašymo ir reputacijos.

## Surinkimas ir bandymai

Surinkimui reikia Windows x64, .NET Framework 4.8 kūrimo įrankių / suderinamo .NET SDK ir NSIS. `build-windows.cmd` kuria programą, paslaugą, Setup įrankį, ClamAV paketo manifestą ir NSIS diegiklį. `test-windows.cmd` paleidžia host logikos testus; `Service\\Invoke-NativeAcceptance.ps1` reikalauja atskiros Windows bandomosios aplinkos.

0.49 Windows surinkimo pataisos: paslaugos ir diegiklio projektams aiškiai pridėta `System.Web.Extensions` nuoroda; `System.TimeoutException` pilnai kvalifikuota, kad Windows SDK kompiliatorius išvengtų dviprasmio tipo vardo.

0.50 programos ir diegiklio surinkimą bei host logikos testus tikrina GitHub Actions Windows CI. Native Windows priėmimo bandymai vis dar reikalingi. 0.49 istorinio kandidato statinės patikros pateiktos `tests/QA_049.json`.

Prieš platinimą būtini Windows x64 build ir testų rezultatai, realus paslaugos / UAC / diegiklio priėmimas, failų apimties ir klaidingų perspėjimų matavimai, saugus leidėjo pasirašymas ir nepriklausomas aptikimo efektyvumo įvertinimas. Iki tol SARGON netinka naudoti kaip vienintelė antivirusinė.

## Viešas šaltinio veidrodis ir programos atnaujinimai

SARGON klientas tikrina viešą GitHub stabilų leidimą ne dažniau kaip kartą per 24 valandas. Jis priima tik įdiegtame `update-publisher.der` raktu patvirtintą manifestą, atsisiunčia diegiklį į naudotojo profilį, sutikrina manifesto dydį ir SHA-256, foninis tikrinimas naujesnį patvirtintą leidimą atsisiunčia ir tyliai įdiegia automatiškai; rankinis patikrinimas vis tiek prašo sutikimo. Diegiklis laukia, kol SARGON užsidarys, ir po diegimo programą paleidžia iš naujo. Šis leidimo parašas patvirtina failo kilmę ir vientisumą; tai nėra Authenticode parašas ar antivirusinis patikrinimas.

Automatinis atnaujinimas dar **neįjungtas šiame viešame leidime**: saugykloje nėra leidėjo viešojo rakto, GitHub Actions paslapčių, stabilaus GitHub Release ar Windows aplinkoje išbandyto diegiklio. Jis saugiai lieka išjungtas, kol savininkas paruoš raktus ir patikrins leidimo grandinę. Niekada neįkelkite privataus rakto į saugyklą.

Leidimo parengimas savininkui (Windows komandas vykdykite aplanke už saugyklos ribų):

1. Su OpenSSL sukurkite RSA 3072 bitų užšifruotą privatų raktą ir išveskite viešąjį DER raktą: `openssl genpkey -algorithm RSA -aes-256-cbc -pkeyopt rsa_keygen_bits:3072 -out update-private.pem`, tada `openssl pkey -in update-private.pem -pubout -outform DER -out update-publisher.der`. OpenSSL paprašys privataus rakto slaptažodžio. Įkelkite tik viešą `update-publisher.der` į saugyklos šaknį.
2. Base64 užkoduokite užšifruotą PEM failą komandų lange ir nukopijuokite į GitHub paslaptį: `[Convert]::ToBase64String([IO.File]::ReadAllBytes('update-private.pem')) | Set-Clipboard`.
3. GitHub saugyklos **Settings → Secrets and variables → Actions** pridėkite `SARGON_UPDATE_PRIVATE_KEY_B64` (iškarpinėje esantį Base64) ir `SARGON_UPDATE_PRIVATE_KEY_PASSWORD` (rakto slaptažodis). Nenaudokite bandomojo ar kitur viešinto rakto.
4. Windows CI patikrins versiją, paleis testus, surinks NSIS diegiklį, pasirašys manifestą ir paskelbs `v<ReleaseIdentity.Version>` žymos GitHub Release. Raktų ir Windows bandymų neparuošus, leidimas nepaskelbiamas.
5. Prieš tikrą platinimą patikrinkite atnaujinimą atskiroje Windows bandomojoje mašinoje, įskaitant netinkamą parašą, nutrūkusį atsisiuntimą, atšaukimą ir ankstesnės versijos atkūrimą. Dabartinis diegiklis neturi automatinio atkūrimo po dalinio diegimo.

`release_tools/create_update_manifest.py` dabar reikalauja užšifruoto RSA rakto ir `SARGON_UPDATE_PRIVATE_KEY_PASSWORD` aplinkos kintamojo. Jis pasirašo tik manifesto metaduomenis; Windows diegimą atlieka SARGON klientas po parašo ir SHA-256 patikros.

Viešos šaltinio saugyklos paskelbimas savaime nereiškia, kad šis bandomasis antiviruso pagalbininkas tinkamas kaip vienintelė apsauga. Laikykite Microsoft Defender įjungtą.
