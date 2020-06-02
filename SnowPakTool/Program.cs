using System;
using System.IO;
using System.Linq;

namespace SnowPakTool {

	public static class Program {

		/*
			/listcb "D:\Games\SnowRunner_backs\mods\tmp\initial.cache_block"
			/packcb "D:\Games\SnowRunner_backs\settings\keys\tmp" "D:\Games\SnowRunner_backs\settings\keys\initial.1.cache_block"
			/unpackcb "D:\Games\SnowRunner_backs\settings\keys\initial.cache_block"
			/zippak "D:\Games\SnowRunner_backs\mods\.staging\initial-pak" "D:\Games\SnowRunner_backs\mods\.staging\initial.pak"
			/zippak "D:\Games\SnowRunner_backs\mods\.staging\shared_textures-pak" "D:\Games\SnowRunner_backs\mods\.staging\shared_textures.pak"
			/listll "D:\Games\SnowRunner_backs\mods\.staging\initial-pak\pak.load_list"
			/createll "D:\Games\SnowRunner_backs\mods\tmp\pak.load_list" "D:\Games\SnowRunner\en_us\preload\paks\client\initial.pak" "D:\Games\SnowRunner\en_us\preload\paks\client\shared.pak" "D:\Games\SnowRunner\en_us\preload\paks\client\shared_sound.pak"
		*/

		public static int Main ( string[] args ) {
			switch ( args.Length > 0 ? args[0] : null ) {

				case "/license":
					PrintLicense ();
					return 0;

				case "/listcb":
					ListCacheBlock ( args );
					return 0;

				case "/unpackcb":
					UnpackCacheBlock ( args );
					return 0;

				case "/packcb":
					PackCacheBlock ( args );
					return 0;

				case "/zippak":
					ZipPakHelper.CreatePak ( args[1] , args[2] );
					return 0;

				case "/listll":
					ListLoadList ( args[1] );
					return 0;

				case "/listllc":
					ListLoadListCompact ( args[1] );
					return 0;

				case "/createll":
					CreateLoadList ( args[1] , args[2] , args[3] , args[4] );
					return 0;

				case "/listsl":
					ListSoundList ( args[1] );
					return 0;

				case "/createsl":
					CreateSoundList ( args[1] , args[2] );
					return 0;

				default:
					PrintHelp ();
					return 1;
			}
		}

		private static void PrintLicense () {
			using var stream = typeof ( Program ).Assembly.GetManifestResourceStream ( $"{nameof ( SnowPakTool )}.LICENSE" )
						?? throw new InvalidOperationException ( "Can't find the license resource." );
			using var reader = new StreamReader ( stream );
			Console.WriteLine ( reader.ReadToEnd () );
		}

		private static void ListCacheBlock ( string[] args ) {
			using ( var stream = File.OpenRead ( args[1] ) ) {
				var reader = new CacheBlockReader ( stream );
				Console.WriteLine ( $"File entries: {reader.FileEntries.Length}" );
				Console.WriteLine ( $"Base offset: {reader.BaseOffset}" );
				var i = 0;
				foreach ( var item in reader.FileEntries ) {
					Console.WriteLine ( $"[{i}] {item.InternalName}: {item.Size} byte(s) at {item.RelativeOffset + reader.BaseOffset}" );
					i++;
				}
			}
		}

		private static void UnpackCacheBlock ( string[] args ) {
			var sourceLocation = Path.GetFullPath ( args[1] );
			if ( !File.Exists ( sourceLocation ) ) throw new IOException ( $"Can't find cache block file '{sourceLocation}'." );
			var sourceDirectory = Path.GetDirectoryName ( sourceLocation ); //file exists, so this is neither a null nor root
			var targetDirectory = args.Length >= 3
						? Path.GetFullPath ( args[2] )
						: Path.Combine ( sourceDirectory , Path.GetFileNameWithoutExtension ( sourceLocation ) );
			if ( Directory.Exists ( targetDirectory ) ) throw new IOException ( $"Target directory '{targetDirectory}' already exists." );
			using ( var stream = File.OpenRead ( sourceLocation ) ) {
				var reader = new CacheBlockReader ( stream );
				reader.UnpackAll ( targetDirectory );
			}
		}

		private static void PackCacheBlock ( string[] args ) {
			var sourceDirectory = Path.GetFullPath ( args[1] );
			var targetLocation = args[2];
			var entries = CacheBlockWriter.GetFileEntries ( sourceDirectory ).OrderBy ( a => a.InternalName ).ToList ();
			using ( var stream = File.Open ( targetLocation , FileMode.CreateNew , FileAccess.Write , FileShare.Read ) ) {
				var writer = new CacheBlockWriter ( stream );
				writer.Pack ( sourceDirectory , entries );
			}
		}

