$foreach$
insert into [Field$$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(kflid${Class}_${PropName}, kcpt${PropType}, kclid${Class},
		$if(PropObject)$ kclid${PropDstType}$else$ null$endif, '${PropName}',0,Null, ${PropMin}, ${PropMax}, ${PropBig})
$endfor$
go
