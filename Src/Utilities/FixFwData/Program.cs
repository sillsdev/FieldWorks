// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Program.cs
// Responsibility: mcconnel
// ---------------------------------------------------------------------------------------------
using System;
using System.Xml.Linq;
using System.Xml;
using System.IO;

using SIL.FieldWorks.FixData;
using SIL.FieldWorks.Common.FwUtils;

namespace FixFwData
{
	class Program
	{
		static void Main(string[] args)
		{
			string pathname = args[0];
			ConsoleProgress prog = new ConsoleProgress();
			FwData data = new FwData(pathname, prog);
			data.FixErrorsAndSave();
			if (prog.DotsWritten)
				Console.WriteLine();
			foreach (var err in data.Errors)
				Console.WriteLine(err);
		}
	}

	/// <summary>
	/// Utility class to provide progress reporting via console output instead of a dialog box.
	/// </summary>
	class ConsoleProgress : IProgress
	{
		int m_min = 0;
		int m_max = 100;
		string m_message;
		int m_pos = 0;
		int m_dots = 0;
		int m_grain = 1;

		/// <summary>
		/// Let the caller know whether we've written any dots out for progress reporting.
		/// </summary>
		public bool DotsWritten
		{
			get { return m_dots > 0; }
		}

		#region IProgress Members

		public bool Canceled
		{
			get { throw new NotImplementedException(); }
		}

		public System.Windows.Forms.Form Form
		{
			get { throw new NotImplementedException(); }
		}

		public int Maximum
		{
			get { return m_max; }
			set
			{
				m_max = value;
				ComputeGranularity();
			}
		}

		private void ComputeGranularity()
		{
			m_grain = ((m_max - m_min) + 79) / 80;
			if (m_grain <= 0)
				m_grain = 1;
		}

		public string Message
		{
			get { return m_message; }
			set
			{
				m_message = value;
				if (DotsWritten)
					Console.WriteLine();
				Console.WriteLine(m_message);
				m_dots = 0;
			}
		}

		public int Minimum
		{
			get { return m_min; }
			set
			{
				m_min = value;
				ComputeGranularity();
			}
		}

		public int Position
		{
			get { return m_pos; }
			set
			{
				m_pos = value;
			}
		}

		public void Step(int amount)
		{
			++m_pos;
			if ((m_pos % m_grain) == 0)
			{
				Console.Write('.');
				++m_dots;
			}
		}

		public int StepSize
		{
			get { return 1; }
			set { }
		}

		public string Title
		{
			get { return String.Empty; }
			set { }
		}

		#endregion
	}
}
