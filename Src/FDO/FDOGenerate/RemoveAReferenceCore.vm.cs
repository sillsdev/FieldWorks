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
		/// Remove one reference to the target object from one of your atomic properties.
		/// (It doesn't matter which one because this is called repeatedly, once for each reference to the target.)
		/// </summary>
		internal override void RemoveAReferenceCore(ICmObject target)
		{
#foreach( $prop in $class.AtomicRefProperties)
			if (m_$prop.NiuginianPropName == target)
			{
				$prop.NiuginianPropName = null;
				return;
			}
#end
			base.RemoveAReferenceCore(target);
		}
		/// <summary>
		/// Replace one reference to the target object in one of your atomic properties with the replacement.
		/// (It doesn't matter which one because this is called repeatedly, once for each reference to the target.)
		/// </summary>
		internal override void ReplaceAReferenceCore(ICmObject target, ICmObject replacement)
		{
#foreach( $prop in $class.AtomicRefProperties)
			if (m_$prop.NiuginianPropName == target)
			{
				$prop.NiuginianPropName = ($prop.Signature)replacement;
				return;
			}
#end
			base.ReplaceAReferenceCore(target, replacement);
		}