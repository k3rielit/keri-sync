## SYNC
A basic file 'synchronizer', or rather a savegame backupper. Uses a config file to check game and backup directories for newer or missing files, and then copies them over. If no config file is present, it generates a default one. To keep the files organized, the backups are always in ```.\saves\<name>```, which is fine for savegames, but not for anything else...

## Config
* Format: ```<name> = <path>```
* Ignores lines starting with ```//```

## To-do
* Optional relative config paths
* Switching to ```<path> = <path>``` in the config
* Making the timer optional and user adjustable, or just removing it...
* Setting console title based on system language.
* Menu system
* Built-in basic editors and tools
* Reveal in File Explorer
* Utilizing command line arguments
* ...
