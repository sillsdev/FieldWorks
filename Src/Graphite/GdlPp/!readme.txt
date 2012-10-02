Alan Ward 10/11/2000:
These files originally came from the Internet. Daniel Stenberg created a C preprocessor for his FrexxEd program, called cpp. These original files are in cpp-1.5.tar.gz. See these URLs:

http://daniel.haxx.se/
http://www.contactor.se/~dast/stuff/
http://www.contactor.se/~dast/source.html

The above source.html site says, "Sources written by me, Daniel Stenberg, alone or together with others. They're all written in C. Use whatever you want, but telling me you're using my stuff would be greatly appriciated!"

Stephen McConnel modifed cpp to properly handle GDL syntax. The files which he modified are:
cpp1.c-orig
cpp2.c-orig
cpp3.c-orig
cpp6.c-orig
cppdef.h-orig
usecpp.c-orig

After you compile GDLPP.exe, put it somewhere on your path. GrCompiler.exe uses it as part of the compilation process.

Alan Ward 10/13/2000:
I notified the author on 10/12/2000 (see below). The file CppDoc.pdf contains documentation for the original program. See below for the main change we made to it.

I found an undocumented feature in the source code. An environment variable CPP_PREFS is examined to set command line switches. I changed this to GDLPP_PREFS. The installer can use it to set the -I switch to specify where the stddef.gdh file is located.

*****
from: Alan Ward
to: daniel@haxx.se
date: 10/12/2000
subj: Re: Use of FrexxEd cpp program

AW> I am trying to contact the Daniel Stenberg who wrote FrexxEd. Are you the right one?

DS> That's me!

Great! On your website (http://www.contactor.se/~dast/source.html) you say, "Use whatever you want, but telling me you're using my stuff would be greatly appriciated!" It would be helpful if your e-mail address was there. :-)

AW> We simply want to inform him we are using the cpp program he wrote (with some slight modifications), as he asked.

DS> Any modifications I would be interested in?

We are developing a software component for rendering writing systems around the world. It is somewhat like Apple's TrueType GX technology (and a little like OpenType and Uniscribe) but more elaborate. We have a web page (http://www.sil.org/computing/graphite) describing the system. Part of the system is a higher level language for describing writing system behaviors. We needed something like the C preprocessor to handle macro definitions and include files. Our problem is that we use the '.' character in a way that conflicts with C. The standard C preprocessor thought we were using floating point numbers when we weren't (since we don't have floating point numbers in our language). We altered your cpp code to handle our syntax. Since this is very specific to our language, I don't think anybody would really be interested in our changes - it's basically a C preprocessor with floating point support eliminated.

> Thanks for letting me know.

Thank you for making your code available.

Alan Ward
SIL International
7500 W Camp Wisdom Rd
Dallas TX 75236
alan_ward@sil.org
