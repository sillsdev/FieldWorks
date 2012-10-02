The original readme.txt file was missing in Perforce.

I (EberhardB) added this file to describe the additional steps I had to take to get this to compile:

1. Build Perl. To do this, get the Perl source code from www.cpan.org.
	Unzip and edit <perlsrc>\Win32\Makefile. I had to make the following changes:

	a)  set CCTYPE to MSVC90 to get it to use the VS.Net 2008 compiler
	b)  It is also necessary to comment out the USE_PERLIO switch so that Perl will use the MSVCRT
		from the compiler (if you don't, certain aspects of the hand-shake between the Perl wrapper
		and the PerlEC DLL won't work)
	c)  Because this is the 2nd release of the PerlEC addin to use the Perl510 distribution, BUT
		using different versions of the run-time DLL (FW 5.4.1 used MSVCRT8 from VS.Net 2005, while
		FW 6.0 used MSVCRT9 from VS.Net 2008), it is necessary to install the newer perl510.dll in
		a different folder and have that in the path (see .\Installer\PerlEC.mm.wxs for details.

	To build the Perl510.dll, you need to Open a cmd window, cd to <perlsrc>\Win32 and do a nmake
	After that succeeded you can do a nmake test

2. Copy Perl510.dll to <fw>\Distfiles
3. Copy perl.exe.manifest to the PXPerlWrapDll directory (I'm not sure what this is... ask Eberhard)
4. Build PXPerlWrap DLLs:
	- In Visual Studio add the following include paths (Tools/Options/Projects and Solutions/VC++ Dirs):
		<perlsrc>
		<perlsrc>\Win32
		<perlsrc>\Win32\Include
	- Add the following Library files directory:
		<perlsrc>
	- Build with configuration "Debug Unicode". This creates <fw>\Distfiles\PXPerlWrap-ud.dll
		and <fw>\Lib\debug\PXPerlwrap-ud.lib
	- Build with configuration "Release Unicode". This creates <fw>\Distfiles\PXPerlWrap-u.dll
		and <fw>\Lib\release\PXPerlwrap-u.lib
