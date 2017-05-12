## --------------------------------------------------------------------------------------------
## Copyright (c) 2006-2013 SIL International
## This software is licensed under the LGPL, version 2.1 or later
## (http://www.gnu.org/licenses/lgpl-2.1.html)
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