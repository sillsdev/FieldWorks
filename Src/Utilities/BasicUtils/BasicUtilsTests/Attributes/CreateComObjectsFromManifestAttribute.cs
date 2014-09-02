// Copyright (c) 2012-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using NUnit.Framework;

namespace SIL.Utils.Attributes
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// NUnit helper class that allows to create COM objects from a manifest file
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class |
		AttributeTargets.Interface)]
	[SuppressUnmanagedCodeSecurity]
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="m_activationContext and m_currentActivation are disposed in AfterTest method")]
	public class CreateComObjectsFromManifestAttribute : TestActionAttribute
	{
#if !__MonoCS__
		private ActivationContextHelper m_activationContext;
		private IDisposable m_currentActivation;
#endif

		/// <summary/>
		public override ActionTargets Targets
		{
			get { return ActionTargets.Suite; }
		}

		/// <summary/>
		public override void BeforeTest(TestDetails testDetails)
		{
			base.BeforeTest(testDetails);

#if !__MonoCS__
			m_activationContext = new ActivationContextHelper("FieldWorks.Tests.manifest");
			m_currentActivation = m_activationContext.Activate();
#endif
		}

		/// <summary/>
		public override void AfterTest(TestDetails testDetails)
		{
#if !__MonoCS__
			m_currentActivation.Dispose();
			m_activationContext.Dispose();
#endif

			base.AfterTest(testDetails);
		}
	}
}
