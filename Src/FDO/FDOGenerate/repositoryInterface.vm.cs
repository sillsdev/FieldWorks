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

	#region I${className}Repository
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Generated interface for: I${className}Repository
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial interface I${className}Repository : IRepository<I$className>
	{
#if ($class.IsSingleton)
		/// <summary>
		/// Gets the one and only ${className} that is in this repository
		/// </summary>
		I${className} Singleton { get; }
#end
	}
	#endregion I${className}Repository
