// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// Original author: Tom Hindle 2010-12-30

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using SIL.FieldWorks;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace LinuxSmokeTest
{
	/// <summary>
	/// This class is instantiated in the app domain used to run fieldworks.
	/// It used to return information to the unittest app domain from the fieldworks app domain.
	/// It is also used to perform simple modifications to the running fieldworks.
	/// </summary>
	public class FieldWorksInfo : MarshalByRefObject
	{
		/// <summary>
		/// Get the name of the first form.
		/// </summary>
		public string GetMainFormName()
		{
			Form mainForm = GetMainForm();
			if (mainForm == null)
				return String.Empty;

			return (mainForm.Name);
		}

		/// <summary>
		/// Close down fieldworks. returns true on success.
		/// </summary>
		public bool Close()
		{
			Form mainForm = GetMainForm();
			if (mainForm == null)
				return false;

			if (mainForm.GetType().ToString() == "SIL.FieldWorks.XWorks.FwXWindow")
			{
				// REVIEW: possibly use mediator to invoke ExitApplication event
				MethodInfo method = mainForm.GetType().GetMethod("OnExitApplication");
				method.Invoke(mainForm, new object[] {null});

				return true;
			}

			if (mainForm.GetType().ToString() == "SIL.FieldWorks.TE.TeMainWnd")
			{
				// REVIEW: possibly use mediator to invoke FileExit event
				MethodInfo method = mainForm.GetType().GetMethod("OnFileExit");
				method.Invoke(mainForm, new object[] {null});

				return true;
			}

			return false;
		}

		/// <summary>
		/// Make fieldworks think that we are not unittests.
		/// </summary>
		public void PretendNotRunningUnitTests()
		{
			MiscUtils.RunningTests = false;
		}

		/// <summary>
		/// Extra cleanup that fieldworks doesn't do correctly.
		/// </summary>
		public void Dispose()
		{
			// StructureMapService doesn't close this
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			Marshal.FinalReleaseComObject(tsf);
		}

		internal Form GetMainForm()
		{
			FormCollection forms = Application.OpenForms;
			if (forms.Count == 0)
				return null;

			return forms[0];
		}
	}

	/// <summary>
	/// HelperClass used by LinuxSmokeTest Unittests.
	/// </summary>
	public class LinuxSmokeTestHelper : IDisposable
	{
		/// <summary>Valid values "Te" or "Flex"</summary>
		public string App { set; internal get; }

		/// <summary>database file</summary>
		public string Db { set; internal get; }

		/// <summary>
		/// app domain that FieldWorks runs in.
		/// </summary>
		protected AppDomain m_FieldWorksDomain;

		/// <summary>
		/// Thread that creates and run the FieldWorks AppDomain.
		/// </summary>
		protected Thread m_applicationThread;

		/// <summary>
		/// Allows querying and modifying Fieldworks from outside its app domain.
		/// </summary>
		protected FieldWorksInfo m_fieldWorksInfo;

		/// <summary>
		/// Start an instance of FieldWorks in its own AppDomain.
		/// </summary>
		public void Start()
		{
			if (String.IsNullOrEmpty(App))
				throw new ApplicationException("App must be set before running FieldWorks");

			if (String.IsNullOrEmpty(Db))
				throw new ApplicationException("Db must be set before running FieldWorks");

			m_applicationThread = new Thread(StartFieldWorksThread);
			m_applicationThread.Start();
		}

		/// <summary>
		/// Attempt to cleanly close fieldworks.
		/// </summary>
		public void CloseFieldWorks()
		{
			GetFieldWorksInfo().Close();
		}

		/// <summary>
		/// Get the name of main FieldWorks form.
		/// </summary>
		public string GetMainApplicationFormName()
		{
			return GetFieldWorksInfo().GetMainFormName();
		}

		#region IDisposable implementation
		public void Dispose()
		{
			if (GetFieldWorksInfo() != null)
				GetFieldWorksInfo().Dispose();

			if (m_FieldWorksDomain != null)
				AppDomain.Unload(m_FieldWorksDomain);

			if (m_applicationThread != null && m_applicationThread.IsAlive)
			{
				m_applicationThread.Abort();
				m_applicationThread.Join(TimeSpan.FromSeconds(2));
				throw new NonCleanShutdownException();
			}
		}
		#endregion

		internal void StartFieldWorksThread()
		{
			m_FieldWorksDomain = AppDomain.CreateDomain("Running FieldWorks Domain");
			GetFieldWorksInfo().PretendNotRunningUnitTests();
			// FIXME: use FwAppArgs.kProject and FwAppArgs.kApp
			m_FieldWorksDomain.ExecuteAssembly(typeof(FieldWorks).Assembly.Location, null, new string[] {"-app", App, "-db", Db});
		}

		/// <summary>
		/// Create FieldWorksInfo instance in the FieldWorksDomain, and return a proxy.
		/// Or return previous created proxy.
		/// </summary>
		internal FieldWorksInfo GetFieldWorksInfo()
		{
			if (m_fieldWorksInfo != null)
				return m_fieldWorksInfo;

			try
			{
				m_FieldWorksDomain.Load(Assembly.GetExecutingAssembly().GetName());
				m_fieldWorksInfo = (FieldWorksInfo)m_FieldWorksDomain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, "LinuxSmokeTest.FieldWorksInfo");
			}
			catch(Exception e)
			{
				Debug.WriteLine(e);
				throw;
			}

			return m_fieldWorksInfo;
		}
	}
}
