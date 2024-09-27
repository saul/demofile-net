# Updating generated schema classes

1. Build https://github.com/saul/cs2schemagen - note you may need to change the path in [main.cpp](https://github.com/saul/CS2SchemaGen/blob/main/src/plugin/main.cpp#L42) depending on the game you're building for
2. Copy the resulting `server.dll` to: `...\Steam\steamapps\common\Deadlock\game\citadel\addons\bin\win64` (you will need to create this directory the first time)
3. Update the game's `gameinfo.gi` to add the `addons` directory to the search path:
   ```diff
            //
            // Search paths are relative to the exe directory\..\
            //
            SearchPaths
            {
   +            Game				citadel/addons
                Game				citadel
                Game				core
            }
   ```
4. Launch the game in `-insecure` mode
5. Open the console and write `schema_dump_all "C:\path-to-your-demofile-net-checkout\src\DemoFile.Game.Deadlock\Schema"` - this will write the JSON files
6. Start a new game with `map street_test`
7. Enable `tv_record_immediate 1`, then record a short HLTV demo with `tv_record deadlock_hltv` and then `tv_stoprecord`
8. Run `DemoFile.SchemaGen`, passing in the path to both the schema directory (as in step 5) and the path to the recorded `deadlock_hltv.dem` path (from step 7)
9. This will update the auto-generated `Schema.cs` for the game that you ran for
10. Commit the changes to a branch. \
    N.B. before committing, please reformat the JSON files using e.g. JetBrains Rider "Reformat Code" to minimise the diff
