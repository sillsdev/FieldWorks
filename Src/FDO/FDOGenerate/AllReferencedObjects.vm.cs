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
		/// Add all the things you refer to to the collection.
		/// </summary>
		internal override void AddAllReferencedObjectsInternal(List<ICmObject> collector)
		{
#foreach( $prop in $class.AtomicRefProperties)
			if ($prop.NiuginianPropName != null)
			{
				collector.Add($prop.NiuginianPropName);
			}
#end
#foreach( $prop in $class.SequenceRefProperties)
			collector.AddRange(($prop.NiuginianPropName).Cast<ICmObject>());
#end
#foreach( $prop in $class.CollectionRefProperties)
			collector.AddRange(($prop.NiuginianPropName).Cast<ICmObject>());
#end
			base.AddAllReferencedObjectsInternal(collector);
		}
