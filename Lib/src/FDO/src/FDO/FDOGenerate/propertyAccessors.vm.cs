## --------------------------------------------------------------------------------------------
## Copyright (c) 2006-2013 SIL International
## This software is licensed under the LGPL, version 2.1 or later
## (http://www.gnu.org/licenses/lgpl-2.1.html)
##
## NVelocity template file
## This file is used by the FdoGenerate task to generate the source code from the XMI
## database model.
## --------------------------------------------------------------------------------------------
#set( $ownerStatus = $class.OwnerStatus )

		#region Property Accessors

#if ( $className == "CmObject" )
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns one of three values to see if objects of this class:
		///		1. Never have an owner,
		///		2. Always have an owner, or
		///		3. Having an owner is optional.
		/// Derived classes
		/// have to implement this property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public abstract ClassOwnershipStatus OwnershipStatus { get; }


		/// <summary>
		/// Get the name of the class from an instance of it.
		/// </summary>
		public abstract string ClassName { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Class ID of the object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[ModelProperty(CellarPropertyType.Integer, (int)CmObjectFields.kflidCmObject_Class, "Integer")]
		public abstract int ClassID { get; }
#else
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns one of three values to see if objects of this class:
		///		1. Never have an owner,
		///		2. Always have an owner, or
		///		3. Haveing an owner is optional.
		/// Derived classes
		/// have to implement this property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override ClassOwnershipStatus OwnershipStatus
		{
			get
			{
#if ( $ownerStatus == "required")
				return ClassOwnershipStatus.kOwnerRequired;
#else
#if ( $ownerStatus == "none")
				return ClassOwnershipStatus.kOwnerProhibited;
#else
				return ClassOwnershipStatus.kOwnerOptional;
#end
#end
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Get the name of the class.</summary>
		/// ------------------------------------------------------------------------------------
		public override string ClassName
		{
			get { return "$className"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Get theID of the class.</summary>
		/// ------------------------------------------------------------------------------------
		public override int ClassID
		{
			get { return $class.Number; }
		}
#end

#foreach( $prop in $class.Properties)
#if( $prop.Cardinality.ToString() == "Basic" )
#parse( "propaccessors_simple.vm.cs" )

#elseif( $prop.Cardinality.ToString() == "Atomic" )
#parse( "propaccessors_atomic.vm.cs" )

#else
#parse( "propaccessors_rel.vm.cs" )

#end
#end

		#endregion Property Accessors