		private static void ListLoadList ( string loadListLocation ) {
			var entries = LoadListFile.ReadEntries ( loadListLocation );
			foreach ( var item in entries ) {
				Console.WriteLine ( item );
				if ( item is LoadListAssetEntry asset && asset.Json != null ) {
					Console.Write ( "    " );
					Console.WriteLine ( asset.Json );
				}
			}

			LoadListFile.ValidateLoadListOrdering ( entries );

			Console.WriteLine ( "\nStages:" );
			var stages = entries.OfType<LoadListStageEntry> ();
			foreach ( var item in stages ) {
				Console.WriteLine ( $"[{item.Index}] {item.Text}" );
			}

			Console.WriteLine ( "\nLoaders:" );
			var loaders = entries.OfType<LoadListAssetEntry> ().GroupBy ( a => a.Loader );
			foreach ( var item in loaders ) {
				Console.WriteLine ( $"{item.Key}: {item.Count ()} asset(s)" );
			}

			Console.WriteLine ( "\nPAKs:" );
			var paks = entries.OfType<LoadListAssetEntry> ().GroupBy ( a => a.PakName );
			foreach ( var pak in paks ) {
				Console.WriteLine ( "|" );
				Console.WriteLine ( $"+-- {pak.Key}: {pak.Count ()} asset(s)" );
				var pss = pak.GroupBy ( a => a.InternalNamePs );
				foreach ( var ps in pss ) {
					Console.WriteLine ( $"|   +-- <{ps.Key}>: {ps.Count ()}" );
					var psLoaders = ps.GroupBy ( a => a.Loader );
					foreach ( var loader in psLoaders ) {
						Console.WriteLine ( $"|   |   +-- {loader.Key}: {loader.Count ()}" );
					}
					Console.WriteLine ( "|   |" );
				}
				Console.WriteLine ( "|   x" );
			}
			Console.WriteLine ( "x" );
		}

		private static void ListLoadListCompact ( string loadListLocation ) {
			var entries = LoadListFile.ReadEntries ( loadListLocation );
			foreach ( var item in entries ) {
				switch ( item ) {
					case LoadListStartEntry _:
					case LoadListEndEntry _:
						Console.WriteLine ( $"--{item.Type}--" );
						break;
					case LoadListStageEntry stage:
						Console.WriteLine ( stage.Text );
						break;
					case LoadListAssetEntry asset:
						Console.WriteLine ( asset.InternalName );
						break;
				}
			}
		}

		private static void CreateLoadList ( string loadListLocation , string initialLocation , string sharedLocation , string sharedSoundLocation ) {
			var initialContainer = FilesContainer.From ( initialLocation );
			var sharedContainer = FilesContainer.From ( sharedLocation );
			var sharedSoundContainer = FilesContainer.From ( sharedSoundLocation );
			var initialContainerFiles = initialContainer.GetFiles ();
			var sharedContainerFiles = sharedContainer.GetFiles ();
			var sharedSoundContainerFiles = sharedSoundContainer.GetFiles ();
			LoadListFile.WriteFileNames ( loadListLocation , initialContainerFiles , sharedContainerFiles , sharedSoundContainerFiles );
		}

		private static void ListSoundList ( string soundListLocation ) {
			var names = SoundListFile.ReadEntries ( soundListLocation ).ToList ();
			foreach ( var name in names ) {
				Console.WriteLine ( name );
			}

			Console.WriteLine ( $"---\n{names.Count} name(s)" );

			var extensions = names.GroupBy ( a => Path.GetExtension ( a ) );
			foreach ( var item in extensions ) {
				Console.WriteLine ( $"{item.Key}: {item.Count ()}" );
			}
		}

		private static void CreateSoundList ( string sourceLocation , string soundListLocation ) {
			Console.WriteLine ( $"Scanning '{sourceLocation}' for .pcm files." );
			var sourceContainer = FilesContainer.From ( sourceLocation );
			var names = sourceContainer.GetFiles ().Where ( a => a.EndsWith ( ".pcm" , StringComparison.OrdinalIgnoreCase ) ).ToList ();
			Console.WriteLine ( $"{names.Count} .pcm files found." );
			Console.WriteLine ( "Writing." );
			SoundListFile.WriteEntries ( soundListLocation , names );
			Console.WriteLine ( "Done." );
		}



		private static void PrintHelp () {
			Console.WriteLine ( "Usage:" );
			Console.WriteLine ( $"  {nameof ( SnowPakTool )} /license" );
			Console.WriteLine ( $"  {nameof ( SnowPakTool )} /listcb file.cache_block" );
			Console.WriteLine ( $"  {nameof ( SnowPakTool )} /unpackcb file.cache_block [directory]" );
			Console.WriteLine ( $"  {nameof ( SnowPakTool )} /packcb directory file.cache_block" );
			Console.WriteLine ( $"  {nameof ( SnowPakTool )} /zippak directory file.pak" );
			Console.WriteLine ( $"  {nameof ( SnowPakTool )} /listll pak.load_list" );
			Console.WriteLine ( $"  {nameof ( SnowPakTool )} /listllc pak.load_list" );
			Console.WriteLine ( $"  {nameof ( SnowPakTool )} /createll pak.load_list initial_pak_or_directory shared_pak_or_directory shared_sound_pak_or_directory" );
			Console.WriteLine ( $"  {nameof ( SnowPakTool )} /listsl sound.sound_list" );
			Console.WriteLine ( $"  {nameof ( SnowPakTool )} /createsl pak_or_directory sound.sound_list" );
		}

	}

}
