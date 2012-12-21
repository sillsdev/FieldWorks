JohnT: This file contains some information on building the Enchant DLLs which we use in FieldWorks. I am not at all happy with the current state of things, but it is the best I have had time to do.

To build the current version of Enchant on Windows (the same as the one we build for Linux), start by executing ExtractSourcesAndApplyWindowsPatches.sh in a git bash window.  This will extract the pristine sources in fieldworks_1.6.1.orig.tar.gz to a subdirectory named x/fieldworks-1.6.1 and then apply both the Windows specific patches found in "Enchant patch.patch", and the common patches found in fieldworks-1.6.1/debian/patches.

Once you have that source, you can find a file I have updated under msvc/Build.win32.readme. (The same information can be read at the start of the patch file.) This gives somewhat involved instructions for getting, patching, and building glib, a library on which enchant depends, and then Enchant itself. Unfortunately, based on an earlier version of the readme, it may be essential to build glib with the exact same version of the C libraries as Enchant, so you can't just get a binary. I don't know for sure whether this is still true. If you can find the glib repository and make your changes relative to that and produce a patch file, please check it in and update these instructions!

The patches to glib are all to repair an apparently out-of-date and incomplete Visual Studio build process. It would be good to try to get these submitted to glib. I don't recall exactly why I worked from glib 2.26.1 or whether this is the latest or why I did not manage to connect my glib work to a repository so I could make a patch. I was very much feeling my way, and moved on once I got something minimally working to meet our needs. Sorry. Some further work might be needed, because I did not manage to get the VS build to do something that apparently it or some other build once did to generate certain include files, I think with updated version information. I just manually copy the pattern file to the active file and manually fix the version.

The patches to Enchant are partly to get it to build with VS 2008 and a more-current version of glib, and partly to make overrides (words specified explicitly as correct or incorrect) case-sensitive, so that for example telling the system that 'bill' is correct does not imply that 'Bill' is correct. The build fixes might be possible to get into the trunk if we could sort out the glib dependency. Again, manual tweaking of version number is necessary (vitally so...if you leave them unchanged, our installer patch mechanism will be broken).

It might be good to check in all these files in our own source tree, but Enchant is 1,148 files (111MB), while glib is another 2128 files and 119MB, and I hate to have a permanent duplicate not linked to the main repositories of these projects.

Once again, sorry this is such a mess.

JohnT
