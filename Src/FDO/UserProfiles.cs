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
// File: UserProfiles.cs
// Responsibility: Edge
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;

namespace SIL.FieldWorks.FDO
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for UserAccount.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class UserAccount : CmObject  //SarahD  never delete this class
	{
		//SarahD -once the database is in place, initialize these variables from the database
		private string m_Name;
		private Guid m_Sid;
		private int m_UserLevel;
		private bool m_IsAdministrator;
		private bool m_AccountDisabled;
		private string m_Password;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="UserAccount"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public UserAccount()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="UserAccount"/> class.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="sid"></param>
		/// <param name="userLevel"></param>
		/// <param name="isAdministrator"></param>
		/// <param name="accountDisabled"></param>
		/// <param name="password"></param>
		/// ------------------------------------------------------------------------------------
		public UserAccount(string name, Guid sid, int userLevel, bool isAdministrator,
			bool accountDisabled, string password)
		{
			m_Name = name;
			m_Sid = sid;
			m_UserLevel = userLevel;
			m_IsAdministrator = isAdministrator;
			m_AccountDisabled = accountDisabled;
			m_Password = password;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the name of a UserAccount
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
		/// Gets or sets the Sid of a UserAccount
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Guid Sid
		{
			get
			{
				return m_Sid;
			}
			set
			{
				m_Sid = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the user level of a UserAccount
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int UserLevel
		{
			get
			{
				return m_UserLevel;
			}
			set
			{
				m_UserLevel = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets whether or not the user is an administrator of UserAccounts
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsAdministrator
		{
			get
			{
				return m_IsAdministrator;
			}
			set
			{
				m_IsAdministrator = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets whether or not the user account is disabled
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool AccountDisabled
		{
			get
			{
				return m_AccountDisabled;
			}
			set
			{
				m_AccountDisabled = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the password of a UserAccount
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Password
		{
			get
			{
				return m_Password;
			}
			set
			{
				m_Password = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the name of the user
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return m_Name;
		}

	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for UserAppFeatAct
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class UserAppFeatAct : CmObject  //TODO: finish summary tags
	{
		private UserAccount m_UserAccount;
		private int m_ApplicationId;
		private int m_FeatureId;
		private int m_ActivatedLevel;

		//SarahD, this class can be deleted once the database is made and the code is
		//auto-generated
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="UserAppFeatAct"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public UserAppFeatAct()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="UserAppFeatAct"/> class.
		/// This is the only way you can modify the application Id and the feature Id
		/// </summary>
		/// <param name="userAccount"></param>
		/// <param name="appId"></param>
		/// <param name="featureId"></param>
		/// <param name="activatedLevel"></param>
		/// ------------------------------------------------------------------------------------
		public UserAppFeatAct(UserAccount userAccount, int appId, int featureId, int activatedLevel)
		{
			m_UserAccount = userAccount;
			m_ApplicationId = appId;
			m_FeatureId = featureId;
			m_ActivatedLevel = activatedLevel;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the user account that uses the Feature
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public UserAccount UserAccount
		{
			get
			{
				return m_UserAccount;
			}
			set
			{
				m_UserAccount = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the application that uses the Feature
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int ApplicationId
		{
			get
			{
				return m_ApplicationId;
			}
			set
			{
				m_ApplicationId = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the id of the Feature
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int FeatureId
		{
			get
			{
				return m_FeatureId;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the activated level of the user's feature
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int ActivatedLevel
		{
			get
			{
				return m_ActivatedLevel;
			}
			set
			{
				m_ActivatedLevel = value;
			}
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class UserAccountCollection : FdoCollectionBase  //TODO: finish summary tags
	{   //SarahD, keep this class too.
		/// <summary>
		/// Constructor.
		/// </summary>
		public UserAccountCollection(FdoCache fdoCache)
			: base(fdoCache) {}

		/// <summary>
		/// Add a UserAccount to the collection.
		/// </summary>
		/// <param name="ua">The UserAccount to add.</param>
		public UserAccount Add(UserAccount ua)
		{
			Debug.Assert(ua != null);
			//TODO:  later i will want to re-enable this line
			//UserAccount uaAdd = (UserAccount)ValidateObject(ua);
			UserAccount uaAdd = ua;
			List.Add(uaAdd);
			return uaAdd;
		}


		/// <summary>
		/// Remove the UserAccount at the specified index.
		/// </summary>
		/// <param name="index">Index of object to remove.</param>
		/// <exception cref="System.ArgumentException">
		/// Thrown when the index is invalid.
		/// </exception>
		public void Remove(int index)
		{
			List.RemoveAt(index);
		}


		/// <summary>
		/// Get the UserAccount at the given index.
		/// </summary>
		/// <param name="index">Index of object to return.</param>
		/// <returns>The UserAccount at the specified index.</returns>
		/// <exception cref="System.ArgumentException">
		/// Thrown when the index is invalid.
		/// </exception>
		public UserAccount Item(int index)
		{
			return (UserAccount)List[index];
		}

	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A collection of activated features for each user
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class UserFeaturesCollection : FdoCollectionBase  //TODO: finish summary tags
	{   //SarahD - not sure if we delete this later or not
		/// <summary>
		/// Constructor.
		/// </summary>
		public UserFeaturesCollection(FdoCache fdoCache)
			: base(fdoCache) {}

		/// <summary>
		/// Add a UserAppFeatAct to the collection.
		/// </summary>
		/// <param name="uafa">The UserAppFeatAct to add.</param>
		public UserAppFeatAct Add(UserAppFeatAct uafa)
		{
			Debug.Assert(uafa != null);
			//TODO:  later i will want to re-enable this line
			//UserAppFeatAct uafaAdd = (UserAppFeatAct)ValidateObject(uafa);
			UserAppFeatAct uafaAdd = uafa;
			List.Add(uafaAdd);
			return uafaAdd;
		}


		/// <summary>
		/// Remove the UserAppFeatAct at the specified index.
		/// </summary>
		/// <param name="index">Index of object to remove.</param>
		/// <exception cref="System.ArgumentException">
		/// Thrown when the index is invalid.
		/// </exception>
		public void Remove(int index)
		{
			List.RemoveAt(index);
		}


		/// <summary>
		/// Get the UserAppFeatAct at the given index.
		/// </summary>
		/// <param name="index">Index of object to return.</param>
		/// <returns>The UserAppFeatAct at the specified index.</returns>
		/// <exception cref="System.ArgumentException">
		/// Thrown when the index is invalid.
		/// </exception>
		public UserAppFeatAct Item(int index)
		{
			return (UserAppFeatAct)List[index];
		}

	}

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
