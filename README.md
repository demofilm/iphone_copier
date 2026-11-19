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
