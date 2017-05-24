## --------------------------------------------------------------------------------------------
## Copyright (c) 2006-2013 SIL International
## This software is licensed under the LGPL, version 2.1 or later
## (http://www.gnu.org/licenses/lgpl-2.1.html)
##
## NVelocity template file
## This file is used by the LcmGenerate task to generate the source code from the XMI
## database model.
## --------------------------------------------------------------------------------------------
#set( $className = $class.Name )
#set( $baseClassName = $class.BaseClass.Name )
#if ($class.Name == "LgWritingSystem")
#set( $classSfx = "FactoryLcm" )
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
		: ILcmFactory<I$className>
#end
	{
	}
	#endregion I${className}$classSfx
