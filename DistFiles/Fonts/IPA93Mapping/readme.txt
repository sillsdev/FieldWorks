If you have existing files that contain SIL IPA93-encoded data, you may wish to convert them to Unicode, or convert from Unicode back to SIL IPA93. You can do this using the program TECkit.

The TECkit package is available from <http://scripts.sil.org/teckit>.

A compiled conversion table called "silipa93.tec" is included, which is set to map SIL IPA93 data to Unicode IPA and vice versa. This may be used with the DropTEC program from the TECkit package to convert plain-text files. To convert legacy data, double-click DropTEC, and then drag and drop silipa93.tec into the first box called "Mapping file" (or browse to select). Choose your desired UTF form for output, and normalization option if required. Then drag your SIL IPA93 data file to the box called "Legacy text file". The resulting data can be viewed with a Unicode font such as Doulos SIL.

The conversion table can also be used by other tools that use the TECkit mapping engine, such as the EncCnvtrs package for Microsoft Word.

The conversion description (mapping source) is provided in two forms. One is an XML description based on the Unicode Technical Report UTR22, with language extensions to support the contextual nature of the conversion, particular from Unicode to bytes. The other is a TECkit mapping source (.map) file, which can be used by the mapping table compiler in the TECkit package.

The XML description provides a specification of the mapping between SIL IPA93 and Unicode and may be used by developers to create or configure their own conversion engines. In addition there is an implementation of an engine capable of using the XML description directly, available from the author.

Martin Hosken
martin_hosken@sil.org

Updated by Jonathan Kew, 2004-07-20
jonathan_kew@sil.org
