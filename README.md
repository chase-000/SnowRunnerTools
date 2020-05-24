# SnowRunnerTools
SnowRunner Tools is a set of utilities to handle [SnowRunner](https://snowrunner-thegame.com/) data files.

Note that its PAK files are really ZIP files with a particular layout and can be extracted with any common tools like 7-Zip,
but require a bit of special handling to create.

## SnowPakTool
Used to compress files into a single PAK file and for packing/unpacking _initial.pak\initial.cache_block_ file that
contains various game data, including UI layout, settings and translations.

As an example, here's how to increase first and second person FOV slider ranges (they're normally limited to 130 max):

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
