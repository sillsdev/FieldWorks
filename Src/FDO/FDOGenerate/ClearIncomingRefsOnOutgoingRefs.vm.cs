## --------------------------------------------------------------------------------------------
## Copyright (C) 2007-2008 SIL International. All rights reserved.
##
## Distributable under the terms of either the Common Public License or the
## GNU Lesser General Public License, as specified in the LICENSING.txt file.
##
## NVelocity template file
## This file is used by the FdoGenerate task to generate the source code from the XMI
## database model.
## --------------------------------------------------------------------------------------------

		/// <summary>
		/// Remove the recipient from the incomingRefs colletions of everything it refers to.
		/// (Used ONLY as part of Undoing object creation.)
		/// </summary>
		protected override void ClearIncomingRefsOnOutgoingRefsInternal()
		{
#foreach( $prop in $class.AtomicProperties)
			if (m_$prop.NiuginianPropName is ICmObjectInternal)
			{
				((ICmObjectInternal)m_$prop.NiuginianPropName).RemoveIncomingRef(this);
			}
#end
#foreach( $prop in $class.CollectionRefProperties )
			if (m_$prop.NiuginianPropName != null)
			{
				((IVector)m_${prop.NiuginianPropName}).ClearForUndo();
			}
#end
#foreach( $prop in $class.SequenceRefProperties )
			if (m_$prop.NiuginianPropName != null)
			{
				((IVector)m_${prop.NiuginianPropName}).ClearForUndo();
			}
#end
		base.ClearIncomingRefsOnOutgoingRefsInternal();
		}