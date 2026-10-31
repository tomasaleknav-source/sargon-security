# SARGON Security Linux 0.1.0 Preview

Lietuviška darbalaukio sąsaja, kuri naudoja vietoje įdiegtą ClamAV failų ir pasirinktų aplankų patikrai.

## Diegimas

1. Išarchyvuokite paketą į savo Linux kompiuterį.
2. Atverkite terminalą tame aplanke ir paleiskite `./install.sh`.
3. Jei paprašys, įveskite savo Linux paskyros slaptažodį, kad būtų įdiegti ClamAV ir Tkinter paketai.
4. Programų meniu pasirinkite **SARGON Security**.
5. Pirmą kartą paspauskite **Atnaujinti bazę** ir patvirtinkite administratoriaus teises.

Jei `install.sh` nepaleidžiamas, terminale įvykdykite `chmod +x install.sh`, tada bandykite vėl.

## Ką ši peržiūros versija daro

- patikrina naudotojo pasirinktą failą arba aplanką su ClamAV;
- leidžia parsisiųsti oficialias ClamAV parašų duomenų bazes per `freshclam`;
- pateikia aiškų rezultatą ir vietinį patikros žurnalą;
- rodo, ar aptiktas ClamAV on-access procesas.

## Ribos

Tai dar nėra pilna antivirusinė programa. Šis leidimas neatlieka viso disko patikros fone, pats nekarantinuoja failų ir negarantuoja, kad švariu pažymėtas failas yra saugus. Aptiktą įtartiną failą automatiškai trinti nebandykite; rezultatą patikrinkite atskirai.

Realaus laiko patikrą ClamAV Linux sistemoje gali teikti `clamd` kartu su `clamonacc`, naudojančiu kernelio `fanotify`. Jų konfigūracija priklauso nuo distribucijos ir stebimų kelių. Šiame leidime ji automatiškai neįjungiama, nes neteisingai parinktas visos sistemos stebėjimas gali sutrikdyti darbą. Žr. oficialų [ClamAV On-Access vadovą](https://docs.clamav.net/manual/OnAccess.html).

## Testai

`python3 -m unittest discover -s . -p 'test_*.py'`

Palaiko įprastas Linux distribucijas su Python 3.10+, Tkinter ir ClamAV; diegiklis atpažįsta apt, dnf, pacman ir zypper paketų tvarkykles.
