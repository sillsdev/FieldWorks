// Copyright (c) 2012-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using SIL.FieldWorks.Common.FwUtils;

namespace FieldWorks.TestUtilities.Attributes
{
	/// <summary>
	/// NUnit helper attribute that sets the message box adapter before running tests and
	/// resets it afterwards
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
	public class SetMessageBoxAdapterAttribute : TestActionAttribute
	{
		private static IMessageBox s_CurrentAdapter;
		private IMessageBox m_PreviousAdapter;
		private Type m_AdapterType;

		/// <inheritdoc />
		public SetMessageBoxAdapterAttribute()
			: this(typeof(MessageBoxStub))
		{
		}

		/// <summary/>
		public SetMessageBoxAdapterAttribute(Type adapterType)
		{
			m_AdapterType = adapterType;
		}

<<<<<<< HEAD:Src/FieldWorks.TestUtilities/Attributes/SetMessageBoxAdapterAttribute.cs
		/// <inheritdoc />
		public override void BeforeTest(ITest testDetails)
||||||| f013144d5:Src/Common/FwUtils/FwUtilsTests/Attributes/SetMessageBoxAdapterAttribute.cs
		/// <summary>
		/// Set the message box adapter
		/// </summary>
		public override void BeforeTest(TestDetails testDetails)
=======
		/// <summary>
		/// Set the message box adapter
		/// </summary>
		public override void BeforeTest(ITest test)
>>>>>>> develop:Src/Common/FwUtils/FwUtilsTests/Attributes/SetMessageBoxAdapterAttribute.cs
		{
			base.BeforeTest(test);
			m_PreviousAdapter = s_CurrentAdapter;
			s_CurrentAdapter = (IMessageBox)Activator.CreateInstance(m_AdapterType);
			MessageBoxUtils.SetMessageBoxAdapter(s_CurrentAdapter);
		}

<<<<<<< HEAD:Src/FieldWorks.TestUtilities/Attributes/SetMessageBoxAdapterAttribute.cs
		/// <inheritdoc />
		public override void AfterTest(ITest testDetails)
||||||| f013144d5:Src/Common/FwUtils/FwUtilsTests/Attributes/SetMessageBoxAdapterAttribute.cs
		/// <summary>
		/// Restore previous message box adapter
		/// </summary>
		public override void AfterTest(TestDetails testDetails)
=======
		/// <summary>
		/// Restore previous message box adapter
		/// </summary>
		public override void AfterTest(ITest test)
>>>>>>> develop:Src/Common/FwUtils/FwUtilsTests/Attributes/SetMessageBoxAdapterAttribute.cs
		{
			base.AfterTest(test);

			s_CurrentAdapter = m_PreviousAdapter;
			if (s_CurrentAdapter != null)
			{
				MessageBoxUtils.SetMessageBoxAdapter(s_CurrentAdapter);
			}
			else
			{
				MessageBoxUtils.Reset();
			}
		}
	}
}
