#!/bin/bash

# Bail if any command fails
set -e
pushd "$(dirname "$0")"

curl 'https://pub-df0163da89b24187b28fd37c8dc7c8a1.r2.dev/demofile-net-demos-9.zip' --output test-demos.zip

unzip -o test-demos.zip

# Clean up to reduce the size of the 'demos' directory
rm test-demos.zip

popd
