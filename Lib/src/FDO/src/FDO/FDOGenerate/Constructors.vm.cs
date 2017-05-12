## --------------------------------------------------------------------------------------------
## Copyright (c) 2007-2013 SIL International
## This software is licensed under the LGPL, version 2.1 or later
## (http://www.gnu.org/licenses/lgpl-2.1.html)
##
## NVelocity template file
## This file is used by the FdoGenerate task to generate the source code from the XMI
## database model.
## --------------------------------------------------------------------------------------------
## This will generate the various methods used by the FDO aware ISilDataAccess implementation.
## Using these methods avoids using Reflection in that implementation.
##
		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="I${className}"/> class.
		/// Used for code like this foo.blah = new ${className}().
		/// </summary>
#if( $class.IsAbstract)
		/// <remarks>new underlying-object constructor limited to subclasses because this class
		/// is abstract in the UML.</remarks>
		/// ------------------------------------------------------------------------------------
		protected ${className}()
#else
		/// ------------------------------------------------------------------------------------
#if ($className == "LgWritingSystem" || $className == "LangProject")
		internal ${className}()
#else
		internal ${className}()
#end
#end
			: base()
		{
			SetDefaultValuesInConstruction();
		}

		/// <summary>
		/// Constructor for bootstrapping objects with fixed Guids.
		/// </summary>
		/// <remarks>
		/// This is not to be used for anything but the BootstrapNewSystem method of the backend provider.
		/// </remarks>
		internal ${className}(FdoCache cache, int hvo, Guid guid)
#if ($className != "CmObject" )
			: base(cache, hvo, guid)
#end
		{
#if ($className == "CmObject" )
			m_cache = cache;
			m_hvo = hvo;
			m_guid = Services.GetInstance<ICmObjectIdFactory>().FromGuid(guid);
			((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterObjectAsCreated(this);
#end
			SetDefaultValuesInConstruction();
		}
		partial void SetDefaultValuesInConstruction();

		#endregion Constructors