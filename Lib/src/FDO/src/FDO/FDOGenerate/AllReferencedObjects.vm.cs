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
