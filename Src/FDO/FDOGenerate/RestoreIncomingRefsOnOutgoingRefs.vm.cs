## --------------------------------------------------------------------------------------------
## Copyright (C) 2007-2008 SIL International. All rights reserved.
##
## Distributable under the terms of either the Common Public License or the
## GNU Lesser General Public License, as specified in the LICENSING.txt file.
##
## NVelocity template file
## This file is used by the FdoGenerate task to generate the source code from the XMI
## database model.
## Note that this method MUST restore all outgoing refs to real objects, not leave them as IDs,
## because (before the Undo) we may already have set flags indicating that all incoming references
## for certain properties have been fully evaluated, and so the incoming refs on the target
## objects must be valid, and there must not be objectIDs left in the properties that could
## generate spurious incoming refs when converted.
## --------------------------------------------------------------------------------------------

		/// <summary>
		/// Remove the recipient from the incomingRefs colletions of everything it refers to.
		/// (Used ONLY as part of Undoing object creation.)
		/// </summary>
		protected override void RestoreIncomingRefsOnOutgoingRefsInternal()
		{
#foreach( $prop in $class.AtomicProperties)
#set( $propTypeClass = $fdogenerate.GetClass($prop.Signature) )
		if (m_$prop.NiuginianPropName != null)
			{
				if (m_$prop.NiuginianPropName is ICmObjectId)
					m_$prop.NiuginianPropName = (I$propTypeClass)ConvertIdToObject((ICmObjectId)m_$prop.NiuginianPropName);
				((ICmObjectInternal)m_$prop.NiuginianPropName).AddIncomingRef(this);
			}
#end
#foreach( $prop in $class.CollectionRefProperties )
			if (m_$prop.NiuginianPropName != null)
			{
				((IVector)m_${prop.NiuginianPropName}).RestoreAfterUndo();
			}
#end
#foreach( $prop in $class.SequenceRefProperties )
			if (m_$prop.NiuginianPropName != null)
			{
				((IVector)m_${prop.NiuginianPropName}).RestoreAfterUndo();
			}
#end
		base.RestoreIncomingRefsOnOutgoingRefsInternal();
		}