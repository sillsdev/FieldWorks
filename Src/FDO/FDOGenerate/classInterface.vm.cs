## --------------------------------------------------------------------------------------------
## Copyright (C) 2006-20009 SIL International. All rights reserved.
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
#set( $classComment = $class.Comment )
#set( $classNotes = $class.Notes )

	#region I$className
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Generated interface for: I$className
#if ($classComment != "")
	///
$classComment
#end
	/// </summary>
#if ($classNotes != "")
	/// <remarks>
$classNotes
	/// </remarks>
#end
	/// ----------------------------------------------------------------------------------------
#if ( $className == "CmObject" )
	public partial interface I$className
#else
	public partial interface I$className : I$baseClassName
#end
	{
#foreach( $prop in $class.Properties)
#parse( "propertyInterface.vm.cs" )

#end
	}
	#endregion I$className
