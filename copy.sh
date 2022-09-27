#!/usr/bin/env bash

ldd love/build/love | grep "=> /" | awk '{print $3}' | xargs -I '{}' cp -v '{}' ./native_lib/usr/lib/

pushd native_lib
rm native_lib_linux_x64.zip 
zip -r native_lib_linux_x64.zip ./usr/lib/
popd
