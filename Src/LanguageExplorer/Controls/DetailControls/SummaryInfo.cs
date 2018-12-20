// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// This is a default summary info suitable for a concordance where the
	/// summary words are just extracted from text, and don't have any
	/// object they represent. Hence, it is initialized simply with a string
	/// and a list of children.
	/// </summary>
	public class SummaryInfo : IConcSliceInfo, IKeyedObject
	{
		private readonly IList m_children;

		public SummaryInfo(ITsString content, IList children)
		{
			ContentString = content;
			m_children = children;
		}

		/// <summary>
		/// Don't have an interesting object.
		/// </summary>
		public int Hvo { get; } = 0;

		/// <summary>
		/// Have no flid
		/// </summary>
		public int ContentStringFlid { get; } = 0;

		/// <summary>
		/// So we just provide the string to display.
		/// </summary>
		public ITsString ContentString { get; }

		public string Key => ContentString.Text;

		/// <summary>
		/// A string that isn't an object field can't be edited.
		/// </summary>
		public bool AllowContentEditing { get; } = false;

		/// <summary>
		/// It's not a context string.
		/// </summary>
		public virtual bool DisplayAsContext { get; } = false;

		/// <summary>
		/// Should not be called
		/// </summary>
		public int ContextStringStartOffset
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Should not be called
		/// </summary>
		public int ContextStringLength
		{
			get
			{
				throw new NotSupportedException();
			}
		}
		/// <summary>
		/// This allows the client to supply a view constructor to control what
		/// appears in the slice. As it returns null, the standard summary VC is used.
		/// </summary>
		public virtual IVwViewConstructor Vc { get; } = null;

		/// <summary>
		/// Returns the number of children.
		/// </summary>
		public int Count => m_children.Count;

		/// <summary>
		/// Returns the specified child.
		/// </summary>
		public IConcSliceInfo ChildAt(int index)
		{
			return (IConcSliceInfo)m_children[index];
		}
	}
}