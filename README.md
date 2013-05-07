Tokay
======================

Tokay is an GUI subystem for .NET applications that uses HTML/Javascript to define the user
interface. It is modeled after Windows Presentation Foundation (WPF) and encourages the use of the
Model-View-View Model design pattern. The UI is rendered using the
[GeckoFX](https://bitbucket.org/geckofx) browser control. Knockout.js is used in the UI layer to
provide data binding support.

This is currently an experiment, not a stable product.

(A [Tokay](http://en.wikipedia.org/wiki/Tokay_gecko) is a large Thai gecko)

# Building

## On Windows

For the first time run build.bat. This will download the required Xulrunner (18.0.2), extract it
to the correct directory (lib\xulrunner) and compile the project.

Afterwards you can use `Tokay.sln` to build in Visual Studio 2010.

## On Linux

Simply run `build.sh`. This will download the required Firefox (18.0.2), extract it to the correct
directory (lib/xulrunner), compile the project and copy the dependencies to output/debug.

After Firefox is downloaded, you can also build in MonoDevelop by opening `Tokay.sln` and
compiling the solution.

# Running the sample

The sample app can be found in `output/debug`. On Linux it can be run with the `testapp` script.
