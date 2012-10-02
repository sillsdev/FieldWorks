// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: NMockConstraints.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Text;
using NMock.Constraints;

namespace SIL.FieldWorks.Test.TestUtils
{
	/// <summary>
	/// Checks that the array gets all expected values, but the order in which they appear
	/// doesn't matter.
	/// </summary>
	public class IgnoreOrderConstraint : BaseConstraint
	{
		private ArrayList m_expectedArgs;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new IgnoreOrder constraint object
		/// </summary>
		/// <param name="args"></param>
		/// ------------------------------------------------------------------------------------
		public IgnoreOrderConstraint(params object[] args)
		{
			m_expectedArgs = new ArrayList(args);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Evaluates the parameter
		/// </summary>
		/// <param name="val"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override bool Eval(object val)
		{
			if (val is Array)
			{
				Array args = (Array)val;
				if (args.Length != m_expectedArgs.Count)
					return false;

				foreach (object o in args)
				{
					if (!m_expectedArgs.Contains(o))
						return false;
				}
				return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expected parameter value. This gets displayed in case the parameter doesn't have
		/// the expected value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string Message
		{
			get
			{
				StringBuilder bldr = new StringBuilder();
				bldr.Append("{ ");
				for (int i = 0; i < m_expectedArgs.Count; i++)
				{
					if (i > 0)
						bldr.Append(", ");
					bldr.Append(m_expectedArgs[i]);
				}
				bldr.Append("}");

				return bldr.ToString();
			}
		}
	}
}
