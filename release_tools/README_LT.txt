SARGON PROGRAMOS ATNAUJINIMAI

Automatinio leidimo eiga aprašyta saugyklos README_LT.md ir vykdoma
.github/workflows/signed-release.yml. Klientas tikrina viešą GitHub Release,
patikrina RSA/SHA-256 parašą ir diegiklio dydį bei SHA-256, tada prieš
diegimą prašo naudotojo sutikimo.

Leidimo pasirašymo raktas turi būti RSA 2048–8192 bitų ir užšifruotas.
Viešąjį SubjectPublicKeyInfo DER failą `update-publisher.der` galima laikyti
saugykloje. Privataus rakto niekada nedėkite į ZIP, Git ar GitHub failus.
GitHub Actions naudoja paslaptis SARGON_UPDATE_PRIVATE_KEY_B64 ir
SARGON_UPDATE_PRIVATE_KEY_PASSWORD. Privačiojo rakto bazė64 kodavimas nėra
šifravimas; saugokite patį privatų raktą užšifruotą stipriu slaptažodžiu.

Manifesto kūrimo įrankiui reikia Python 3, OpenSSL ir šių aplinkos kintamųjų:
  SARGON_UPDATE_PRIVATE_KEY_PASSWORD

Įrankis pasirašo leidimo versiją, galiojimo laiką, failo dydį ir SHA-256.
Jis pats nieko nepublikuoja ir neįdiegia. Tik Windows workflow sukuria
GitHub Release. Prieš platinimą būtini Windows bandymai; dalinio diegimo
atkūrimas dar neįgyvendintas.
