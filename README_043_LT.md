# SARGON Security 0.43 — sumų šaltinio prieigos pataisa

## Nauja 0.43

Ataskaitoje 2026-10-22 08:48 „Sumų atnaujinimas“ nutrūko dar prieš tinklo užklausą dėl trūkstamo / netinkamo rakto. Ši operacija nėra ClamAV bazių atnaujinimas ar failo skenavimas.

- „Atnaujinti…“ lange papildomos MalwareBazaar sumos turi atskirą mygtuką ir būsenos langą. Ten matyti, ar rakto nėra, jo formatas netinkamas, įrašo nepavyko perskaityti ar raktas išsaugotas. Išsaugotas raktas dar nereiškia, kad tiekėjas jį priims.
- Kol raktas neparuoštas, abu sumų atsisiuntimo mygtukai išjungti. „Įvesti / pakeisti raktą…“ atveria ryšio nuostatas; savo MalwareBazaar Auth-Key įveskite trečiame lauke. Saugojimas šifruotas DPAPI šiai Windows paskyrai. Išsaugojimas užklausos automatiškai nesiunčia.
- Sugadinto raktų įrašo atveju paaiškinama, kad ryšio nuostatų šalinimo mygtukas pašalins abu ryšio raktus. Sugadintas įrašas automatiškai netrinamas.
- HTTP401/403 prieigos atmetimas paaiškinamas nurodant tiekėją, raktą ir teises. Kitos tinklo klaidos automatiškai nevadinamos blogu raktu. Rakto ar nepatikimo tiekėjo atsakymo teksto į diagnostiką nekopijuojame.
- Seno meniu ir paruošimo vedlio mygtukai nukreipia į tą patį prieigos langą. Sumų užklausa pradedama tik aiškiai pasirinkus ir patvirtinus; siunčiamas Auth-Key ir IP, failai ar jų sumos sumų atnaujinimui nesiunčiami.
- ClamAV bazių atnaujinimui MalwareBazaar Auth-Key nereikalingas. Šis pakeitimas rakto nesukuria ir nesuteikia tiekėjo naudojimo teisių.

472 logikos patikros praėjo, įskaitant 19 naujų prieigos patikrų. Sąsajos bandyme trūkstamas raktas išjungia abu atsisiuntimus ir palieka aktyvų rakto nustatymų mygtuką. Tikro tiekėjo rakto, gyvų užklausų ir 0.43 Windows diegimo čia nebandėme.

## Išlikę 0.42 sąsajos pataisymai

- Pradėjus operaciją ankstesnė ataskaita paslepiama. Rodomas dabartinio veiksmo pavadinimas, pradžios laikas ir paaiškinimas, kad rezultato dar nėra. Baigus pasirodo tos operacijos ataskaita; senesni rezultatai lieka istorijoje.
- Skydelyje foninė failo patikra įvardijama atskirai nuo vartotojo pasirinkto veiksmo. Veiklos animacija nėra visos sistemos patikros įrodymas.
- Stebėjimo eilės nepriimtų įvykių skaičius aiškiai atskirtas nuo virusų radinių. Perspėjimas apie ankstesnę spragą išlieka; jos automatiškai nevadiname patikrinta.
- Sėkmingai baigęs darbą atskiras paslaugos diegimo langas turi aktyvų mygtuką „Baigti ir grįžti į diegiklį“. Jis uždaro paruošimo langą ir leidžia pagrindiniam diegikliui tęsti. Kryželio spėlioti nebereikia. Bazių atnaujinimas vis dar atskiras paslaugos darbas.
- Pagrindinio diegiklio detalių lange paaiškinta, kad laukiama atskiro paruošimo lango, o ne rodoma atsisiuntimo eiga.

453 logikos patikros ir papildomi GUI scenarijai praėjo: ankstesnė ataskaita pakeičiama vykdomo veiksmo tekstu, skydelyje yra operacijos pavadinimas, baigimo mygtukas uždaro nemodalinį paruošimo langą sėkmingu rezultatu. Bandymuose tikras diegimas nevykdytas.

