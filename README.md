# The Todl Programming Language
[![Main build](https://github.com/ChrisKXu/todl/actions/workflows/main_build.yml/badge.svg)](https://github.com/ChrisKXu/todl/actions/workflows/main_build.yml)

Todl is an experimental, general-purpose programming language that is built and runs on the .NET platform.

## System requirements
Todl can be run anywhere where .NET is supported, even on a Raspberry Pi or inside a docker container! Please refer to [this](https://github.com/dotnet/core/blob/main/release-notes/8.0/supported-os.md) page on the complete list of supported platforms and operating systems.

## Building and testing
Please make sure latest .NET 8 is installed and then run
```bash
$ dotnet build src
```
to build the project. Alternatively you can run
```bash
$ dotnet test src
```
to run all the tests.

## Running the samples
A great way to play with Todl is by running the samples. To do so, please make sure that the latest .NET 8 is installed and then run the following
```bash
$ dotnet publish src
$ dotnet run --project samples/hello/hello.tdlproj
```

## Special thanks
This project is inspired by the [Minsk](https://github.com/terrajobst/minsk) project. I would like to give a big shout-out to @terrajobst for his [videos](https://www.youtube.com/playlist?list%253DPLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y) that taught me how to build a compiler from sratch!
