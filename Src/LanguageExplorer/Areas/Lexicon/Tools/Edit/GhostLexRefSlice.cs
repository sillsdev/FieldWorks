// Copyright (c) 2014-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.Framework.DetailControls;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// A slice that is used for the ghost "Components" and "Variant of" fields of ILexEntry.
	/// It tries to behave like an empty list of components, but in fact the only real content is
	/// the chooser button, which behaves much like the one in an EntrySequenceReferenceSlice
	/// as used to display the ComponentLexemes of a LexEntryRef, except the list is always
	/// empty and hence not really there.
	/// </summary>
	internal sealed class GhostLexRefSlice : Slice
	{
		/// <summary />
		public GhostLexRefSlice()
		{
		}

		/// <summary />
		public override void FinishInit()
		{
			this.Control = new GhostLexRefLauncher(m_obj, m_configurationNode);
		}

		/// <summary />
		public override void Install(DataTree parent)
		{
			// JohnT: This is an awful way to make the button fit neatly, but I can't find a better one.
			Control.Height = Height;
			// It doesn't need most of the usual info, but the Mediator is important if the user
			// asks to Create a new lex entry from inside the first dialog (LT-9679).
			// We'd pass 0 and null for flid and fieldname, but there are Asserts to prevent this.
			var btnLauncher = (ButtonLauncher) Control;
			if (btnLauncher.PropertyTable == null)
			{
				btnLauncher.InitializeFlexComponent(PropertyTable, Publisher, Subscriber);
			}
			btnLauncher.Initialize(m_cache, m_obj, 1, "nonsence", null, null, null);
			base.Install(parent);
		}
	}
}
