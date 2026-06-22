// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// winforms-free-lexeme-editor.md D4 (wave 4) — the dialog-launcher lane: a claimed slice
	/// builds an Avalonia value row + "..." button (<see cref="FwDialogLauncherField"/>) whose
	/// button calls the host-injected <see cref="ILegacyDialogLauncher"/> seam with the row's
	/// (object, node); without injected services the button renders disabled and launching is a
	/// no-op. Value text matches the legacy launcher views (feature-structure ShortName/LongName,
	/// media file path).
	/// </summary>
	[TestFixture]
	public class DialogLauncherPluginTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry m_entry;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.LexemeFormOA = morph;
				morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("casa", Cache.DefaultVernWs));
			});
		}

		private sealed class FakeLegacyDialogLauncher : ILegacyDialogLauncher
		{
			public int Calls;
			public ICmObject LastObject;
			public ViewNode LastNode;
			public bool Result = true;

			public bool LaunchFor(ICmObject obj, ViewNode node)
			{
				Calls++;
				LastObject = obj;
				LastNode = node;
				return Result;
			}
		}

		private static ViewNode LauncherNode(string field, string legacyClassName, string label = "Launcher")
			=> new ViewNode("Test/Launcher/#0", ViewNodeKind.Field, label, null, field, "custom",
				EditorClassification.Dynamic, null, ViewVisibility.Always, ViewExpansion.NotApplicable,
				false, null, Array.Empty<ViewNode>(), customEditorClass: legacyClassName,
				customEditorAssembly: "LexEdDll.dll");

		private IMoStemMsa MakeStemMsaWithFeatures(out IFsFeatStruc fs)
		{
			IMoStemMsa msa = null;
			IFsFeatStruc featStruc = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				msa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
				m_entry.MorphoSyntaxAnalysesOC.Add(msa);
				featStruc = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
				msa.MsFeaturesOA = featStruc;
			});
			fs = featStruc;
			return msa;
		}

		private ICmMedia MakePronunciationMedia(string internalPath)
		{
			ICmMedia media = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var pronunciation = Cache.ServiceLocator.GetInstance<ILexPronunciationFactory>().Create();
				m_entry.PronunciationsOS.Add(pronunciation);
				media = Cache.ServiceLocator.GetInstance<ICmMediaFactory>().Create();
				pronunciation.MediaFilesOS.Add(media);
				if (internalPath != null)
				{
					var folder = Cache.ServiceLocator.GetInstance<ICmFolderFactory>().Create();
					Cache.LangProject.MediaOC.Add(folder);
					var file = Cache.ServiceLocator.GetInstance<ICmFileFactory>().Create();
					folder.FilesOC.Add(file);
					file.InternalPath = internalPath;
					media.MediaFileRA = file;
				}
			});
			return media;
		}

		[Test]
		public void LauncherPlugin_WithServices_BuildsRowWiredToTheSeam_WithTheRightObjectAndNode()
		{
			var media = MakePronunciationMedia("AudioVisual\\hello.wav");
			var node = LauncherNode("MediaFile", DialogLauncherPlugins.AudioVisualSliceClassName, "Media File");
			var fake = new FakeLegacyDialogLauncher();
			var services = new RegionEditorServices { LegacyDialogLauncher = fake };

			var control = DialogLauncherPlugins.CreateAudioVisual()
				.BuildControl(media, node, null, Cache, services);

			Assert.That(control, Is.InstanceOf<FwDialogLauncherField>());
			var row = (FwDialogLauncherField)control;
			Assert.That(row.CanLaunch, Is.True, "an injected launcher service enables the button");
			Assert.That(row.Value, Does.EndWith("hello.wav"),
				"the value text is the media file path (AudioVisualVc parity)");

			row.Launch(); // the button-click path
			Assert.That(fake.Calls, Is.EqualTo(1));
			Assert.That(fake.LastObject, Is.SameAs(media), "the seam receives the row's own object");
			Assert.That(fake.LastNode, Is.SameAs(node), "the seam receives the row's own typed node");
		}

		[Test]
		public void LauncherPlugin_WithoutServices_RendersTheRowDisabled_AndLaunchIsANoOp()
		{
			var media = MakePronunciationMedia(null);
			var node = LauncherNode("MediaFile", DialogLauncherPlugins.AudioVisualSliceClassName, "Media File");

			// Both the four-argument (classic) and five-argument (null services) paths degrade the
			// same way: value renders, button disabled.
			var plugin = DialogLauncherPlugins.CreateAudioVisual();
			foreach (var control in new[]
			{
				plugin.BuildControl(media, node, null, Cache),
				plugin.BuildControl(media, node, null, Cache, null),
				plugin.BuildControl(media, node, null, Cache, new RegionEditorServices())
			})
			{
				Assert.That(control, Is.InstanceOf<FwDialogLauncherField>());
				var row = (FwDialogLauncherField)control;
				Assert.That(row.CanLaunch, Is.False, "no launcher service: the button is disabled");
				Assert.That(() => row.Launch(), Throws.Nothing);
			}
		}

		[Test]
		public void MsaLauncherPlugin_ValueIsTheFeatureStructureShortName_AndSeamGetsTheMsa()
		{
			var msa = MakeStemMsaWithFeatures(out var fs);
			var node = LauncherNode("MsFeatures", DialogLauncherPlugins.MsaFeatureSliceClassName,
				"Inflection Features");
			var fake = new FakeLegacyDialogLauncher();
			var services = new RegionEditorServices { LegacyDialogLauncher = fake };

			var row = (FwDialogLauncherField)DialogLauncherPlugins.CreateMsaInflectionFeatures()
				.BuildControl(msa, node, null, Cache, services);

			// MsaInflectionFeatureListDlgLauncherView renders the structure with
			// CmAnalObjectVc kfragShortName — i.e. the feature structure's ShortName.
			Assert.That(row.Value, Is.EqualTo(fs.ShortName ?? string.Empty));

			row.Launch();
			Assert.That(fake.LastObject, Is.SameAs(msa));
			Assert.That(fake.LastNode, Is.SameAs(node));
		}

		[Test]
		public void ResolveFeatureStructure_MirrorsTheLegacySliceInstall()
		{
			var msa = MakeStemMsaWithFeatures(out var fs);

			// field= resolves the owning atomic flid on the row's object (slice GetFlid).
			var withField = DialogLauncherPlugins.ResolveFeatureStructure(msa,
				LauncherNode("MsFeatures", DialogLauncherPlugins.MsaFeatureSliceClassName), Cache, out var flid);
			Assert.That(withField, Is.SameAs(fs));
			Assert.That(flid, Is.EqualTo(MoStemMsaTags.kflidMsFeatures));

			// No field: the row's object IS the structure (FsFeatStruc-Detail-FeatureSpecs).
			var selfBound = DialogLauncherPlugins.ResolveFeatureStructure(fs,
				LauncherNode(null, DialogLauncherPlugins.MsaFeatureSliceClassName), Cache, out var selfFlid);
			Assert.That(selfBound, Is.SameAs(fs));
			Assert.That(selfFlid, Is.EqualTo(FsFeatStrucTags.kflidFeatureSpecs));

			// An empty owning field resolves the flid (the dialog creates the structure) but no fs.
			IMoStemMsa bareMsa = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				bareMsa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
				m_entry.MorphoSyntaxAnalysesOC.Add(bareMsa);
			});
			var missing = DialogLauncherPlugins.ResolveFeatureStructure(bareMsa,
				LauncherNode("MsFeatures", DialogLauncherPlugins.MsaFeatureSliceClassName), Cache, out var bareFlid);
			Assert.That(missing, Is.Null);
			Assert.That(bareFlid, Is.EqualTo(MoStemMsaTags.kflidMsFeatures));
		}

		[Test]
		public void AudioVisualValueReader_ReadsTheMediaFilePath_AndToleratesAMissingFile()
		{
			var media = MakePronunciationMedia("AudioVisual\\hello.wav");
			Assert.That(DialogLauncherPlugins.ReadMediaFilePath(media), Does.EndWith("hello.wav"));
			Assert.That(DialogLauncherPlugins.ReadMediaFilePath(media.MediaFileRA), Does.EndWith("hello.wav"),
				"a CmFile row reads the same path (legacy initializes its launcher with the file)");

			var bare = MakePronunciationMedia(null);
			Assert.That(DialogLauncherPlugins.ReadMediaFilePath(bare), Is.Empty,
				"no media file yet: empty value, never a throw");
			Assert.That(DialogLauncherPlugins.ReadMediaFilePath(m_entry), Is.Empty,
				"a non-media object degrades to an empty value");
		}

		private sealed class FakeServiceAwarePlugin : IServiceAwareRegionEditorPlugin
		{
			public RegionEditorServices LastServices;
			public int FiveArgCalls;
			public int FourArgCalls;

			public string LegacyClassName => AvaloniaCompanionSlices.MessageSliceClassName;

			public Avalonia.Controls.Control BuildControl(ICmObject obj, ViewNode node,
				IRegionEditContext editContext, LcmCache cache)
			{
				FourArgCalls++;
				return null;
			}

			public Avalonia.Controls.Control BuildControl(ICmObject obj, ViewNode node,
				IRegionEditContext editContext, LcmCache cache, RegionEditorServices services)
			{
				FiveArgCalls++;
				LastServices = services;
				return null;
			}
		}

		[Test]
		public void Compose_ThreadsHostServicesIntoServiceAwarePluginFactories()
		{
			var registry = new RegionEditorPluginRegistry();
			var plugin = new FakeServiceAwarePlugin();
			registry.Register(plugin);
			var services = new RegionEditorServices { LegacyDialogLauncher = new FakeLegacyDialogLauncher() };

			var composed = FullEntryRegionComposer.Compose(m_entry, Cache, plugins: registry,
				services: services);
			var row = composed.Model.Fields.Single(f => f.Kind == RegionFieldKind.Custom);
			row.ControlFactory();

			Assert.That(plugin.FiveArgCalls, Is.EqualTo(1),
				"a service-aware plugin builds through the five-argument overload");
			Assert.That(plugin.FourArgCalls, Is.EqualTo(0));
			Assert.That(plugin.LastServices, Is.SameAs(services),
				"the factory closes over the host's own services instance");
		}

		[Test]
		public void Compose_WithoutServices_HandsServiceAwarePluginsNull()
		{
			var registry = new RegionEditorPluginRegistry();
			var plugin = new FakeServiceAwarePlugin();
			registry.Register(plugin);

			var composed = FullEntryRegionComposer.Compose(m_entry, Cache, plugins: registry);
			composed.Model.Fields.Single(f => f.Kind == RegionFieldKind.Custom).ControlFactory();

			Assert.That(plugin.FiveArgCalls, Is.EqualTo(1));
			Assert.That(plugin.LastServices, Is.Null, "services are optional by contract (default null)");
		}
	}
}
