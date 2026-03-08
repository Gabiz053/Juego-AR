from PIL import Image

img = Image.open(r'C:\Unity\Juego-AR\Assets\_Project\Textures\UI\minecraft_sand.png')
px = img.load()
w, h = img.size
print(f"Size: {w}x{h}")

# Collect all unique colours and their frequency
from collections import Counter
colours = Counter()
for y in range(h):
    for x in range(w):
        p = px[x, y]
        if p[3] > 200:  # skip transparent
            colours[(p[0], p[1], p[2])] += 1

print("\nAll colours sorted by frequency:")
for col, count in colours.most_common():
    r, g, b = col
    pct = count / (w*h) * 100
    print(f"  rgb({r:3d},{g:3d},{b:3d})  #{r:02x}{g:02x}{b:02x}  {pct:.1f}%")
