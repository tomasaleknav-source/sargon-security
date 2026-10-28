# SARGON 0.44 — visų užsakytų punktų būsena

| Punktas | Kas padaryta | Ko dar reikia |
|---|---|---|
| Windows bandymai | Nuosavų neutralių failų kontrolės veiksmas GUI, išsaugoma ataskaita ir ankstesni SCM / priėmimo scenarijai | Tikras Windows x64 vykdymas, perkrovimo, teisių, atnaujinimo ir karantino priėmimas. Čia nevykdyta |
| Apsauga realiuoju laiku | Išliko ribotas GUI aplanko stebėjimas ir suplanuota atskira paslauga | Failų blokavimo prieš paleidimą mechanizmas neįgyvendintas. Reikia projektuoti, kurti ir išbandyti tinkamą filtrą / jį aptarnaujantį variklį |
| Aptikimo kokybė | Ankstesnė neutrali / EICAR kontrolė, klaidos ir ribos atskiriamos nuo saugumo verdikto | Nepriklausomos laboratorijos vertinimas ir klaidingų radinių matavimas; jų rezultatai nesukurti |
| Pasirašymas / atnaujinimai | Pataisyta GUI versijos riba; manifestų patikra, apsaugotas paslaugos atnaujinimas; pridėtas Authenticode pasirašymo scenarijus | Tikras leidėjo sertifikatas ir raktas, patikimo atspaudo nustatymas, produkcinis atnaujinimų serveris ir native grąžinimo bandymas |
| Windows saugos centras | Programa teisingai nurodo, kad nėra registruotas antivirusinės teikėjas | Oficialus integravimo kelias ir jo reikalavimų įvykdymas; netikras antivirusinės įrašas nekuriamas |
| Apkrova | Ankstesnis AC / CPU valdymas, turinio cache; naujos trumpos native kontrolės laiko / proceso RAM / CPU ir sistemos CPU matavimo laukai | Matavimai silpname Windows kompiuteryje, pilna disko / vaikinių procesų apkrova ir ilgalaikis baterijos bandymas |
| Paprastas valdymas | Trys pagrindiniai mygtukai ir pasirinkimas tarp vartotojo / paslaugos radinių; techniniai meniu suskleisti | Tikras Windows DPI / kelių ekranų ir naudotojo patogumo priėmimas |

Tai dalinė visų punktų realizacija, ne pareiškimas, kad visi užbaigti. Programos negalima laikyti patvirtinta vienintele antivirusine.

## Oficialūs reikalavimai, peržiūrėti 2026-10-21

- Microsoft Virus Initiative narystei reikalinga realaus laiko aptikimo / prevencijos / šalinimo funkcija, leidėjo atsakomybė už Windows suderinamumo atnaujinimus, sutartys, Trusted Signing ir per paskutinius 12 mėnesių atliktas nepriklausomas sertifikavimas. Paraiška negarantuoja priėmimo: https://learn.microsoft.com/en-us/defender-xdr/virus-initiative-criteria
- Naujos viešai platinamos branduolio tvarkyklės turi atitikti Microsoft pasirašymo reikalavimus: https://learn.microsoft.com/en-us/windows-hardware/drivers/install/kernel-mode-code-signing-requirements--windows-vista-and-later-
- Oficialus Scanner minifilter pavyzdys yra mokomasis pavyzdys, ne gatava antivirusinė: https://github.com/microsoft/Windows-driver-samples/tree/main/filesys/miniFilter/scanner

Nei sertifikato, nei laboratorijos įvertinimo, nei Windows įvykdymo neįmanoma pakeisti vien šaltinio kodo pakeitimu.

## Kaip surinkti ir pasirašyti turint savo leidėjo tapatybę

1. build-windows.cmd sukuria bandomąjį diegiklį ir build/service-package.
2. Windows SDK aplinkoje paleisti Service/Sign-WindowsRelease.ps1 nurodant -Package build/service-package, -MainBinary build/TelefonoSargasPC.exe, -CertificateThumbprint savo sertifikato atspaudą ir -TimestampServer savo patvirtintą HTTPS RFC3161 serverį. Scenarijus pasirašo konkrečius paketo EXE, atnaujina jų sumas, pasirašo manifestą ir sinchronizuoja pagrindinį GUI EXE su pasirašyta kopija. Savarankiškai sugeneruotas bandymo sertifikatas nėra viešai patikimas leidėjo sertifikatas.
3. Iš naujo paleisti makensis installer.nsi. Po to to paties scenarijaus atskiru -Installer keliu pasirašyti galutinį diegiklį. Po pasirašymo nebeperkurti paketo ar diegiklio.
4. Nepriklausomai patvirtinti paslaugos leidėjo SHA256 atspaudą ir naudoti -PublisherSha256 / --publisher pirmam patikimam diegimui. Paprastas nepasirašytos beta pasirinkimas produkcinio pasitikėjimo nesukuria.
5. Atlikti WINDOWS_PATIKRA_LT.md priėmimą ir nepriklausomą apsaugos vertinimą. Scenarijų buvimas ar parsavimas nėra tikro pasirašymo įrodymas.
