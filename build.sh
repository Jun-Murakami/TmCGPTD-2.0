#!/bin/bash

dotnet restore -r osx-x64
dotnet publish -c Release --self-contained -p:PublishSingleFile=false --runtime osx-x64
mkdir -p TmCGPTD.app/Contents/MacOS
cp -r bin/Release/net6.0/osx-x64/publish/* TmCGPTD.app/Contents/MacOS
cp Info.plist TmCGPTD.app/Contents/
mkdir TmCGPTD.app/Contents/Resources
cp TmCGPTD.icns TmCGPTD.app/Contents/Resources