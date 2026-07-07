#!/usr/bin/env bash
# Nis backend-in me auto-reload: çdo ndryshim në kod e rinis serverin automatikisht.
cd "$(dirname "$0")"
dotnet watch run --launch-profile http
