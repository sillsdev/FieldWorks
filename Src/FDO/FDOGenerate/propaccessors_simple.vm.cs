## --------------------------------------------------------------------------------------------
## Copyright (c) 2006-2013 SIL International
## This software is licensed under the LGPL, version 2.1 or later
## (http://www.gnu.org/licenses/lgpl-2.1.html)
##
## NVelocity template file
## This file is used by the FdoGenerate task to generate the source code from the XMI
## database model.
## --------------------------------------------------------------------------------------------
#set( $propComment = $prop.Comment )
#set( $propNotes = $prop.Notes )
#if ( $prop.IsHandGenerated)
#set( $generated = "_Generated" )
#else
#set( $generated = "" )
#end
#if( $prop.CSharpType == "???" )
		// Type not implemented in FDO
#elseif( $prop.Signature == "MultiString" || $prop.Signature == "MultiUnicode")
##
## No "set" property is needed; one "gets" the accessor and then can use that to set
## individual string alternates
#if ($prop.Signature == "MultiString")
#set( $cmd = "MultiString" )
#else
#set( $cmd = "MultiUnicode" )
#end
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ${prop.Name} Accessor.
#if ($propComment != "")
		///
$propComment
#end
		/// </summary>
#if ($propNotes != "")
		/// <remarks>
$propNotes
		/// </remarks>
#end
		/// ------------------------------------------------------------------------------------
#if( $prop.IsHandGenerated)
#if ( $prop.Signature == "MultiString" )
		private IMultiString ${prop.NiuginianPropName}$generated
#else
		private IMultiUnicode ${prop.NiuginianPropName}$generated
#end
#else
		[ModelProperty(CellarPropertyType.$cmd, $prop.Number, "$prop.CSharpType")]
#if ( $prop.Signature == "MultiString" )
		public IMultiString $prop.NiuginianPropName
#else
		public IMultiUnicode $prop.NiuginianPropName
#end
#end
		{
			get
			{
				lock (SyncRoot)
				{
					if (m_$prop.NiuginianPropName == null)
						m_$prop.NiuginianPropName = new ${prop.CSharpType}(this, $prop.Number);
				}
				return m_$prop.NiuginianPropName;
			}
		}
#elseif( $prop.Signature == "String")
##
## No "set" property is needed; one "gets" the accessor and then can use that to set
## individual string alternates
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get or set the ${prop.Name}
#if ($propComment != "")
		///
$propComment
#end
		/// </summary>
#if ($propNotes != "")
		/// <remarks>
$propNotes
		/// </remarks>
#end
		/// ------------------------------------------------------------------------------------
#if( $prop.IsHandGenerated)
		private ITsString $prop.NiuginianPropName$generated
#else
		[ModelProperty(CellarPropertyType.String, $prop.Number, "$prop.CSharpType")]
		public ITsString $prop.NiuginianPropName
