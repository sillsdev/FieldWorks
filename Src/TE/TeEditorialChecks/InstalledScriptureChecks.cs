// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: InstlledScriptureChecks.cs
// Responsibility: TE Team

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SILUBS.SharedScrUtils;
using System.Resources;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;

namespace SIL.FieldWorks.TE.TeEditorialChecks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Key that allows checks to be sorted correclty in the tree
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScrCheckKey : IComparable
	{
		private float m_relativeOrder;
		private string m_name;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ScrCheckKey"/> class.
		/// </summary>
		/// <param name="relativeOrder">A number that can be used to order this check relative
		/// to other checks in the same group when displaying checks in the UI.</param>
		/// <param name="name">The name of the check that can be used to disambiguate the order
		/// if two checks have the same relativeOrder.</param>
		/// ------------------------------------------------------------------------------------
		public ScrCheckKey(float relativeOrder, string name)
		{
			m_relativeOrder = relativeOrder;
			m_name = name;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name.
		/// </summary>
		/// <value>The name.</value>
		/// ------------------------------------------------------------------------------------
		internal string Name
		{
			get { return m_name; }
		}

		#region IComparable Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares the current instance with another object of the same type.
		/// </summary>
		/// <param name="obj">An object to compare with this instance.</param>
		/// <returns>
		/// A 32-bit signed integer that indicates the relative order of the objects being
		/// compared. The return value has these meanings: Value Meaning Less than zero This
		/// instance is less than <paramref name="obj"/>. Zero This instance is equal to
		/// <paramref name="obj"/>. Greater than zero This instance is greater than
		/// <paramref name="obj"/>.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">
		/// 	<paramref name="obj"/> is not the same type as this instance. </exception>
		/// ------------------------------------------------------------------------------------
		public int CompareTo(object obj)
		{
			if (!(obj is ScrCheckKey))
				throw new ArgumentException();

			ScrCheckKey right = (ScrCheckKey)obj;

			if (m_relativeOrder == right.m_relativeOrder)
				return m_name.CompareTo(right.m_name);
			return m_relativeOrder.CompareTo(right.m_relativeOrder);
		}
		#endregion
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class containingin static methods for loading Scripture Checks dynamically by TE
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class InstalledScriptureChecks
	{
		/// <summary>List of available scripture checks</summary>
		private static List<Type> s_scrCheckList;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the installed list of scripture checks.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static SortedList<ScrCheckKey, IScriptureCheck> GetChecks(ScrChecksDataSource dataSource)
		{
			LoadAvailableChecks();

			return InstantiateChecks(dataSource);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the requested property of the given check as a localized string.
		/// </summary>
		/// <param name="resourceManager">The resource manager.</param>
		/// <param name="checkId">The check id.</param>
		/// <param name="sProperty">The name of the property we want ("Name" or "Description").
		/// </param>
		/// <param name="sDefaultVal">The default value if not found in the resources.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		internal static string GetCheckProperty(ResourceManager resourceManager, Guid checkId,
			string sProperty, string sDefaultVal)
		{
			string s = resourceManager.GetString("kstid" + sProperty + "_" +
				checkId.ToString("N").ToUpperInvariant());
			Debug.Assert(!string.IsNullOrEmpty(s), "Missing " + sProperty + " for the " +
				sDefaultVal + " check in the ScrFDO resources. Check ID = " + checkId.ToString());

			return s ?? sDefaultVal;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use reflection to determine the available Scripture checks (i.e. classes that
		/// implement IScriptureCheck).
		/// </summary>
		/// <exception cref="ApplicationException">Thrown if the scripture checks directory
		/// doesn't exist or there no scripture checks in that directory.</exception>
		/// ------------------------------------------------------------------------------------
		private static void LoadAvailableChecks()
		{
			if (s_scrCheckList != null)
				return;

			s_scrCheckList = new List<Type>();

			string directory = FwDirectoryFinder.EditorialChecksDirectory;

			string[] dllFilenames = Directory.GetFiles(directory, "*.dll");
			foreach (string dllFile in dllFilenames)
			{
				Assembly asm = null;
				try
				{
					asm = Assembly.LoadFrom(dllFile);
				}
				catch
				{
					Debug.WriteLine("Unable to read assembly " + dllFile);
					continue;
				}

				Type[] typesInAssembly = asm.GetTypes();
				foreach (Type type in typesInAssembly)
				{
					if (type.GetInterface(typeof(IScriptureCheck).FullName) != null)
						s_scrCheckList.Add(type);
				}
			}

			if (s_scrCheckList.Count == 0)
			{
				string msg = ResourceHelper.GetResourceString("kstidUnableToFindEditorialChecks");
				throw new ApplicationException(string.Format(msg, directory));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the scripture checks and updates the annotation definitions by checking
		/// that for each check we have a corresponding annotation definition in the database.
		/// </summary>
		/// <param name="dataSource">The data source.</param>
		/// ------------------------------------------------------------------------------------
		private static SortedList<ScrCheckKey, IScriptureCheck> InstantiateChecks(
			ScrChecksDataSource dataSource)
		{
			FdoCache cache = dataSource.Cache;

			Dictionary<Guid, ICmAnnotationDefn> errorTypes = new Dictionary<Guid, ICmAnnotationDefn>();

			ICmAnnotationDefn annDefnChkError =
				cache.ServiceLocator.GetInstance<ICmAnnotationDefnRepository>().GetObject(CmAnnotationDefnTags.kguidAnnCheckingError);

			foreach (ICmAnnotationDefn errorType in annDefnChkError.SubPossibilitiesOS)
				errorTypes[errorType.Guid] = errorType;

			SortedList<ScrCheckKey, IScriptureCheck> scriptureChecks =
				new SortedList<ScrCheckKey, IScriptureCheck>();

			foreach (Type type in s_scrCheckList)
			{
				IScriptureCheck scrCheck =
					(IScriptureCheck)Activator.CreateInstance(type, dataSource);

				if (scrCheck == null)
					continue;

				// Get the localized version of the check name
				string scrCheckName = GetCheckProperty(ScrFdoResources.ResourceManager,
					scrCheck.CheckId, "Name", scrCheck.CheckName);
				scriptureChecks.Add(new ScrCheckKey(scrCheck.RelativeOrder, scrCheckName), scrCheck);

				ICmAnnotationDefn chkType;
				if (!errorTypes.TryGetValue(scrCheck.CheckId, out chkType))
				{
					chkType = cache.ServiceLocator.GetInstance<ICmAnnotationDefnFactory>().Create(scrCheck.CheckId, annDefnChkError);
					annDefnChkError.SubPossibilitiesOS.Add(chkType);
					chkType.IsProtected = true;
					chkType.Name.UserDefaultWritingSystem =
						TsStringUtils.MakeTss(scrCheckName, cache.DefaultUserWs);
					chkType.Description.UserDefaultWritingSystem =
						TsStringUtils.MakeTss(scrCheck.Description, cache.DefaultUserWs);
					chkType.HelpId = scrCheck.CheckName.Replace(' ', '_');
					InheritAttributes(annDefnChkError, chkType);
				}
				else if (chkType.Name.UserDefaultWritingSystem.Text != scrCheckName)
				{
					// Store the localized version of the check name as the current UI name.
					chkType.Name.UserDefaultWritingSystem =
						TsStringUtils.MakeTss(scrCheckName, cache.DefaultUserWs);
				}
			}
			return scriptureChecks;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy "inheritable" attributes from the high-level error annotation definition to
		/// the given subpossibility.
		/// </summary>
		/// <param name="copyFrom">The ann defn to copy attributes from.</param>
		/// <param name="copyTo">The ann defn to copy attributes to.</param>
		/// ------------------------------------------------------------------------------------
		private static void InheritAttributes(ICmAnnotationDefn copyFrom, ICmAnnotationDefn copyTo)
		{
			copyTo.AllowsComment = copyFrom.AllowsComment;
			copyTo.AllowsFeatureStructure = copyFrom.AllowsFeatureStructure;
			copyTo.AllowsInstanceOf = copyFrom.AllowsInstanceOf;
			copyTo.CanCreateOrphan = copyFrom.CanCreateOrphan;
			copyTo.CopyCutPastable = copyFrom.CopyCutPastable;
			copyTo.Hidden = copyFrom.Hidden;
			copyTo.Multi = copyFrom.Multi;
			copyTo.PromptUser = copyFrom.PromptUser;
			copyTo.Severity = copyFrom.Severity;
			copyTo.UserCanCreate = copyFrom.UserCanCreate;
			copyTo.ZeroWidth = copyFrom.ZeroWidth;
		}
	}
}
