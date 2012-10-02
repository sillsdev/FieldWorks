// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CircularReferencesTask.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Serialization;
using NAnt.Core.Attributes;
using NAnt.Core;
using NAnt.Core.Tasks;

namespace SIL.FieldWorks.Build.Tasks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Checks for circular references
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TaskName("chkrefs")]
	public class CircularReferencesTask: Task
	{
		/// <summary>The visiting state</summary>
		private enum State
		{
			/// <summary>Not yet visited</summary>
			None,
			/// <summary>Processing not yet finished</summary>
			Visiting,
			/// <summary>Already checked</summary>
			Visited
		}

		/// <summary>The cache of references</summary>
		protected ReferenceCache m_Cache = new ReferenceCache();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="CircularReferencesTask"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CircularReferencesTask()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the reference cache
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void LoadReferences()
		{
			XmlSerializer serializer = new XmlSerializer(typeof(ReferenceCache));
			try
			{
				TextReader reader = new StreamReader(ReferenceCacheName);
				try
				{
					m_Cache = (ReferenceCache)serializer.Deserialize(reader);
				}
				catch(Exception e)
				{
					System.Diagnostics.Debug.WriteLine("Exception: " + e.Message);
				}
				finally
				{
					reader.Close();
				}
			}
			catch
			{
				// file doesn't exist
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the reference cache file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string ReferenceCacheName
		{
			get
			{
				return Path.Combine(Properties["dir.nantbuild"], "references.xml");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// <para>
		/// Performs a single step in a recursive depth-first-search traversal
		/// of the target reference tree.
		/// </para>
		/// <para>
		/// The current target is first set to the "visiting" state, and pushed
		/// onto the "visiting" stack.
		/// </para>
		/// <para>
		/// An exception is then thrown if any child of the current node is in
		/// the visiting state, as that implies a circular dependency. The
		/// exception contains details of the cycle, using elements of the
		/// "visiting" stack.
		/// </para>
		/// <para>
		/// If any child has not already been "visited", this method is called
		/// recursively on it.
		/// </para>
		/// <para>
		/// The current target is set to the "visited" state.
		/// </para>
		/// </summary>
		/// <param name="assembly">The assembly to inspect.</param>
		/// <param name="states">A mapping from targets to states. The states in question are
		/// <see cref="State.Visiting"/> or <see cref="State.Visited"/>. Must not be
		/// <see langword="null"/>.</param>
		/// <param name="visiting">A stack of targets which are currently being visited. Must
		/// not be <see langword="null" />.</param>
		/// <returns><c>false</c> if circular reference was detected, otherwise <c>true</c>.
		/// </returns>
		/// <exception cref="BuildException">A circular dependency is detected.</exception>
		/// ------------------------------------------------------------------------------------
		private bool CheckAssembly(XmlAssembly assembly, Hashtable states, Stack visiting)
		{
			if (assembly == null)
				return true;

			string name = assembly.AssemblyName;
			ArrayList references = assembly.References;
			Log(Level.Debug, "Checking reference for {0}", name);
			states[name] = State.Visiting;
			visiting.Push(name);

			foreach (Reference reference in references)
			{
				object state = states[reference.Name];
				if (state == null)
				{	// not been visited
					if (Verbose)
						Project.Indent();
					try
					{
						if (!CheckAssembly(m_Cache.Assemblies[reference.Name], states, visiting))
							return false;
					}
					finally
					{
						if (Verbose)
							Project.Unindent();
					}
				}
				else if ((State)state == State.Visiting)
				{
					// Currently visiting this node, so have a cycle
					string msg = CreateCircularString(reference.Name, visiting);
					Log(Level.Info, msg);

					if (FailOnError)
						throw new BuildException(msg);
					return false;
				}
			}

			string popName = (string)visiting.Pop();
			if (name != popName)
			{
				string msg = string.Format("Unexpected internal error: expected to pop {0} but got {1}",
					name, popName);
				throw new Exception(msg);
			}

			states[name] = State.Visited;

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check for circular references
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ExecuteTask()
		{
			LoadReferences();

			Log(Level.Verbose, "Checking references");

			Hashtable states = new Hashtable();
			Stack visiting = new Stack();

			TargetCollection targets =
				Project.TopologicalTargetSort(Project.DefaultTargetName, Project.Targets);

			// loop through all targets - backwards, because top level gets build at the end
			// we do it in hierarchical order, so that we get the same result regardless of the
			// order of assemblies in references.xml
			for (int i = targets.Count - 1; i >= 0; i--)
			{
				// find assembly that corresponds to target
				Target target = (Target)targets[i];
				XmlAssembly assembly = null;
				if (target.Name.ToLower().EndsWith("exe"))
				{	// TeExe -> Te.Exe
					string assemblyName = target.Name.Insert(target.Name.Length -3, ".");
					assembly = m_Cache.Assemblies[assemblyName];
				}
				if (assembly == null)
				{	// TeDll -> TeDll.dll
					assembly = m_Cache.Assemblies[target.Name + ".dll"];
					if (assembly == null)
					{	// Te -> Te.exe
						assembly = m_Cache.Assemblies[target.Name + ".exe"];
					}
				}

				if (!CheckAssembly(assembly, states, visiting))
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Builds an appropriate description detailing a specified circular
		/// dependency.
		/// </summary>
		/// <param name="end">The dependency to stop at. Must not be <see langword="null" />.</param>
		/// <param name="stack">A stack of dependencies. Must not be <see langword="null" />.</param>
		/// <returns>
		/// A <see cref="BuildException" /> detailing the specified circular
		/// dependency.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private static string CreateCircularString(string end, Stack stack)
		{
			StringBuilder sb = new StringBuilder("Circular reference: ");
			sb.Append(end);

			string c;

			for (c = (string)stack.Pop(); c != end; c = (string)stack.Pop())
			{
				sb.Append(" <- ");
				sb.Append(c);
			}

			return sb.ToString();
		}
	}
}
