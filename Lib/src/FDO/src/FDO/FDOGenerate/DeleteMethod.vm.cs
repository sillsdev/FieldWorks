## --------------------------------------------------------------------------------------------
## Copyright (c) 2007-2013 SIL International
## This software is licensed under the LGPL, version 2.1 or later
## (http://www.gnu.org/licenses/lgpl-2.1.html)
##
## NVelocity template file
## This file is used by the FdoGenerate task to generate the source code from the XMI
## database model.
## --------------------------------------------------------------------------------------------

		/// <summary>
		/// Delete the object, and anything it owns.
		/// </summary>
		/// <remarks>
		/// NB: This method should *never, ever* be used directly by FDO clients.
		/// It is used by FDO to handle side effects of setting an atomic owning property,
		/// or removing an object from an owning sequence/collection vector.
		/// The FDO code generator will override this method to do class-specific
		/// deletions, such as deleting owned objects.
		/// </remarks>
		protected override void DeleteObjectBasics()
		{
#if ( $class.Name == "LgWritingSystem" )
			throw new InvalidOperationException("Don't even think of nuking a WS.");
#else
#foreach( $prop in $class.AtomicProperties)
			if (m_$prop.NiuginianPropName != null)
			{
				// Suppress notifications on modifying a deleted object,
				// and suppress validation because some subclasses don't
				// normally allow setting to null.
				Set${prop.NiuginianPropName}(null, true);
				m_$prop.NiuginianPropName = null;
			}
#end
#foreach( $prop in $class.VectorProperties)
			if (m_$prop.NiuginianPropName != null)
			{
				// Remove objects from the vector (the vector will handle the side effects)
				((IFdoClearForDelete)m_${prop.NiuginianPropName}).Clear(true);
				m_$prop.NiuginianPropName = null;
			}
#end
			base.DeleteObjectBasics();
#end
		}