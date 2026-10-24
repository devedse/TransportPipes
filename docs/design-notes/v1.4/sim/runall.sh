#!/bin/bash
# the final runs, all at once
cd "$(dirname "$0")"
base=$(cat final_rules.txt)
python3 showcase.py "{\"rules\":{$base},\"days\":35,\"apm\":6,\"era2\":12,\"schedule\":\"regular\"}" out/show_regular.json &
python3 showcase.py "{\"rules\":{$base},\"days\":35,\"apm\":2,\"era2\":12,\"schedule\":\"regular\"}" out/show_regular_apm2.json &
python3 showcase.py "{\"rules\":{$base},\"days\":35,\"apm\":6,\"era2\":12,\"schedule\":\"keen\"}" out/show_keen.json &
python3 showcase.py "{\"rules\":{$base},\"days\":35,\"apm\":6,\"era2\":12,\"schedule\":\"casual\"}" out/show_casual.json &
python3 showcase.py "{\"rules\":{$base},\"days\":45,\"apm\":6,\"schedule\":\"regular\"}" out/show_era1only.json &
python3 crew.py "{\"rules\":{$base},\"days\":35,\"apm\":6,\"pool\":false,\"era2\":12}" out/crew9.json &
python3 solo_spec.py "{\"rules\":{$base},\"days\":35,\"apm\":6,\"era2\":12}" out/solo9.json &
python3 solo_same.py "{\"rules\":{$base},\"days\":35,\"apm\":6,\"era2\":12}" out/solo_wide.json &
python3 first.py > out/first.log &
wait
echo all done
