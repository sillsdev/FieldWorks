using System;
using System.Reflection;                // to access SpellFixer via reflection (so it doesn't have to be present)
using Microsoft.Win32;                  // for RegistryKey
using ECInterfaces;                     // for IEncConverter
using System.Drawing;                   // for Font

namespace SilEncConverters31
{
	public class SpellFixerByReflection
	{
		// this is directly from SpellFixerEC.cs, but needed to avoid the dependency
		public const string cstrAttributeFontToUse = "SpellingFixer Display Font";

		internal const string cstrSpellFixerProgID = "SpellingFixerEC.SpellingFixerEC";

		private Object m_aSpellFixer = null;
		private MethodInfo m_fnAssignCorrectSpelling = null;
		private MethodInfo m_fnFindReplacementRule = null;
		private MethodInfo m_fnQueryForSpellingCorrectionIfTableEmpty = null;
		private MethodInfo m_fnLoginProject = null;
		private MethodInfo m_fnEditSpellingFixes = null;
		private PropertyInfo m_propSpellFixerEncConverter = null;
		private PropertyInfo m_propSpellFixerEncConverterName = null;
		private PropertyInfo m_propProjectFont = null;

		public SpellFixerByReflection()
		{
			// double-check that this should work...
			Type typeSpellFixer = Type.GetTypeFromProgID(cstrSpellFixerProgID);
			if (typeSpellFixer != null)
			{
				Type[] aTypeParams = new Type[] { typeof(string) };
				m_fnAssignCorrectSpelling = typeSpellFixer.GetMethod("AssignCorrectSpelling", aTypeParams);
				m_fnFindReplacementRule = typeSpellFixer.GetMethod("FindReplacementRule", aTypeParams);
				m_fnQueryForSpellingCorrectionIfTableEmpty = typeSpellFixer.GetMethod("QueryForSpellingCorrectionIfTableEmpty", aTypeParams);
				m_fnLoginProject = typeSpellFixer.GetMethod("LoginProject");
				m_fnEditSpellingFixes = typeSpellFixer.GetMethod("EditSpellingFixes");
				m_propSpellFixerEncConverter = typeSpellFixer.GetProperty("SpellFixerEncConverter");
				m_propSpellFixerEncConverterName = typeSpellFixer.GetProperty("SpellFixerEncConverterName");
				m_propProjectFont = typeSpellFixer.GetProperty("ProjectFont");
				m_aSpellFixer = Activator.CreateInstance(typeSpellFixer);
			}
		}

		public void AssignCorrectSpelling(string strInput)
		{
			if (m_fnAssignCorrectSpelling != null)
			{
				object[] oParams = new object[] { strInput };
				m_fnAssignCorrectSpelling.Invoke(m_aSpellFixer, oParams);
			}
		}

		public void FindReplacementRule(string strWord)
		{
			if (m_fnFindReplacementRule != null)
			{
				object[] oParams = new object[] { strWord };
				m_fnFindReplacementRule.Invoke(m_aSpellFixer, oParams);
			}
		}

		public void QueryForSpellingCorrectionIfTableEmpty(string strBadWord)
		{
			if (m_fnQueryForSpellingCorrectionIfTableEmpty != null)
			{
				object[] oParams = new object[] { strBadWord };
				m_fnQueryForSpellingCorrectionIfTableEmpty.Invoke(m_aSpellFixer, oParams);
			}
		}

		public void LoginProject()
		{
			if (m_fnLoginProject != null)
			{
				m_fnLoginProject.Invoke(m_aSpellFixer, null);
			}
		}

		public void EditSpellingFixes()
		{
			if (m_fnEditSpellingFixes != null)
			{
				m_fnEditSpellingFixes.Invoke(m_aSpellFixer, null);
			}
		}

		public IEncConverter SpellFixerEncConverter
		{
			get
			{
				IEncConverter aEC = null;
				if (m_propSpellFixerEncConverter != null)
					aEC = (IEncConverter)m_propSpellFixerEncConverter.GetValue(m_aSpellFixer, null);
				return aEC;
			}
		}

		public string SpellFixerEncConverterName
		{
			get
			{
				string strName = null;
				if (m_propSpellFixerEncConverterName != null)
					strName = (string)m_propSpellFixerEncConverterName.GetValue(m_aSpellFixer, null);
				return strName;
			}
		}

		public Font ProjectFont
		{
			get
			{
				Font font = null;
				if (m_propProjectFont != null)
					font = (Font)m_propProjectFont.GetValue(m_aSpellFixer, null);
				return font;
			}
		}

		public static bool IsSpellFixerAvailable
		{
			get
			{
				RegistryKey keySF = Registry.ClassesRoot.OpenSubKey(cstrSpellFixerProgID, false);
				if (keySF != null)
				{
					try
					{
						// if we can get the type, then we should be able to instantiate it (don't waste
						//  the time actually trying to instantiate it, a) because it takes a long time, and
						//  b) unless we *really* want to load it, we don't want the user to have to choose
						//  a project (the default constructor of SF puts up a choose project dialog).
						Type typeSpellFixer = Type.GetTypeFromProgID(cstrSpellFixerProgID);
						if (typeSpellFixer != null)
							return true;
					}
					catch { }
				}

				return false;
			}
		}
	}
}