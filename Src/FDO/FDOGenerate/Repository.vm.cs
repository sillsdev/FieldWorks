## --------------------------------------------------------------------------------------------
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

	#region ${className}Repository
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Generated implementation of: I${className}Repository
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal partial class ${className}Repository : FdoRepositoryBase<I${className}>, I${className}Repository
	{
#if ($class.IsSingleton)
		private I${className} m_cachedSingleton;
#end
		internal ${className}Repository(FdoCache cache, IDataReader dataReader) : base (cache, dataReader)
		{
		}

		/// <summary>
		/// Gets the class ID for this FDO object.
		/// </summary>
		protected override int ClassId
		{
			get { return $class.Number; }
		}

#if ($class.IsSingleton)
		/// <summary>
		/// Gets the one and only ${className} that is in this repository
		/// </summary>
		public I${className} Singleton
		{
			get
			{
				Debug.Assert(Count <= 1);
				Debug.Assert(m_cachedSingleton == null || m_cachedSingleton.IsValidObject);
				if (m_cachedSingleton == null)
					m_cachedSingleton = AllInstances().FirstOrDefault();
				return m_cachedSingleton;
			}
		}
#end
	}
	#endregion ${className}Repository
