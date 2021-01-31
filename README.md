# Cs_minis

This repo includes mini projects written in C # 

## Battery Watcher
If the battery percentage reach the setting threshold, the program send a notification. If the battery percentage fall under the setting, the program will hibernate the machine.
  - Upper treshold: 80%
  - Lower trehshold: 40%
  - Hibernate treshold: 10%
These settings can change in "settings.json"

## Sleeper
If the battery percentage fall under 15% the machine will be shutting down. 
Options: 
  - Timed shutting down,
  - Hibernate instead of shutting down
