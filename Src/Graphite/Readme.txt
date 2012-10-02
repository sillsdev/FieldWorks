						 Graphtite Library release 0.8


TOC	- Why Graphite Exists
	- How it Works
	- The Future

=============================================================================

						 Why Graphite exists.

Graphite is a Smart Font Rendering Technology designed specifically to help
meet the needs of a very large number of "minority language" communities for
local extensibility of complex script behaviors.  Discussion of this primary
need for extensibility will be limited to just the next paragraph before
moving on to technology issues.

There are, of course, many factors involved in a minority language
community's choise of "writing system" for their mother-tongue.  One
frequently prominent consideration is that mother-tongue literacy can greatly
increase the overall success rate of the community in pursuing opportunities
available in the closest 'language-of-education'.  Naturally this effect is
enhanced if the writing systems are very similar - but only if primary
literacy is not seriously impeaded by failing to adapt the writing system
sufficiently to represent the community's first language well.

Graphite was designed and developed because other smart font rendering
technologies which we know and love couldn't meet this need for practical
local extensibility.  The combination of Apple Type Services for Unicode
Imaging (ATSUI) along and Apple Advanced Typography (AAT) Font format has
basically the same theoretical degree of extensiblility as Graphite.
Unfortunately the lack of a high level, distributable, and readily modifable
form of description for the complex-script behaviors in AAT Fonts affords no
practical starting point for a local extention of those behaviors.  The
combination of Uniscribe and OpenType Fonts is limited to those complex
behaviors that Uniscribe supports.

The progress of support for OpenType in the FreeType project, the open
development of major-language rendering engines in the Pango Project, and the
OpenTypeLayoutEngine some bit of a GXLayoutEngine in the ICU Project are all
encouraging beginnings for Multi-Platform/Multi-Smart-Font-Rendering-
Technology text layout support.  Graphite's initial implimentation is for
Windows, because that is where we needed it most. But, to fully meet the need
it was designed for, support for this sort of practical local writing system
extensibility must ultimately become ubitiquous. That need could be met in
the reasonably near term by Graphite acting as a compliment to other smart
font rendering technologies.


						 How it works

The Graphite Description Language (GDL) makes it at least possible for a
font's script behavior to be distributed as "open source" independently of
the font itself.  The Graphite Compiler puts the readily modifiable GDL
description of writing system behavior into a table within the font in a
compiled form.  The Graphite rendering engine interprets the compiled GDL
table in a font and effectively encapsulates all of the specified writing
system behaviors as it provides services (typically) to a more general text
layout layer.

						 The Future

Graphite is an Open Source Project now.  Where it goes from here depends a
lot on the how the Open Source Community responds to it.  It also depends
somewhat on how (in the CPL's terminology) the "initial Contributor"
participates with that community.  We look forward to learning how to do that
well.

-----------------------------------------------------------------------------

See the "Release_Notes.txt" file for important information about this release.
