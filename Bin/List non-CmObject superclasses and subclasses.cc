c List non-CmObject superclasses and subclasses.cc
c Ken Zook  26 Jun 99
c Converts CM xml files into a list of class names and non-CmObject superclasses

group(LookForClass)
'<class' > use(ParseClass)
endfile > dup
'' > omit

group(ParseClass)
'id="' > store(ClassName)
'abbr="' > store(ClassAbbr)
'abstract="' > store(ClassAbs)
'base="' > store(ClassBase)
' ' >
nl >
'"' > endstore
'>' > ifneq(ClassBase) 'CmObject' begin out(ClassBase) ' ' out(ClassName) nl end
	store(ClassName,ClassBase) endstore
	use(LookForClass)
