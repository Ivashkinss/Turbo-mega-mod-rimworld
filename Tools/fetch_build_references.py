"""Fetch pinned compile-only RimWorld 1.6 references for a machine running 1.7."""
import hashlib
import urllib.request
import zipfile
from pathlib import Path

BUILD = Path(__file__).resolve().parent / "Build"
VERSION = "1.6.4871"
SHA256 = "81b168db70a9aceaeab293b1519bb695bc5167f4510966a45f7ff38cb236f54e"


def main():
    BUILD.mkdir(exist_ok=True)
    package = BUILD / f"krafs.rimworld.ref.{VERSION}.nupkg"
    if not package.is_file():
        urllib.request.urlretrieve(
            f"https://api.nuget.org/v3-flatcontainer/krafs.rimworld.ref/{VERSION}/{package.name}", package)
    if hashlib.sha256(package.read_bytes()).hexdigest() != SHA256:
        raise ValueError("Reference package checksum differs from the pinned package")
    destination = BUILD / f"RimRef-{VERSION}"
    with zipfile.ZipFile(package) as archive:
        for entry in archive.infolist():
            if not (destination / entry.filename).resolve().is_relative_to(destination.resolve()):
                raise ValueError("Archive entry escapes reference directory")
        archive.extractall(destination)
    print(destination / "ref/net472")


if __name__ == "__main__":
    main()
