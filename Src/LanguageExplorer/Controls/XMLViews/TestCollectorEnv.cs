// Copyright (c) 2006-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This subclass is used to test whether the call to Display produces anything.
	/// Adding literals doesn't count.
	/// </summary>
	internal class TestCollectorEnv : CollectorEnv, ICollectPicturePathsOnly
	{
		private readonly HashSet<int> m_notedStringPropertyDependencies = new HashSet<int>();

		/// <summary />
		/// <param name="baseEnv">The base env.</param>
		/// <param name="sda">Date access to get prop values etc.</param>
		/// <param name="hvoRoot">The root object to display, if m_baseEnv is null.
		/// If baseEnv is not null, hvoRoot is ignored.</param>
		public TestCollectorEnv(IVwEnv baseEnv, ISilDataAccess sda, int hvoRoot)
			: base(baseEnv, sda, hvoRoot)
		{
		}

		/// <summary>
		/// Gets the result.
		/// </summary>
		public bool Result { get; private set; } = false;

		/// <summary>
		/// This collector is done as soon as we know there's something there.
		/// </summary>
		protected override bool Finished => Result;

		/// <summary>
		/// Accumulate a string into our result. The base implementation does nothing.
		/// </summary>
		public override void AddResultString(string s)
		{
			base.AddResultString(s);
			if (string.IsNullOrEmpty(s))
			{
				return;
			}
			// Review JohnT: should we test for non-blank? Maybe that could be configurable?
			Result = true;
		}

		/// <summary />
		public override void AddStringAltMember(int tag, int ws, IVwViewConstructor _vwvc)
		{
			base.AddStringAltMember(tag, ws, _vwvc);
			// (LT-6224) if we want the display to update for any empty multistring alternatives,
			// we will just note a dependency on the string property, and that will cover all
			// of the writing systems for that string property
			// We only need to do this once.
			if (NoteEmptyDependencies && !m_notedStringPropertyDependencies.Contains(tag))
			{
				NoteEmptyDependency(tag);
				// NOTE: This should get cleared in OpenTheObject().
				m_notedStringPropertyDependencies.Add(tag);
			}
		}

		/// <summary>
		/// We need to clear the list for noting string property dependencies
		/// any time we're in the context of a different object.
		/// </summary>
		protected override void OpenTheObject(int hvo, int ihvo)
		{
			m_notedStringPropertyDependencies.Clear();
			base.OpenTheObject(hvo, ihvo);
		}

		/// <summary>
		/// Do nothing. We don't want to count literals in this test class.
		/// </summary>
		public override void AddString(ITsString tss)
		{
		}

		/// <summary>
		/// Flag whether or not to note dependencies on empty properties.
		/// </summary>
		public bool NoteEmptyDependencies { get; set; } = false;

		/// <summary>
		/// When testing whether anything exists, add notifiers for empty properties if the
		/// caller has so requested.
		/// </summary>
		public override void AddObjProp(int tag, IVwViewConstructor vc, int frag)
		{
			base.AddObjProp(tag, vc, frag);
			if (NoteEmptyDependencies)
			{
				NoteEmptyObjectDependency(tag);
			}
		}

		/// <summary>
		/// When testing whether anything exists, add notifiers for empty vectors if the
		/// caller has so requested.
		/// </summary>
		public override void AddObjVec(int tag, IVwViewConstructor vc, int frag)
		{
			base.AddObjVec(tag, vc, frag);
			if (NoteEmptyDependencies)
			{
				NoteEmptyVectorDependency(tag);
			}
		}

		/// <summary>
		/// When testing whether anything exists, add notifiers for empty vectors if the
		/// caller has so requested.
		/// </summary>
		public override void AddReversedObjVecItems(int tag, IVwViewConstructor vc, int frag)
		{
			base.AddReversedObjVecItems(tag, vc, frag);
			if (NoteEmptyDependencies)
			{
				NoteEmptyVectorDependency(tag);
			}
		}

		private void NoteEmptyObjectDependency(int tag)
		{
			var hvoObj = DataAccess.get_ObjectProp(OpenObject, tag);
			if (hvoObj == 0)
			{
				NoteEmptyDependency(tag);
			}
		}

		private void NoteEmptyVectorDependency(int tag)
		{
			var chvo = DataAccess.get_VecSize(OpenObject, tag);
			if (chvo == 0)
			{
				NoteEmptyDependency(tag);
			}
		}

		private void NoteEmptyDependency(int tag)
		{
			NoteDependency(new[] { OpenObject }, new[] { tag }, 1);
		}

		/// <summary>
		/// The only thing a test environment cares about pictures is that we got one, so it is not empty.
		/// </summary>
		public void APictureIsBeingAdded()
		{
			Result = true;
		}
	}
}