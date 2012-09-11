# SED script to extract iids from generated COM header files for use on Linux
# called from extract_iids.cmd

s/\r$//
/struct __declspec(uuid("/{
	h
	n
	# Remove C comments
	s|/\*.*\*/||g
	# Remove trailing semi-colons
	s/;$//
	# Get rid of everything after colons (inheritances)
	# s/[ \t]*:.*\n//
	s/:.*//
	G
	# Prepend beginning of libcom-style GUID definition
	s/^[ \t]*/DEFINE_UUIDOF(/
	s/[ \t]*struct __declspec(uuid(/,/
	s/)$/;/
	# Change to static initialization
	s/"\(........\)-\(....\)-\(....\)-\(....\)-\(............\)"/0x\1,0x\2,0x\3,{\4\5}/
	s/{\(..\)\(..\)\(..\)\(..\)\(..\)\(..\)\(..\)\(..\)}/0x\1,0x\2,0x\3,0x\4,0x\5,0x\6,0x\7,0x\8/
	p
}
/MIDL_INTERFACE("/{
	h
	n
	G
	s/[ \t]*:.*\n//
	s/^[ \t]*/DEFINE_UUIDOF(/
	s/[ \t]*MIDL_INTERFACE(/,/
	s/$/;/
	s/"\(........\)-\(....\)-\(....\)-\(....\)-\(............\)"/0x\1,0x\2,0x\3,{\4\5}/
	s/{\(..\)\(..\)\(..\)\(..\)\(..\)\(..\)\(..\)\(..\)}/0x\1,0x\2,0x\3,0x\4,0x\5,0x\6,0x\7,0x\8/
	p
}
/DECLSPEC_UUID("/{
	h
	n
	G
	s/^/DEFINE_UUIDOF(/
	s/class DECLSPEC_UUID(/,/
	s/[ \t]*;.*\n//
	s/$/;/
	s/"\(........\)-\(....\)-\(....\)-\(....\)-\(............\)"/0x\1,0x\2,0x\3,{\4\5}/
	s/{\(..\)\(..\)\(..\)\(..\)\(..\)\(..\)\(..\)\(..\)}/0x\1,0x\2,0x\3,0x\4,0x\5,0x\6,0x\7,0x\8/
	p
}