#end
		{
			get
			{
				lock (SyncRoot)
				{
					if (m_${prop.NiuginianPropName} == null)
						return Cache.TsStrFactory.EmptyString(Cache.WritingSystemFactory.UserWs);
				}
				return m_${prop.NiuginianPropName};
			}
#if( $prop.IsSetterInternal)
			internal set
#else
			set
#end
			{
				var newValue = value;
				Validate${prop.NiuginianPropName}(ref newValue);
				var originalValue = m_$prop.NiuginianPropName;
				newValue = TsStringUtils.NormalizeNfd(newValue); // Store it as NFD in FDO.
				if (newValue == originalValue)
					return;
				else if (newValue != null && originalValue != null && newValue.Equals(originalValue))
					return;
				m_$prop.NiuginianPropName = newValue;
				((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterObjectAsModified(this, $prop.Number, originalValue, newValue);
				${prop.NiuginianPropName}SideEffects(originalValue, newValue);
			}
		}
		partial void Validate${prop.NiuginianPropName}(ref ITsString newValue);
		partial void ${prop.NiuginianPropName}SideEffects(ITsString originalValue, ITsString newValue);
#elseif( $prop.Signature == "TextPropBinary")
##
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the ${prop.Name}
#if ($propComment != "")
		///
$propComment
#end
		/// </summary>
#if ($propNotes != "")
		/// <remarks>
$propNotes
		/// </remarks>
#end
		/// ------------------------------------------------------------------------------------
#if( $prop.IsHandGenerated)
		private $prop.CSharpType $prop.NiuginianPropName$generated
#else
		[ModelProperty(CellarPropertyType.Binary, $prop.Number, "$prop.CSharpType")]
		public $prop.CSharpType $prop.NiuginianPropName
#end
		{
			get { return m_$prop.NiuginianPropName; }
#if( $prop.IsSetterInternal)
			internal set
#else
			set
#end
			{
				var newValue = value;
				Validate${prop.NiuginianPropName}(ref newValue);
				var originalValue = m_$prop.NiuginianPropName;
				if (newValue == originalValue)
					return;
				m_$prop.NiuginianPropName = newValue;
				((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterObjectAsModified(this, $prop.Number, originalValue, newValue);
				${prop.NiuginianPropName}SideEffects(originalValue, newValue);
			}
		}
		partial void Validate${prop.NiuginianPropName}(ref $prop.CSharpType newValue);
		partial void ${prop.NiuginianPropName}SideEffects($prop.CSharpType originalValue, $prop.CSharpType newValue);
#elseif( $prop.Signature == "Guid")
##
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the ${prop.Name}
#if ($propComment != "")
		///
$propComment
#end
		/// </summary>
#if ($propNotes != "")
		/// <remarks>
$propNotes
		/// </remarks>
#end
		/// ------------------------------------------------------------------------------------
#if( $prop.IsHandGenerated)
		private $prop.CSharpType $prop.NiuginianPropName$generated
#else
		[ModelProperty(CellarPropertyType.Guid, $prop.Number, "$prop.CSharpType")]
		public $prop.CSharpType $prop.NiuginianPropName
#end
		{
			get { return m_$prop.NiuginianPropName; }
#if( $prop.IsSetterInternal)
			internal set
#else
			set
#end
			{
				var newValue = value;
				Validate${prop.NiuginianPropName}(ref newValue);
				var originalValue = m_$prop.NiuginianPropName;
				if (newValue == originalValue)
					return;
				m_$prop.NiuginianPropName = newValue;
				((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterObjectAsModified(this, $prop.Number, originalValue, newValue);
				${prop.NiuginianPropName}SideEffects(originalValue, newValue);
			}
		}
		partial void Validate${prop.NiuginianPropName}(ref $prop.CSharpType newValue);
		partial void ${prop.NiuginianPropName}SideEffects($prop.CSharpType originalValue, $prop.CSharpType newValue);
#elseif( $prop.Signature == "Unicode")
##
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the ${prop.Name}
#if ($propComment != "")
		///
$propComment
#end
		/// </summary>
#if ($propNotes != "")
		/// <remarks>
$propNotes
		/// </remarks>
#end
		/// ------------------------------------------------------------------------------------
#if( $prop.IsHandGenerated)
		private $prop.CSharpType $prop.NiuginianPropName$generated
#else
		[ModelProperty(CellarPropertyType.Unicode, $prop.Number, "$prop.CSharpType")]
		public $prop.CSharpType $prop.NiuginianPropName
#end
		{
			get { return m_$prop.NiuginianPropName; }
#if( $prop.IsSetterInternal)
			internal set
#else
			set
#end
			{
				var newValue = value;
				Validate${prop.NiuginianPropName}(ref newValue);
				if (!string.IsNullOrEmpty(newValue))
					newValue = newValue.Normalize(NormalizationForm.FormD); // Store it as NFD in FDO.
				var originalValue = m_$prop.NiuginianPropName;
				if (newValue == originalValue)
					return;
				m_$prop.NiuginianPropName = newValue;
				((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterObjectAsModified(this, $prop.Number, originalValue, newValue);
				${prop.NiuginianPropName}SideEffects(originalValue, newValue);
			}
		}
		partial void Validate${prop.NiuginianPropName}(ref $prop.CSharpType newValue);
		partial void ${prop.NiuginianPropName}SideEffects($prop.CSharpType originalValue, $prop.CSharpType newValue);
#elseif( $prop.Signature == "Boolean")
##
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the ${prop.Name}
#if ($propComment != "")
		///
$propComment
#end
		/// </summary>
#if ($propNotes != "")
		/// <remarks>
$propNotes
		/// </remarks>
#end
		/// ------------------------------------------------------------------------------------
#if( $prop.IsHandGenerated)
		private $prop.CSharpType $prop.NiuginianPropName$generated
#else
		[ModelProperty(CellarPropertyType.Boolean, $prop.Number, "$prop.CSharpType")]
		public $prop.CSharpType $prop.NiuginianPropName
#end
		{
			get { return m_$prop.NiuginianPropName; }
#if( $prop.IsSetterInternal)
			internal set
#else
			set
#end
			{
				var newValue = value;
				Validate${prop.NiuginianPropName}(ref newValue);
				var originalValue = m_$prop.NiuginianPropName;
				if (newValue == originalValue)
					return;
				m_$prop.NiuginianPropName = newValue;
				((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterObjectAsModified(this, $prop.Number, originalValue, newValue);
				${prop.NiuginianPropName}SideEffects(originalValue, newValue);
			}
		}
		partial void Validate${prop.NiuginianPropName}(ref $prop.CSharpType newValue);
		partial void ${prop.NiuginianPropName}SideEffects($prop.CSharpType originalValue, $prop.CSharpType newValue);
#elseif( $prop.Signature == "Time")
##
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the ${prop.Name}
#if ($propComment != "")
		///
$propComment
#end
		/// </summary>
#if ($propNotes != "")
		/// <remarks>
$propNotes
		/// </remarks>
#end
		/// ------------------------------------------------------------------------------------
#if( $prop.IsHandGenerated)
		private $prop.CSharpType $prop.NiuginianPropName$generated
#else
		[ModelProperty(CellarPropertyType.Time, $prop.Number, "$prop.CSharpType")]
		public $prop.CSharpType $prop.NiuginianPropName
#end
		{
			get { return m_$prop.NiuginianPropName; }
#if( $prop.IsSetterInternal)
			internal set
#else
			set
#end
			{
				var newValue = value;
				Validate${prop.NiuginianPropName}(ref newValue);
				var originalValue = m_$prop.NiuginianPropName;
				if (newValue == originalValue)
					return;
				m_$prop.NiuginianPropName = newValue;
				((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterObjectAsModified(this, $prop.Number, originalValue, newValue);
				${prop.NiuginianPropName}SideEffects(originalValue, newValue);
			}
		}
		partial void Validate${prop.NiuginianPropName}(ref $prop.CSharpType newValue);
		partial void ${prop.NiuginianPropName}SideEffects($prop.CSharpType originalValue, $prop.CSharpType newValue);
#elseif( $prop.Signature == "GenDate")
##
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the ${prop.Name}
#if ($propComment != "")
		///
$propComment
#end
		/// </summary>
#if ($propNotes != "")
		/// <remarks>
$propNotes
		/// </remarks>
#end
		/// ------------------------------------------------------------------------------------
#if( $prop.IsHandGenerated)
		private $prop.CSharpType $prop.NiuginianPropName$generated
#else
		[ModelProperty(CellarPropertyType.GenDate, $prop.Number, "$prop.CSharpType")]
		public $prop.CSharpType $prop.NiuginianPropName
#end
		{
			get { return m_$prop.NiuginianPropName; }
#if( $prop.IsSetterInternal)
			internal set
#else
			set
#end
			{
				var newValue = value;
				Validate${prop.NiuginianPropName}(ref newValue);
				var originalValue = m_$prop.NiuginianPropName;
				if (newValue == originalValue)
					return;
				m_$prop.NiuginianPropName = newValue;
				((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterObjectAsModified(this, $prop.Number, originalValue, newValue);
				${prop.NiuginianPropName}SideEffects(originalValue, newValue);
			}
		}
		partial void Validate${prop.NiuginianPropName}(ref $prop.CSharpType newValue);
		partial void ${prop.NiuginianPropName}SideEffects($prop.CSharpType originalValue, $prop.CSharpType newValue);
#elseif( $prop.Signature == "Binary")
##
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the ${prop.Name}
#if ($propComment != "")
		///
$propComment
#end
		/// </summary>
#if ($propNotes != "")
		/// <remarks>
$propNotes
		/// </remarks>
#end
		/// ------------------------------------------------------------------------------------
#if( $prop.IsHandGenerated)
		private $prop.CSharpType $prop.NiuginianPropName$generated
#else
		[ModelProperty(CellarPropertyType.Binary, $prop.Number, "$prop.CSharpType")]
		public $prop.CSharpType $prop.NiuginianPropName
#end
		{
			get { return m_$prop.NiuginianPropName; }
#if( $prop.IsSetterInternal)
			internal set
#else
			set
#end
			{
				var newValue = value;
				Validate${prop.NiuginianPropName}(ref newValue);
				var originalValue = m_$prop.NiuginianPropName;
				if (newValue == originalValue)
					return;
				m_$prop.NiuginianPropName = newValue;
				((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterObjectAsModified(this, $prop.Number, originalValue, newValue);
				${prop.NiuginianPropName}SideEffects(originalValue, newValue);
			}
		}
		partial void Validate${prop.NiuginianPropName}(ref $prop.CSharpType newValue);
		partial void ${prop.NiuginianPropName}SideEffects($prop.CSharpType originalValue, $prop.CSharpType newValue);
#elseif( $prop.Signature == "Integer")
##
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the ${prop.Name}
#if ($propComment != "")
		///
$propComment
#end
		/// </summary>
#if ($propNotes != "")
		/// <remarks>
$propNotes
		/// </remarks>
#end
		/// ------------------------------------------------------------------------------------
#if( $prop.IsHandGenerated)
		private $prop.CSharpType $prop.NiuginianPropName$generated
#else
		[ModelProperty(CellarPropertyType.Integer, $prop.Number, "$prop.CSharpType")]
		public $prop.CSharpType $prop.NiuginianPropName
#end
		{
#if( $prop.OverridenType == "")
			get { return m_$prop.NiuginianPropName;}
#if( $prop.IsSetterInternal)
			internal set
#else
			set
#end
			{
				var newValue = value;
				Validate${prop.NiuginianPropName}(ref newValue);
				var originalValue = m_$prop.NiuginianPropName;
				if (newValue == originalValue)
					return;
				m_$prop.NiuginianPropName = newValue;
				((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterObjectAsModified(this, $prop.Number, originalValue, newValue);
				${prop.NiuginianPropName}SideEffects(originalValue, newValue);
			}
#else
			get { return ($prop.OverridenType)m_$prop.NiuginianPropName;}
#if( $prop.IsSetterInternal)
			internal set
#else
			set
#end
			{
				var newValue = value;
				Validate${prop.NiuginianPropName}(ref newValue);
				var originalValue = ($prop.CSharpType)m_$prop.NiuginianPropName;
				if (newValue == originalValue)
					return;
				m_$prop.NiuginianPropName = (int)newValue;
				((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterObjectAsModified(this, $prop.Number, (int)originalValue, (int)newValue);
				${prop.NiuginianPropName}SideEffects(originalValue, newValue);
			}
#end
		}
#if( $prop.OverridenType == "")
		partial void Validate${prop.NiuginianPropName}(ref int newValue);
		partial void ${prop.NiuginianPropName}SideEffects(int originalValue, int newValue);
#else
		partial void Validate${prop.NiuginianPropName}(ref $prop.CSharpType newValue);
		partial void ${prop.NiuginianPropName}SideEffects($prop.CSharpType originalValue, $prop.CSharpType newValue);
#end
#else
		Force Compiler failure for unknown data type.
#end
#if($prop.Name == "DateModified")
		// This class has DateModified, so we can update it.
		internal override void UpdateDateModifiedInternal()
		{
			DateModified = DateTime.Now;
		}
		// This class has DateModified, so it is the one we are looking for.
		internal override void CollectDateModifiedObjectInternal(HashSet<ICmObjectInternal> owners)
		{
			owners.Add(this);
		}
#end