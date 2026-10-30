# The 1.4 design: sources

`docs/v1.4-design.html` is built from these files.

- `part1.html` … `part4.html`: the text, with placeholders (`{{FIG:…}}`, `{{TABLE:…}}`, `{{EC:…}}`, `{{PACE:…}}`) that
  `build.py` fills in.
- `figs.py`: the drawings to scale (town, group, Yard, world, charts of the Eras and tracks).
- `figs2.py`, `econ_section.py`: the charts, base maps and tables of the worked-out economy (§17), drawn from the model's
  runs.
- `model.py`: the goods, recipes and building costs of all five Eras (the resources and buildings tables).
- `img/`: the pipe-lane mock-ups (Codex, prompt in `art/prompts/20-pipe-lanes.md`).
- `sim/`: the economy model. It plays plots minute by minute with the rules of the document: land and deposits, Hub
  Orders, copies and levels with their goods, what pipes carry, research on both lines (tracks and the Era tree),
  Company Level, parcels, the outskirts, the Monument and the Wonder, a crew trading in its Yard (`crew.py`, also
  without trade), every Era-1 track mastered against the same plot played wide (`tracks.py`). `final_rules.txt` holds
  the numbers the document settles on.
- `sim/eras.py` (phase C): one regular player through all five Eras, the town's Eras on the days the design aims at
  (12, 33, 61, 89); later Eras' buildings take the land of older ones, the Deep Level and three outposts add room. Eight
  times what it gives in an Era sized the Wonders of Eras 3 to 5, the first mission, the later Era tree and the outposts:

  ```sh
  base=$(cat sim/final_rules.txt); for l2 in bauxite oil rareEarth titaniumOre; do
    python3 sim/eras.py "{\"rules\":{$base},\"days\":125,\"lacks2\":\"$l2\"}" sim/out/eras/$l2.json & done; wait
  ```

To rebuild:

```sh
docs/design-notes/v1.4/sim/runall.sh     # about eight minutes; writes sim/out/*.json
python3 docs/design-notes/v1.4/build.py  # writes docs/v1.4-design.html
```

Phase B's bots on the real server must land on the model's numbers (§22 and §23 of the document).
