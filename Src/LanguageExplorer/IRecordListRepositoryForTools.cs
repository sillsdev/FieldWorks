// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer
{
	/// <summary>
	/// Interface that language Explorer tools use to create record lists when they are not in the main record list repository.
	/// </summary>
	/// <remarks>
	/// Clients that are not tools should never use this interface.
	/// </remarks>
	internal interface IRecordListRepositoryForTools : IRecordListRepository
	{
		/// <summary>
		/// Get a record list with the given <paramref name="recordListId"/>, creating one, if needed using <paramref name="recordListFactoryMethod"/>.
		/// </summary>
		/// <param name="recordListId">The record list Id to return.</param>
		/// <param name="statusBar"></param>
		/// <param name="recordListFactoryMethod">The method called to create the record list, if not found in the repository.</param>
		/// <returns>The record list instance with the specified Id.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="recordListFactoryMethod"/> doesn't know how to make a record list with the given Id.</exception>
		IRecordList GetRecordList(string recordListId, StatusBar statusBar, Func<LcmCache, FlexComponentParameters, string, StatusBar, IRecordList> recordListFactoryMethod);

		/// <summary>
		/// Get a record list for a custom possibility list with the given <paramref name="recordListId"/>, creating one, if needed using <paramref name="recordListFactoryMethod"/>.
		/// </summary>
		/// <param name="recordListId">The record list Id to return.</param>
		/// <param name="statusBar"></param>
		/// <param name="customList">The user created possibility list.</param>
		/// <param name="recordListFactoryMethod">The method called to create the record list, if not found in the repository.</param>
		/// <returns>The record list instance with the specified Id.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="recordListFactoryMethod"/> doesn't know how to make a record list with the given Id.</exception>
		IRecordList GetRecordList(string recordListId, StatusBar statusBar, ICmPossibilityList customList, Func<ICmPossibilityList, LcmCache, FlexComponentParameters, string, StatusBar, IRecordList> recordListFactoryMethod);
	}
}