// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
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
		protected int m_targetClass;

		/// <summary />
		/// <param name="listItemsClass">the class of objects at the root parent of the NeedPropertyInfo tree.
		/// Typically the destination class of flidSource</param>
		public NeededPropertyInfo(int listItemsClass)
		{
			m_targetClass = listItemsClass;
			Source = 0; // don't really how we got to the root parent class.
			Parent = null;
			IsSequence = true;
		}

		/// <summary />
		protected NeededPropertyInfo(int flidSource, NeededPropertyInfo parent, bool fSeq)
		{
			Source = flidSource;
			Parent = parent;
			IsSequence = fSeq;
		}

		/// <summary>
		/// The source property containing the objects about which we want info.
		/// </summary>
		public int Source { get; }

		/// <summary>
		/// Gets a value indicating whether this instance has atomic fields.
		/// </summary>
		public bool HasAtomicFields
		{
			get
			{
				if (AtomicFields.Count > 0)
				{
					return true;
				}
				foreach (var info in SeqFields)
				{
					if (!info.IsSequence)
					{
						return true;
					}
				}
				return false;
			}
		}

		/// <summary>
		/// Answer the class of objects for which we are collecting fields.
		/// By default this is the destination class of the field that contains them.
		/// We override this in a subclass for certain virtual and phony properties.
		/// </summary>
		internal int TargetClass(XmlVc vc)
		{
			return TargetClass(vc.DataAccess);
		}

		/// <summary>
		/// Answer the class of objects for which we are collecting fields.
		/// By default this is the destination class of the field that contains them.
		/// If needed, override this in a subclass for certain virtual and phony properties.
		/// </summary>
		public virtual int TargetClass(ISilDataAccess sda)
		{
			if (m_targetClass == 0 && Source != 0)
			{
				m_targetClass = sda.MetaDataCache.GetDstClsId(this.Source);
			}
			return m_targetClass;
		}

		/// <summary>
		/// Gets a value indicating whether this instance is sequence.
		/// </summary>
		public bool IsSequence { get; }

		/// <summary>
		/// Gets the parent.
		/// </summary>
		public NeededPropertyInfo Parent { get; }

		/// <summary>
		/// Gets the seq depth.
		/// </summary>
		public int SeqDepth
		{
			get
			{
				var depth = 0;
				var info = this;
				while (info != null)
				{
					if (info.IsSequence)
					{
						depth++;
					}
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
				var depth = 0;
				var info = Parent;
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
		public List<PropWs> AtomicFields { get; } = new List<PropWs>();

		/// <summary>
		/// Add an atomic flid.
		/// </summary>
		public void AddAtomicField(int flid, int ws)
		{
			var pw = new PropWs(flid, ws);
			if (AtomicFields.Contains(pw))
			{
				return;
			}
			AtomicFields.Add(pw);
		}

		/// <summary>
		/// Sequence properties of objects that can occur in the source field.
		/// </summary>
		public List<NeededPropertyInfo> SeqFields { get; } = new List<NeededPropertyInfo>();

		/// <summary>
		/// Add (or retrieve) info about a object (atomic or seq) flid. May include virtuals.
		/// </summary>
		public NeededPropertyInfo AddObjField(int flid, bool fSeq)
		{
			var info = SeqFields.Find(item => item.Source == flid);
			if (info == null)
			{
				info = new NeededPropertyInfo(flid, this, fSeq);
				SeqFields.Add(info);
			}
			return info;
		}

		/// <summary>
		/// Add (or retrieve) info about a virtual object flid.
		/// </summary>
		public NeededPropertyInfo AddVirtualObjField(int flid, bool fSeq, int dstClass)
		{
			var info = SeqFields.Find(item => item.Source == flid) as VirtualNeededPropertyInfo;
			if (info == null)
			{
				info = new VirtualNeededPropertyInfo(flid, this, fSeq, dstClass);
				SeqFields.Add(info);
			}
			return info;
		}

		internal void DumpFieldInfo(IFwMetaDataCache mdc)
		{
			if (Depth == 0)
			{
				Debug.WriteLine(string.Empty);
			}
			for (var i = 0; i < Depth; ++i)
			{
				Debug.Write("    ");
			}
			if (Source != 0)
			{
				Debug.WriteLine("[" + Depth + "]info.Source = " + Source + " = " +
				GetFancyFieldName(Source, mdc));
			}
			else
			{
				Debug.WriteLine("Root (target) class: " + mdc.GetClassName(m_targetClass));
			}
			for (var i = 0; i < AtomicFields.Count; ++i)
			{
				for (var j = 0; j < Depth; ++j)
				{
					Debug.Write("    ");
				}
				Debug.WriteLine("    Atomic[" + i + "] flid = " + AtomicFields[i].Flid + "(" + GetFancyFieldName(AtomicFields[i].Flid, mdc) + "); ws = " + AtomicFields[i].Ws);
			}
			foreach (var propertyInfo in SeqFields)
			{
				propertyInfo.DumpFieldInfo(mdc);
			}
		}

		private static string GetFancyFieldName(int flid, IFwMetaDataCache mdc)
		{
			var f = mdc.GetFieldName(flid);
			var c = mdc.GetOwnClsName(flid);
			return c + '_' + f;
		}

		private sealed class VirtualNeededPropertyInfo : NeededPropertyInfo
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
}