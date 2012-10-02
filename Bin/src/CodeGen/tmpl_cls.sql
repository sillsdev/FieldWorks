$if(First)$
insert into Module$$ ([Id], [Name], [Ver], [VerBack])
	values(kmid${Module}, '${Module}', ${ModVer}, ${ModVerBack})
#if kmid${Module} == kmidCellar
insert into Class$$ ([Id], [Mod], [Base], [Abstract], [Name])
	values(kclidCmObject, kmidCellar, kclidCmObject, 1, 'CmObject')
#endif

$endif$
insert into Class$$ ([Id], [Mod], [Base], [Abstract], [Name])
	values(kclid${Class}, kmid${Module}, kclid${Base}, $if(Abstract)1$else0$endif, '${Class}')
