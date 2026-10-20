#!/usr/bin/env bash
# Runs prompt batches one after the other: queue.sh <prompt names...> (output in ../../transportpipes-art/<name>)
cd "$(dirname "$0")"
for name in "$@"; do
  if ls "../../transportpipes-art/$name"/*.png > /dev/null 2>&1 && [ -f "../../transportpipes-art/$name/.done" ]; then
    echo "$name: done before"
    continue
  fi
  echo "$name: $(date +%H:%M) start"
  if ./codex-batch.sh "prompts/$name.md" "../../transportpipes-art/$name"; then
    touch "../../transportpipes-art/$name/.done"
    echo "$name: $(date +%H:%M) $(ls ../../transportpipes-art/$name/*.png | wc -l) images"
  else
    echo "$name: failed"
    exit 1
  fi
done
