## --------------------------------------------------------------------------------------------
## Copyright (C) 2006-2008 SIL International. All rights reserved.
##
## Distributable under the terms of either the Common Public License or the
## GNU Lesser General Public License, as specified in the LICENSING.txt file.
##
## NVelocity template file
## This file is used by the FdoGenerate task to generate the source code from the XMI
## database model.
## --------------------------------------------------------------------------------------------
		#region Data Members

#foreach( $prop in $class.Properties)
#if( $prop.Cardinality.ToString() == "Basic" )
#parse( "datamembers_simple.vm.cs" )

#elseif( $prop.Cardinality.ToString() == "Atomic" )
#parse( "datamembers_atomic.vm.cs" )

#else
#parse( "datamembers_rel.vm.cs" )

#end
#end
		#endregion Data Members