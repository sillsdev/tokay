TokaySharp
======================

TokaySharp makes it easier to use html/js/css as the fronted gui system for .net desktop applications.

It is currently a [GeckoFX](https://bitbucket.org/geckofx) control (WinForms) integrated with Knockoutjs for data binding.

This is currently an experiment, not a stable product.

(A [Tokay](http://en.wikipedia.org/wiki/Tokay_gecko) is a large Thai gecko)

# Building

## On Windows

For the first time run build.bat. This will download the required xulrunner (18.0.2), extract it
to the correct directory (lib\xulrunner) and compile the project.

Afterwards you can use `TokaySharp.sln` to build in Visual Studio 2010.

## On Linux

Simply run `build.sh`. This will download the required Firefox (18.0.2), extract it to the correct
directory (lib/xulrunner), compile the project and copy the dependencies to output/debug.

After Firefox got downloaded you can also build in MonoDevelop by opening `TokaySharp.sln` and
compiling the solution in MD.

# Running the sample

The sample app can be found `output/debug`. On Linux it can be run with the `testapp` script.
