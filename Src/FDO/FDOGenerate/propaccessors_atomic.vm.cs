## --------------------------------------------------------------------------------------------
## Copyright (C) 2006-2008 SIL International. All rights reserved.
##
## Distributable under the terms of either the Common Public License or the
## GNU Lesser General Public License, as specified in the LICENSING.txt file.
##
## NVelocity template file
## This file is used by the FdoGenerate task to generate the source code from the XMI
## database model.
## --------------------------------------------------------------------------------------------
#set( $propComment = $prop.Comment )
#set( $propNotes = $prop.Notes )
#set( $propTypeClass = $fdogenerate.GetClass($prop.Signature) )
#if ( $prop.IsHandGenerated)
#set( $generated = "_Generated" )
#else
#set( $generated = "" )
#end
## ${prop.CSharpType}

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
		private I$propTypeClass $prop.NiuginianPropName$generated
#else
#if( $prop.IsOwning)
		[ModelProperty(CellarPropertyType.OwningAtomic, $prop.Number, "$propTypeClass")]
#else
		[ModelProperty(CellarPropertyType.ReferenceAtomic, $prop.Number, "$propTypeClass")]
#end
		public I$propTypeClass $prop.NiuginianPropName
#end
		{
			get
			{
				lock (SyncRoot)
				{
					if (m_$prop.NiuginianPropName is ICmObjectId)
#if( $prop.IsOwning)
						m_$prop.NiuginianPropName = (I$propTypeClass)ConvertIdToObject((ICmObjectId)m_$prop.NiuginianPropName);
#else
						m_$prop.NiuginianPropName = (I$propTypeClass)ConvertIdToAtomicRef((ICmObjectId)m_$prop.NiuginianPropName);
#end
				}
				return (I$propTypeClass)m_$prop.NiuginianPropName;
			}
			set
			{
				Set${prop.NiuginianPropName}(value, false);
			}
		}
		// This method does most of the stuff int the main setter, but avoids UOW notification and validation.
		// It should only be called by the main setter (with forDeleteObj true) and DeleteObjectBasics (with it false).
		private void Set${prop.NiuginianPropName}(I$propTypeClass val, bool forDeleteObj)
		{
			var oldObjValue = $prop.NiuginianPropName;
			var newObjValue = val;
			if (!forDeleteObj)
				Validate${prop.NiuginianPropName}(ref newObjValue);
			if (newObjValue == oldObjValue)
				return;

			var oldValue = (oldObjValue != null) ? oldObjValue.Guid : Guid.Empty;
			var newValue = Guid.Empty;
			if (newObjValue == null)
			{
				// Get rid of the value.
				m_$prop.NiuginianPropName = null;
			}
			else
			{
				// Sanity checks.
				// 1. If Hvo of "newObjValue" is 'kHvoObjectDeleted' then throw an exception.
				if (newObjValue.Hvo == (int)SpecialHVOValues.kHvoObjectDeleted)
					throw new FDOObjectDeletedException(String.Format("Using deleted object: {0}.", newObjValue.Guid));
				// 2. If Hvo of "newObjValue" is 'kHvoUninitializedObject'.
				if (newObjValue.Hvo == (int)SpecialHVOValues.kHvoUninitializedObject)
				{
#if( $prop.IsOwning)
					// The new value was created using new Foo().
					((ICmObjectInternal)val).InitializeNewCmObject(Cache, this, $prop.Number, 0);
#else
					// Can't reference such an unitialized object.
					// Even a legitimate unowned object has a valid Hvo.
					throw new FDOObjectUninitializedException(String.Format("Using unowned object in reference property: {0}.", newObjValue.Guid));
#end
				}
#if( $prop.IsOwning)
				else
				{
					// The new value is already owned by some other object.
					((ICmObjectInternal)val).SetOwner(this, $prop.Number, 0);
				}
#end

				newValue = newObjValue.Guid;
				m_$prop.NiuginianPropName = newObjValue;
			}
			// Need to remove the reference of the removed object before we delete the object
			// so undo/redo will be able to create/delete objects in the correct order.
			if (!forDeleteObj)
#if( $prop.IsOwning)
				((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterObjectAsModified(this, $prop.Number, oldValue, newValue);
#else
				((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterObjectAsModifiedRef(this, $prop.Number, oldValue, newValue);
#end
${prop.NiuginianPropName}SideEffects(oldObjValue, newObjValue); // Call before delete!
#if( $prop.IsOwning)
			// Get rid of old owned value.
			if (oldObjValue != null)
					((ICmObjectInternal)oldObjValue).DeleteObject();
#else
			if (oldObjValue != null)
				((ICmObjectInternal)oldObjValue).RemoveIncomingRef(this);
			 if (newObjValue != null)
				((ICmObjectInternal)newObjValue).AddIncomingRef(this);
#end
		}
		partial void Validate${prop.NiuginianPropName}(ref I$propTypeClass newObjValue);
		partial void ${prop.NiuginianPropName}SideEffects(I$propTypeClass oldObjValue, I$propTypeClass newObjValue);
