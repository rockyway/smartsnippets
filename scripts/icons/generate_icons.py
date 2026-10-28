#!/usr/bin/env python3
"""Regenerate checked-in icons: pip install -r scripts/icons/requirements.txt; python scripts/icons/generate_icons.py [--check]."""
from pathlib import Path
import argparse
import io
import struct
import sys

import cairosvg
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
SOURCES = ROOT / 'assets/icons'
ASSETS = ROOT / 'src/SmartSnippets.App/Assets'
APP_SIZES = (16, 20, 24, 32, 40, 48, 64, 256)
TRAY_SIZES = (16, 20, 24, 32, 40, 48)


def render(name, size, color=None):
    svg = (SOURCES / name).read_text(encoding='utf-8')
    if color:
        svg = svg.replace('#1b1b1b', color)
    return Image.open(io.BytesIO(cairosvg.svg2png(bytestring=svg.encode(), output_width=size, output_height=size))).convert('RGBA')


def tray(size, theme, state):
    # Supersampling the dot keeps the transparent ring smooth at every DPI.
    name = f'tray-{size}.svg' if size in (16, 20) else 'tray.svg'
    image = render(name, size * 4, '#1b1b1b' if theme == 'Light' else '#ffffff')
    if state != 'Ready':
        scale = size * 4 / 24
        x = y = 19 * scale
        r = 4.2 * scale
        ring = r + 0.8 * scale
        draw = ImageDraw.Draw(image)
        draw.ellipse((x-ring, y-ring, x+ring, y+ring), fill=(0, 0, 0, 0))
        draw.ellipse((x-r, y-r, x+r, y+r), fill='#2f8bff' if state == 'PopupOpen' else '#e5484d')
    return image.resize((size, size), Image.Resampling.LANCZOS)


def dib(image):
    w, h = image.size
    rgba = image.tobytes('raw', 'BGRA')
    stride = w * 4
    pixels = b''.join(rgba[y * stride:(y + 1) * stride] for y in range(h - 1, -1, -1))
    mask_stride = ((w + 31) // 32) * 4
    mask = bytearray(mask_stride * h)
    alpha = image.getchannel('A')
    for y in range(h):
        for x in range(w):
            if alpha.getpixel((x, h - 1 - y)) == 0:
                mask[y * mask_stride + x // 8] |= 0x80 >> (x % 8)
    header = struct.pack('<IiiHHIIiiII', 40, w, h * 2, 1, 32, 0, len(pixels), 0, 0, 0, 0)
    return header + pixels + mask


def ico(images):
    entries, payload = [], bytearray()
    offset = 6 + 16 * len(images)
    for image in images:
        size = image.width
        if size == 256:
            stream = io.BytesIO()
            image.save(stream, format='PNG', optimize=False)
            data = stream.getvalue()
        else:
            data = dib(image)
        entries.append(struct.pack('<BBBBHHII', size % 256, size % 256, 0, 0, 1, 32, len(data), offset + len(payload)))
        payload.extend(data)
    return struct.pack('<HHH', 0, 1, len(images)) + b''.join(entries) + payload


def png(image):
    data = io.BytesIO()
    image.save(data, format='PNG', optimize=False)
    return data.getvalue()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    app = [render('app-16.svg' if s == 16 else 'app.svg', s) for s in APP_SIZES]
    outputs = {ASSETS / 'SmartSnippets.ico': ico(app)}
    variants = {}
    for theme in ('Light', 'Dark'):
        for state in ('Ready', 'PopupOpen', 'HotkeyUnavailable'):
            images = [tray(s, theme, state) for s in TRAY_SIZES]
            variants[(theme, state)] = images
            outputs[ASSETS / f'Tray-{theme}-{state}.ico'] = ico(images)
    # Contact sheet displays every frame on both taskbar colors at native size.
    sheet = Image.new('RGBA', (800, 7 * 74), 'white')
    draw = ImageDraw.Draw(sheet)
    rows = [('App', app)] + [(f'{t} {s}', v) for (t, s), v in variants.items()]
    for row, (label, images) in enumerate(rows):
        y = row * 74
        draw.text((4, y + 4), label, fill='black')
        for j, image in enumerate(images):
            # Keep the 256px frame legible without overlapping adjacent cells.
            thumb = image.copy()
            thumb.thumbnail((48, 48), Image.Resampling.LANCZOS)
            x = 120 + j * 84
            draw.rectangle((x, y, x + 39, y + 63), fill='#f3f3f3')
            draw.rectangle((x + 40, y, x + 79, y + 63), fill='#202020')
            if thumb.width > 36:
                thumb.thumbnail((36, 36), Image.Resampling.LANCZOS)
            sheet.alpha_composite(thumb, (x + (40 - thumb.width) // 2, y + (64 - thumb.height) // 2))
            sheet.alpha_composite(thumb, (x + 40 + (40 - thumb.width) // 2, y + (64 - thumb.height) // 2))
    outputs[SOURCES / 'preview.png'] = png(sheet.convert('RGB'))
    failed = False
    for path, content in outputs.items():
        if path.suffix == '.ico':
            count = struct.unpack_from('<H', content, 4)[0]
            sizes = [content[6 + 16 * i] or 256 for i in range(count)]
            print(f'{path.name}: {sizes}')
        if args.check:
            if not path.exists() or path.read_bytes() != content:
                print(f'DIFF: {path.relative_to(ROOT)}', file=sys.stderr)
                failed = True
        else:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(content)
    return int(failed)


if __name__ == '__main__':
    sys.exit(main())
