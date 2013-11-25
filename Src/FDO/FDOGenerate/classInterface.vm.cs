## --------------------------------------------------------------------------------------------
## Copyright (c) 2006-2013 SIL International
## This software is licensed under the LGPL, version 2.1 or later
## (http://www.gnu.org/licenses/lgpl-2.1.html)
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
