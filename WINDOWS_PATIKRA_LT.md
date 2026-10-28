# Windows 0.44 priėmimas — NEATLIKTAS

Būtina izoliuota Windows 10/11 x64 mašina arba VM su .NET Framework 4.8 ir atkūrimo tašku. Šio sąrašo buvimas nereiškia, kad bandymai įvykdyti.

1. Bendras diegiklis: švarus diegimas ir atnaujinimas; UAC atšaukimas; diegimo lango uždarymas; skirtinga UAC paskyra; esamo GUI profilio ir DPAPI kopijų išlikimas. Atšauktas diegimas negali būti parodytas sėkmingu.
2. SCM / ACL: virtuali paslaugos paskyra, automatinė paleistis po perkrovimo be vartotojo; įprastas vartotojas negali keisti Program Files kodo / konfigūracijos ir privačių įrašų, tačiau skaito viešą būseną ir bendras bazes. Paslauga negali perrašyti savo kodo.
3. Bendra parengtis: sustabdyta paslauga, dingęs / pasenęs / ateities signalas, neprieinamos bazės, aktyvus atnaujinimas ir pristabdymas. Nė vienas neturi rodyti visos sistemos saugumo garantijos.
4. Neutralus failas ir pasirenkama EICAR kontrolė naudojant NativeAcceptance įrankį. Esama antivirusinė gali perimti testą; to nevadinti SARGON sėkme. Gyvų virusų kasdienėje mašinoje nenaudoti.
5. Radinių langas: perpatikra; pasikeitusio failo atmetimas; kopijos patvirtinimas ir originalo izoliavimo būsena; atkūrimas į naują kelią; atsisakymas perrašyti esamą failą; kitos paskyros DPAPI atsisakymas. Testuoti su atkuriamomis neutraliomis kopijomis ir aiškiai atskirtu kontroliniu testu.
6. Apkrova: AC / baterija / nežinoma būsena, CPU 80 % ir tęsimui du matavimai iki 60 %, tęsiamas planas. Išmatuoti CPU, RAM, diską ir bateriją dideliame diske; vien sumų cache nėra greičio įrodymas.
7. Cache: tas pats dydis ir data su pakeistu turiniu; naujas failas; pakeista bazė ir vietinių sumų rinkinys; persikrovimas; 7 dienų galiojimas; užrakintas failas. Neaiškumas turi lemti naują patikrą arba apimties spragą.
8. Atnaujinimo grąžinimas: pakeistas paketas, sena versija, neteisingas parašas, nepatvirtintas naujas heartbeat, atšaukimas. Patikrinti ankstesnę SCM registraciją, planą, nuostatas, versijos ribą ir leidėjo atspaudą po nesėkmės.
9. Pašalinimas: UAC atšaukimas išsaugo įdiegtą GUI ir paslaugą; sėkmingas pašalinimas išregistruoja paslaugą ir išsaugo atkuriamas kopijas. Pakartotinis diegimas nepraranda duomenų.

Invoke-NativeAcceptance.ps1 vykdo tik dalį ankstesnių kontrolinių bandymų ir nepakeičia šio sąrašo. Visos sistemos aptikimo efektyvumas ir klaidingi radiniai turi būti vertinami atskirai. Nėra failų sistemos filtro ar atminties / įkrovos apsaugos, todėl vienintele antivirusine šios laidos vadinti negalima.


0.44 pirmas vietinis žingsnis: GUI → Patikros → Windows kontrolė ir apkrova. Patvirtinus tikrinami tik paties įrankio sukurti neutralūs failai. Pateikite last-native-audit.json kartu su paslaugos būsena; ši trumpa kontrolė nepatikrina visų aukščiau išvardytų punktų ir neįrodo tikrų virusų aptikimo. Windows saugos centro registracija ir filtras vis dar neįgyvendinti.


0.44 papildomai: patikrinti, kad įprastas vartotojas negali siųsti SCM 128/129 komandos, o administratorius gali; ManualControlReady=true tik su tinkama DACL. Patikrinti bateriją be leidimo (laukia AC), vienkartinį leidimą (atnaujina tik vieną kartą), dublikatų atmetimą, eilę po vykstančio skenavimo, uždarytą laukimo langą, paslaugos persileidimą laukimo metu ir užklausos susiejimą. Nepavykus FreshClam turi būti INCOMPLETE su priežastimi. Atsisiuntus bazes pakartoti nuosavo neutralaus failo ir konkretaus vartotojo failo skenavimą. Tai dar NEATLIKTI bandymai, ne rezultatai.
