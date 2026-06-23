// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// legacy-screenshot-capture (option 3): renders legacy WinForms dialogs to "before" PNGs for the
// WinForms->Avalonia migration docs (Docs/migration/**). THROWAWAY evidence tooling — dies with the
// legacy code after the phase-2 migration; it exists only to capture a faithful visual baseline to
// build and verify the Avalonia surfaces against. NO desktop/mouse interaction (pure in-process
// construct + DrawToBitmap), so it runs unattended without grabbing the cursor.
//
// Two capture modes:
//   Cap(...)      simple dialogs: Show() off-screen + DrawToBitmap. No message loop.
//   CapLoop(...)  app-coupled dialogs (matching-entries search-browse, e.g. EntryGoDlg family):
//                 run the dialog on a REAL pumping STA message loop (Application.Run) with the app's
//                 "WindowConfiguration" provided (CaptureContext loads it), then a Forms.Timer
//                 captures + closes once it's painted; a watchdog backstops a hang.
//
// DATA FLAVORS (CaptureContext): "sena3" = read-only temp COPY of Sena 3 (real stylesheet + data);
// "minimal" = in-memory base cache. BEFORE/AFTER: this emits "<name>-before.png"; the Avalonia
// "<name>-after.png" comes from the surface's FwAvaloniaDialogs(Tests) visual test (the
// fieldworks-semantic-render-parity lane, same flavor); both attach to the JIRA ticket.
//
// Run: .\test.ps1 -SkipNative -TestProject LexTextControlsTests -TestFilter "FullyQualifiedName~ScreenshotHarness"

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using Microsoft.Win32;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// A throwaway capture context: an LcmCache "flavor" plus the minimal app context (Mediator,
	/// PropertyTable seeded with a real stylesheet + the app's WindowConfiguration) that legacy
	/// dialogs need to construct + render outside the running app.
	/// </summary>
	internal sealed class CaptureContext : IDisposable
	{
		public LcmCache Cache;
		public Mediator Mediator;
		public PropertyTable PropertyTable;
		public IHelpTopicProvider Help { get; private set; }
		public IApp App { get; private set; }
		private LcmStyleSheet m_styleSheet;
		private Form m_fakeWindow;
		private bool m_ownsCache;
		private string m_tempDir;

		private sealed class FakeMainWindow : Form { public LcmStyleSheet StyleSheet { get; set; } }
		private sealed class NullHelp : IHelpTopicProvider
		{
			public string GetHelpString(string id) => string.Empty;
			public string HelpFile => string.Empty;
		}
		/// <summary>Minimal IApp — the matching-entries browser's XmlVc/LayoutCache asserts app != null
		/// and uses app.ApplicationName for the layout inventory.</summary>
		private sealed class StubApp : IApp
		{
			public string ResourceString(string stid) => stid;
			public RegistryKey SettingsKey => null;
			public MsrSysType MeasurementSystem { get; set; }
			public Form ActiveMainWindow => null;
			public string ApplicationName => "Language Explorer";
			public PictureHolder PictureHolder => null;
			public void RestartSpellChecking() { }
			public void Synchronize() { }
			public void EnableMainWindows(bool fEnable) { }
			public void RemoveFindReplaceDialog() { }
			public bool ShowFindReplaceDialog(bool fReplace, RootSite rootsite) => false;
			public void HandleOutgoingLink(FwAppArgs link) { }
			public string SupportEmailAddress => string.Empty;
		}

		public static CaptureContext Minimal(LcmCache inMemoryCache)
		{
			var ctx = new CaptureContext { Cache = inMemoryCache, m_ownsCache = false };
			ctx.BuildAppContext();
			return ctx;
		}

		public static CaptureContext Sena3()
		{
			var src = Path.Combine(FwDirectoryFinder.ProjectsDirectory, "Sena 3");
			if (!Directory.Exists(src))
				throw new DirectoryNotFoundException("Sena 3 project not found at " + src);
			var temp = Path.Combine(Path.GetTempPath(), "fwcap", "Sena 3 " + Guid.NewGuid().ToString("N").Substring(0, 8));
			Directory.CreateDirectory(temp);
			foreach (var f in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
			{
				var rel = f.Substring(src.Length).TrimStart('\\', '/');
				var dest = Path.Combine(temp, rel);
				Directory.CreateDirectory(Path.GetDirectoryName(dest));
				File.Copy(f, dest, true);
			}
			var fwdata = Path.Combine(temp, "Sena 3.fwdata");
			var cache = LcmCache.CreateCacheFromExistingData(
				new TestProjectId(BackendProviderType.kXMLWithMemoryOnlyWsMgr, fwdata),
				"en", new DummyLcmUI(), FwDirectoryFinder.LcmDirectories, new LcmSettings(), new DummyProgressDlg());
			var ctx = new CaptureContext { Cache = cache, m_ownsCache = true, m_tempDir = temp };
			ctx.BuildAppContext();
			return ctx;
		}

		private void BuildAppContext()
		{
			Mediator = new Mediator();
			PropertyTable = new PropertyTable(Mediator);
			m_styleSheet = new LcmStyleSheet();
			try { m_styleSheet.Init(Cache, Cache.LangProject.Hvo, LangProjectTags.kflidStyles); } catch { }
			m_fakeWindow = new FakeMainWindow { StyleSheet = m_styleSheet };
			Help = new NullHelp();
			App = new StubApp();
			PropertyTable.SetProperty("cache", Cache, false);
			PropertyTable.SetProperty("window", m_fakeWindow, false);
			PropertyTable.SetProperty("currentContentControl", "lexiconEdit", false);
			PropertyTable.SetProperty("LcmStyleSheet", m_styleSheet, false);
			PropertyTable.SetProperty("HelpTopicProvider", Help, false);
			PropertyTable.SetProperty("App", App, false); // matching-entries browser needs a non-null IApp
			// The app's window config (commands, the matchingEntries guicontrol, etc.). Without this the
			// GoDlg family NREs in InitializeMatchingObjects. Same loader as LexEntryUi.EnsureWindowConfiguration.
			try
			{
				var configFile = FwDirectoryFinder.GetCodeFile("Language Explorer/Configuration/Main.xml");
				var config = XWindow.LoadConfigurationWithIncludes(configFile, true);
				PropertyTable.SetProperty("WindowConfiguration", config.SelectSingleNode("window"), false);
			}
			catch { /* best effort; simple dialogs don't need it */ }
		}

		public ILexEntry FirstEntry => Cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances().FirstOrDefault();
		public IPartOfSpeech FirstPos => Cache.LangProject.PartsOfSpeechOA?.PossibilitiesOS.OfType<IPartOfSpeech>().FirstOrDefault();
		public ICmPicture FirstPicture => Cache.ServiceLocator.GetInstance<ICmPictureRepository>().AllInstances().FirstOrDefault();
		public IFsFeatStruc FirstFeatStruc => Cache.ServiceLocator.GetInstance<IFsFeatStrucRepository>().AllInstances().FirstOrDefault();
		public ICmPossibilityList FirstList => Cache.LangProject.PartsOfSpeechOA;
		public ITsString Vern(string s) => TsStringUtils.MakeString(s, Cache.DefaultVernWs);

		public void Dispose()
		{
			try { PropertyTable?.Dispose(); } catch { }
			try { Mediator?.Dispose(); } catch { }
			try { m_fakeWindow?.Dispose(); } catch { }
			if (m_ownsCache) { try { Cache?.Dispose(); } catch { } }
			if (m_tempDir != null && Directory.Exists(m_tempDir)) { try { Directory.Delete(m_tempDir, true); } catch { } }
		}
	}

	/// <summary>Renders legacy WinForms dialogs to "before" PNGs for the migration docs.</summary>
	[TestFixture]
	[Category("ScreenshotHarness")]
	[Explicit("Capture utility — renders dialogs to PNGs; run only via -TestFilter \"FullyQualifiedName~ScreenshotHarness\", never in the normal suite.")]
	public class ScreenshotHarnessTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private readonly List<string> m_log = new List<string>();

		private static string RepoRoot => Directory.GetParent(FwDirectoryFinder.SourceDirectory).FullName;

		private static void SwallowThreadException(object sender, System.Threading.ThreadExceptionEventArgs e) { /* no modal; capture already taken */ }

		/// <summary>
		/// Render the (already-realized) dialog to "&lt;name&gt;-before.png", plus one PNG per tab
		/// ("&lt;name&gt;-tab-&lt;tab&gt;.png") for any TabControl so every tab's layout is captured.
		/// </summary>
		private static void Save(Form dlg, string name, string docRelDir)
		{
			var dir = Path.Combine(RepoRoot, "Docs", "migration", docRelDir, "images");
			Directory.CreateDirectory(dir);
			SaveBitmap(dlg, Path.Combine(dir, name + "-before.png"), throwIfBlank: true); // main shot (default tab)
			foreach (var tc in FindControls<TabControl>(dlg))
			{
				if (tc.TabPages.Count <= 1) continue; // single tab is already the main shot
				foreach (TabPage tp in tc.TabPages)
				{
					try
					{
						tc.SelectedTab = tp;
						Application.DoEvents();
						var label = string.IsNullOrWhiteSpace(tp.Text) ? tp.Name : tp.Text;
						var safe = Regex.Replace(label.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
						if (safe.Length == 0) safe = "tab" + tc.TabPages.IndexOf(tp);
						SaveBitmap(dlg, Path.Combine(dir, name + "-tab-" + safe + ".png"), throwIfBlank: false);
					}
					catch { }
				}
			}
		}

		private static void SaveBitmap(Form dlg, string path, bool throwIfBlank)
		{
			int w = Math.Max(dlg.Width, 1), h = Math.Max(dlg.Height, 1);
			using (var bmp = new Bitmap(w, h))
			{
				dlg.DrawToBitmap(bmp, new Rectangle(0, 0, w, h));
				bmp.Save(path, ImageFormat.Png);
			}
			if (throwIfBlank && new FileInfo(path).Length <= 2000) throw new Exception("blank/empty PNG");
		}

		private static IEnumerable<T> FindControls<T>(Control root) where T : Control
		{
			var found = new List<T>();
			void Walk(Control c) { foreach (Control ch in c.Controls) { if (ch is T t) found.Add(t); Walk(ch); } }
			Walk(root);
			return found;
		}

		private static string OnPickup(string name, Exception ex)
		{
			while (ex is TargetInvocationException && ex.InnerException != null) ex = ex.InnerException; // unwrap reflection wrapper
			var msg = string.Join(" | ", ex.Message.Split('\n').Select(s => s.Trim()).Where(s => s.Length > 0).Take(3));
			var frame = ex.StackTrace?.Split('\n').Select(s => s.Trim()).FirstOrDefault(s =>
				s.Contains("SIL.FieldWorks") && !s.Contains("TraceListener") && !s.Contains("SuppressAssertDialogs")
				&& !s.Contains("AssertionDialog") && !s.Contains("TraceInternal"))
				?? ex.StackTrace?.Split('\n').FirstOrDefault()?.Trim();
			return $"on-pickup  {name}  ({ex.GetType().Name}: {msg}) @ {frame}";
		}

		/// <summary>
		/// Construct the dialog and render it on its OWN timed STA thread running a REAL message loop
		/// (Application.Run + a timer that captures-then-closes once it's painted). The pumping loop lets
		/// dialogs that initialize asynchronously finish (Show()+DoEvents alone leaves them half-built);
		/// a synchronous hang (matching-entries) just blocks the thread → the Join timeout abandons it
		/// without killing the batch. Trace listeners are cleared by the test, so asserts never pop a modal.
		/// </summary>
		private void Cap(string name, string docRelDir, Func<Form> factory, int timeoutSec = 18)
		{
			string result = null;
			var t = new System.Threading.Thread(() =>
			{
				// Swallow WinForms unhandled-exception dialogs (e.g. "COM object separated from its RCW"
				// thrown when a Views-backed dialog disposes cross-apartment — AFTER its shot is taken).
				// These modals both interrupt the user and block the unattended run.
				try { Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException); } catch { }
				Application.ThreadException -= SwallowThreadException;
				Application.ThreadException += SwallowThreadException;
				Form dlg = null;
				try
				{
					dlg = factory();
					dlg.StartPosition = FormStartPosition.Manual;
					dlg.Location = new Point(-5000, -5000);
					dlg.ShowInTaskbar = false;
					var capTimer = new System.Windows.Forms.Timer { Interval = 1200 };
					capTimer.Tick += (s, e) =>
					{
						capTimer.Stop();
						try { Save(dlg, name, docRelDir); result = "captured  " + name; }
						catch (Exception ex) { result = OnPickup(name, ex); }
						try { dlg.Close(); } catch { }
					};
					dlg.Shown += (s, e) => capTimer.Start();
					Application.Run(dlg); // pumps until the dialog closes (the timer closes it)
					if (result == null) result = "on-pickup  " + name + "  (closed before capture)";
				}
				catch (Exception ex) { result = OnPickup(name, ex); }
				finally { try { dlg?.Dispose(); } catch { } }
			});
			t.SetApartmentState(System.Threading.ApartmentState.STA);
			t.IsBackground = true;
			t.Start();
			if (!t.Join(TimeSpan.FromSeconds(timeoutSec)) && result == null)
				result = "on-pickup  " + name + "  (hang/timeout " + timeoutSec + "s)"; // keep a capture that already succeeded even if close hung
			m_log.Add(result ?? ("on-pickup  " + name + "  (no result)"));
		}

		/// <summary>
		/// App-coupled dialog: run on a real pumping message loop so its matching-entries search-browse
		/// initializes, then a timer captures + closes; a watchdog force-closes on a hang.
		/// </summary>
		private void CapLoop(string name, string docRelDir, Func<Form> factory, int paintMs = 2500, int watchdogMs = 12000)
		{
			Form dlg = null;
			Exception err = null;
			bool captured = false;
			System.Threading.Timer watchdog = null;
			try
			{
				dlg = factory(); // SetDlgInfo runs here; WindowConfiguration is set so the GoDlg family won't NRE
				dlg.StartPosition = FormStartPosition.Manual;
				dlg.Location = new Point(-5000, -5000);
				dlg.ShowInTaskbar = false;
				var capTimer = new System.Windows.Forms.Timer { Interval = paintMs };
				capTimer.Tick += (s, e) =>
				{
					capTimer.Stop();
					try { Save(dlg, name, docRelDir); captured = true; }
					catch (Exception ex) { err = ex; }
					try { dlg.Close(); } catch { }
				};
				dlg.Shown += (s, e) => capTimer.Start();
				var d = dlg;
				watchdog = new System.Threading.Timer(_ =>
				{
					try { if (d.IsHandleCreated) d.BeginInvoke((Action)(() => { try { d.Close(); } catch { } })); } catch { }
				}, null, watchdogMs, System.Threading.Timeout.Infinite);
				Application.Run(dlg); // returns when the dialog closes
				m_log.Add(captured ? "captured(loop)  " + name
					: err != null ? OnPickup(name, err) : "on-pickup  " + name + "  (closed before capture)");
			}
			catch (Exception ex) { m_log.Add(OnPickup(name, ex)); }
			finally { try { watchdog?.Dispose(); } catch { } try { dlg?.Dispose(); } catch { } }
		}

		private static string Kebab(string cls)
		{
			var s = cls;
			if (s.EndsWith("Dlg")) s = s.Substring(0, s.Length - 3);
			else if (s.EndsWith("Dialog")) s = s.Substring(0, s.Length - 6);
			var sb = new System.Text.StringBuilder();
			for (int i = 0; i < s.Length; i++)
			{
				char c = s[i];
				if (char.IsUpper(c) && i > 0 && (char.IsLower(s[i - 1]) || (i + 1 < s.Length && char.IsLower(s[i + 1])))) sb.Append('-');
				sb.Append(char.ToLowerInvariant(c));
			}
			return sb.ToString();
		}

		/// <summary>Best-effort value for a ctor/SetDlgInfo parameter from the capture context.</summary>
		private static object ArgFor(ParameterInfo p, CaptureContext ctx)
		{
			var pt = p.ParameterType;
			// Per-dialog-type overrides (gated by the declaring dialog so they never regress other dialogs).
			var dt = p.Member?.DeclaringType?.Name;
			if (dt == "FwChooserDlg" && pt.IsArray) return Array.CreateInstance(pt.GetElementType(), 0); // initiallySelectedHvos
			if (dt == "MergeWritingSystemDlg")
			{
				if (pt == typeof(string)) return ctx.Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Id;
				if (pt != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(pt))
					return ctx.Cache.ServiceLocator.WritingSystems.AllWritingSystems.ToList();
			}
			if (dt == "UtilityDlg" && pt == typeof(XmlNode)) return ctx.PropertyTable.GetValue<XmlNode>("WindowConfiguration");
			if (pt == typeof(LcmCache)) return ctx.Cache;
			if (pt == typeof(Mediator)) return ctx.Mediator;
			if (pt == typeof(PropertyTable)) return ctx.PropertyTable;
			if (typeof(IHelpTopicProvider).IsAssignableFrom(pt)) return ctx.Help;
			if (typeof(IApp).IsAssignableFrom(pt)) return ctx.App;
			if (pt == typeof(ITsString)) return ctx.Vern("dawa");
			// Specific LCModel types BEFORE the generic ICmObject fallback (else we pass a LexEntry where
			// a list/picture/feat-struct is required -> ArgumentException).
			if (typeof(ICmPossibilityList).IsAssignableFrom(pt)) return ctx.FirstList;
			if (typeof(ICmPicture).IsAssignableFrom(pt)) return ctx.FirstPicture;
			if (typeof(IFsFeatStruc).IsAssignableFrom(pt)) return ctx.FirstFeatStruc;
			if (typeof(IPartOfSpeech).IsAssignableFrom(pt)) return ctx.FirstPos;
			if (typeof(ILexEntry).IsAssignableFrom(pt)) return ctx.FirstEntry;
			if (typeof(ICmObject).IsAssignableFrom(pt)) return pt.IsInstanceOfType(ctx.FirstEntry) ? (object)ctx.FirstEntry : null;
			// CmObjectUi (FdoUi) — make one for the seeded entry via its static factory (reflected).
			if (pt.Name == "CmObjectUi")
			{
				var mk = pt.GetMethod("MakeUi", new[] { typeof(LcmCache), typeof(int) });
				return (ctx.FirstEntry != null && mk != null) ? mk.Invoke(null, new object[] { ctx.Cache, ctx.FirstEntry.Hvo }) : null;
			}
			if (pt.Name == "WindowParams")
			{
				var wp = Activator.CreateInstance(pt);
				foreach (var f in pt.GetFields()) if (f.FieldType == typeof(string)) f.SetValue(wp, "Sample");
				return wp;
			}
			// NOTE: arrays/collections left null — supplying an empty array regressed ~11 dialogs that
			// treat null as "no selection / default" but break on an empty collection.
			if (pt == typeof(string)) return string.Empty;
			if (pt == typeof(int)) return 0; // hvo-guessing regressed more than it fixed; keep 0
			if (pt == typeof(bool)) return false;
			if (pt.IsValueType) return Activator.CreateInstance(pt);
			return null;
		}

		private static Form ReflectBuild(Type t, CaptureContext ctx, bool ctorOnly = false)
		{
			Form dlg;
			var ctor0 = t.GetConstructor(Type.EmptyTypes);
			var ctorH = t.GetConstructor(new[] { typeof(IHelpTopicProvider) });
			if (ctor0 != null) dlg = (Form)ctor0.Invoke(null);
			else if (ctorH != null) dlg = (Form)ctorH.Invoke(new object[] { ctx.Help });
			else
			{
				var ctor = t.GetConstructors().OrderBy(c => c.GetParameters().Length).FirstOrDefault();
				if (ctor == null) throw new Exception("no usable ctor");
				dlg = (Form)ctor.Invoke(ctor.GetParameters().Select(p => ArgFor(p, ctx)).ToArray());
			}
			if (ctorOnly) return dlg; // capture chrome only (skip the data-populating SetDlgInfo that hangs/NREs)
			var sdi = t.GetMethods().Where(m => m.Name == "SetDlgInfo" && !m.IsGenericMethod)
				.OrderByDescending(m => m.GetParameters().Length).FirstOrDefault();
			if (sdi != null)
				sdi.Invoke(dlg, sdi.GetParameters().Select(p => ArgFor(p, ctx)).ToArray());
			return dlg;
		}

		/// <summary>
		/// Reflect over a dialog assembly and auto-capture every constructable Form. ctorOnly=false runs
		/// the full SetDlgInfo (populated data); ctorOnly=true captures just the ctor-built chrome (the
		/// fallback for dialogs whose SetDlgInfo hangs/NREs — still a valid layout "before").
		/// </summary>
		private void ReflectSweep(string dll, string docRelDir, CaptureContext ctx, ISet<string> skip, bool ctorOnly)
		{
			Assembly asm;
			try { asm = Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(typeof(ScreenshotHarnessTests).Assembly.Location), dll)); }
			catch (Exception ex) { m_log.Add("on-pickup  (load " + dll + ": " + ex.Message.Split('\n')[0] + ")"); return; }
			Type[] types;
			try { types = asm.GetTypes(); }
			catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(x => x != null).ToArray(); }
			foreach (var t in types.Where(t => t != null && t.IsPublic && t.IsClass && !t.IsAbstract
				&& typeof(Form).IsAssignableFrom(t)
				&& (t.Name.EndsWith("Dlg") || t.Name.EndsWith("Dialog"))))
			{
				if (t.Name == "BaseGoDlg" || t.Name == "MasterListDlg") continue; // abstract base classes, not real dialogs
				var name = Kebab(t.Name);
				if (skip.Contains(name)) continue;
				if (!ctorOnly)
				{
					// Full pass: skip the matching-entries family — its SetDlgInfo builds a BrowseViewer
					// synchronously and blocks. (The ctor-only pass below captures their chrome instead.)
					bool goFamily = false;
					for (var b = t; b != null; b = b.BaseType) if (b.Name == "BaseGoDlg") { goFamily = true; break; }
					if (goFamily || t.Name == "InsertEntryDlg" || t.Name == "AddNewSenseDlg") continue;
				}
				// Chrome (ctor-only) renders fast or hangs — short timeout; full pass allows a bit longer.
				Cap(name, docRelDir, () => ReflectBuild(t, ctx, ctorOnly), timeoutSec: ctorOnly ? 6 : 10);
			}
		}

		[Test]
		[Timeout(1200000)] // full reflection sweep across dialog assemblies; override any default per-test timeout
		public void ScreenshotHarness_CaptureLegacyDialogs()
		{
			// Clear trace listeners for the duration: stops Debug.Assert MODAL POPUPS (the assert dialogs
			// that were appearing on screen) and makes asserts no-op so dialogs proceed past benign ones;
			// any real failure then surfaces as a catchable exception we log as a trace (never a popup).
			var savedListeners = new System.Diagnostics.TraceListener[System.Diagnostics.Trace.Listeners.Count];
			System.Diagnostics.Trace.Listeners.CopyTo(savedListeners, 0);
			System.Diagnostics.Trace.Listeners.Clear();
			try
			{
			// (1) Simple feature-tree dialog — in-memory base cache is enough.
			using (var min = CaptureContext.Minimal(Cache))
			{
				Cap("feature-chooser", ".", () =>
				{
					var lp = min.Cache.LanguageProject;
					var doc = new XmlDocument();
					var fsDir = Path.Combine(FwDirectoryFinder.SourceDirectory, "LexText", "LexTextControls", "LexTextControlsTests");
					doc.Load(Path.Combine(fsDir, "FeatureSystem2.xml"));
					var msfs = lp.MsFeatureSystemOA;
					msfs.AddFeatureFromXml(doc.SelectSingleNode("//item[@id='vNeut']"));
					msfs.AddFeatureFromXml(doc.SelectSingleNode("//item[@id='vFem']"));
					var cobj = min.Cache.ServiceLocator.GetInstance<ILexEntryInflTypeFactory>().Create();
					lp.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.Add(cobj);
					var dlg = new FeatureSystemInflectionFeatureListDlg();
					dlg.SetDlgInfo(min.Cache, null, null, cobj, 0);
					return dlg;
				});
			}

			// (2) Sena 3 real-data flavor.
			CaptureContext sena;
			try { sena = CaptureContext.Sena3(); }
			catch (Exception ex) { m_log.Add("on-pickup  (Sena3 open failed: " + ex.Message.Split('\n')[0] + ")"); sena = null; }
			if (sena != null)
				using (sena)
				{
					Cap("msa-inflection-feature-list", "dialogs", () => { var d = new MsaInflectionFeatureListDlg(); d.SetDlgInfo(sena.Cache, sena.Mediator, sena.PropertyTable, sena.FirstPos); return d; });

					// Hand-wired bespoke-domain-object dialog (the generic ArgFor can't supply these):
					// MsaCreatorDlg needs an IPersistenceProvider + a SandboxGenericMSA.
					Cap("msa-creator", "dialogs", () =>
					{
						var pp = new PersistenceProvider(sena.Mediator, sena.PropertyTable);
						var msa = new SandboxGenericMSA { MsaType = MsaType.kStem };
						var d = new MsaCreatorDlg();
						d.SetDlgInfo(sena.Cache, pp, sena.Mediator, sena.PropertyTable, sena.FirstEntry, msa, 0, false, "Create a New Grammatical Info");
						return d;
					});

					// BREADTH: reflect over every dialog assembly and auto-capture each constructable Form
					// (matching-entries/search-browse family auto-skipped -> live-capture). Resilient: each
					// runs on its own timed thread, so a failure/hang is logged and the sweep continues.
					var dlls = new[] { "LexTextControls.dll", "FwCoreDlgs.dll", "FdoUi.dll", "xWorks.dll" };
					var done = new HashSet<string> { "feature-chooser", "msa-inflection-feature-list", "msa-creator" };
					// Pass A — full SetDlgInfo (populated data); matching-entries family skipped (hangs).
					foreach (var dll in dlls) ReflectSweep(dll, "dialogs", sena, done, ctorOnly: false);
					// Lock in pass-A successes so the chrome pass doesn't overwrite a populated shot.
					foreach (var line in m_log.ToArray())
						if (line.StartsWith("captured")) done.Add(line.Substring(8).Trim());
					// Pass B — ctor-only CHROME for everything still un-captured (matching-entries, Gecko,
					// clerk-backed, SetDlgInfo-NRE): construct the dialog, skip the hanging SetDlgInfo, and
					// capture its layout. Empty of data but a structurally accurate "before".
					foreach (var dll in dlls) ReflectSweep(dll, "dialogs", sena, done, ctorOnly: true);

					// LIVE-CAPTURE / on-pickup — the matching-entries search-browse family (InsertEntryDlg,
					// EntryGoDlg, MergeEntryDlg, LinkMSADlg). Even with WindowConfiguration + a message loop +
					// IApp, MatchingObjectsBrowser builds a full BrowseViewer SYNCHRONOUSLY in SetDlgInfo and
					// blocks (the loop never pumps, so the watchdog can't rescue it). It needs the real app
					// (RecordClerk etc.). Capture these live from the running app or on JIRA pickup.
				}

			TestContext.WriteLine("=== ScreenshotHarness results ===");
			foreach (var line in m_log) TestContext.WriteLine(line);
			var captured = m_log.Count(l => l.StartsWith("captured"));
			TestContext.WriteLine($"captured {captured} / {m_log.Count}");
			Assert.That(captured, Is.GreaterThanOrEqualTo(1), "no dialogs captured; see log:\n" + string.Join("\n", m_log));
			}
			finally
			{
				System.Diagnostics.Trace.Listeners.Clear();
				System.Diagnostics.Trace.Listeners.AddRange(savedListeners);
			}
		}
	}
}
