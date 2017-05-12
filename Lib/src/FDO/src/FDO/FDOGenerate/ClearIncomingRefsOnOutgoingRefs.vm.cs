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