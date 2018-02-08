// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Application;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This class publishes a list of objects through the ISilDataAccess interface.
	/// It defines a virtual property which contains a list of objects, and supports
	/// notifying changes to it. Other requests are passed through. This can be used
	/// to publish a list of objects for a BrowseViewer.
	/// In addition, it can implement the 'owning' property on which the virtual one
	/// is based. This is used for properties like "all wordforms" that don't have
	/// any logical object to be a virtual property of.
	/// </summary>
	public class ObjectListPublisher : DomainDataByFlidDecoratorBase, IClearValues
	{
		private Dictionary<int, int[]> m_values = new Dictionary<int, int[]>();

		/// <summary>
		/// The base value for fake flids.
		/// </summary>
		public const int MinMadeUpFieldIdentifier = 89999000;
		/// <summary>
		/// The (fixed) tag used to retrieve the owning property.
		/// </summary>
		public const int OwningFlid = 9999935;
		private int[] m_owningPropValues;
		private int m_owningDestClass;
		private string m_owningClassName;

		/// <summary>
		/// Make one, wrapping some other ISilDataAccessManged (typically the main DomainDataByFlid).
		/// </summary>
		public ObjectListPublisher(ISilDataAccessManaged domainDataByFlid, int flid)
			: base(domainDataByFlid)
		{
			SetOverrideMdc(new ObjectListPublisherMdc(MetaDataCache as IFwMetaDataCacheManaged, this));
			MadeUpFieldIdentifier = flid;
		}

		/// <summary>
		/// Set the values to be returned when asked for information about OwningFlid
		/// </summary>
		public void SetOwningPropValue(int[] newValue)
		{
			m_owningPropValues = newValue;
		}

		/// <summary>
		/// Set the properties that we will pretend the fake owning property has.
		/// </summary>
		public void SetOwningPropInfo(int destClass, string className, string fieldName)
		{
			m_owningDestClass = destClass;
			m_owningClassName = className;
			OwningFieldName = fieldName;
		}

		/// <summary>
		/// The field name we are claiming to display, if it has been explicitly set.
		/// </summary>
		public string OwningFieldName { get; private set; }

		/// <summary>
		/// Determine absolutely what the new list of hvos will be.
		/// </summary>
		public void CacheVecProp(int hvoObj, int[] hvos)
		{
			if (hvos == null)
			{
				throw new ArgumentNullException("Should not pass null to CacheVecProp");
			}
			var cvDel = 0;
			int[] old;
			if (m_values.TryGetValue(hvoObj, out old))
			{
				cvDel = old.Length;
			}

			m_values[hvoObj] = hvos;
			SendPropChanged(hvoObj, 0, hvos.Length, cvDel);
		}

		/// <summary>
		/// Get the length of the specified sequence or collection property.
		/// </summary>
		public override int get_VecSize(int hvo, int tag)
		{
			if (tag != MadeUpFieldIdentifier)
			{
				return tag == OwningFlid ? m_owningPropValues.Length : base.get_VecSize(hvo, tag);
			}
			int[] old;
			return m_values.TryGetValue(hvo, out old) ? old.Length : 0;

		}

		/// <summary>
		/// Obtain one item from an object sequence or collection property.
		/// @error E_INVALIDARG if index is out of range.
		/// </summary>
		public override int get_VecItem(int hvo, int tag, int index)
		{
			if (tag == MadeUpFieldIdentifier)
			{
				int[] old;
				if (m_values.TryGetValue(hvo, out old))
				{
					return old[index];
				}
				throw new InvalidOperationException("trying to get item from an invalid fake property");
			}
			return tag == OwningFlid ? m_owningPropValues[index] : base.get_VecItem(hvo, tag, index);
		}

		/// <summary>
		/// Get the Ids of the entire vector property.
		/// </summary>
		public override int[] VecProp(int hvo, int tag)
		{
			if (tag != MadeUpFieldIdentifier)
			{
				return tag == OwningFlid ? m_owningPropValues : base.VecProp(hvo, tag);
			}
			int[] old;
			return m_values.TryGetValue(hvo, out old) ? old : new int[0];
		}

		/// <summary>
		/// Override to allow clients to replace in our private property.
		/// We will automatically generate a PropChanged on the private property immediately (for private clients).
		/// </summary>
		public override void Replace(int hvoObj, int tag, int ihvoMin, int ihvoLim, int[] rghvo, int chvo)
		{
			if (tag == MadeUpFieldIdentifier)
			{
				Replace(hvoObj, ihvoMin, rghvo, ihvoLim - ihvoMin);
				return;
			}
			base.Replace(hvoObj, tag, ihvoMin, ihvoLim, rghvo, chvo);
		}

		/// <summary>
		/// Replaces the specified hvo.
		/// </summary>
		public void Replace(int hvo, int ivMin, int[] insertions, int cvDel)
		{
			int[] oldHvos;
			if (!m_values.TryGetValue(hvo, out oldHvos))
			{
				oldHvos = new int[0];
			}
			var newHvos = new int[oldHvos.Length + insertions.Length - cvDel];
			Array.Copy(oldHvos, 0, newHvos, 0, ivMin); // copy up to ivMin
			Array.Copy(insertions, 0, newHvos, ivMin, insertions.Length); // insert new ones
			Array.Copy(oldHvos, ivMin + cvDel, newHvos, ivMin + insertions.Length, oldHvos.Length - ivMin - cvDel); // copy remaining undeleted ones.
			m_values[hvo] = newHvos;
			SendPropChanged(hvo, ivMin, insertions.Length, cvDel);
		}

		/// <summary>
		/// Clear out the local m_values (typically during a Refresh, when they have become invalid, to prevent
		/// reuse of invalid values before they are properly restored).
		/// </summary>
		public void ClearValues()
		{
			m_values.Clear();
		}

		private void SendPropChanged(int hvo, int ivMin, int cvIns, int cvDel)
		{
			SendPropChanged(hvo, MadeUpFieldIdentifier, ivMin, cvIns, cvDel);
		}

		internal int MadeUpFieldIdentifier { get; }

		private sealed class ObjectListPublisherMdc : LcmMetaDataCacheDecoratorBase
		{
			private ObjectListPublisher m_publisher;

			public ObjectListPublisherMdc(IFwMetaDataCacheManaged metaDataCache, ObjectListPublisher publisher)
				: base(metaDataCache)
			{
				m_publisher = publisher;
			}

			public override void AddVirtualProp(string bstrClass, string bstrField, int luFlid, int type)
			{
				throw new NotSupportedException();
			}

			/// <summary>
			/// Override to give the class of objects in the 'owning' property.
			/// </summary>
			public override int GetDstClsId(int flid)
			{
				if (flid == OwningFlid)
				{
					return m_publisher.m_owningDestClass;
				}
				if (flid == m_publisher.MadeUpFieldIdentifier && flid >= MinMadeUpFieldIdentifier)
				{
					return 0;
				}
				return base.GetDstClsId(flid);
			}

			/// <summary>
			/// Override to give the name of the class of the 'owning' property.
			/// </summary>
			public override string GetFieldName(int flid)
			{
				if (flid == OwningFlid)
				{
					return m_publisher.OwningFieldName;
				}
				if (flid == m_publisher.MadeUpFieldIdentifier && flid >= MinMadeUpFieldIdentifier)
				{
					return string.Empty;
				}

				return base.GetFieldName(flid);
			}

			/// <summary>
			/// Override to give the name of the 'owning' property.
			/// </summary>
			public override string GetOwnClsName(int flid)
			{
				if (flid == OwningFlid)
				{
					return m_publisher.m_owningClassName;
				}

				if (flid == m_publisher.MadeUpFieldIdentifier && flid >= MinMadeUpFieldIdentifier)
				{
					return string.Empty;
				}
				return base.GetOwnClsName(flid);
			}

			/// <summary>
			/// Override to give the type of the 'owning' and fake properties.
			/// </summary>
			public override int GetFieldType(int flid)
			{
				if (flid == OwningFlid)
				{
					return (int)CellarPropertyType.OwningSequence;
				}
				if (flid == m_publisher.MadeUpFieldIdentifier && flid >= MinMadeUpFieldIdentifier)
				{
					return (int)CellarPropertyType.OwningSequence;
				}
				return base.GetFieldType(flid);
			}

			/// <summary>
			/// Override to let the fake flid be looked up by name.
			/// </summary>
			public override int GetFieldId(string bstrClassName, string bstrFieldName, bool fIncludeBaseClasses)
			{
				if (bstrClassName == m_publisher.m_owningClassName && bstrFieldName == m_publisher.OwningFieldName)
				{
					return m_publisher.MadeUpFieldIdentifier;
				}
				return base.GetFieldId(bstrClassName, bstrFieldName, fIncludeBaseClasses);
			}
		}
	}
}
