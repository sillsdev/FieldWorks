// Copyright (c) 2004-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// Use this class to collect the objects displayed in a cell.
	/// By default, we'll assume the objects being displayed in the cell are of the same
	/// basic class.
	/// </summary>
	internal class ItemsCollectorEnv : CollectorEnv
	{
		private readonly HashSet<int> m_hvosInCell = new HashSet<int>();

		/// <summary>
		/// This constructor should be used if you want to provide a separate cache decorator.
		/// </summary>
		public ItemsCollectorEnv(IVwEnv env, ISilDataAccess sda, int hvoRoot)
			: base(env, sda, hvoRoot)
		{
		}

		/// <summary>
		/// Return the list of hvos used to build the display in DisplayCell.
		/// </summary>
		public ISet<int> HvosCollectedInCell
		{
			get
			{
				// Allow a zero in the set only if it's the only element.
				if (m_hvosInCell.Count > 1 && m_hvosInCell.Contains(0))
				{
					m_hvosInCell.Remove(0);
				}
				return m_hvosInCell;
			}
		}

		/// <summary />
		public override void AddResultString(string s)
		{
			base.AddResultString(s);
			AddOwnerOfBasicPropToCellHvos();
		}

		private void AddOwnerOfBasicPropToCellHvos()
		{
			var top = PeekStack;
			if (top != null)
			{
				m_hvosInCell.Add(top.m_hvo);
			}
		}

		/// <summary />
		public override void AddIntProp(int tag)
		{
			base.AddIntProp(tag);
			AddOwnerOfBasicPropToCellHvos();
		}

		/// <summary />
		public override void AddGenDateProp(int tag)
		{
			base.AddGenDateProp(tag);
			AddOwnerOfBasicPropToCellHvos();
		}

		/// <summary />
		public override void AddTimeProp(int tag, uint flags)
		{
			base.AddTimeProp(tag, flags);
			AddOwnerOfBasicPropToCellHvos();
		}

		/// <summary />
		public override void AddObjProp(int tag, IVwViewConstructor vc, int frag)
		{
			base.AddObjProp(tag, vc, frag);
			var hvoItem = m_sda.get_ObjectProp(m_hvoCurr, tag);
			if (hvoItem == 0 && m_hvosInCell.Count == 0)
			{
				m_hvosInCell.Add(0);
			}
		}
	}
}