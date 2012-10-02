// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2004' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: NMockConstraints.cs
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using NMock.Constraints;
using SIL.Utils;

namespace SIL.FieldWorks.Test.TestUtils
{
	/// <summary>
	/// Checks that the array gets all expected values, but the order in which they appear
	/// doesn't matter.
	/// </summary>
	public class IgnoreOrderConstraint : BaseConstraint
	{
		private List<object> m_expectedArgs;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new IgnoreOrder constraint object
		/// </summary>
		/// <param name="args"></param>
		/// ------------------------------------------------------------------------------------
		public IgnoreOrderConstraint(params object[] args)
		{
			m_expectedArgs = new List<object>(args);
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
			Array args = val as Array;
			if (args != null)
			{
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
				bldr.Append(m_expectedArgs.ToString(", "));
				bldr.Append("}");

				return bldr.ToString();
			}
		}
	}
}