Vartotojo 0.41 ataskaita (2026-10-22 00:32) patvirtino vieno 293736 baitų ZIP failo ClamAV patikros baigtį be žymų ir nepatikrintos apimties (8284 ms). Tai vieno failo rezultatas, ne visos sistemos ar naujos laidos patvirtinimas.

## Išlikę 0.41 pataisymai

Ankstesnis bendras pranešimas „Stebimas failas nepasiekiamas arba viršija 32 MiB“ pakeistas konkrečia priežastimi, failo keliu, kontekstu ir dydžiu, kai jis nustatomas. Neperskaitytas failas įskaičiuojamas į nepatikrintą apimtį. Tai nėra viruso radinys ar saugumo patvirtinimas.

Foninis aplanko stebėjimas išlaiko 32 MiB ribą. Dingęs ar per didelis failas nestabdo kitų eilėje esančių failų. Laikinos skaitymo / prieigos klaidos kartojamos tam failui po 1, 2, 4, 8 ir 16 minučių; naujas failo pakeitimas gali pradėti naują bandymą. Variklio ar bazių gedimai ir toliau pristabdo foninę patikrą. Trūkstama apsauga nėra slepiama didinant ribas.

Rankinė aplanko sumų eilė išlaiko 256 MiB vieno failo ribą. Per dideli failai pažymimi praleistais ir išsaugomi apimties skaičiuje, užuot neribotai sukęsi eilėje. Dingusius failus galima bandyti dar kartą. Kiti failai tęsiami. Ši eilė yra SHA-256 sumų patikra, ne visų failų ClamAV skenavimas. Konkretaus failo ClamAV patikra lieka atskira funkcija.

Ataskaitose aiškiai įvardijama operacija, įskaitant foninį stebėjimą. Foninė operacija vis dar gali tapti naujausia rodoma / eksportuojama ataskaita — perskaitykite jos kontekstą.

## Įdiegimas

Užbaikite SARGON per piktogramos prie laikrodžio meniu. Paleiskite SARGON_Security_PC_v0.43_Diegimas.exe. Atnaujinkite ir pasirinktą paslaugos komponentą administratoriaus lange. Nuostatos ir DPAPI kopijų tapatybė išsaugomi. Senos aplanko eilės suderinamos; pasikeitus failams pradėkite naują aplanko patikrą.

Po diegimo patikrinkite mažą įprastą failą, tada aplanką su juo ir vienu daugiau nei 256 MiB failu: mažas failas turi būti perskaitytas, didelis pažymėtas nepatikrintu. Foninio stebėjimo riba atskirai yra 32 MiB. Patikrinkite ataskaitos operacijos pavadinimą ir priežastį.

## Patikrinimai ir ribos

453 C# logikos patikros praėjo, iš jų 29 naujos: dydžiai, trūkstami / užrakinti failai, atšaukimas, eilės tęsimas, kartojimų tvarka ir išsaugoti praleidimai. Sąsaja patikrinta Mono/Linux; PowerShell ir paketo patikrų rezultatai pateikti tests aplanke. 0.43 natyvioje Windows aplinkoje čia nevykdyta.

Vartotojo pateikta 0.40 Windows ataskaita patvirtino vieno DOCX failo sėkmingą ClamAV patikrą ir atskirą EICAR kontrolę. Tai nėra 0.43 patvirtinimas ar bendras aptikimo efektyvumo įrodymas.

Tai bandomoji, nepatvirtinta vienintelė antivirusinė. Nėra failų blokavimo prieš paleidimą, atminties / įkrovos / procesų elgsenos apsaugos, Windows saugos centro registracijos ar nepriklausomo efektyvumo patvirtinimo. Leidimas nepasirašytas Authenticode. Esamos antivirusinės neišjunkite.

Android 0.23 ir istorinis 1396 sumų sąrašas nepakeisti. Oficialus ClamAV ir bazės atsisiunčiami atskirai. Ankstesnių komponentų aprašymas: Service/README_LT.md; Windows priėmimo planas: WINDOWS_PATIKRA_LT.md.
