"""Pure bundle metadata checks, not old-OS runtime acceptance."""
from pathlib import Path
import plistlib
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from build_native_bundle import bundle_info, ENTRY


class BundleMetadataTests(unittest.TestCase):
    def test_launch_floor_uses_supported_runtime_not_lower_link_target(self):
        info = plistlib.loads(plistlib.dumps(bundle_info()))
        self.assertEqual('15.0', info['LSMinimumSystemVersion'])
        self.assertEqual('org.smartsnippets.feasibility018', info['CFBundleIdentifier'])
        self.assertEqual(ENTRY, info['CFBundleExecutable'])
        self.assertEqual('APPL', info['CFBundlePackageType'])
        self.assertTrue(info['NSHighResolutionCapable'])

    def test_each_bundle_gets_its_own_metadata(self):
        first = bundle_info()
        first['LSMinimumSystemVersion'] = '1.0'
        self.assertEqual('15.0', bundle_info()['LSMinimumSystemVersion'])


if __name__ == '__main__':
    unittest.main()
