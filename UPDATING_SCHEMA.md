# Updating generated schema classes

1. Create a new branch
2. Run the [`update-schema` workflow](https://github.com/saul/demofile-net/actions/workflows/update-schema.yml) on the branch
3. Start a new game of CS2
4. Enable `tv_record_immediate 1`, then record a short HLTV demo with `tv_record hltv` and then `tv_stoprecord`
5. Run `DemoFile.SdkGen`, passing in the path to both the recorded `hltv.dem` demo (from step 4) and the path to the game project (e.g. `./src/DemoFile.Game.Cs`). \
   This will update the auto-generated `Schema.cs` for the game that you ran for
6. Commit the changes to a branch.
