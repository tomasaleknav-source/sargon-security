# SARGON Security — diegimas Windows ir Android

Diegimo failai pateikti čia, kad juos būtų paprasta parsisiųsti ir įdiegti kituose įrenginiuose.

| Įrenginys | Versija | Diegimo failas |
|---|---:|---|
| **Windows kompiuteris** | 0.52 | [⬇️ Atsisiųsti SARGON Windows diegiklį (.exe)](https://github.com/tomasaleknav-source/sargon-security/releases/download/v52/SARGON_Security_PC_Setup.exe) |
| **Android telefonas** | 0.52.3 | [⬇️ Atsisiųsti SARGON Android paketą (.apk)](https://github.com/tomasaleknav-source/sargon-security/releases/download/v0.52.3-android/SARGON_Android_0.52.3.apk) |

[Peržiūrėti visus SARGON leidimus](https://github.com/tomasaleknav-source/sargon-security/releases)

Norėdami įdiegti kitame įrenginyje, atverkite šį puslapį tame įrenginyje ir pasirinkite atitinkamą failą. Windows sistemoje paleiskite atsisiųstą `.exe`; Android telefone atverkite atsisiųstą `.apk` ir vykdykite telefono nurodymus. Windows gali rodyti SmartScreen perspėjimą, nes diegiklis neturi Authenticode leidėjo sertifikato.

**Svarbu:** Windows versija valdo Microsoft Defender ir naudoja ClamAV kaip papildomą failų patikrą — ji nepakeičia Defender ir nėra pilnavertė antivirusinė. Android versija yra telefono saugos pagalbininkas; ji nepakeičia Google Play Protect ir neskenuoja viso telefono. Palikite Defender ir Play Protect įjungtus.

---

## Ankstesnės 0.49 Windows versijos techninės pastabos

**Tai šaltinio kandidatas, ne paruoštas diegiklis.** Šiame pakete nėra sukompiliuoto 0.49 `.exe`; Windows surinkimas, diegimas ir priėmimo bandymai šiame vykdymo kompiuteryje neatlikti. Nemokamas pagrindinis McAfee antivirusinės pakaitalas yra Windows integruotas Microsoft Defender. Šis SARGON leidimas jį valdo ir tikrina jo būseną; SARGON pats nėra antiviruso variklis. ClamAV yra tik papildoma failų patikra.

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
- Radiniai pateikiami peržiūrai; prieš karantiną jie tikrinami iš naujo. Paslauga originalų automatiškai netrina. Ankstesnė karantino kopija, atkūrimas į naują kelią, pasirašytų atnaujinimų patikra ir atnaujinimo grąžinimo kelias lieka bandomi komponentai.
- Nėra AMSI tiekėjo, branduolio minifilter tvarkyklės, failų atidarymo / vykdymo blokavimo, atminties, įkrovos ar procesų elgsenos apsaugos ir Windows Security Center antivirusinio tiekėjo registracijos.
- Nėra tikro leidėjo sertifikato ar pasirašytos viešos atnaujinimų infrastruktūros. SmartScreen perspėjimo pašalinimas neįmanomas vien sukūrus naują diegiklį; reikia pasirašyto leidimo ir reputacijos.

## Surinkimas ir bandymai

Surinkimui reikia Windows x64, .NET Framework 4.8 kūrimo įrankių / suderinamo .NET SDK ir NSIS. `build-windows.cmd` kuria programą, paslaugą, Setup įrankį, ClamAV paketo manifestą ir NSIS diegiklį. `test-windows.cmd` paleidžia host logikos testus; `Service\\Invoke-NativeAcceptance.ps1` reikalauja atskiros Windows bandomosios aplinkos.

0.49 Windows surinkimo pataisos: paslaugos ir diegiklio projektams aiškiai pridėta `System.Web.Extensions` nuoroda; `System.TimeoutException` pilnai kvalifikuota, kad Windows SDK kompiliatorius išvengtų dviprasmio tipo vardo.

Šiame vykdymo kompiuteryje nebuvo C# kompiliatoriaus, Windows aplinkos ar NSIS. Todėl kodas nekompiliuotas, testai nevykdyti ir 0.49 diegiklis nesukurtas. Atliktos tik šaltinio statinės patikros; rezultatai pateikti `tests/QA_049.json`.

Prieš platinimą būtini Windows x64 build ir testų rezultatai, realus paslaugos / UAC / diegiklio priėmimas, failų apimties ir klaidingų perspėjimų matavimai, saugus leidėjo pasirašymas ir nepriklausomas aptikimo efektyvumo įvertinimas. Iki tol SARGON netinka naudoti kaip vienintelė antivirusinė.

## Vieši leidimai ir automatiniai atnaujinimai

Windows kompiuterio diegiklis ir Android paketas parsisiunčiami iš šioje saugykloje paskelbtų viešų leidimų. Windows 0.52 leidime automatinio atnaujinimo srautas tikrina pasirašytą aprašą ir SHA-256 prieš siūlydamas diegti naują leidimą. Tai nėra Authenticode leidėjo sertifikatas, todėl Windows vis tiek gali rodyti SmartScreen perspėjimą.

SARGON nėra savarankiškas visos sistemos antiviruso variklis. Windows apsaugai palikite aktyvų Microsoft Defender, o Android telefone — Google Play Protect. Diegimo failai ir jų versijų aprašai pateikiami leidimų puslapyje; parsisiųskite tik iš oficialių nuorodų viršuje.
