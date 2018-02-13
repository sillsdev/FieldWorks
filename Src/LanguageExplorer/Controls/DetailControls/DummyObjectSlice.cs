// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class DummyObjectSlice : Slice
	{
		private XElement m_node; // Node with name="seq" that controls the sequence we're a dummy for
		// Path of parent slice info up to and including m_node.
		// We can't use a List<int>, as the Arraylist may hold XmlNodes and ints, at least.
		private ArrayList m_path;
		private readonly int m_flid; // sequence field we're a dummy for
		private int m_ihvoMin; // index in sequence of first object we stand for.
		private readonly string m_layoutName;
		private readonly string m_layoutChoiceField;
		private XElement m_caller; // Typically "partRef" node that invoked the part containing the <seq>

		/// <summary>
		/// Create a slice. Note that callers that will further modify path should pass a Clone.
		/// </summary>
		public DummyObjectSlice(int indent, XElement node, ArrayList path, ICmObject obj, int flid, int ihvoMin, string layoutName, string layoutChoiceField, XElement caller)
		{
			Indent = indent;
			m_node = node;
			m_path = path;
			Object = obj;
			m_flid = flid;
			m_ihvoMin = ihvoMin;
			m_layoutName = layoutName;
			m_layoutChoiceField = layoutChoiceField;
			m_caller = caller;
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.
				m_path?.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_node = null;
			m_path = null;
			m_caller = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		public override bool IsRealSlice => false;

		/// <summary>
		/// Turn this dummy slice into whatever it stands for, replacing itself in the data tree's
		/// slices (where it occupies slot index) with whatever is appropriate.
		/// </summary>
		public override Slice BecomeReal(int index)
		{
			// We stand in for the slice at 'index', and that is to be replaced. But we might stand for earlier
			// slices too: how many indicates what we have to add to m_ihvoMin.

			// Note: I (RandyR) don't think the same one can stand in for multiple dummies now.
			// We don't use a dummy slice in more than one place.
			// Each are created individually, if more than one is needed.
			var ihvo = m_ihvoMin;
			for (var islice = index - 1; islice >= 0 && ContainingDataTree.Slices[islice] == this; islice--)
			{
				ihvo++;
			}
			var hvo = Cache.DomainDataByFlid.get_VecItem(Object.Hvo, m_flid, ihvo);
			// In the course of becoming real, we may get disposed. That clears m_path, which
			// has various bad effects on called objects that are trying to use it, as well as
			// causing failure here when we try to remove the thing we added temporarily.
			// Work with a copy, so Dispose can't get at it.
			var path = new ArrayList(m_path);
			if (ihvo == m_ihvoMin)
			{
				// made the first element real. Increment start ihvo: the first thing we are a
				// dummy for got one greater
				m_ihvoMin++;
			}
			else if (index < ContainingDataTree.Slices.Count && ContainingDataTree.Slices[index + 1] == this)
			{
				// Any occurrences after index get replaced by a new one with suitable ihvoMin.
				// Note this must be done before we insert an unknown number of extra slices
				// by calling CreateSlicesFor.
				var dosRep = new DummyObjectSlice(Indent, m_node, path, Object, m_flid, ihvo + 1, m_layoutName, m_layoutChoiceField, m_caller) {Cache = Cache, ParentSlice = ParentSlice};
				for (var islice = index + 1;
					islice < ContainingDataTree.Slices.Count && ContainingDataTree.Slices[islice] == this;
					islice++)
				{
					ContainingDataTree.RawSetSlice(islice, dosRep);
				}
			}

			// Save these, we may get disposed soon, can't get them from member data any more.
			var containingTree = ContainingDataTree;
			var parentSlice = ParentSlice;
			path.Add(hvo);
			var objItem = ContainingDataTree.Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
			var oldPos = ContainingDataTree.AutoScrollPosition;
			ContainingDataTree.CreateSlicesFor(objItem, parentSlice, m_layoutName, m_layoutChoiceField, Indent, index + 1, path, new ObjSeqHashMap(), m_caller);
			// If inserting slices somehow altered the scroll position, for example as the
			// silly Panel tries to make the selected control visible, put it back!
			if (containingTree.AutoScrollPosition != oldPos)
			{
				containingTree.AutoScrollPosition = new Point(-oldPos.X, -oldPos.Y);
			}
			// No need to remove, we added to copy.
			return containingTree.Slices.Count > index + 1 ? containingTree.Slices[index + 1] as Slice : null;
		}

		protected override void WndProc(ref Message m)
		{
			var aspY = AutoScrollPosition.Y;
			base.WndProc (ref m);
#if DEBUG
			if (aspY != AutoScrollPosition.Y)
			{
				Debug.WriteLine("ASP changed during processing message " + m.Msg);
			}
#endif
		}
	}
}