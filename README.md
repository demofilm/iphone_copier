# iPhone Copier

Visuele dual-pane browser (Avalonia 12 / .NET 9) om een gekopieerde iPhone-fotobibliotheek
(100+ mapjes zoals `100APPLE`, `101APPLE`…) in één map samen te voegen.

- **Links**: bronmap met boom van mapjes (vinkjes + aantallen) en thumbnails van de gekozen map.
- **Rechts**: doelmap met thumbnails van wat er al staat.
- **Verplaatsen of kopiëren**, lege mapjes opruimen, naamconflicten oplossen (`IMG_1_1.JPG`),
  identieke dubbelen overslaan (byte-voor-byte vergelijking). Opnieuw uitvoeren is veilig.
- Thumbnails voor JPG/PNG/GIF/BMP/WebP; HEIC en video tonen een placeholder-tegel (wel gewoon overgezet).

## Draaien
```
dotnet run --project src/IPhoneCopier
dotnet test
```

## Licentie, gebruik op eigen risico
**Freeware** - gratis te gebruiken en ongewijzigd te delen; niet verkopen/verhandelen en niet aanpassen of
aangepaste versies verspreiden zonder toestemming. Zie [LICENSE.txt](LICENSE.txt).
**Gebruik is op eigen risico**: de app kan bestanden verplaatsen. De maker is niet verantwoordelijk voor dataverlies of
andere problemen. Maak altijd eerst een back-up of werk op een kopie.

Bevalt het? [Buy me a beer](https://www.paypal.com/paypalme/lordleuter)
