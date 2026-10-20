"""Writes the Codex prompts for the generated art: python3 make-prompts.py <batch name> <ids...>"""
import sys, yaml

STYLE = open('prompts/style.md').read().strip()

BUILDING_RULES = """Rules for every building image (they are aligned by a script, so follow them exactly):
- Square 1024×1024 PNG with a transparent background.
- The standard isometric game camera (2:1 dimetric), the same angle, light (from the top left) and style for all.
- The building stands on its own ground plate that covers exactly its footprint: a flat diamond twice as wide as it
  is tall, made of N×N tiles (N is given per building), paved with stone slabs, concrete or packed earth that suits
  the building, with a thin darker edge.
- The ground plate is the widest part of the picture: nothing may stick out past its left and right corners, and its
  bottom corner is the lowest point of the picture. The plate is centred horizontally, with a small empty margin around
  everything. The building may rise high above the plate.
- A 1×1 building is a small, simple object; a 2×2 one a proper building; a 3×3 one a big complex.
- No cast shadows outside the plate, no people, no text, no logos, no letters or numbers on signs."""

def building_prompt(items):
    lines = []
    for b in items:
        line = f"- {b['id']}.png (footprint {b['size']}×{b['size']}): {b['look']}"
        if b.get('rotor'):
            line += (f". Draw it WITHOUT its blades (only the hub where they attach). Also make {b['id']}-rotor.png: only"
                     " the blades and hub seen straight from the front, the hub exactly in the centre of the square"
                     " image, transparent background, same style.")
        lines.append(line)
    return (f"Use your image generation tool to create game sprites. Read the style and the rules first.\n\nSTYLE\n{STYLE}\n\n"
            f"{BUILDING_RULES}\n\nThe images:\n" + "\n".join(lines) +
            "\n\nSave every PNG into the current working directory with exactly the given file name. Make each image"
            " separately. Do not write any code.")

TERRAIN_RULES = """Rules for every terrain texture:
- Square 1024×1024 PNG that fills the whole image (no transparency), seen straight from above (top-down, no
  perspective, no horizon, no objects that cast long shadows).
- It must tile seamlessly: what leaves one edge continues on the opposite edge; no vignette, no border, even lighting.
- Painted in the game's style: soft, slightly stylised, with small details (grass blades, pebbles, ripples), not a
  photo. Medium contrast so buildings stand out on it."""

def terrain_prompt(items):
    lines = [f"- {t['id']}.png: {t['look']}" for t in items]
    return (f"Use your image generation tool to create seamless terrain textures for a game. Read the style and the rules"
            f" first.\n\nSTYLE\n{STYLE}\n\n{TERRAIN_RULES}\n\nThe textures:\n" + "\n".join(lines) +
            "\n\nSave every PNG into the current working directory with exactly the given file name. Make each image"
            " separately. Do not write any code.")

PROP_RULES = """Rules for every prop image (they are aligned by a script, so follow them exactly):
- Square 1024×1024 PNG with a transparent background, the object centred with a small margin.
- The standard isometric game camera (2:1 dimetric), light from the top left, the same style for all.
- A 'prop' stands on the ground: draw only the object (with at most a tiny patch of ground at its foot), its foot at
  the bottom middle of the picture.
- A 'decal' lies flat on the ground: draw it as seen from the isometric angle, spread over a diamond twice as wide as
  it is tall, with no ground plate, no outline around it, soft edges that blend into any ground.
- No cast shadows, no people, no text."""

def prop_prompt(items):
    lines = [f"- {t['id']}.png ({t['kind']}): {t['look']}" for t in items]
    return (f"Use your image generation tool to create game sprites. Read the style and the rules first.\n\nSTYLE\n{STYLE}\n\n"
            f"{PROP_RULES}\n\nThe images:\n" + "\n".join(lines) +
            "\n\nSave every PNG into the current working directory with exactly the given file name. Make each image"
            " separately. Do not write any code.")

if __name__ == '__main__':
    name, kind, *ids = sys.argv[1:]
    source = {'buildings': 'buildings.yaml', 'terrain': 'terrain.yaml', 'props': 'props.yaml'}[kind]
    items = {t['id']: t for t in yaml.safe_load(open(source))}
    chosen = [items[i] for i in ids] if ids else list(items.values())
    prompt = {'buildings': building_prompt, 'terrain': terrain_prompt, 'props': prop_prompt}[kind](chosen)
    open(f'prompts/{name}.md', 'w').write(prompt)
    print(f'prompts/{name}.md: {len(chosen)} {kind}')
