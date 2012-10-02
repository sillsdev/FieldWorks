using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.Common.Controls
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
	public class ObjectListPublisher : DomainDataByFlidDecoratorBase
	{
		private Dictionary<int, int[]> m_values = new Dictionary<int, int[]>();
		private int m_flid;
		/// <summary>
		/// The base value for fake flids.
		/// </summary>
		public const int MinFakeFlid = 89999000;
		/// <summary>
		/// The (fixed) tag used to retrieve the owning property.
		/// </summary>
		public const int OwningFlid = 9999935;
		private int[] m_owningPropValues;
		private int m_owningDestClass;
		private string m_owningClassName;
		private string m_owningFieldName;

		/// <summary>
		/// Make one, wrapping some other ISilDataAccessManged (typically the main DomainDataByFlid).
		/// </summary>
		/// <param name="domainDataByFlid">The domainDataByFlid.</param>
		/// <param name="flid">The flid.</param>
		public ObjectListPublisher(ISilDataAccessManaged domainDataByFlid, int flid)
			: base(domainDataByFlid)
		{
			SetOverrideMdc(new ObjectListPublisherMdc(MetaDataCache as IFwMetaDataCacheManaged, this));
			m_flid = flid;
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
			m_owningFieldName = fieldName;
		}

		/// <summary>
		/// The field name we are claiming to display, if it has been explicitly set.
		/// </summary>
		public string OwningFieldName { get { return m_owningFieldName; } }

		/// <summary>
		/// Determine absolutely what the new list of hvos will be.
		/// </summary>
		/// <param name="hvoObj">The hvo.</param>
		/// <param name="hvos">The hvos.</param>
		public void CacheVecProp(int hvoObj, int[] hvos)
		{
			if (hvos == null)
				throw new ArgumentNullException("Should not pass null to CacheVecProp");
			int cvDel = 0;
			int[] old;
			if (m_values.TryGetValue(hvoObj, out old))
				cvDel = old.Length;

			m_values[hvoObj] = hvos;
			SendPropChanged(hvoObj, 0, hvos.Length, cvDel);
		}

		/// <summary>
		/// Get the length of the specified sequence or collection property.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		public override int get_VecSize(int hvo, int tag)
		{
			if (tag == m_flid)
			{
				int[] old;
				if (m_values.TryGetValue(hvo, out old))
					return old.Length;
				return 0;
			}
			else if (tag == OwningFlid)
				return m_owningPropValues.Length;

			return base.get_VecSize(hvo, tag);
		}

		/// <summary>
		/// Obtain one item from an object sequence or collection property.
		/// @error E_INVALIDARG if index is out of range.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="index">Indicates the item of interest. &lt;b&gt;Zero based&lt;/b&gt;.</param>
		/// <returns></returns>
		public override int get_VecItem(int hvo, int tag, int index)
		{
			if (tag == m_flid)
			{
				int[] old;
				if (m_values.TryGetValue(hvo, out old))
					return old[index];
				throw new InvalidOperationException("trying to get item from an invalid fake property");
			}
			else if (tag == OwningFlid)
				return m_owningPropValues[index];
			return base.get_VecItem(hvo, tag, index);
		}

		/// <summary>
		/// Get the Ids of the entire vector property.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns>The Ids of entire vector property</returns>
		public override int[] VecProp(int hvo, int tag)
		{
			if (tag == m_flid)
			{
				int[] old;
				if (m_values.TryGetValue(hvo, out old))
					return old;
				return new int[0];
			}
			else if (tag == OwningFlid)
				return m_owningPropValues;
			return base.VecProp(hvo, tag);
		}

		/// <summary>
		/// Override to allow clients to replace in our private property.
		/// We will automatically generate a PropChanged on the private property immediately (for private clients).
		/// </summary>
		/// <param name="hvoObj"></param>
		/// <param name="tag"></param>
		/// <param name="ihvoMin"></param>
		/// <param name="ihvoLim"></param>
		/// <param name="_rghvo"></param>
		/// <param name="chvo"></param>
		public override void Replace(int hvoObj, int tag, int ihvoMin, int ihvoLim, int[] _rghvo, int chvo)
		{
			if (tag == m_flid)
			{
				Replace(hvoObj, ihvoMin, _rghvo, ihvoLim - ihvoMin);
				return;
			}
			base.Replace(hvoObj, tag, ihvoMin, ihvoLim, _rghvo, chvo);
		}

		/// <summary>
		/// Replaces the specified hvo.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <param name="ivMin">The iv min.</param>
		/// <param name="insertions">The insertions.</param>
		/// <param name="cvDel">The cv del.</param>
		public void Replace(int hvo, int ivMin, int[] insertions, int cvDel)
		{
			int[] oldHvos;
			if (!m_values.TryGetValue(hvo, out oldHvos))
				oldHvos = new int[0];
			int[] newHvos = new int[oldHvos.Length + insertions.Length - cvDel];
			Array.Copy(oldHvos, 0, newHvos, 0, ivMin); // copy up to ivMin
			Array.Copy(insertions, 0, newHvos, ivMin, insertions.Length); // insert new ones
			Array.Copy(oldHvos, ivMin + cvDel, newHvos, ivMin + insertions.Length, oldHvos.Length - ivMin - cvDel); // copy remaining undeleted ones.
			m_values[hvo] = newHvos;
			SendPropChanged(hvo, ivMin, insertions.Length, cvDel);
		}

		private void SendPropChanged(int hvo, int ivMin, int cvIns, int cvDel)
		{
			SendPropChanged(hvo, m_flid, ivMin, cvIns, cvDel);
		}

		internal int FakeFlid
		{
			get { return m_flid; }
		}

		class ObjectListPublisherMdc : FdoMetaDataCacheDecoratorBase
		{
			private ObjectListPublisher m_publisher;

			public ObjectListPublisherMdc(IFwMetaDataCacheManaged metaDataCache, ObjectListPublisher publisher)
				: base(metaDataCache)
			{
				m_publisher = publisher;
			}

			public override void AddVirtualProp(string bstrClass, string bstrField, int luFlid, int type)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Override to give the class of objects in the 'owning' property.
			/// </summary>
			public override int GetDstClsId(int flid)
			{
				if (flid == ObjectListPublisher.OwningFlid)
					return m_publisher.m_owningDestClass;
				if (flid == m_publisher.FakeFlid && flid >= ObjectListPublisher.MinFakeFlid)
					return 0;
				return base.GetDstClsId(flid);
			}

			/// <summary>
			/// Override to give the name of the class of the 'owning' property.
			/// </summary>
			public override string GetFieldName(int flid)
			{
				if (flid == ObjectListPublisher.OwningFlid)
					return m_publisher.m_owningFieldName;
				if (flid == m_publisher.FakeFlid && flid >= ObjectListPublisher.MinFakeFlid)
					return String.Empty;

				return base.GetFieldName(flid);
			}

			/// <summary>
			/// Override to give the name of the 'owning' property.
			/// </summary>
			public override string GetOwnClsName(int flid)
			{
				if (flid == ObjectListPublisher.OwningFlid)
					return m_publisher.m_owningClassName;
				else if (flid == m_publisher.FakeFlid && flid >= ObjectListPublisher.MinFakeFlid)
					return String.Empty;
				else
					return base.GetOwnClsName(flid);
			}

			/// <summary>
			/// Override to give the type of the 'owning' and fake properties.
			/// </summary>
			public override int GetFieldType(int flid)
			{
				if (flid == ObjectListPublisher.OwningFlid)
					return (int)CellarPropertyType.OwningSequence;
				else if (flid == m_publisher.FakeFlid && flid >= ObjectListPublisher.MinFakeFlid)
					return (int)CellarPropertyType.OwningSequence;
				else
					return base.GetFieldType(flid);
			}

			/// <summary>
			/// Override to let the fake flid be looked up by name.
			/// </summary>
			public override int GetFieldId(string bstrClassName, string bstrFieldName, bool fIncludeBaseClasses)
			{
				if (bstrClassName == m_publisher.m_owningClassName && bstrFieldName == m_publisher.m_owningFieldName)
					return m_publisher.FakeFlid;
				return base.GetFieldId(bstrClassName, bstrFieldName, fIncludeBaseClasses);
			}
		}
	}
}
