# Connection databases — B.17.3's company-connection route

*Surveyed 10/08/2026 during the part-B audit. Read through `op=dbase file=<path>`, which drives `PsDBaseDatabase`.*

> The manual's own HINT: *"create a database with frequently utilized and maybe **company-specific connections**, which are then always available to all program users within your company."*

⭐⭐ **The route to encoding Eretz Barzel's own connection standard.** These are plain dBASE files — the manual notes they can be edited with *"any standard dBASE editor"* — and `PsDBaseDatabase` exposes `PutRecord` and `AppendNewRecord` as well as reads.

⭐ **And they speak B.14's language.** `BasePlate.dbf` stores `HOLEX = "2*100"` and `HOLEY = "1*"` — the same drill-field layout syntax `drillfield x= y=` takes. The connection databases and the drilling API are one vocabulary.

Root: `C:\Program Files\Bentley\ProStructures Ss6 R1\AutoCAD 2015\Prg\Plugins`

| macro | file | records | fields |
|---|---|---:|---|
| `AECChute` | `AECChute.dbf` | 59 | `SHAPE`, `LENGTH`, `WIDTH`, `THICKNESS`, `OFFSETX`, `OFFSETY`, `BOLTTYPE`, `DIAMETER`, `WORKLOOSE`, `ANGLE`, `SHAPETYPE`, `SHAPESIZE`, `LOWERONLY`, `PITCH1`, `PITCH2`, `BRACEBYANG`, `DISTANCE`, `UPPEREND`, `LOWEREND` |
| `BasePlate` | `BasePlate.dbf` | 56 | `SHAPE`, `CODE`, `LENGTH`, `WIDTH`, `THICKNESS`, `DIAMETER`, `WORKLOOSE`, `HOLEX`, `HOLEY`, `AF`, `AS` |
| `BasePlateChinese` | `BasePlate.dbf` | 56 | `SHAPE`, `CODE`, `LENGTH`, `WIDTH`, `THICKNESS`, `DIAMETER`, `WORKLOOSE`, `HOLEX`, `HOLEY`, `AF`, `AS` |
| `BeamBeamClamp` | `BeamBeamClamp.dbf` | 5 | `DIAMETER`, `OFFSET` |
| `PipeStrap` | `PipeStrap.dbf` | 33 | `STANDARD`, `SIZE`, `LENGTH`, `WIDTH`, `DIAMETER`, `THREAD_LEN`, `THREAD_DIA`, `SHAPE_TYPE`, `SHAPE_SIZE` |
| `PurlinBeamBraceFly` | `PurlinBeamBraceFly.dbf` | 60 | `SHAPE`, `LENGTH`, `WIDTH`, `THICKNESS`, `OFFSETX`, `OFFSETY`, `BOLTTYPE`, `DIAMETER`, `WORKLOOSE`, `ANGLE`, `SHAPETYPE`, `SHAPESIZE`, `LOWERONLY`, `PITCH1`, `PITCH2`, `BRACEBYANG`, `DISTANCE`, `UPPEREND`, `LOWEREND` |

⚠️ **Read only for now.** These files live in `Program Files` and define how connections get built; `op=dbase` deliberately does not write. Adding EB's own connections is a decision for Amir, not a side effect of an audit.
