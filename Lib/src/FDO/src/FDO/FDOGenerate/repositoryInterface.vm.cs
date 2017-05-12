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
