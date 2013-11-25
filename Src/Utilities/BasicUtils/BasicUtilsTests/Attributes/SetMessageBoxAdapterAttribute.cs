// Copyright (c) 2012-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;

namespace SIL.Utils.Attributes
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// NUnit helper attribute that sets the message box adapter before running tests and
	/// resets it afterwards
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class |
		AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
	public class SetMessageBoxAdapterAttribute : TestActionAttribute
	{
		private static IMessageBox s_CurrentAdapter;
		private IMessageBox m_PreviousAdapter;
		private Type m_AdapterType;

		/// <summary/>
		public SetMessageBoxAdapterAttribute(): this(typeof(MessageBoxStub))
		{
		}

		/// <summary/>
		public SetMessageBoxAdapterAttribute(Type adapterType)
		{
			m_AdapterType = adapterType;
		}

		/// <summary>
		/// Set the message box adapter
		/// </summary>
		public override void BeforeTest(TestDetails testDetails)
		{
			base.BeforeTest(testDetails);
			m_PreviousAdapter = s_CurrentAdapter;
			s_CurrentAdapter = (IMessageBox)Activator.CreateInstance(m_AdapterType);
			MessageBoxUtils.Manager.SetMessageBoxAdapter(s_CurrentAdapter);
		}

		/// <summary>
		/// Restore previous message box adapter
		/// </summary>
		public override void AfterTest(TestDetails testDetails)
		{
			base.AfterTest(testDetails);

			s_CurrentAdapter = m_PreviousAdapter;
			if (s_CurrentAdapter != null)
				MessageBoxUtils.Manager.SetMessageBoxAdapter(s_CurrentAdapter);
			else
				MessageBoxUtils.Manager.Reset();
		}
	}
}
