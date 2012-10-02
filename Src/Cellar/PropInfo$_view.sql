if object_id('PropInfo$') is not null begin
	print 'removing proc PropInfo$'
	drop view [PropInfo$]
end
go
print 'creating view PropInfo$'
go
create view [PropInfo$] as
select convert(char(25), [Class$].[Name]) as [Class],
	convert(char(5), [Class$].[Id]) as [Clid],
	convert(char(25), [Field$].[Name]) as [Property],
	convert(char(8), [Field$].[Id]) as [Flid],
	convert(char(20), case [Field$].[Type]
		when 1 then 'Boolean'
		when 2 then 'Integer'
		when 3 then 'Numeric'
		when 4 then 'Float'
		when 5 then 'Time'
		when 6 then 'Guid'
		when 7 then 'Image'
		when 8 then 'GenDate'
		when 9 then 'Binary'
		when 13 then 'String'
		when 14 then 'MultiString'
		when 15 then 'Unicode'
		when 16 then 'MultiUnicode'
		when 17 then 'BigString'
		when 18 then 'MultiBigString'
		when 19 then 'BigUnicode'
		when 20 then 'MultiBigUnicode'
		when 23 then 'OwningAtom'
		when 24 then 'ReferenceAtom'
		when 25 then 'OwningCollection'
		when 26 then 'ReferenceCollection'
		when 27 then 'OwningSequence'
		when 28 then 'ReferenceSequence'
		else 'Unknown'
		end) as [Type],
	convert(char(3), [Field$].[Type]) as [Tid],
	convert(char(25), (select [Name] from [Class$] where [Class$].[Id] = [DstCls])) as [Signature],
	convert(char(5), case [Field$].[Custom]
		when 0 then 'false'
		when 1 then 'true'
		else 'BOGUS'
		end) as [Custom],
	convert(char(20), [Field$].[CustomId]) as [CustomId]
from [Field$] join [Class$]
	on [Field$].[Class] = [Class$].[Id]
go
