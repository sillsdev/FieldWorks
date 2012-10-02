// FmtWrtSysDlgModel.cs
// User: Jean-Marc Giffin at 12:29 P 16/06/2008

using System;
using SIL.FieldWorks.Common.Framework;

namespace SIL.FieldWorks.WorldPad
{
	public class FmtWrtSysDlgModel : IDialogModel
	{
		public struct Language {
			private string name_;
			private string description_;
			private string code_;

			public Language(string name, string description, string code)
			{
				name_ = name;
				description_ = description;
				code_ = code;
			}

			public string Name
			{
				get { return name_; }
				set { name_ = value; }
			}

			public string Description
			{
				get { return description_; }
				set { description_ = value; }
			}

			public string Code
			{
				get { return code_; }
				set { code_ = value; }
			}
		}

		public static Language LANGUAGE_ENGLISH = new Language("English", "Our Language", "123");
		public static Language LANGUAGE_SOOBANESE = new Language("Soobanese", "Foreign Language", "456");
		public static Language LANGUAGE_OOGA_BOOGA = new Language("Ooga Booga", "Mark's Language", "789");
		public static Language[] languages_ = { LANGUAGE_ENGLISH, LANGUAGE_SOOBANESE, LANGUAGE_OOGA_BOOGA };

		private Gtk.TreeView writingSystemsList_;
		private Gtk.TreeStore store_;
		private int selection_;

		public FmtWrtSysDlgModel()
		{
		}

		public Gtk.TreeView WritingSystemsList
		{
			set { writingSystemsList_ = value; }
			get { return writingSystemsList_; }
		}

		public Language CurrentLanguage
		{
			set { }
			get { return languages_[selection_]; }
		}

		public Language[] Languages
		{
			get { return languages_; }
			set { }
		}

		public int Selection
		{
			set	{ selection_ = value; }
			get { return selection_; }
		}

		public static string[] GetLanguageNames() {
			string[] names = new string[languages_.Length];
			for (int i = 0; i < languages_.Length; i++)
				names[i] = languages_[i].Name;
			return names;
		}
	}
}
