"""Build-only entry point for the linked-source fixture gate; never launches it."""
import importlib.util
from pathlib import Path
import sys

spec = importlib.util.spec_from_file_location('full_host_observer', Path(__file__).with_name('run_uno_candidate_acceptance.py'))
observer = importlib.util.module_from_spec(spec)
spec.loader.exec_module(observer)
try:
    if len(sys.argv) != 3:
        raise ValueError('arguments')
    if sys.argv[1] == '--capture-source':
        Path(sys.argv[2]).write_text(observer.source_fingerprint() + '\n', encoding='ascii')
    else:
        observer.validate_fixture(Path(sys.argv[1]), sys.argv[2])
        print('BUILD_CHECKS_ONLY')
except BaseException:
    print('BUILD_BLOCKED')
    sys.exit(1)
