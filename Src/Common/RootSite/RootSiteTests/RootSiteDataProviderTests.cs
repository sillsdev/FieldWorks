// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SimpleRootSiteDataProviderTests.cs
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Forms;
using NUnit.Framework;

using SIL.CoreImpl;
using SIL.FieldWorks.CacheLight;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.Common.RootSites
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class SimpleRootSiteDataProviderTestsBase : BaseTest
	{

		public sealed class SimpleRootSiteDataProviderTestsHelper : IDisposable
		{
			/// <summary>The root site</summary>
			private Control m_site;

			public SimpleRootSiteDataProviderTestsHelper(Control view)
			{
				m_site = view;
			}

			internal Rect GetRootSiteScreenRect()
			{
				var drawingScreenBounds = m_site.RectangleToScreen(m_site.Bounds);
				return new Rect(drawingScreenBounds.X, drawingScreenBounds.Y,
					drawingScreenBounds.Width, drawingScreenBounds.Height);
			}

			#region Disposable stuff
			#if DEBUG
			/// <summary/>
			~SimpleRootSiteDataProviderTestsHelper()
			{
				Dispose(false);
			}
			#endif

			/// <summary/>
			public bool IsDisposed { get; private set; }

			/// <summary/>
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			/// <summary/>
			private void Dispose(bool fDisposing)
			{
				System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().ToString() + " *******");
				if (fDisposing && !IsDisposed)
				{
					// dispose managed and unmanaged objects
					m_site.Dispose();
				}
				m_site = null;
				IsDisposed = true;
			}
			#endregion
		}

		/// <summary>Defines the possible languages</summary>
		internal struct Lng
		{
			/// <summary>English paragraphs</summary>
			internal bool English;
			internal bool French;
		}

		#region Data members

		internal const int kflidMultiString = 101002;

		internal const int kclsidWritingSystem = 999;

		/// <summary>The data cache</summary>
		protected RealDataCache m_cache;

		/// <summary>Writing System Manager (reset for each test)</summary>
		protected IWritingSystemManager m_wsManager;
		/// <summary>Id of English Writing System (reset for each test)</summary>
		internal protected int m_wsEng;

		/// <summary>
		/// Id of French Writing System
		/// </summary>
		internal int m_wsFr;

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fixture setup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			SimpleRootsiteTestsBase.SetupTestModel(Properties.Resources.RootSiteDataProviderCacheModel_xml);

			m_cache = new RealDataCache { MetaDataCache = MetaDataCache.CreateMetaDataCache("TestModel.xml") };

			if (m_wsManager != null)
				throw new ApplicationException("m_wsManager was not null");
			m_wsManager = new PalasoWritingSystemManager();
			m_cache.WritingSystemFactory = m_wsManager;
			m_wsEng = m_wsManager.Set("en").Handle;
			m_wsFr = m_wsManager.Set("fr").Handle;

			FixtureStyleSheet = new SimpleStyleSheet(m_cache);
		}

		internal protected IVwStylesheet FixtureStyleSheet { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Teardown
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public override void FixtureTeardown()
		{
			// GrowToWord causes a Char Property Engine to be created, and the test runner
			// fails if we don't shut the factory down.
			var disposable = m_wsManager as IDisposable;
			if (disposable != null)
				disposable.Dispose();
			m_wsManager = null;
			m_cache.Dispose();
			m_cache = null;

			base.FixtureTeardown();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new basic view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public virtual void TestSetup()
		{
			m_cache.ClearAllData();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shuts down the view
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void TestTearDown()
		{
		}
	}

	static class IListExtensionMethod
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Extension method for IList&lt;KeyValuePair&gt;
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void Add<TKey, TValue>(this IList<KeyValuePair<TKey, TValue>> kvPairList,
			TKey key, TValue value)
		{
			kvPairList.Add(new KeyValuePair<TKey, TValue>(key, value));
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Unit tests for <see cref="SimpleRootSiteDataProvider"/>.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class RootSiteDataProviderTests : SimpleRootSiteDataProviderTestsBase
	{
		#region SimpleRootSiteDataProviderTests

		#region FragmentRoot tests
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the GetSelectionInfo method passing different combinations of parameters.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "UIAutomation disabled on Linux")]
		public void GetUIAutomationObject_FragmentRoot()
		{
			using (var site = new SimpleRootSiteDataProviderView(m_cache))
																		{
				site.StyleSheet = FixtureStyleSheet;
				site.WritingSystemFactory = m_wsManager;
				using (new SimpleRootSiteDataProviderTestsHelper(site))
				{
					site.ShowForm(m_wsEng, "Not really testing the text contents",
						new SimpleRootSiteDataProviderVc.DisplayOptions());

					var dataProvider = new SimpleRootSiteDataProvider(site);
					Assert.IsInstanceOf<int>(
						dataProvider.GetPropertyValue(AutomationElementIdentifiers.ControlTypeProperty.Id));
					Assert.AreEqual(ControlType.Group.Id,
						(int)dataProvider.GetPropertyValue(AutomationElementIdentifiers.ControlTypeProperty.Id),
						"Expected the control to be ControlType.Group until someone thinks of something better");
					Assert.IsNull(dataProvider.GetPropertyValue(AutomationElementIdentifiers.NameProperty.Id),
						"ControlType.Group takes its name from the control's label. It doesn't need to be provided.");
					Assert.IsTrue(
						(bool)dataProvider.GetPropertyValue(AutomationElementIdentifiers.IsControlElementProperty.Id),
						"ControlType.Group always enables the Control view");
					Assert.IsTrue(
						(bool)dataProvider.GetPropertyValue(AutomationElementIdentifiers.IsContentElementProperty.Id),
						"ControlType.Group always enables the Content view");
					Assert.IsInstanceOf<string>(
						dataProvider.GetPropertyValue(AutomationElementIdentifiers.AutomationIdProperty.Id));
					Assert.AreEqual("", dataProvider.GetPropertyValue(AutomationElementIdentifiers.AutomationIdProperty.Id));
					Assert.IsInstanceOf<string>(
						dataProvider.GetPropertyValue(AutomationElementIdentifiers.ClassNameProperty.Id));
					Assert.AreEqual(site.GetType().Name,
						dataProvider.GetPropertyValue(AutomationElementIdentifiers.ClassNameProperty.Id));

					Assert.AreSame(dataProvider, dataProvider.GetPatternProvider(TextPatternIdentifiers.Pattern.Id),
						"We expect SimpleRootSiteDataProvider to be able to provide text.");
					Assert.AreSame(dataProvider, dataProvider.FragmentRoot);
					Assert.IsNull(dataProvider.GetRuntimeId(), "FragmentRoot providers should return null GetRuntimeId");
				}
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void FragmentRoot_DocumentRange_OneString()
		{
			using (var site = new RootSiteDataProviderView(m_cache))
						   {
				site.StyleSheet = FixtureStyleSheet;
				site.WritingSystemFactory = m_wsManager;
			using (new SimpleRootSiteDataProviderTestsHelper(site))
			{
				const string expected = "Should only show this string";
				site.ShowForm(m_wsEng, expected, new SimpleRootSiteDataProviderVc.DisplayOptions());
				var dataProvider = new SimpleRootSiteDataProvider(site);
				var doc = dataProvider.DocumentRange;
				Assert.NotNull(doc, "DocumentRange shouldn't be null");
				Assert.AreEqual(expected, doc.GetText(-1));
			}
		}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "UIAutomation disabled on Linux")]
		public void FragmentRoot_DocumentRange_OneString_ReadOnly()
		{
			using (var site = new RootSiteDataProviderView(m_cache))
						   {
				site.StyleSheet = FixtureStyleSheet;
				site.WritingSystemFactory = m_wsManager;

			using (new SimpleRootSiteDataProviderTestsHelper(site))
			{
				const string expected = "This string should be in a read-only view";
				site.ShowForm(m_wsEng, expected,
							  new SimpleRootSiteDataProviderVc.DisplayOptions { ReadOnlyView = true });
				Assert.IsTrue(site.ReadOnlyView, "site should now be read-only");
				var dataProvider = new SimpleRootSiteDataProvider(site);
				var doc = dataProvider.DocumentRange;
				Assert.NotNull(doc, "DocumentRange shouldn't be null");
				Assert.IsInstanceOf<bool>(doc.GetAttributeValue(TextPatternIdentifiers.IsReadOnlyAttribute.Id));
				Assert.IsTrue((bool)doc.GetAttributeValue(TextPatternIdentifiers.IsReadOnlyAttribute.Id));
				Assert.AreEqual(expected, doc.GetText(-1));

				var firstChild = dataProvider.Navigate(NavigateDirection.FirstChild);
				Assert.IsNull(firstChild, "FragmentRoot (ReadOnly view) should not have a custom edit control.");
			}
		}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude="Linux", Reason="UIAutomation disabled on Linux")]
		public void FragmentRoot_HasCustomEditControl()
		{
			using (var site = new RootSiteDataProviderView(m_cache))
						   {
				site.StyleSheet = FixtureStyleSheet;
				site.WritingSystemFactory = m_wsManager;
			using (var helper = new SimpleRootSiteDataProviderTestsHelper(site))
			{
				const string expected = "Should have a custom edit control";
				site.ShowForm(m_wsEng, expected,
							  new SimpleRootSiteDataProviderVc.DisplayOptions());
				var dataProvider = new SimpleRootSiteDataProvider(site);
				Assert.AreEqual(expected, dataProvider.DocumentRange.GetText(-1));

				var firstChild = dataProvider.Navigate(NavigateDirection.FirstChild);
				Assert.NotNull(firstChild, "FragmentRoot should have a custom edit control.");
				Assert.IsInstanceOf<SimpleRootSiteEditControl>(firstChild);
				Assert.IsInstanceOf<ITextProvider>(firstChild);
				var lastChild = dataProvider.Navigate(NavigateDirection.LastChild);
				Assert.AreSame(firstChild, lastChild);

				var childTextProvider = (ITextProvider)firstChild;
				Assert.AreEqual(expected, childTextProvider.DocumentRange.GetText(-1));

				// make sure the bounding rectangle of the child is contained in the parent's bounding rectangle.
				var rsBoundintRect = helper.GetRootSiteScreenRect();
				Assert.IsTrue(
					rsBoundintRect.Contains(firstChild.BoundingRectangle),
					"Edit control BoundingRectangle should be contained within the root fragment bounding rectangle");
			}
		}
		}

		[Test]
		[Platform(Exclude="Linux", Reason="UIAutomation disabled on Linux")]
		public void FragmentRoot_HasCustomEditControl_ChangeValue()
		{
			using (var site = new RootSiteDataProviderView(m_cache))
						   {
				site.StyleSheet = FixtureStyleSheet;
				site.WritingSystemFactory = m_wsManager;
			using (var helper = new SimpleRootSiteDataProviderTestsHelper(site))
			{
				const string expected = "This has been replaced";
				site.ShowForm(m_wsEng, "This should get changed.",
							  new SimpleRootSiteDataProviderVc.DisplayOptions());
				var dataProvider = new SimpleRootSiteDataProvider(site);
				IRawElementProviderFragment firstChild = dataProvider.Navigate(NavigateDirection.FirstChild);
				Assert.NotNull(firstChild, "FragmentRoot should have a custom edit control.");
				Assert.IsInstanceOf<SimpleRootSiteEditControl>(firstChild);
				Assert.IsInstanceOf<IValueProvider>(firstChild);

				var originalBoundingRect = firstChild.BoundingRectangle;
				var childValueProvider = (IValueProvider)firstChild;
				childValueProvider.SetValue(expected);
				Assert.AreSame(firstChild, dataProvider.Navigate(NavigateDirection.FirstChild));
				Assert.AreEqual(expected, childValueProvider.Value);

				Assert.IsFalse(firstChild.BoundingRectangle.Equals(originalBoundingRect),
							   "The bounding rectangle of the control should have changed from " + originalBoundingRect);
				Assert.Less(firstChild.BoundingRectangle.Width, originalBoundingRect.Width);
				Assert.AreEqual(originalBoundingRect.Height, firstChild.BoundingRectangle.Height);
				Assert.AreEqual(originalBoundingRect.X, firstChild.BoundingRectangle.X);
				Assert.AreEqual(originalBoundingRect.Y, firstChild.BoundingRectangle.Y);

				var childTextProvider = (ITextProvider)firstChild;
				Assert.AreEqual(expected, childTextProvider.DocumentRange.GetText(-1));
				Assert.AreEqual(expected, dataProvider.DocumentRange.GetText(-1));

				var lastChild = dataProvider.Navigate(NavigateDirection.LastChild);
				Assert.AreSame(firstChild, lastChild);
			}
		}
		}

		[Test]
		[Platform(Exclude = "Linux", Reason = "UIAutomation disabled on Linux")]
		public void FragmentRoot_MultiString_TwoEditControls()
		{
			using (var site = new RootSiteDataProvider_MultiStringView(m_cache))
			{
				site.StyleSheet = FixtureStyleSheet;
				site.WritingSystemFactory = m_wsManager;
			using (var helper = new SimpleRootSiteDataProviderTestsHelper(site))
			{
				const string engExpected = "This is the English sentence.";
				const string frExpected = "This is the French sentence.";
				string expected = engExpected + Environment.NewLine + frExpected;
				IList<KeyValuePair<int, string>> vkvp = new List<KeyValuePair<int, string>>();
				vkvp.Add(m_wsEng, engExpected);
				vkvp.Add(m_wsFr, frExpected);
				site.ShowForm(vkvp,
						new SimpleRootSiteDataProvider_MultiStringViewVc.DisplayOptions
							{ReadOnlyView = false, LiteralStringLabels = false});
				Assert.IsFalse(site.ReadOnlyView, "site should not be read-only");
				var dataProvider = new SimpleRootSiteDataProvider(site, site.CreateUIAutomationEditControls);
				var doc = dataProvider.DocumentRange;
				Assert.NotNull(doc, "DocumentRange shouldn't be null");
				Assert.IsInstanceOf<bool>(doc.GetAttributeValue(TextPatternIdentifiers.IsReadOnlyAttribute.Id));
				Assert.IsFalse((bool)doc.GetAttributeValue(TextPatternIdentifiers.IsReadOnlyAttribute.Id));
				Assert.AreEqual(expected, doc.GetText(-1));

				var firstChild = dataProvider.Navigate(NavigateDirection.FirstChild);
				Assert.NotNull(firstChild, "FragmentRoot should have a custom edit control.");
				Assert.IsInstanceOf<SimpleRootSiteEditControl>(firstChild);
				Assert.IsInstanceOf<ITextProvider>(firstChild);
				var lastChild = dataProvider.Navigate(NavigateDirection.LastChild);
				Assert.AreNotSame(firstChild, lastChild);

				var engTextProvider = (ITextProvider)firstChild;
				Assert.AreEqual(engExpected, engTextProvider.DocumentRange.GetText(-1));

				var frTextProvider = (ITextProvider)lastChild;
				Assert.AreEqual(frExpected, frTextProvider.DocumentRange.GetText(-1));

				// make sure the bounding rectangle of the child is contained in the parent's bounding rectangle.
				var rsBoundintRect = helper.GetRootSiteScreenRect();
				Assert.IsTrue(
					rsBoundintRect.Contains(firstChild.BoundingRectangle),
					"Edit control BoundingRectangle should be contained within the root fragment bounding rectangle");
				Assert.IsTrue(
					rsBoundintRect.Contains(lastChild.BoundingRectangle),
					"Edit control BoundingRectangle should be contained within the root fragment bounding rectangle");
				Assert.AreEqual(firstChild.BoundingRectangle.Left, lastChild.BoundingRectangle.Left, 0);
				Rect intersection = Rect.Intersect(firstChild.BoundingRectangle, lastChild.BoundingRectangle);
				Assert.AreEqual(0, intersection.Height * intersection.Width, 0,
							   "Sibling edit control BoundingRectangles should not intersect with each other");

				// make sure we've only created two EditControl children.
				var nextSibling = firstChild.Navigate(NavigateDirection.NextSibling);
				Assert.AreSame(lastChild, nextSibling, "Navigate.NextSibling");
				Assert.IsNull(firstChild.Navigate(NavigateDirection.PreviousSibling));

				var previousSibling = lastChild.Navigate(NavigateDirection.PreviousSibling);
				Assert.AreSame(firstChild, previousSibling);
				Assert.IsNull(lastChild.Navigate(NavigateDirection.NextSibling));
			}
		}
		}

		[Test]
		[Platform(Exclude = "Linux", Reason = "UIAutomation disabled on Linux")]
		public void FragmentRoot_MultiString_ReadOnly()
		{
			using (var site = new RootSiteDataProvider_MultiStringView(m_cache))
						   {
				site.StyleSheet = FixtureStyleSheet;
				site.WritingSystemFactory = m_wsManager;
			using (var helper = new SimpleRootSiteDataProviderTestsHelper(site))
			{
				const string engExpected = "This is the English sentence.";
				const string frExpected = "This is the French sentence.";
				string expected = engExpected + Environment.NewLine + frExpected;
				IList<KeyValuePair<int, string>> vkvp = new List<KeyValuePair<int, string>>();
				vkvp.Add(m_wsEng, engExpected);
				vkvp.Add(m_wsFr, frExpected);
				site.ShowForm(vkvp,
							  new SimpleRootSiteDataProvider_MultiStringViewVc.DisplayOptions { ReadOnlyView = true, LiteralStringLabels = false });
				Assert.IsTrue(site.ReadOnlyView, "site should now be read-only");
				var dataProvider = new SimpleRootSiteDataProvider(site);
				var doc = dataProvider.DocumentRange;
				Assert.NotNull(doc, "DocumentRange shouldn't be null");
				Assert.IsInstanceOf<bool>(doc.GetAttributeValue(TextPatternIdentifiers.IsReadOnlyAttribute.Id));
				Assert.IsTrue((bool)doc.GetAttributeValue(TextPatternIdentifiers.IsReadOnlyAttribute.Id));
				Assert.AreEqual(expected, doc.GetText(-1));

				var firstChild = dataProvider.Navigate(NavigateDirection.FirstChild);
				Assert.IsNull(firstChild, "FragmentRoot (ReadOnly view) should not have a custom edit control.");
			}
		}
		}
		#endregion

		#region StringPropertyCollectorEnv tests
		[Test]
		public void StringPropertyCollectorEnv_MultiStringView_ReadOnlyView()
		{
			using (var site = new RootSiteDataProvider_MultiStringView(m_cache))
						   {
				site.StyleSheet = FixtureStyleSheet;
				site.WritingSystemFactory = m_wsManager;
			using (var helper = new SimpleRootSiteDataProviderTestsHelper(site))
			{
				const string engExpected = "This is the English sentence.";
				const string frExpected = "This is the French sentence.";
				string expected = engExpected + Environment.NewLine + frExpected;
				IList<KeyValuePair<int, string>> vkvp = new List<KeyValuePair<int, string>>();
				vkvp.Add(m_wsEng, engExpected);
				vkvp.Add(m_wsFr, frExpected);
				site.ShowForm(vkvp,
							  new SimpleRootSiteDataProvider_MultiStringViewVc.DisplayOptions { ReadOnlyView = true, LiteralStringLabels = false });
				var editableSelections = site.CollectEditableStringPropSelections();
				Assert.AreEqual(0, editableSelections.Count);
			}
		}
		}

		[Test]
		public void StringPropertyCollectorEnv_MultiStringView_TwoEditableFields()
		{
			using (var site = new RootSiteDataProvider_MultiStringView(m_cache))
						   {
				site.StyleSheet = FixtureStyleSheet;
				site.WritingSystemFactory = m_wsManager;
			using (var helper = new SimpleRootSiteDataProviderTestsHelper(site))
			{
				const string engExpected = "This is the English sentence.";
				const string frExpected = "This is the French sentence.";
				string expected = engExpected + Environment.NewLine + frExpected;
				IList<KeyValuePair<int, string>> vkvp = new List<KeyValuePair<int, string>>();
				vkvp.Add(m_wsEng, engExpected);
				vkvp.Add(m_wsFr, frExpected);
				site.ShowForm(vkvp,
						new SimpleRootSiteDataProvider_MultiStringViewVc.DisplayOptions
							{ReadOnlyView = false, LiteralStringLabels = false});
				var editableSelections = site.CollectEditableStringPropSelections();
				Assert.IsNotNull(editableSelections);
				Assert.AreEqual(2, editableSelections.Count);
				var sli1 = CollectorEnvServices.MakeLocationInfo(site.RootBox, editableSelections[0]);
				Assert.AreEqual(0, sli1.m_location.Length);
				Assert.AreEqual(SimpleRootSiteDataProvider_MultiStringViewVc.kflidMultiString, sli1.m_tag);
				Assert.AreEqual(m_wsEng, sli1.m_ws);
				Assert.AreEqual(0, sli1.m_ichMin, "m_ichMin");
				Assert.AreEqual(0, sli1.m_ichLim, "m_ichLim");
				Assert.AreEqual(0, sli1.m_cpropPrev);

				var sli2 = CollectorEnvServices.MakeLocationInfo(site.RootBox, editableSelections[1]);
				Assert.AreEqual(SimpleRootSiteDataProvider_MultiStringViewVc.kflidMultiString, sli2.m_tag);
				Assert.AreEqual(m_wsFr, sli2.m_ws);
				Assert.AreEqual(0, sli2.m_ichMin, "m_ichMin");
				Assert.AreEqual(0, sli2.m_ichLim, "m_ichLim");
				Assert.AreEqual(1, sli2.m_cpropPrev);

				// Verify we can actually use these to make valid selections.
				var sel1 = editableSelections[0];
				Assert.IsNotNull(sel1);
				Assert.IsTrue(sel1.IsEditable, "sel1.IsEditable");
				var sel2 = editableSelections[1];
				Assert.IsNotNull(sel2);
				Assert.IsTrue(sel2.IsEditable, "sel2.IsEditable");
			}
		}
		}

		[Test]
		public void StringPropertyCollectorEnv_MultiStringView_WithLabels()
		{
			using (var site = new RootSiteDataProvider_MultiStringView(m_cache))
			{
				site.StyleSheet = FixtureStyleSheet;
				site.WritingSystemFactory = m_wsManager;
			using (new SimpleRootSiteDataProviderTestsHelper(site))
			{
				const string engExpected = "This is the English sentence.";
				const string frExpected = "This is the French sentence.";
				string expected = engExpected + Environment.NewLine + frExpected;
				IList<KeyValuePair<int, string>> vkvp = new List<KeyValuePair<int, string>>();
				vkvp.Add(m_wsEng, engExpected);
				vkvp.Add(m_wsFr, frExpected);
				site.ShowForm(vkvp,
							  new SimpleRootSiteDataProvider_MultiStringViewVc.DisplayOptions { ReadOnlyView = false, LiteralStringLabels = true });
				var editableSelections = site.CollectEditableStringPropSelections();
				Assert.AreEqual(2, editableSelections.Count);
				var sel1 = editableSelections[0];
				Assert.IsNotNull(sel1, "sel1");
				Assert.IsTrue(sel1.IsEditable, "sel1.IsEditable");
				var sel2 = editableSelections[1];
				Assert.IsNotNull(sel2, "sel2");
				Assert.IsTrue(sel2.IsEditable, "sel2.IsEditable");
			}
		}
		}
		#endregion

		[Test]
		public void PictureCollectorEnv_NoPicture()
		{
			using (var site = new RootSiteDataProviderViewBase(m_cache))
			{
				site.StyleSheet = FixtureStyleSheet;
				site.WritingSystemFactory = m_wsManager;
			using (new SimpleRootSiteDataProviderTestsHelper(site))
			{
				site.MakeRoot(SimpleRootSiteDataProviderBaseVc.kfragRoot, () => new NoPictureVc());
				site.ShowForm();
				var pictureSelections = CollectorEnvServices.CollectPictureSelectionPoints(site.RootBox);
				Assert.AreEqual(0, pictureSelections.Count());
			}
		}
		}

		[Test]
		public void PictureCollectorEnv_OnePicture()
		{
			using (var site = new RootSiteDataProviderViewBase(m_cache))
			{
				site.StyleSheet = FixtureStyleSheet;
				site.WritingSystemFactory = m_wsManager;
			using (new SimpleRootSiteDataProviderTestsHelper(site))
			{
				site.MakeRoot(SimpleRootSiteDataProviderBaseVc.kfragRoot, () => new OnePictureVc());
				site.ShowForm();
				var pictureSelections = CollectorEnvServices.CollectPictureSelectionPoints(site.RootBox);
				Assert.AreEqual(1, pictureSelections.Count(), "picture count");
			}
		}
		}

		[Test]
		[Platform(Exclude="Linux", Reason="UIAutomation disabled on Linux")]
		public void ImageControl_OnePicture()
		{
			using (var site = new RootSiteDataProviderViewBase(m_cache))
			{
				site.StyleSheet = FixtureStyleSheet;
				site.WritingSystemFactory = m_wsManager;
			using (new SimpleRootSiteDataProviderTestsHelper(site))
			{
				site.MakeRoot(SimpleRootSiteDataProviderBaseVc.kfragRoot, () => new OnePictureVc());
				site.ShowForm();
				var dataProvider = new SimpleRootSiteDataProvider(site, childNavigationProvider =>
																			RootSiteServices.CreateUIAutomationImageControls(
																				childNavigationProvider, site.RootBox));
				var firstChild = dataProvider.Navigate(NavigateDirection.FirstChild);
				Assert.IsNotNull(firstChild, "firstChild");
				Assert.IsInstanceOf<ImageControl>(firstChild);
				var lastChild = dataProvider.Navigate(NavigateDirection.LastChild);
				Assert.AreSame(firstChild, lastChild);
			}
		}
		}

		[Test]
		[Platform(Exclude="Linux", Reason="UIAutomation disabled on Linux")]
		public void ImageAndEditControls()
		{
			using (var site = new RootSiteDataProviderViewBase(m_cache))
			{
				site.StyleSheet = FixtureStyleSheet;
				site.WritingSystemFactory = m_wsManager;
			using (new SimpleRootSiteDataProviderTestsHelper(site))
			{
				const string expectedText = "edit box";
				ITsStrFactory tsStrFactory = TsStrFactoryClass.Create();
				site.VwCache.CacheStringProp(site.RootHvo, RootSiteDataProviderViewBase.kflidSimpleTsString,
					tsStrFactory.MakeString(expectedText, m_wsEng));
				site.MakeRoot(SimpleRootSiteDataProviderBaseVc.kfragRoot, () => new OnePictureOneEditBoxVc());
				site.ShowForm();

				var dataProvider = new SimpleRootSiteDataProvider(site, childNavigationProvider =>
																			RootSiteServices.CreateUIAutomationControls(
																				childNavigationProvider, site.RootBox));
				var firstChild = dataProvider.Navigate(NavigateDirection.FirstChild);
				Assert.IsNotNull(firstChild, "firstChild");
				Assert.IsInstanceOf<ImageControl>(firstChild, "firstChild");

				var lastChild = dataProvider.Navigate(NavigateDirection.LastChild);
				Assert.IsNotNull(lastChild);
				Assert.AreNotSame(firstChild, lastChild);
				Assert.IsInstanceOf<SimpleRootSiteEditControl>(lastChild, "lastChild");
				var childTextProvider = (ITextProvider)lastChild;
				Assert.AreEqual(expectedText, childTextProvider.DocumentRange.GetText(-1));

				IRawElementProviderFragment nextSibling = firstChild.Navigate(NavigateDirection.NextSibling);
				Assert.IsNotNull(nextSibling);
				Assert.AreSame(lastChild, nextSibling);

				IRawElementProviderFragment previousSibling = lastChild.Navigate(NavigateDirection.PreviousSibling);
				Assert.IsNotNull(previousSibling);
				Assert.AreSame(firstChild, previousSibling);
			}
		}
		}

		[Test]
		[Platform(Exclude="Linux", Reason="UIAutomation disabled on Linux")]
		public void ButtonUIA_OnePicture()
		{
			using (var site = new RootSiteDataProviderViewBase(m_cache))
			{
				site.StyleSheet = FixtureStyleSheet;
				site.WritingSystemFactory = m_wsManager;
			using (new SimpleRootSiteDataProviderTestsHelper(site))
			{
				site.MakeRoot(SimpleRootSiteDataProviderBaseVc.kfragRoot, () => new OnePictureVc());
				site.ShowForm();
				bool fInvoked = false;
				var dataProvider = new SimpleRootSiteDataProvider(site, childNavigationProvider =>
																		RootSiteServices.CreateUIAutomationInvokeButtons(
																			childNavigationProvider, site.RootBox,
																			sel => { fInvoked = true; }));
				var firstChild = dataProvider.Navigate(NavigateDirection.FirstChild);
				Assert.IsNotNull(firstChild, "firstChild");
				Assert.IsInstanceOf<UiaInvokeButton>(firstChild);
				var button = firstChild as UiaInvokeButton;
				var firstButtonChild = button.Navigate(NavigateDirection.FirstChild);
				Assert.IsNotNull(firstButtonChild, "button child control");
				Assert.IsInstanceOf<ImageControl>(firstButtonChild, "button child control");
				button.Invoke();
				Assert.IsTrue(fInvoked, "Invoked");

				var lastButtonChild = button.Navigate(NavigateDirection.LastChild);
				Assert.AreSame(firstButtonChild, lastButtonChild);
			}
		}
		}

	#endregion
	}
}
