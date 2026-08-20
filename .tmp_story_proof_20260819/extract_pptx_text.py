import re
import sys
import zipfile
import xml.etree.ElementTree as ET


def main() -> None:
    pptx_path = sys.argv[1]
    with zipfile.ZipFile(pptx_path) as archive:
        slide_names = sorted(
            (
                name
                for name in archive.namelist()
                if re.fullmatch(r"ppt/slides/slide\d+\.xml", name)
            ),
            key=lambda name: int(re.search(r"\d+", name).group()),
        )
        namespace = {"a": "http://schemas.openxmlformats.org/drawingml/2006/main"}
        for slide_number, slide_name in enumerate(slide_names, start=1):
            root = ET.fromstring(archive.read(slide_name))
            print(f"--- SLIDE {slide_number} ---")
            print("\n".join(node.text or "" for node in root.findall(".//a:t", namespace)))


if __name__ == "__main__":
    main()
