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
## This will generate the various methods used by the FDO aware ISilDataAccess implementation.
## Using these methods avoids using Reflection in that implementation.
##
		/// <summary>
		/// Get the size of a vector (seq or col) property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		protected override int GetVectorSizeInternal(int flid)
		{
			switch (flid)
			{
				default:
					return base.GetVectorSizeInternal(flid);
#foreach( $prop in $class.VectorProperties )
				case $prop.Number:
					lock (SyncRoot)
						return m_$prop.NiuginianPropName == null ? 0 : m_${prop.NiuginianPropName}.Count;
#end
			}
		}

		/// <summary>
		/// Get an integer type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="index">The property to read.</param>
		protected override int GetVectorItemInternal(int flid, int index)
		{
			switch (flid)
			{
				default:
					return base.GetVectorItemInternal(flid, index);
#foreach( $prop in $class.CollectionProperties )
				case $prop.Number:
					lock (SyncRoot)
						return m_$prop.NiuginianPropName == null ? FdoCache.kNullHvo : m_${prop.NiuginianPropName}.ToArray()[index].Hvo;
#end
#foreach( $prop in $class.SequenceProperties )
				case $prop.Number:
					lock (SyncRoot)
						return m_$prop.NiuginianPropName == null ? FdoCache.kNullHvo : m_${prop.NiuginianPropName}[index].Hvo;
#end
			}
		}

		/// <summary>
		/// Get the index of 'hvo' in 'flid'.
		/// Returns -1 if 'hvo' is not in 'flid'.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="hvo">The object to get the index of.</param>
		/// <remarks>
		/// If 'flid' is for a collection, then the returned index is
		/// essentially meaningless, as collections are unordered sets.
		/// </remarks>
		protected override int GetObjIndexInternal(int flid, int hvo)
		{
			switch (flid)
			{
				default:
					return base.GetObjIndexInternal(flid, hvo);
#foreach( $prop in $class.CollectionProperties )
				case $prop.Number:
					lock (SyncRoot)
					{
						if (m_${prop.NiuginianPropName} == null)
						{
							return -1;
						}
						else
						{
							var target = GetObjectFromHvo(hvo);
							if (!(target is I$prop.Signature))
								return -1;
							return Array.IndexOf(m_${prop.NiuginianPropName}.ToArray(),(I$prop.Signature)target);
						}
					}
#end
#foreach( $prop in $class.SequenceProperties )
				case $prop.Number:
					lock (SyncRoot)
#if ($class.Name == "Segment" && $prop.Name == "Analyses")
						return m_${prop.NiuginianPropName} == null ? -1 : m_${prop.NiuginianPropName}.IndexOf((IAnalysis)GetObjectFromHvo(hvo));
#else
						return m_${prop.NiuginianPropName} == null ? -1 : m_${prop.NiuginianPropName}.IndexOf((I$prop.Signature)GetObjectFromHvo(hvo));
#end
#end
			}
		}

		/// <summary>
		/// Get the hvos in a vector (collection or sequence) type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		protected override IEnumerable<ICmObject> GetVectorPropertyInternal(int flid)
		{
			switch (flid)
			{
				default:
					return base.GetVectorPropertyInternal(flid);
#foreach( $prop in $class.VectorProperties )
				case $prop.Number:
					lock (SyncRoot)
						return m_${prop.NiuginianPropName} == null ? new ICmObject[0] : m_${prop.NiuginianPropName}.Objects;
#end
			}
		}

		/// <summary>
		/// Set an vector (col or seq) type property.
		/// </summary>
		/// <param name="flid">The property to set.</param>
		/// <param name="newValue">The new value.</param>
		/// <param name="useAccessor"></param>
		protected override void SetPropertyInternal(int flid, Guid[] newValue, bool useAccessor)
		{
			switch (flid)
			{
				default:
					base.SetPropertyInternal(flid, newValue, useAccessor);
					break;
#foreach( $prop in $class.VectorProperties )
				case $prop.Number:
					((IFdoVectorInternal)${prop.NiuginianPropName}).ReplaceAll(newValue, useAccessor);
					break;
#end
			}
		}
#if ($class.SequenceProperties.Count > 0)

		/// <summary>
		/// Replace items in a sequence.
		/// </summary>
		protected override void ReplaceInternal(int flid, int start, int numberToDelete, IEnumerable<ICmObject> thingsToAdd)
		{
			switch (flid)
			{
				default:
					base.ReplaceInternal(flid, start, numberToDelete, thingsToAdd);
					break;
#foreach( $prop in $class.SequenceProperties )
				case $prop.Number:
					${prop.NiuginianPropName}.Replace(start, numberToDelete, thingsToAdd);
					break;
#end
			}
		}
#end
#if ($class.CollectionProperties.Count > 0)

		/// <summary>
		/// Replace items in a collection. (NOT currently implemented for sequences).
		/// </summary>
		protected override void ReplaceInternal(int flid, IEnumerable<ICmObject> thingsToRemove, IEnumerable<ICmObject> thingsToAdd)
		{
			switch (flid)
			{
				default:
					base.ReplaceInternal(flid, thingsToRemove, thingsToAdd);
					break;
#foreach( $prop in $class.CollectionProperties )
				case $prop.Number:
					${prop.NiuginianPropName}.Replace(thingsToRemove, thingsToAdd);
					break;
#end
			}
		}
#end
