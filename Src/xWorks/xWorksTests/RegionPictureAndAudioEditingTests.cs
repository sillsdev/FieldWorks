// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// §19d (T1/T3/T4) — picture insert/replace/delete/metadata + the picture ORC, and the voice-WS audio
	/// value write/clear, all against a REAL in-memory LCModel cache. The picture writes go through the
	/// shared <see cref="RegionPictureEditor"/> and the composed <see cref="ComposedRegionEditContext"/>;
	/// the ORC builds a <c>kodtGuidMoveableObjDisp</c> run; the audio value rides the SAME text setter as
	/// any multistring alternative (a voice alternative's text is the filename). The actual audio device is
	/// NOT exercised here (T1 asserts the model write, not a real mic — see the headless view tests for the
	/// affordance presence).
	/// </summary>
	[TestFixture]
	public class RegionPictureAndAudioEditingTests : MemoryOnlyBackendProviderTestBase
	// Non-restored base: setup wraps creation in a NonUndoableUnitOfWorkHelper.Do that COMPLETES before the
	// test body, so the edit context's own fenced LcmRegionEditSession opens cleanly (no nested task). Each
	// test commits/cancels its session so it never leaks into the next test's setup.
	{
		private ILexEntry m_entry;
		private ILexSense m_sense;
		private string m_tempDir;
		private string m_imageA;
		private string m_imageB;

		public override void TestSetup()
		{
			base.TestSetup();
			m_tempDir = Path.Combine(Path.GetTempPath(), "fw-pic-tests-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(m_tempDir);
			m_imageA = WritePng(Path.Combine(m_tempDir, "a.png"), 8, 8);
			m_imageB = WritePng(Path.Combine(m_tempDir, "b.png"), 12, 10);

			// LinkedFilesRootDir under the temp dir so UpdatePicture copies into a real, writable Pictures folder.
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.LangProject.LinkedFilesRootDir = m_tempDir;
				m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.LexemeFormOA = morph;
				morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("casa", Cache.DefaultVernWs));
				m_sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				m_entry.SensesOS.Add(m_sense);
			});
		}

		public override void TestTearDown()
		{
			try { if (m_tempDir != null && Directory.Exists(m_tempDir)) Directory.Delete(m_tempDir, true); }
			catch (Exception) { /* best effort temp cleanup */ }
			base.TestTearDown();
		}

		private static string WritePng(string path, int w, int h)
		{
			using (var bmp = new Bitmap(w, h))
			using (var g = Graphics.FromImage(bmp))
			{
				g.Clear(Color.CornflowerBlue);
				bmp.Save(path, ImageFormat.Png);
			}
			return path;
		}

		// A composed edit context with no setters (the picture ops use Cache/Entry/field.PictureHvo, not
		// the setter dictionaries) — except the ORC test, which supplies a rich-text setter writing the
		// sense gloss.
		private ComposedRegionEditContext NewContext(
			IReadOnlyDictionary<string, Func<string, RegionRichTextValue, bool>> richSetters = null)
		{
			return new ComposedRegionEditContext(Cache, m_entry,
				new Dictionary<string, Func<string, string, bool>>(),
				new Dictionary<string, Func<string, bool>>(),
				richTextSetters: richSetters);
		}

		private LexicalEditRegionField PictureField(int pictureHvo, int ownerHvo)
		{
			var f = new LexicalEditRegionField("pic", "Picture", "Pictures", null, RegionFieldKind.Image,
				default(SIL.FieldWorks.Common.FwAvalonia.ViewDefinition.EditorClassification), "pic", null,
				default(SIL.FieldWorks.Common.FwAvalonia.ViewDefinition.SurfaceRouting),
				new List<RegionWsValue> { new RegionWsValue("", string.Empty) }, null, null,
				isEditable: true, objectHvo: ownerHvo);
			f.PictureHvo = pictureHvo;
			return f;
		}

		// ----- T1: insert -----
		[Test]
		public void TryInsertPicture_CreatesPictureWithCaption_InTheSenseVector()
		{
			var ctx = NewContext();
			var field = PictureField(pictureHvo: 0, ownerHvo: m_sense.Hvo);

			var ok = ctx.TryInsertPicture(field, m_imageA, new RegionPictureMetadata(caption: "a house"));
			ctx.Commit();

			Assert.That(ok, Is.True);
			Assert.That(m_sense.PicturesOS.Count, Is.EqualTo(1));
			Assert.That(m_sense.PicturesOS[0].Caption.AnalysisDefaultWritingSystem.Text, Is.EqualTo("a house"));
		}

		[Test]
		public void TryInsertPicture_MissingFile_ReturnsFalse_NoPicture_NoOpenSession()
		{
			var ctx = NewContext();
			var field = PictureField(0, m_sense.Hvo);

			var ok = ctx.TryInsertPicture(field, Path.Combine(m_tempDir, "does-not-exist.png"),
				new RegionPictureMetadata(caption: "x"));

			Assert.That(ok, Is.False);
			Assert.That(m_sense.PicturesOS.Count, Is.EqualTo(0));
			Assert.That(ctx.IsOpen, Is.False, "a rejected insert must not strand an open fence");
		}

		// ----- T1: replace -----
		[Test]
		public void TryReplacePictureFile_SwapsTheFile_KeepsCaption()
		{
			var picture = InsertPictureDirectly(m_imageA, "keep me");
			var ctx = NewContext();
			var field = PictureField(picture.Hvo, m_sense.Hvo);

			var ok = ctx.TryReplacePictureFile(field, m_imageB);
			ctx.Commit();

			Assert.That(ok, Is.True);
			Assert.That(picture.Caption.AnalysisDefaultWritingSystem.Text, Is.EqualTo("keep me"));
			Assert.That(Path.GetFileName(picture.PictureFileRA.InternalPath), Does.Contain("b"));
		}

		// ----- T1: delete (+ T3 delete-then-undo) -----
		[Test]
		public void TryDeletePicture_RemovesFromVector_AndUndoRestores()
		{
			var picture = InsertPictureDirectly(m_imageA, "doomed");
			Assert.That(m_sense.PicturesOS.Count, Is.EqualTo(1));
			var ctx = NewContext();
			var field = PictureField(picture.Hvo, m_sense.Hvo);

			var ok = ctx.TryDeletePicture(field);
			ctx.Commit();
			Assert.That(ok, Is.True);
			Assert.That(m_sense.PicturesOS.Count, Is.EqualTo(0));

			// T3: delete-then-undo restores the picture (one undoable step on the global stack).
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.True);
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_sense.PicturesOS.Count, Is.EqualTo(1));
		}

		// ----- T1: metadata (+ T3 empty caption) -----
		[Test]
		public void TrySetPictureMetadata_WritesCaptionAndDescription()
		{
			var picture = InsertPictureDirectly(m_imageA, "old");
			var ctx = NewContext();
			var field = PictureField(picture.Hvo, m_sense.Hvo);

			var ok = ctx.TrySetPictureMetadata(field,
				new RegionPictureMetadata(caption: "new caption", description: "a description"));
			ctx.Commit();

			Assert.That(ok, Is.True);
			Assert.That(picture.Caption.AnalysisDefaultWritingSystem.Text, Is.EqualTo("new caption"));
			Assert.That(picture.Description.AnalysisDefaultWritingSystem.Text, Is.EqualTo("a description"));
		}

		[Test]
		public void TrySetPictureMetadata_EmptyCaption_ClearsIt()
		{
			var picture = InsertPictureDirectly(m_imageA, "had one");
			var ctx = NewContext();
			var field = PictureField(picture.Hvo, m_sense.Hvo);

			ctx.TrySetPictureMetadata(field, new RegionPictureMetadata(caption: string.Empty));
			ctx.Commit();

			Assert.That(picture.Caption.AnalysisDefaultWritingSystem.Text ?? string.Empty, Is.Empty);
		}

		// ----- T1: picture ORC (closes §19c→§19d) -----
		[Test]
		public void TryInsertPictureOrc_BuildsAPictureOrcRun_InTheValue()
		{
			// A rich-text setter writing the sense gloss (the field under test).
			RegionRichTextValue written = null;
			var setters = new Dictionary<string, Func<string, RegionRichTextValue, bool>>
			{
				["gloss"] = (ws, value) => { written = value; return true; }
			};
			var ctx = NewContext(setters);
			var field = new LexicalEditRegionField("gloss", "Gloss", "Gloss", null, RegionFieldKind.Text,
				default(SIL.FieldWorks.Common.FwAvalonia.ViewDefinition.EditorClassification), "gloss", null,
				default(SIL.FieldWorks.Common.FwAvalonia.ViewDefinition.SurfaceRouting),
				new List<RegionWsValue> { new RegionWsValue("en", "hi", wsTag: "en") }, null, null,
				isEditable: true, objectHvo: m_sense.Hvo);

			var before = Cache.ServiceLocator.GetInstance<ICmPictureRepository>().AllInstances().Count();
			var ok = ctx.TryInsertPictureOrc(field, "en", caretPosition: 1, m_imageA,
				new RegionPictureMetadata(caption: "inline"));
			ctx.Commit();

			Assert.That(ok, Is.True);
			Assert.That(written, Is.Not.Null);
			Assert.That(written.Runs.Any(r => r.OrcKind == RegionOrcKind.Picture), Is.True,
				"the rebuilt value must carry a picture ORC run (kodtGuidMoveableObjDisp)");
			var after = Cache.ServiceLocator.GetInstance<ICmPictureRepository>().AllInstances().Count();
			Assert.That(after - before, Is.EqualTo(1),
				"the inline picture is created (like FwEditingHelper) and referenced by the ORC");
		}

		// ----- T1/T4: audio voice-WS value write + clear -----
		[Test]
		public void AudioVoiceWs_WriteAndClear_RoundTripsThroughTheTextSetter()
		{
			// A voice WS value is a multistring alternative whose text is the filename; the composer makes
			// the row editable and the SAME text setter writes/clears it. Here we drive the setter directly.
			string staged = null;
			var setters = new Dictionary<string, Func<string, string, bool>>
			{
				["audio"] = (ws, value) => { staged = value; return true; }
			};
			var ctx = new ComposedRegionEditContext(Cache, m_entry, setters,
				new Dictionary<string, Func<string, bool>>());
			var field = new LexicalEditRegionField("audio", "Pronunciation", "Form", null, RegionFieldKind.Text,
				default(SIL.FieldWorks.Common.FwAvalonia.ViewDefinition.EditorClassification), "audio", null,
				default(SIL.FieldWorks.Common.FwAvalonia.ViewDefinition.SurfaceRouting),
				new List<RegionWsValue> { new RegionWsValue("aud", string.Empty, wsTag: "aud-Zxxx-x-audio", isAudio: true) },
				null, null, isEditable: true, objectHvo: m_entry.Hvo);

			Assert.That(ctx.TrySetText(field, "aud-Zxxx-x-audio", "casa.wav"), Is.True);
			Assert.That(staged, Is.EqualTo("casa.wav"), "record writes the filename through the text setter");

			Assert.That(ctx.TrySetText(field, "aud-Zxxx-x-audio", string.Empty), Is.True);
			Assert.That(staged, Is.Empty, "clear empties the voice-WS value");

			ctx.Commit(); // close the fenced session so it never leaks into the next test's setup

		}

		private ICmPicture InsertPictureDirectly(string file, string caption)
		{
			ICmPicture picture = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				picture = RegionPictureEditor.CreatePicture(Cache, m_sense,
					m_sense.Cache.MetaDataCacheAccessor.GetFieldId2(m_sense.ClassID, "Pictures", true),
					file, new RegionPictureMetadata(caption: caption));
			});
			return picture;
		}
	}
}
