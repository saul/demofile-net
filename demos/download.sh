#!/bin/bash

# Bail if any command fails
set -e
pushd `dirname "$0"`

curl 'https://demofiledemos.blob.core.windows.net/demoblobs/navi-javelins-vs-9-pandas-fearless-m1-mirage.zip' --output test-demos.zip

unzip -o test-demos.zip

# Clean up to reduce the size of the 'demos' directory
rm test-demos.zip

popd
