# Layout.cs

This is a C# utility for creating simple GUI layouts based on containers.
It's heavily inspired by a C header library with the same purpose:https://github.com/randrew/layout

# How to use Layout.cs

Copy the Layout.cs file from this repository into your project.
use Program.cs as an example to get it set up.

# Features that aren't done
- Wrap (partially implemented, missing most edge cases)
- Max size (not started)
- min size isn't applied to expanding items and items within fill containers
- Layout.h doesn't have this, but storing custom data using the Layout.cs API instead of mapping item references to their data would be nice
- A complete test of every feature
- actual proper documentation