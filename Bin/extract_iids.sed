# SED script to extract iids from generated COM header files for use on Linux
# called from extract_iids.cmd

/struct __declspec(uuid("/{
	h
	n
	# Remove C comments
	s|/\*.*\*/||g
	# Remove trailing semi-colons
	s/;\r$//
	# Get rid of everything after colons (inheritances)
	# s/[ \t]*:.*\n//
	s/:.*//
	G
	# Prepend beginning of libcom-style GUID definition
	s/^[ \t]*/template<> const GUID __uuidof(/
	s/[ \t]*struct __declspec(uuid/)/
	s/)\r$/;/
	p
}
/MIDL_INTERFACE("/{
	h
	n
	G
	s/[ \t]*:.*\n//
	s/^[ \t]*/template<> const GUID __uuidof(/
	s/[ \t]*MIDL_INTERFACE/)/
	s/$/;/
	p
}
/DECLSPEC_UUID("/{
	h
	n
	G
	s/^/template<> const GUID __uuidof(/
	s/class DECLSPEC_UUID/)/
	s/[ \t]*;.*\n//
	s/$/;/
	p
}
