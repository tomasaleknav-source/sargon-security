SARGON PROGRAMOS ATNAUJINIMU PARUOSIMAS - BANDOMOJI DALIS

Sis paketas dar NEIJUNGIA automatinio programos atnaujinimo. Pridetas
leidimo manifesto kurimo irankis, suderinamas su esama UpdateGate patikra.
Jis pasiraso versija, failo dydi ir SHA-256 suma, bet nieko nepublikuoja,
neatsisiuncia ir neįdiegia. Privataus rakto i ZIP ar serveri nedekite.

Irankis tik bandomasis: jis skaito neuzsifruota RSA privatu rakta ir netinka
tikram leidybos raktui saugoti. Gamybai reikia apsaugoto rakto ir saugios,
nepriklausomai patikrintos pasirasymo infrastruktūros.

Dar reikia patikimo HTTPS serverio, patvirtinto leidejo viešojo rakto,
automatinio kliento, saugaus diegimo, atkurimo po klaidu ir Windows bandymu.
Dabartine antivirusine apsauga turi likti ijungta.

Bandomajam naudojimui reikalingas Python 3, OpenSSL ir atskiras TESTINIS
RSA raktas. Pagalba: python create_update_manifest.py --help
