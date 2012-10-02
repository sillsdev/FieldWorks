## --------------------------------------------------------------------------------------------
## Copyright (C) 2006 SIL International. All rights reserved.
##
## Distributable under the terms of either the Common Public License or the
## GNU Lesser General Public License, as specified in the LICENSING.txt file.
##
## NVelocity template file
## This file is used by the FdoGenerate task to generate the source code from the XMI
## database model.
## --------------------------------------------------------------------------------------------
#set( $className = $class.Name )
#set( $baseClassName = $class.BaseClass.Name )

	#region Interface for $className
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Generated interface for: $className
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial interface I$className : I$baseClassName
	{
		#region Property Accessors (I$className)
#foreach( $prop in $class.Properties)
#if( $prop.Cardinality.ToString() == "Basic" )
				#parse( "propaccessors_simple_interface.vm.cs" )
#elseif( $prop.Cardinality.ToString() == "Atomic" )
				#parse( "propaccessors_simple_interface.vm.cs" )
				#parse( "propaccessors_atomic_interface.vm.cs" )
#else
				#parse( "propaccessors_rel_interface.vm.cs" )
#end
#end
		#endregion Property Accessors (I$className)
	}
	#endregion Interface for $className
