from __future__ import annotations

from pathlib import Path
import shutil
import sys

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
SITE_ROOT = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else None
SIZE = 1024

paper = "#F7F7F4"
line = "#D8D8D2"
ink = "#191918"
muted = "#787873"
focus = "#4D6374"

image = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
draw = ImageDraw.Draw(image)

draw.rounded_rectangle((0, 0, 1023, 1023), radius=168, fill=paper)
draw.rounded_rectangle((32, 32, 991, 991), radius=140, outline=line, width=20)

draw.line((244, 240, 712, 240, 712, 504), fill=ink, width=40, joint="curve")
draw.line((244, 240, 244, 712, 548, 712), fill=ink, width=40, joint="curve")
draw.line((354, 360, 584, 360), fill=muted, width=22)
draw.line((354, 448, 516, 448), fill=muted, width=22)
draw.rectangle((548, 548, 792, 792), fill=ink)
draw.line((712, 284, 712, 496), fill=focus, width=20)

brand_png = ROOT / "docs/brand/hiddenwindow-mark.png"
app_png = ROOT / "src/HiddenWindow/Assets/HiddenWindow-v2.png"
app_ico = ROOT / "src/HiddenWindow/Assets/HiddenWindow.ico"

image.save(brand_png)
image.resize((256, 256), Image.Resampling.LANCZOS).save(app_png)
# bitmap_format="bmp"：ExtractIconEx/ExtractAssociatedIcon 等 Win32 API 不解析 PNG 条目
image.save(app_ico, format="ICO", sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)], bitmap_format="bmp")

if SITE_ROOT is not None:
    public = SITE_ROOT / "public"
    public.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(brand_png, public / "icon.png")
    image.save(public / "favicon.ico", format="ICO", sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64)], bitmap_format="bmp")
