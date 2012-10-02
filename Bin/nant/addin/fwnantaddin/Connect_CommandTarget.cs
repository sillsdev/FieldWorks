using System;
using System.Collections.Specialized;
using Extensibility;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.CommandBars;

namespace FwNantAddin2
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class Connect : IDTCommandTarget
	{
		#region Callbacks
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a new solution is opened
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Opened()
		{
#if DEBUG
			//			m_nantCommands.OutputBuildDebug.WriteLine("SolutionEvents::Opened");
			System.Diagnostics.Debug.WriteLine("SolutionEvents::Opened");
#endif
			m_nantCommands.OnProjectOpened();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a project is added to the solution
		/// </summary>
		/// <param name="project">The project.</param>
		/// <remarks>Do we need this method?</remarks>
		/// ------------------------------------------------------------------------------------
		public void OnProjectAdded(Project project)
		{
#if DEBUG
			//			m_nantCommands.OutputBuildDebug.WriteLine("SolutionEvents::ProjectAdded");
			//			m_nantCommands.OutputBuildDebug.WriteLine("\tProject: " + project.UniqueName);
#endif
		}

		// Do we need this method?
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when [build begin].
		/// </summary>
		/// <param name="scope">The scope.</param>
		/// <param name="action">The action.</param>
		/// <remarks>Do we need this method?</remarks>
		/// ------------------------------------------------------------------------------------
		public void OnBuildBegin(vsBuildScope scope, vsBuildAction action)
		{
#if DEBUG
			//			m_nantCommands.OutputBuildDebug.WriteLine("BuildEvents::OnBuildBegin");
#endif
			if (action == vsBuildAction.vsBuildActionRebuildAll)
				throw new Exception();
		}
		#endregion

		#region IDTCommandTarget Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Implements the Exec method of the IDTCommandTarget interface.
		/// This is called when the command is invoked.
		/// </summary>
		/// <param name="cmdName">Name of the CMD.</param>
		/// <param name="executeOption">The execute option.</param>
		/// <param name="variantIn">The variant in.</param>
		/// <param name="variantOut">The variant out.</param>
		/// <param name="handled">if set to <c>true</c> [handled].</param>
		/// <seealso class="Exec"/>
		/// ------------------------------------------------------------------------------------
		public void Exec(string cmdName, vsCommandExecOption executeOption, ref object variantIn,
			ref object variantOut, ref bool handled)
		{
			handled = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Implements the QueryStatus method of the IDTCommandTarget interface.
		/// This is called when the command's availability is updated
		/// </summary>
		/// <param name="cmdName">Name of the CMD.</param>
		/// <param name="neededText">The needed text.</param>
		/// <param name="statusOption">The status option.</param>
		/// <param name="commandText">The command text.</param>
		/// <seealso class="Exec"/>
		/// ------------------------------------------------------------------------------------
		public void QueryStatus(string cmdName, vsCommandStatusTextWanted neededText,
			ref vsCommandStatus statusOption, ref object commandText)
		{
		}

		#endregion
	}
}
