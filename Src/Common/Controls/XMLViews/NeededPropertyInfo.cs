// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
// --------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// This class stores (a) a list of flids of atomic non-object properties that we need
	/// to know about;
	/// (b) a list of object properties, for each of which, we need to know the
	/// contents of the property, and then we need to recursively load info about
	/// the objects in that property. This includes both atomic object properties, where a single
	/// query can be used for both the main and related objects, and sequence properties where
	/// a distinct query is used for each sequence.
	/// We reuse this class for info about the child objects, so it also stores
	/// a flid for obtaining child objects. This is also set for the root
	/// NeededPropertyInfo, since sometimes knowing what to add depends on
	/// the signature of the sequence property.
	/// </summary>
	public class NeededPropertyInfo
	{
		/// <summary>
		/// the class of objects at this property info level (destination of m_flidSource).
		/// </summary>
		protected int m_targetClass = 0;
		// the property from which the objects whose properties we want come.
		private int m_flidSource;
		private List<PropWs> m_atomicFlids = new List<PropWs>(); // atomic properties of target objects
		private List<NeededPropertyInfo> m_sequenceInfo = new List<NeededPropertyInfo>();
		private NeededPropertyInfo m_parent; // if it is in a list of child properties, note of which object.
		private bool m_fSeq; // whether m_flidSource is a sequence property.

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:NeededPropertyInfo"/> class.
		/// </summary>
		/// <param name="listItemsClass">the class of objects at the root parent of the NeedPropertyInfo tree.
		/// Typically the destination class of flidSource</param>
		/// ------------------------------------------------------------------------------------
		public NeededPropertyInfo(int listItemsClass)
		{
			m_targetClass = listItemsClass;
			m_flidSource = 0;	// don't really how we got to the root parent class.
			m_parent = null;
			m_fSeq = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:NeededPropertyInfo"/> class.
		/// </summary>
		/// <param name="flidSource">The flid source.</param>
		/// <param name="parent">The parent.</param>
		/// <param name="fSeq">if set to <c>true</c> [f seq].</param>
		/// ------------------------------------------------------------------------------------
		protected NeededPropertyInfo(int flidSource, NeededPropertyInfo parent, bool fSeq)
		{
			m_flidSource = flidSource;
			m_parent = parent;
			m_fSeq = fSeq;
		}

		/// <summary>
		/// The source property containing the objects about which we want info.
		/// </summary>
		public int Source
		{
			get { return m_flidSource; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance has atomic fields.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance has atomic fields; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public bool HasAtomicFields
		{
			get
			{
				if (m_atomicFlids.Count > 0)
					return true;
				foreach (NeededPropertyInfo info in m_sequenceInfo)
					if (!info.m_fSeq)
						return true;
				return false;
			}
		}

		/// <summary>
		/// Answer the class of objects for which we are collecting fields.
		/// By default this is the destination class of the field that contains them.
		/// We override this in a subclass for certain virtual and phony properties.
		/// </summary>
		/// <param name="vc"></param>
		/// <returns></returns>
		internal int TargetClass(XmlVc vc)
		{
			return TargetClass(vc.DataAccess);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Answer the class of objects for which we are collecting fields.
		/// By default this is the destination class of the field that contains them.
		/// If needed, override this in a subclass for certain virtual and phony properties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int TargetClass(ISilDataAccess sda)
		{
			if (m_targetClass == 0 && Source != 0)
				m_targetClass = sda.MetaDataCache.GetDstClsId(this.Source);
			return m_targetClass;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is sequence.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is sequence; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public bool IsSequence
		{
			get { return m_fSeq; }
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the parent.
		/// </summary>
		/// <value>The parent.</value>
		/// ------------------------------------------------------------------------------------
		public NeededPropertyInfo Parent
		{
			get { return m_parent; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the seq depth.
		/// </summary>
		/// <value>The seq depth.</value>
		/// ------------------------------------------------------------------------------------
		public int SeqDepth
		{
			get
			{
				int depth = 0;
				NeededPropertyInfo info = this;
				while (info != null)
				{
					if (info.m_fSeq)
						depth++;
					info = info.Parent;
				}
				return depth;
			}
		}

		/// <summary>
		/// The number of layers of parent (not counting this)
		/// </summary>
		public int Depth
		{
			get
			{
				int depth = 0;
				NeededPropertyInfo info = m_parent;
				while (info != null)
				{
					depth++;
					info = info.Parent;
				}
				return depth;
			}
		}

		/// <summary>
		/// Atomic properties of objects that can occur in the source field.
		/// </summary>
		public List<PropWs> AtomicFields
		{
			get { return m_atomicFlids; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add an atomic flid.
		/// </summary>
		/// <param name="flid">The flid.</param>
		/// <param name="ws">The ws.</param>
		/// ------------------------------------------------------------------------------------
		public void AddAtomicField(int flid, int ws)
		{
			PropWs pw = new PropWs(flid, ws);
			if (m_atomicFlids.Contains(pw))
				return;
			m_atomicFlids.Add(pw);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sequence properties of objects that can occur in the source field.
		/// </summary>
		/// <value>The seq fields.</value>
		/// ------------------------------------------------------------------------------------
		public List<NeededPropertyInfo> SeqFields
		{
			get { return m_sequenceInfo; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add (or retrieve) info about a object (atomic or seq) flid. May include virtuals.
		/// </summary>
		/// <param name="flid">The flid.</param>
		/// <param name="fSeq">if set to <c>true</c> [f seq].</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public NeededPropertyInfo AddObjField(int flid, bool fSeq)
		{
			NeededPropertyInfo info =
				m_sequenceInfo.Find(delegate(NeededPropertyInfo item)
					{ return item.Source == flid; });
			if (info == null)
			{
				info = new NeededPropertyInfo(flid, this, fSeq);
				m_sequenceInfo.Add(info);
			}
			return info;
		}

		/// <summary>
		/// Add (or retrieve) info about a virtual object flid.
		/// </summary>
		/// <param name="flid"></param>
		/// <param name="fSeq"></param>
		/// <param name="dstClass"></param>
		/// <returns></returns>
		public NeededPropertyInfo AddVirtualObjField(int flid, bool fSeq, int dstClass)
		{
			VirtualNeededPropertyInfo info =
				m_sequenceInfo.Find(delegate(NeededPropertyInfo item)
					{ return item.Source == flid; }) as VirtualNeededPropertyInfo;
			if (info == null)
			{
				info = new VirtualNeededPropertyInfo(flid, this, fSeq, dstClass);
				m_sequenceInfo.Add(info);
			}
			return info;
		}

		internal void DumpFieldInfo(IFwMetaDataCache mdc)
		{
			if (this.Depth == 0)
				Debug.WriteLine("");
			for (int i = 0; i < this.Depth; ++i)
				Debug.Write("    ");
			if (Source != 0)
			{
				Debug.WriteLine("[" + this.Depth + "]info.Source = " + this.Source + " = " +
				GetFancyFieldName(this.Source, mdc));
			}
			else
			{
				Debug.WriteLine("Root (target) class: " + mdc.GetClassName(m_targetClass));
			}

			for (int i = 0; i < this.AtomicFields.Count; ++i)
			{
				for (int j = 0; j < this.Depth; ++j)
					Debug.Write("    ");
				Debug.WriteLine("    Atomic[" + i + "] flid = " + this.AtomicFields[i].flid + "(" +
					GetFancyFieldName(this.AtomicFields[i].flid, mdc) + "); ws = " + this.AtomicFields[i].ws);
			}
			for (int i = 0; i < this.SeqFields.Count; ++i)
				this.SeqFields[i].DumpFieldInfo(mdc);
		}

		private string GetFancyFieldName(int flid, IFwMetaDataCache mdc)
		{
			string f = mdc.GetFieldName(flid);
			string c = mdc.GetOwnClsName(flid);
			return c + '_' + f;
		}
	}

	internal class VirtualNeededPropertyInfo : NeededPropertyInfo
	{
		public VirtualNeededPropertyInfo(int flidSource, NeededPropertyInfo parent, bool fSeq, int dstClsId)
			: base(flidSource, parent, fSeq)
		{
			m_targetClass = dstClsId;
		}

		/// <summary>
		/// Override: this class knows the appropriate destination class.
		/// </summary>
		public override int TargetClass(ISilDataAccess sda)
		{
			return m_targetClass;
		}
	}

}
