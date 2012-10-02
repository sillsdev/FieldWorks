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
## Note: we never want this to be abstract, 'cause someone should be able to instantiate it if
## they don't know the actual class, the just know an hvo
## However, we omit the constructor they need to create a new, underlying db object of this
## type if it is 'abstract' in the UML model
#set( $className = $class.Name )

	#region ${className}
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Generated, partial class for wrapping a ${className}
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class $className : $class.BaseClass.Name, I$className
	{
		/// <summary>the Class$ number of this FieldWorks class </summary>
		/// <remarks>this version can be used in static contexts, where you do not have an
		/// instance of the class.</remarks>
		public static new readonly int kClassId = $class.Number;

		/// <summary>the Class$ number of this FieldWorks class </summary>
		/// <remarks>this version can be used in switch statements</remarks>
		public const int kclsid${className} = $class.Number;

		/// <summary>the Class$ number of this FieldWorks class as a string</summary>
		public const string kclsid${className}String = "$class.Number";

		/// <summary> the name of the SQL View which gives the basic information for this class</summary>
		public static new readonly string FullViewName = "${className}_";

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Instantiates the object with the correct class, either this one or a  subclass of
		/// this one.
		/// </summary>
		/// <param name="cache">The FDO cache.</param>
		/// <param name="hvo">The hvo.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static new I$className CreateFromDBObject(FdoCache cache, int hvo)
		{
			int cls = cache.GetClassOfObject(hvo);
			I$className newObject = null;
			if (cls == kClassId && !cache.GetAbstract(cls))
			{
				newObject = new ${className}(cache, hvo) as I$className;
			}
			else
			{
				ICmObject cmObj = CmObject.CreateFromDBObject(cache, hvo);
				if (!(cmObj is I$className))
					throw new Exception(string.Format(
						"Tried to create an FDO object based on db object(hvo={0}, class={1}), " +
						"but that class is not fit in this signature (${className})", hvo, cls));
				newObject = cmObj as I$className;
			}
			return newObject;
		}

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:${className}"/> class.
		/// Used for code like this foo.blah = new ${className}().
		/// </summary>
#if( $class.IsAbstract )
		/// <remarks>new underlying-object constructor limitted to subclasses because this class
		/// is abstract in the UML.</remarks>
		/// ------------------------------------------------------------------------------------
		protected ${className}()
#else
		/// ------------------------------------------------------------------------------------
		public ${className}()
#end
			: base()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:${className}"/> class.
		/// </summary>
		/// <param name="cache">The FDO cache.</param>
		/// <param name="hvo">The hvo.</param>
		/// ------------------------------------------------------------------------------------
		public ${className}(FdoCache cache, int hvo)
			:base(cache, hvo)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:${className}"/> class.
		/// Use this constructor where you want to have control over loading/validating objects.
		/// </summary>
		/// <param name="cache">The FDO cache.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="fCheckValidity"><c>true</c> to check if HVO is valid.</param>
		/// <param name="fLoadIntoCache"><c>true</c> to load into cache.</param>
		/// ------------------------------------------------------------------------------------
		public ${className}(FdoCache cache, int hvo, bool fCheckValidity, bool fLoadIntoCache)
			:base(cache, hvo, fCheckValidity, fLoadIntoCache)
		{
		}
		#endregion // Constructors

		#region Definition for Fields
		// ==========================================================================
		/// <summary>
		/// Field Tags (${className})
		/// </summary>
		public enum ${className}Tags: int
		{
#foreach( $flid in $class.Properties)
			/// <summary>${flid.Name}: $flid.Signature</summary>
			kflid${flid.Name} = ${flid.Number},
#end
			/// <summary/>
			kdummyEnd
		}

		// ==========================================================================
		//		PopulateCsBasic STATIC	(${className})
		// ==========================================================================

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fill in the columnSpec with the columns which contain the basic cable information,
		/// corresponding to a "classname_" SQL view
		/// </summary>
		/// <param name="cs">The column spec</param>
		/// ------------------------------------------------------------------------------------
		protected new static void PopulateCsBasic(IDbColSpec cs)
		{
			${class.BaseClass.GetRelativeQualifiedSignature($class.Parent)}.PopulateCsBasic(cs);
${class.ProcessColumnSpecs("			")}
		}

		// ==========================================================================
		//		OwningAtomicFlids	(${className})
		// ==========================================================================
		/// <summary>
		/// the static list of the flids of the owning atomic properties of this
		/// class and all of its superclasses
		/// </summary>
		protected internal new static readonly int[] OwningAtomicFlids =
		{
${class.ProcessOwningAtomicFlid("			")}
		};

		// ==========================================================================
		//		VectorFlids	(${className})
		// ==========================================================================
		/// <summary>
		/// the static list of the flids of the vector properties of this class and
		/// all of its superclasses
		/// </summary>
		protected internal new static readonly int[] VectorFlids =
		{
${class.ProcessVectorFlids("			")}
		};

		// ==========================================================================
		//		VectorViewNames	(${className})
		// ==========================================================================
		/// <summary>
		/// the static list of the view names needed to retrieve the vector
		/// properties of this class and all of its superclasses
		/// </summary>
		/// <returns>an array of strings.</returns>
		protected internal new static readonly string[] VectorViewNames =
		{
${class.ProcessVectorViewNames("			")}
		};

		// ==========================================================================
		//		VectorIsSequence	(${className})
		// ==========================================================================
		/// <summary>
		/// the static list of bools where true = sequence, false=collection.
		///	needed to retrieve the vector properties of this class and all of its
		/// superclasses
		/// </summary>
		/// <returns>an array of bools.</returns>
		protected internal new static readonly bool[] VectorIsSequence =
		{
${class.ProcessVectorIsSequence("			")}
		};
		#endregion // Definition for Fields

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns <c>true</c> if objects of this class never have an owner.
		/// </summary>
		/// <returns><c>false</c> in most cases, <c>true</c> if class is ownerless.</returns>
		/// ------------------------------------------------------------------------------------
		protected override bool ClassIsOwnerless()
		{
			return ${class.IsOwnerless()};
		}

		#region Accessors (${className})
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// the Cellar ID of this class of objects
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override int ClassID
		{
			get { return kclsid${className}; }
		}
		#foreach( $prop in $class.Properties)
			#if( $prop.Cardinality.ToString() == "Basic" )
				#parse( "propaccessors_simple.vm.cs" )
			#elseif( $prop.Cardinality.ToString() == "Atomic" )
				#parse( "propaccessors_simple.vm.cs" )
				#parse( "propaccessors_atomic.vm.cs" )
			#else
				#parse( "propaccessors_rel.vm.cs" )
			#end
		#end

		#endregion // Accessors (${className})
	}
	#endregion // ${className}
