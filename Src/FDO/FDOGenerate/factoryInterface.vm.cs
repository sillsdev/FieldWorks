## --------------------------------------------------------------------------------------------
## Copyright (C) 2006-2009 SIL International. All rights reserved.
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
#if ($class.Name == "LgWritingSystem")
#set( $classSfx = "FactoryFdo" )
#else
#set( $classSfx = "Factory" )
#end

	#region I${className}$classSfx
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Generated interface for: I${className}$classSfx
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial interface I${className}$classSfx
#if ($class.GenerateFullCreateMethod)
		: IFdoFactory<I$className>
#end
	{
	}
	#endregion I${className}$classSfx
