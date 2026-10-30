"""Property-only integration checks: no compilation, GUI or native execution."""
import json
from pathlib import Path
import shutil
import subprocess
import sys
import unittest


PROBE = Path(__file__).resolve().parents[1]
ROOT = PROBE.parents[1]


@unittest.skipUnless(sys.platform == "win32" and shutil.which("dotnet"),
                     "Windows SDK property-evaluation checks require dotnet on Windows")
class PackageModeTests(unittest.TestCase):
    def evaluate(self, project, tfm, manual=False, items=False, compile_items=False):
        args = ["dotnet", "msbuild", str(project), "-p:Configuration=Release",
                "-p:TargetFramework=" + tfm,
                "-getProperty:DisableImplicitUnoPackages,ImplicitUsings,ManagePackageVersionsCentrally,PublishAot"]
        if manual:
            args.append("-p:ProbeManualWindowsPackages=true")
        if items:
            args.append("-getItem:PackageReference")
        if compile_items:
            args.append("-getItem:Compile")
        result = subprocess.run(args, cwd=project.parent, capture_output=True, text=True, timeout=60)
        self.assertEqual(0, result.returncode, "MSBuild property evaluation failed; inspect locally, not as runtime evidence")
        return json.loads(result.stdout)

    def test_default_windows_keeps_implicit_packages_and_aot(self):
        p = self.evaluate(PROBE / "SmartSnippets.UnoProbe.csproj", "net10.0-windows10.0.26100")["Properties"]
        self.assertNotEqual("true", p["DisableImplicitUnoPackages"])
        self.assertEqual("true", p["PublishAot"])
        self.assertEqual("false", p["ManagePackageVersionsCentrally"])

    def test_manual_windows_has_explicit_native_packages_and_no_uno_using(self):
        document = self.evaluate(PROBE / "SmartSnippets.UnoProbe.csproj", "net10.0-windows10.0.26100", True, True)
        p = document["Properties"]
        self.assertEqual("true", p["DisableImplicitUnoPackages"])
        self.assertEqual("disable", p["ImplicitUsings"])
        self.assertEqual("true", p["PublishAot"])
        packages = {item["Identity"]: item.get("Version", "") for item in document["Items"]["PackageReference"]}
        self.assertEqual("1.8.260921001", packages["Microsoft.WindowsAppSDK"])
        self.assertEqual("10.0.28000.2705", packages["Microsoft.Windows.SDK.BuildTools"])
        self.assertNotIn("Uno.WinUI", packages)

    def test_manual_flag_does_not_disable_desktop_uno(self):
        p = self.evaluate(PROBE / "SmartSnippets.UnoProbe.csproj", "net10.0-desktop", True)["Properties"]
        self.assertNotEqual("true", p["DisableImplicitUnoPackages"])
        self.assertEqual("enable", p["ImplicitUsings"])
        self.assertEqual("true", p["PublishAot"])

    def test_serializer_risk_sources_are_not_part_of_uno_application(self):
        document = self.evaluate(PROBE / "SmartSnippets.UnoProbe.csproj", "net10.0-windows10.0.26100",
                                 manual=True, compile_items=True)
        sources = [item["Identity"].replace("\\", "/") for item in document["Items"]["Compile"]]
        self.assertIn("App.xaml.cs", sources)
        self.assertFalse(any("macos-json-risk/" in path for path in sources))

    def test_production_libraries_keep_central_package_management(self):
        for name in ("Core", "Presentation"):
            with self.subTest(project=name):
                p = self.evaluate(ROOT / "src" / ("SmartSnippets." + name) / ("SmartSnippets." + name + ".csproj"), "net10.0")["Properties"]
                self.assertEqual("true", p["ManagePackageVersionsCentrally"])
                self.assertNotEqual("true", p["DisableImplicitUnoPackages"])
                self.assertEqual("enable", p["ImplicitUsings"])


if __name__ == "__main__":
    unittest.main()
