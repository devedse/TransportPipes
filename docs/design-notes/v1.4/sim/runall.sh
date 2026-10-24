#!/bin/bash
# the final runs, all at once
cd "$(dirname "$0")"
base=$(cat final_rules.txt)
python3 showcase.py "{\"rules\":{$base},\"days\":35,\"apm\":6,\"era2\":12,\"schedule\":\"regular\"}" out/show_regular.json &
python3 showcase.py "{\"rules\":{$base},\"days\":35,\"apm\":2,\"era2\":12,\"schedule\":\"regular\"}" out/show_regular_apm2.json &
python3 showcase.py "{\"rules\":{$base},\"days\":35,\"apm\":6,\"era2\":12,\"schedule\":\"keen\"}" out/show_keen.json &
python3 showcase.py "{\"rules\":{$base},\"days\":35,\"apm\":6,\"era2\":12,\"schedule\":\"casual\"}" out/show_casual.json &
python3 showcase.py "{\"rules\":{$base},\"days\":35,\"apm\":2,\"era2\":12,\"schedule\":\"casual\"}" out/show_casual_apm2.json &
python3 showcase.py "{\"rules\":{$base},\"days\":45,\"apm\":6,\"schedule\":\"regular\"}" out/show_era1only.json &
python3 crew.py "{\"rules\":{$base},\"days\":35,\"apm\":6,\"pool\":false,\"era2\":12}" out/crew9.json &
python3 crew.py "{\"rules\":{$base},\"days\":35,\"apm\":6,\"pool\":false,\"era2\":12,\"trade\":false}" out/crew_notrade.json &
python3 solo_spec.py "{\"rules\":{$base},\"days\":35,\"apm\":6,\"era2\":12}" out/solo9.json &
python3 first.py > out/first.log &
wait
# every Era-1 track mastered against the same plot played wide (§12), four at a time
i=0
for t in power iron copper stone sand wood; do
  for m in deep wide; do
    python3 tracks.py "{\"rules\":{$base},\"days\":35,\"apm\":6,\"era2\":12,\"track\":\"$t\",\"mode\":\"$m\"}" out/tracks_${t}_${m}.json &
    i=$((i+1))
    if [ $((i % 4)) -eq 0 ]; then wait; fi
  done
done
wait
echo all done
