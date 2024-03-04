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
$ dotnet publish src --configuration Debug
$ dotnet run --project samples/hello/hello.tdlproj
```

## Special thanks
* [Minsk](https://github.com/terrajobst/minsk), from which this project is inspired.
* [SharpLab.io](https://sharplab.io/), where I learned the MSIL stuff.
