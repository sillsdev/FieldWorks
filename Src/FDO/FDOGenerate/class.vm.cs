## --------------------------------------------------------------------------------------------
## Copyright (c) 2006-2013 SIL International
## This software is licensed under the LGPL, version 2.1 or later
## (http://www.gnu.org/licenses/lgpl-2.1.html)
##
## NVelocity template file
## This file is used by the FdoGenerate task to generate the source code from the XMI
## database model.
## --------------------------------------------------------------------------------------------
## We omit the constructor they need to create a new, underlying db object of this
## type if it is 'abstract' in the UML model
#set( $className = $class.Name )
#set( $classComment = $class.Comment )
#set( $classNotes = $class.Notes )

	#region ${className} (I${className})
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Generated, partial class for a ${className}
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
	[ModelClass($class.Number, "I${className}")]
#if ( $className == "CmObject" )
	internal abstract partial class $className : I${className}
#else
#if( $class.IsAbstract)
	internal abstract partial class $className : $class.BaseClass.Name,  I${className}
#else
	internal partial class $className : $class.BaseClass.Name,  I${className}
#end
#end
	{
#if ( $class.Properties.Count > 0)
## Data members
#parse( "datamembers.vm.cs" )

#end
## Constructors
#parse( "Constructors.vm.cs" )

## Property Accessors
#parse( "propertyAccessors.vm.cs" )
#if ( $className != "CmObject" && $class.Properties.Count > 0)

## Other methods
#parse( "OtherMethods.vm.cs" )

#end
	}
	#endregion I${className}
