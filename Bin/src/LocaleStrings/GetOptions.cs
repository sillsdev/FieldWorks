using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.FieldWorks.Common.Utils
{
	class CommandLineOptions
	{
		/// <summary>
		/// This class must be subclassed for each type of argument present in the command line.
		/// </summary>
		public abstract class Param
		{
			protected string m_sShortName;
			protected string m_sLongName;
			protected string m_sDescription;
			protected bool m_fHasValue = false;

			public Param(string sShort, string sLong, string sDescription)
			{
				m_sShortName = sShort;
				m_sLongName = sLong;
				m_sDescription = sDescription;
			}

			public string ShortName
			{
				get { return m_sShortName; }
			}

			public string LongName
			{
				get { return m_sLongName; }
			}

			public string Description
			{
				get { return m_sDescription; }
			}

			public bool HasValue
			{
				get { return m_fHasValue; }
			}

			public virtual bool NeedArgument
			{
				get { return false; }
			}

			public virtual void SetValue(string sValue)
			{
			}
		}

		/// <summary>
		/// This class represents command line options which do not have an argument.
		/// </summary>
		public class BoolParam : Param
		{
			bool m_value;

			public BoolParam(string sShort, string sLong, string sDescription, bool defaultValue)
				: base(sShort, sLong, sDescription)
			{
				m_value = defaultValue;
			}

			public override void SetValue(string sValue)
			{
				m_value = !m_value;
				m_fHasValue = true;
			}

			public bool Value
			{
				get { return m_value; }
			}
		}

		/// <summary>
		/// This class represents command line options which take a single integer argument.
		/// </summary>
		public class IntParam : Param
		{
			int m_value;

			public IntParam(string sShort, string sLong, string sDescription, int defaultValue)
				: base(sShort, sLong, sDescription)
			{
				m_value = defaultValue;
			}

			public override bool NeedArgument
			{
				get { return true; }
			}

			public override void SetValue(string sValue)
			{
				int x;
				if (Int32.TryParse(sValue, out x))
				{
					m_value = x;
					m_fHasValue = true;
				}
				else
				{
					throw new Exception(String.Format("Invalid value \"{0}\" for -{1} (integer) option",
						sValue, m_sShortName));
				}
			}

			public int Value
			{
				get { return m_value; }
			}
		}

		/// <summary>
		/// This class represents command line options which take a single string argument.
		/// </summary>
		public class StringParam : Param
		{
			string m_value;

			public StringParam(string sShort, string sLong, string sDescription, string defaultValue)
				: base(sShort, sLong, sDescription)
			{
				m_value = defaultValue;
			}

			public override bool NeedArgument
			{
				get { return true; }
			}

			public override void SetValue(string sValue)
			{
				m_value = sValue;
				m_fHasValue = true;
			}

			public string Value
			{
				get { return m_value; }
			}
		}

		/// <summary>
		/// This class allows a command line option to appear more than once, storing the
		/// arguments in a list.
		/// </summary>
		public class StringListParam : Param
		{
			List<string> m_value = null;

			public StringListParam(string sShort, string sLong, string sDescription,
				List<string> defaultValue)
				: base(sShort, sLong, sDescription)
			{
				m_value = defaultValue;
			}

			public override bool NeedArgument
			{
				get { return true; }
			}

			public override void SetValue(string sValue)
			{
				if (m_value == null)
					m_value = new List<string>();
				m_value.Add(sValue);
			}

			public List<string> Value
			{
				get { return m_value; }
			}
		}

		static string s_sError = null;

		/// <summary>
		/// This static method parses the command line, storing the parameter values, and
		/// returning the index of the first non-option command line argument.
		/// </summary>
		/// <param name="args"></param>
		/// <param name="rgParam"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		static public bool Parse(string[] args, ref Param[] rgParam, out int index)
		{
			index = args.Length;
			try
			{
				for (int i = 0; i < args.Length; ++i)
				{
					if (args[i].StartsWith("-"))
					{
						bool fOk;
						if (args[i].StartsWith("--"))
							fOk = CheckLongParamNames(args, rgParam, ref i);
						else
							fOk = CheckShortParamNames(args, rgParam, ref i);
						if (!fOk)
							throw new Exception(String.Format("Invalid option found in {0}", args[i]));
					}
					else
					{
						index = i;
						return true;
					}
				}
				index = args.Length;
				return true;
			}
			catch (Exception ex)
			{
				s_sError = ex.Message;
				return false;
			}
		}

		private static bool CheckShortParamNames(string[] args, Param[] rgParam, ref int i)
		{
			for (int ind = 1; ind < args[i].Length; ++ind)
			{
				bool fOk = false;
				for (int j = 0; j < rgParam.Length; ++j)
				{
					if (rgParam[j].ShortName == args[i].Substring(ind, 1))
					{
						if (rgParam[j].NeedArgument)
						{
							if (++ind < args[i].Length)
							{
								rgParam[j].SetValue(args[i].Substring(ind));
								return true;
							}
							else if (++i < args.Length)
							{
								rgParam[j].SetValue(args[i]);
								return true;
							}
							else
							{
								throw new Exception(String.Format("Missing argument for -{0} option",
									rgParam[j].ShortName));
							}
						}
						else
						{
							rgParam[j].SetValue(String.Empty);
							fOk = true;
							break;
						}
					}
				}
				if (!fOk)
				{
					// UNKNOWN OPTION ERROR MESSAGE
					return false;
				}
			}
			return true;
		}

		private static bool CheckLongParamNames(string[] args, Param[] rgParam, ref int i)
		{
			for (int j = 0; j < rgParam.Length; ++j)
			{
				if (rgParam[j].LongName == args[i].Substring(2))
				{
					if (rgParam[j].NeedArgument)
					{
						if (++i < args.Length)
						{
							rgParam[j].SetValue(args[i]);
							return true;
						}
						else
						{
							throw new Exception(String.Format("Missing argument for --{0} option",
								rgParam[j].ShortName));
						}
					}
					else
					{
						rgParam[j].SetValue(String.Empty);
						return true;
					}
				}
			}
			// UNKNOWN OPTION ERROR MESSAGE
			return false;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="rgParam"></param>
		public static void Usage(Param[] rgParam)
		{
			int cLen = 0;
			for (int i = 0; i < rgParam.Length; ++i)
			{
				if (!String.IsNullOrEmpty(rgParam[i].LongName))
				{
					int cch = rgParam[i].LongName.Length;
					if (cLen < cch)
						cLen = cch;
				}
			}
			cLen += 4;
			for (int i = 0; i < rgParam.Length; ++i)
			{
				string sShort;
				if (String.IsNullOrEmpty(rgParam[i].ShortName))
					sShort = "  ";
				else
					sShort = String.Format("-{0}", rgParam[i].ShortName);
				string sLong;
				if (String.IsNullOrEmpty(rgParam[i].LongName))
					sLong = "    ";
				else
					sLong = String.Format("(--{0})", rgParam[i].LongName);
				while (sLong.Length < cLen)
					sLong = sLong + " ";
				string sLine = String.Format("  {0} {1} = {2}",
					sShort, sLong, rgParam[i].Description);
				Console.WriteLine(sLine);
			}
		}
	}
}
