## --------------------------------------------------------------------------------------------
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
#if ($class.Name == "LgWritingSystem")
#set( $classSfx = "FactoryFdo" )
#else
#set( $classSfx = "Factory" )
#end

	#region ${className}$classSfx
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Generated implementation of: I${className}$classSfx
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal partial class ${className}$classSfx : I${className}$classSfx, IFdoFactoryInternal
	{
		private readonly FdoCache m_cache;

		/// <summary>
		/// Construction.
		/// </summary>
		/// <param name="cache"></param>
		internal ${className}$classSfx(FdoCache cache)
		{
			if (cache == null) throw new ArgumentNullException("cache");
			m_cache = cache;
		}

#if ($class.GenerateFullCreateMethod)
		/// <summary>
		/// Basic creation method for an $className.
		/// </summary>
		/// <returns>A new, unowned, $className.</returns>
		public I$className Create()
		{
#if ($class.IsSingleton)
			if (m_cache.ServiceLocator.GetInstance<I${className}Repository>().Singleton != null)
				throw new InvalidOperationException("Can not create more than one ${className}");
#end
			var newby = new $className();
			if (newby.OwnershipStatus != ClassOwnershipStatus.kOwnerRequired)
				((ICmObjectInternal)newby).InitializeNewOwnerlessCmObject(m_cache);
			return newby;
		}
#end

		/// <summary>
		/// Basic creation method for an ICmObject
		/// </summary>
		/// <returns>A new, unowned, ICmObject with a Guid, but no Hvo.</returns>
		public ICmObject CreateInternal()
		{
#if ($class.GenerateFullCreateMethod)
			return Create();
#else
			var newby = new $className();
			Debug.Assert(newby.OwnershipStatus != ClassOwnershipStatus.kOwnerProhibited);
			return newby;
#end
		}
	}
	#endregion ${className}$classSfx
