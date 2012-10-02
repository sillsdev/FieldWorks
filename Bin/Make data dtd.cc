c MakeDataDtd.cc
c Ken Zook  22 Feb 00
c Turns a CM xml file into a section of a data DTD
c Required by hand:
c After running this table, run class.cc to convert @class to kclid number.
c  Add standard stuff
c Add the definition lines for CmObject
c Move all ENTITY lines to the top of the file
c  In ENTITY lines for superclass, insert all possible concrete subclasses + self if concrete
c  In ELEMENT lines for all subclasses, add all properties inherited from superclasses
c Add info for FwDatabase
c "List non-CmObject superclasses and subclasses.cc" is helpful in finding superclass/subclass relationships

define(WriteClassInfo) > nl nl
	'<!-- ****** ' out(ClassName) ' (' out(ClassAbbr) ') ' ifeq(ClassAbs) 'true' begin 'an abstract' end else begin 'a' end
	' subclass of ' out(ClassBase) ' ****** -->' nl
	append(Entities) '<!ENTITY % ' outs(ClassName) ' "' outs(ClassName) '" >' nl endstore
c	ifneq(ClassAbs) 'true'
c		begin
			'<!ELEMENT ' out(ClassName) ' ( ' out(PropNames) '%custom; )* >' nl
			'<!ATTLIST ' out(ClassName) ' id ID #IMPLIED >' nl
			'<!ATTLIST ' out(ClassName) ' ord CDATA #IMPLIED >' nl nl
			out(PropElements)
c		end
	ifneq(ClassAbs) 'true' begin append(ObjectClasses) outs(ClassName) ' | ' end
	store(PropNames,ClassName,ClassAbbr,ClassAbs,ClassBase,PropElements) endstore
	use(LookForClass)

group(LookForClass)
'<class' > use(ParseClass)
endfile > nl nl
	out(Entities) nl nl
	'<!ENTITY % CmObject "' out(ObjectClasses) '" >' nl nl
	endfile
'' > omit

group(ParseClass)
'id = "' > next
'id="' > store(ClassName)
'abbr = "' > next
'abbr="' > store(ClassAbbr)
'abstract = "' > next
'abstract="' > store(ClassAbs)
'num = "' > next
'num="' > store(ClassNum)
'base = "' > next
'base="' > store(ClassBase)
' ' >
nl >
'"' > endstore
'>' > store(PropPrefix) 'p' endstore use(ParseProps)

group(ParseProps)
'<basic' > use(ParseBasic)
'<rel' > use(ParseRel)
'<owning' > use(ParseOwning)
'</props>' > do(WriteClassInfo)
'</class>' > do(WriteClassInfo)
endfile > endfile
'' > omit

group(ParseBasic)
'id = "' > next
'id="' > store(PropName)
'card = "' > next
'card="' > store(PropCard)
'sig = "' > next
'sig = "MultiString"' > next
'sig="MultiString"' > store(PropSig) 'AStr' endstore set(PropRpt)
'sig = "MultiUnicode"' > next
'sig="MultiUnicode"' > store(PropSig) 'AUni' endstore set(PropRpt)
'sig = "String"' > next
'sig="String"' > store(PropSig) 'Str' endstore
'sig = "Unicode"' > next
'sig="Unicode"' > store(PropSig) 'Uni' endstore
'sig = "' > next
'sig="' > store(PropSig)
'prefix = "' > next
'prefix="' > store(PropPrefix)
'num = "' > next
'num="' > store(PropNum)
'access = "' > next
'access="' > store(Junk)
'big = "' > next
'big="' > store(Junk)
'max = "' > next
'max="' > store(Junk)
'min = "' > next
'min="' > store(Junk)
'prec = "' > next
'prec="' > store(Junk)
'scale = "' > next
'scale="' > store(Junk)
'enum = "' > next
'enum="' > store(Junk)
'bits = "' > next
'bits="' > store(Junk)
'readOnly = "' > next
'readOnly="' > store(Junk)
' ' >
nl >
'"' > endstore
'>' > append(PropNames) outs(PropName) '@' outs(ClassName) ' | '
	append(PropElements)
	'<!ELEMENT ' outs(PropName) '@' outs(ClassName) ' ( ' outs(PropSig) ' )' if(PropRpt) begin '*' end else begin '?' end ' >' nl
	store(PropName,PropCard,PropSig,PropNum,PropPrefix) 'p' endstore clear(PropRpt)
	use(ParseProps)

group(ParseRel)
'id = "' > next
'id="' > store(PropName)
'card = "' > next
'card="' > store(PropCard)
'kind = "' > next
'kind="' > store(PropKind)
'sig = "' > next
'sig="' > store(PropSig)
'access = "' > next
'access="' > store(Junk)
'readOnly = "' > next
'readOnly="' > store(Junk)
'lazyLoad = "' > next
'lazyLoad="' > store(Junk)
'sortBy = "' > next
'sortBy="' > store(Junk)
'prefix = "' > next
'prefix="' > store(PropPrefix)
'num = "' > next
'num="' > store(PropNum)
'inverse = "' > next
'inverse="' > store(Junk)
'inverseClass = "' > next
'inverseClass="' > store(Junk)
'basedOn = "' > next
'basedOn="' > store(Junk)
' ' >
nl >
'"' > endstore
'>' > append(PropNames) outs(PropName) '@' outs(ClassName) ' | '
	append(PropElements)
	'<!ELEMENT ' outs(PropName) '@' outs(ClassName) ifeq(PropKind) 'backref'
		begin
			' EMPTY'
		end
		else
		begin
			' ( Link )' ifeq(PropCard) 'atomic' begin '?' end else begin '*' end
		end
		 ' >' nl
	ifneq(PropCard) 'atomic'
		begin '<!ATTLIST ' outs(PropName) '@' outs(ClassName) ' size CDATA #IMPLIED >' nl end
	store(PropName,PropCard,PropSig,PropKind,PropNum,PropPrefix) 'p' endstore
	use(ParseProps)

group(ParseOwning)
'id = "' > next
'id="' > store(PropName)
'card = "' > next
'card="' > store(PropCard)
'sig = "' > next
'sig="' > store(PropSig)
'access = "' > next
'access="' > store(Junk)
'prefix = "' > next
'prefix="' > store(PropPrefix)
'num = "' > next
'num="' > store(PropNum)
'readOnly = "' > next
'readOnly="' > store(Junk)
'lazyLoad = "' > next
'lazyLoad="' > store(Junk)
'sortBy = "' > next
'sortBy="' > store(Junk)
' ' >
nl >
'"' > endstore
'>' > append(PropNames) outs(PropName) '@' outs(ClassName) ' | '
	append(PropElements)
	'<!ELEMENT ' outs(PropName) '@' outs(ClassName) ' ( %' outs(PropSig) '; )' ifeq(PropCard) 'atomic'
		begin '?' end else begin '*' end ' >' nl
	ifneq(PropCard) 'atomic'
		begin '<!ATTLIST ' outs(PropName) '@' outs(ClassName) ' size CDATA #IMPLIED >' nl end
	store(PropName,PropCard,PropSig,PropNum,PropPrefix) 'p' endstore
	use(ParseProps)
