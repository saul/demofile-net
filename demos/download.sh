#!/bin/bash

# Bail if any command fails
set -e
pushd `dirname "$0"`

sudo apt-get install unzip

curl 'https://demofiledemos.blob.core.windows.net/demoblobs/space-vs-forward-m1-ancient.zip' --output test-demos.zip

unzip -o test-demos.zip

# Clean up to reduce the size of the 'demos' directory
rm test-demos.zip

popd
