#!/usr/bin/env bash
# Generates images with Codex image generation: codex-batch.sh <prompt.md> <output dir> [reference images...]
# Uses gpt-6.1-sol at reasoning effort high. The login is a temporary copy of a DeveAgentManager account (allowed by
# the owner for the game art: their personal account, then Ketty's, never the business one); it is deleted
# afterwards and never printed. On a usage limit the next account is tried.
set -u
prompt="$1"
out="$2"
shift 2
refs=()
for image in "$@"; do refs+=(-i "$(realpath "$image")"); done
mkdir -p "$out"
home="$(mktemp -d /tmp/tp-codex-XXXXXX)"
trap 'rm -rf "$home"' EXIT
printf 'check_for_update_on_startup = false\n' > "$home/config.toml"
for account in 258f6ca989ba40beade2ec5f9bc9062e 2bdf85cd0fa04e68baec04cad10ea739; do
  cp "$HOME/.deveagentmanager/accounts/$account/codex/auth.json" "$home/auth.json" && chmod 600 "$home/auth.json"
  log="$out/codex-$(date +%H%M%S)-${account:0:4}.log"
  CODEX_HOME="$home" timeout 2400 codex exec -m gpt-6.1-sol -c model_reasoning_effort="high" --skip-git-repo-check \
    --sandbox workspace-write -C "$out" "${refs[@]}" -- "$(cat "$prompt")" < /dev/null > "$log" 2>&1
  if grep -q "usage limit" "$log"; then
    echo "account ${account:0:4}: usage limit, trying the next one"
    continue
  fi
  echo "account ${account:0:4}: done ($(tail -c 300 "$log" | tr '\n' ' '))"
  exit 0
done
echo "all accounts hit their usage limit"
exit 1
