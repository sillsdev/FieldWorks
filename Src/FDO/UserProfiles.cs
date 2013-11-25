// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: UserProfiles.cs
// Responsibility: Edge
//
// <remarks>
// </remarks>

using System;
using System.Diagnostics;

namespace SIL.FieldWorks.FDO
{
	// JohnT: the UserProfile class, which this file was named after, became obsolete and was deleted.
	// Unfortunately a couple of things still use the Feature class which I think was meant to be a helper.

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class Feature  //TODO: finish summary tags
	{
		private int m_FeatureId;
		private string m_Name;
		private int m_DefaultMinUserLevel;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="Feature"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Feature()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="Feature"/> class.  Feature Id,
		/// Feature Name,
		/// </summary>
		/// <param name="featureId">The feature's Id number</param>
		/// <param name="name">The feature's name</param>
		/// <param name="defaultMinUserLevel">The user level (beginner, intermediate, advanced)
		/// at which the feature is first activated by default</param>
		/// ------------------------------------------------------------------------------------
		public Feature(int featureId, string name, int defaultMinUserLevel)
		{
			m_FeatureId = featureId;
			m_Name = name;
			m_DefaultMinUserLevel = defaultMinUserLevel;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the FeatureId of a Feature
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int FeatureId
		{
			get
			{
				return m_FeatureId;
			}
			set
			{
				m_FeatureId = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the name of a Feature
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Name
		{
			get
			{
				return m_Name;
			}
			set
			{
				m_Name = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the default minimum user level of a Feature
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int DefaultMinUserLevel
		{
			get
			{
				return m_DefaultMinUserLevel;
			}
			set
			{
				m_DefaultMinUserLevel = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return m_Name;
		}
	}
}
