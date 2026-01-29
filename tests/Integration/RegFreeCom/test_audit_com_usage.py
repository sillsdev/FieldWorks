import unittest
import tempfile
import shutil
from pathlib import Path
from scripts.regfree.audit_com_usage import (
    scan_project_for_com_usage,
    ComIndicators,
    ProjectAnalyzer,
)


class TestComAudit(unittest.TestCase):
    def setUp(self):
        self.test_dir = tempfile.mkdtemp()
        self.project_path = Path(self.test_dir)

    def tearDown(self):
        shutil.rmtree(self.test_dir)

    def create_cs_file(self, name, content):
        file_path = self.project_path / name
        file_path.parent.mkdir(parents=True, exist_ok=True)
        file_path.write_text(content, encoding="utf-8")
        return file_path

    def create_csproj_file(self, name, content):
        file_path = self.project_path / name
        file_path.parent.mkdir(parents=True, exist_ok=True)
        file_path.write_text(content, encoding="utf-8")
        return file_path

    def test_detects_dllimport_ole32(self):
        self.create_cs_file(
            "Interop.cs",
            """
            using System.Runtime.InteropServices;
            class Test {
                [DllImport("ole32.dll")]
                public static extern int CoCreateInstance();
            }
        """,
        )
        indicators, _ = scan_project_for_com_usage(self.project_path)
        self.assertGreater(indicators.dll_import_ole32, 0)
        self.assertTrue(indicators.uses_com)

    def test_detects_comimport(self):
        self.create_cs_file(
            "MyCom.cs",
            """
            [ComImport]
            [Guid("...")]
            class MyClass {}
        """,
        )
        indicators, _ = scan_project_for_com_usage(self.project_path)
        self.assertGreater(indicators.com_import_attribute, 0)
        self.assertTrue(indicators.uses_com)

    def test_detects_fwkernel_usage(self):
        self.create_cs_file(
            "Logic.cs",
            """
            using SIL.FieldWorks.FwKernel;
            class Logic {
                void Do() { var x = new FwKernel(); }
            }
        """,
        )
        indicators, _ = scan_project_for_com_usage(self.project_path)
        self.assertGreater(indicators.fw_kernel_reference, 0)

    def test_detects_views_usage(self):
        self.create_cs_file(
            "View.cs",
            """
            using SIL.FieldWorks.Common.COMInterfaces; // often implies Views
            // or direct Views usage
            using Views;
        """,
        )
        indicators, _ = scan_project_for_com_usage(self.project_path)
        self.assertGreater(indicators.views_reference, 0)

    def test_detects_project_reference_views(self):
        self.create_csproj_file(
            "Test.csproj",
            """
            <Project>
                <ItemGroup>
                    <ProjectReference Include="../ViewsInterfaces/ViewsInterfaces.csproj" />
                </ItemGroup>
            </Project>
            """,
        )
        indicators, _ = scan_project_for_com_usage(self.project_path)
        self.assertGreater(indicators.project_reference_views, 0)
        self.assertTrue(indicators.uses_com)

    def test_detects_package_reference_lcmodel(self):
        self.create_csproj_file(
            "Test.csproj",
            """
            <Project>
                <ItemGroup>
                    <PackageReference Include="SIL.LCModel" Version="1.0.0" />
                </ItemGroup>
            </Project>
            """,
        )
        indicators, _ = scan_project_for_com_usage(self.project_path)
        self.assertGreater(indicators.package_reference_lcmodel, 0)
        self.assertTrue(indicators.uses_com)

    def test_transitive_dependency_check(self):
        # Create Lib project that uses COM
        lib_dir = self.project_path / "Lib"
        lib_dir.mkdir()
        lib_csproj = lib_dir / "Lib.csproj"
        lib_csproj.write_text(
            """
            <Project>
                <ItemGroup>
                    <ProjectReference Include="../ViewsInterfaces/ViewsInterfaces.csproj" />
                </ItemGroup>
            </Project>
            """,
            encoding="utf-8",
        )

        # Create App project that references Lib
        app_dir = self.project_path / "App"
        app_dir.mkdir()
        app_csproj = app_dir / "App.csproj"
        app_csproj.write_text(
            """
            <Project>
                <ItemGroup>
                    <ProjectReference Include="../Lib/Lib.csproj" />
                </ItemGroup>
            </Project>
            """,
            encoding="utf-8",
        )

        analyzer = ProjectAnalyzer()
        indicators, details = analyzer.analyze_project(app_csproj)

        self.assertTrue(indicators.uses_com)
        self.assertGreater(indicators.project_reference_views, 0)
        self.assertTrue(any("Dependency Lib.csproj uses COM" in d for d in details))

    def test_no_com_usage(self):
        self.create_cs_file(
            "Plain.cs",
            """
            using System;
            class Plain { void Run() { Console.WriteLine("Hi"); } }
        """,
        )
        indicators, _ = scan_project_for_com_usage(self.project_path)
        self.assertFalse(indicators.uses_com)


if __name__ == "__main__":
    unittest.main()
