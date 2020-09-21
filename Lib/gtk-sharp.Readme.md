# Readme for atk-sharp, gtk-sharp, and glib-sharp

Having these three assemblies in VCS is a hack that got necessary when moving to Mono 5.
For whatever reason when compiling, `msbuild` decided to get version 3.0 of these assemblies
from the GAC, no matter of the settings in the .csproj file. However, using version 3.0 doesn't work
at runtime. I tried a lot of things, but in the end gave up and decided to put these three
assemblies in the `Lib` folder. That has the additional benefit that we have these assemblies
available on Windows as well and so don't need conditional compilation for the Linux specific
file adapters.

It would be nicer if we could get these assemblies from a nuget package, but there is none
available for version 2.12, only for GTK# 3.

__NOTE:__ it is important that these three assemblies don't get copied to the output directory so that
the ones from the GAC get used and other dependencies will be found. This can be achieved by setting
`<private>False</private>` tag on the references.