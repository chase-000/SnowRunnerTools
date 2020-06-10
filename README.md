# SnowRunnerTools
SnowRunner Tools is a set of utilities to handle [SnowRunner](https://snowrunner-thegame.com/) data files.

Note that its PAK files are really ZIP files with a particular layout and can be extracted with any common tools like 7-Zip,
but require a bit of special handling to create.

**BE SURE TO MAKE NECESSARY BACKUPS FIRST**

## SnowPakTool
* Compress files into a single PAK file,
* Create _initial.pak\pak.load_list_ (initial assets loading order),
* Create _shared_sound.pak\sound.sound_list_,
* Packing/unpacking _initial.pak\initial.cache_block_ (various game data: UI layout, settings, translations, etc.)

As an example, here's how to increase first and third person FOV slider ranges (they're normally limited to 130 max):

```bat
set client=D:\Games\SnowRunner\en_us\preload\paks\client

7z x -o"initial-pak" "%client%\initial.pak"
snowpaktool cb unpack --allow-mixing "initial-pak\initial.cache_block" "initial-pak"
del "initial-pak\initial.cache_block"
patch -u -i mods\fov_sliders_180.diff "initial-pak\[ps]\ui_settings_controller.sso"
del "%client%\initial.pak"
snowpaktool pak pack --mixed-cache-block "initial-pak" "%client%\initial.pak"
```

## SnowTruckConfig
* Add top-center crane sockets to lift vehicles more easily
* Change customization cameras FOV
* Rename game objects to include detailed info in their names

This example adds a new crane socket to Hummer H2:

```bat
set client=D:\Games\SnowRunner\en_us\preload\paks\client

7z x -o"initial-pak" "%client%\initial.pak"
snowpaktool cb unpack --allow-mixing "initial-pak\initial.cache_block" "initial-pak"
del "initial-pak\initial.cache_block"
snowtruckconfig truck CraneSocket add top-central "initial-pak\[media]\classes\trucks\hummer_h2.xml"
del "%client%\initial.pak"
snowpaktool pak pack --mixed-cache-block "initial-pak" "%client%\initial.pak"
```
