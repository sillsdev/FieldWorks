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