"""Independently verify the three accepted original restaurant PNGs."""
import hashlib
import json
from pathlib import Path
from PIL import Image

PROJECT = Path(__file__).resolve().parents[1]


def main():
    report = json.loads((PROJECT / 'Documentation/GeminiRestaurantAssets.json').read_text(encoding='utf-8-sig'))
    assert report['previewInspected'] and report['verifiedIndependently']
    assert len(report['assets']) == 3
    for asset in report['assets']:
        path = PROJECT / asset['projectFile']
        assert hashlib.sha256(path.read_bytes()).hexdigest() == asset['sha256'], path
        with Image.open(path) as im:
            assert im.size == (128, 128) and im.mode == 'RGBA', path
            alpha = im.getchannel('A')
            assert alpha.getextrema() == (0, 255), path
            left, top, right, bottom = alpha.getbbox()
            margin = min(left, top, 128 - right, 128 - bottom)
            assert margin == asset['margin'] and margin >= asset['requiredMargin'], path
    print(json.dumps({'restaurantAssets': 3, 'passed': True}))


if __name__ == '__main__':
    main()
