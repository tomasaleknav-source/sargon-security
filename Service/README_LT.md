# SARGON 0.49 paslaugos paketas — bandomasis

SARGONProtection paslauga paleidžiama su Windows ir periodiškai tęsia pasirinktų vietinių diskų skenavimo eilę. Failų pokyčių įvykiai tikrinami po įrašymo; tai nėra vykdymo blokavimas.

## Masinių pakeitimų audito signalas

Jei per 15 sekundžių stebimi bent 100 skirtingų failų kelių, paslauga trumpam parodo perspėjimą, atnaujina viešos būsenos signalą ir išsaugo `Private/last-bulk-change.txt`. Keliai saugomi ribotai, o po perspėjimo jų nebekaupiama iki naujo lango. Signalas reiškia tik „daug failų pasikeitė“: procesas neidentifikuojamas, grėsmė nepatvirtinama, failai neblokuojami ir nešalinami. Atsarginės kopijos ar teisėtas paketinis darbas gali sukelti tą patį signalą.

## Variklio parengtis ir įvykių aprėptis

Paslaugos paleidimo metu `clamscan` turi nuskaityti neutralų kontrolinį failą su esamomis bazėmis. Vien CVD/CLD failų buvimas nelaikomas parengtimi. Patikra kartojama kas 24 val. ir po atnaujinimo. Nesėkmė palieka `DatabaseReady=false` ir blokuoja periodinį skenavimą, kol runtime parengtis vėl patvirtinama. Atskiras EICAR bandymas yra pasirinktinis ir nėra tikro aptikimo efektyvumo įrodymas.

`FileSystemWatcher` stebi konfigūruotas šaknis rekursyviai, taiko 4 s nusistovėjimo langą ir ribotą 512 įvykių eilę. Įvykių praradimas bei stebėjimo klaidos pažymimi kaip aprėpties spragos. Paslauga kaitalioja pakeistų failų patikrą su periodinės diskų eilės žingsniais, taiko apkrovos / maitinimo ribas ir veikia žemesniu prioritetu. Ji negali užblokuoti failo atidarymo ar vykdymo; sustabdytos paslaugos laikas nestebimas.

Radinys patenka į esamą administratoriaus peržiūros langą. Failas prieš naudotojo patvirtintą veiksmą tikrinamas iš naujo. Paslauga originalų automatiškai netrina; karantino ir atkūrimo veiksmai reikalauja atskiro patvirtinimo.

## Pasirašymas ir priėmimas

`release.json` yra 0.49 versijos manifestas. Pasirašymo skriptams reikia tikro leidėjo sertifikato ir privataus rakto; šis paketas jais nepasirašytas. `test-windows.cmd` vykdo host logikos bandymus, o `Service\\Invoke-NativeAcceptance.ps1` — pasirinktinius native Windows bandymus. Šioje aplinkoje build ir jokie Windows testai nevykdyti.

Tai bandomasis periodinio skenavimo bei perspėjimų komponentas, ne pilna antivirusinė ir ne Windows Security Center tiekėjas. Nenaudokite kaip vienintelės apsaugos.
