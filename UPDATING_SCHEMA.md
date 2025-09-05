# Updating generated schema classes

## CS2

1. Find the latest CS2 demo on hltv.org: https://www.hltv.org/results?gameType=CS2
2. Open the DevTools and find the download link from the "Demo sponsored by Bitskins" element
3. Open the DevTools 'Network' tab and navigate to the download link from the previous step
4. Once redirected, right-click -> _Copy_ -> _Copy as cURL (bash)_
5. Remove the backslashes from the copied command
6. Create a new branch
7. Run the [`update-schema-cs2` workflow](https://github.com/saul/demofile-net/actions/workflows/update-schema-cs2.yml) on the branch. Use the copied cURL command from the previous step

## Deadlock

1. Create a new branch
2. Run the [`update-schema-deadlock` workflow](https://github.com/saul/demofile-net/actions/workflows/update-schema-deadlock.yml) on the branch
3. Accept the Steam Guard notification
