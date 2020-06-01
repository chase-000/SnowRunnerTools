# SnowRunnerTools
SnowRunner Tools is a set of utilities to handle [SnowRunner](https://snowrunner-thegame.com/) data files.

Note that its PAK files are really ZIP files with a particular layout and can be extracted with any common tools like 7-Zip,
but require a bit of special handling to create.

## SnowPakTool
* Compress files into a single PAK file,
* Create _initial.pak\pak.load_list_ (initial assets loading order),
* Create _shared_sound.pak\sound.sound_list_,
* Packing/unpacking _initial.pak\initial.cache_block_ (various game data: UI layout, settings, translations, etc.)

As an example, here's how to increase first and third person FOV slider ranges (they're normally limited to 130 max):

```bat
set client=D:\Games\SnowRunner\en_us\preload\paks\client

7z x -oinitial_pak "%client%\initial.pak"
snowpaktool /unpackcb initial_pak\initial.cache_block initial_cache_block
patch -u -i ui_settings_controller.sso.diff initial_cache_block\[ps]\ui_settings_controller.sso
del initial_pak\initial.cache_block
snowpaktool /packcb initial_cache_block initial_pak\initial.cache_block
del "%client%\initial.pak"
snowpaktool /zippak initial_pak "%client%\initial.pak"
```
