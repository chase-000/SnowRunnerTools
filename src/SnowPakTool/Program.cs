using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Reflection;
using SnowPakTool.Zip;

namespace SnowPakTool {

	public static class Program {

		public static int Main ( string[] args ) {

			var root = new RootCommand ( typeof ( Program ).Assembly.GetCustomAttribute<AssemblyTitleAttribute> ().Title );
			root.AddLicenseOption ();


			var cmdPak = new Command ( "pak" , "Process PAK files" );
			root.Add ( cmdPak );

			var cmdPakList = new Command ( "list" , "List contents of a PAK file" ) { IsHidden = true };
			cmdPakList.AddArgument ( new Argument<FileInfo> ( "source" , "Path to the PAK file" ).ExistingOnly () );
			cmdPakList.AddOption ( new Option<LocalHeaderField[]> ( "--local-header" ) { IsHidden = true } ); //uglifies help output
			cmdPakList.AddOption ( new Option ( "--sort" ) );
			cmdPakList.Handler = CommandHandler.Create<FileInfo , LocalHeaderField[] , bool> ( DoPakList );
			cmdPak.Add ( cmdPakList );

			var cmdPakPack = new Command ( "pack" , "Pack contents of a directory into a single PAK file" );
			cmdPakPack.AddArgument ( new Argument<DirectoryInfo> ( "source" , "Path to the directory containing files" ).ExistingOnly () );
			cmdPakPack.AddArgument ( new Argument<FileInfo> ( "target" , "Path to a PAK file that will be created" ).NonExistingOnly () );
			cmdPakPack.AddOption ( new Option ( "--mixed-cache-block" , "Indicates the source directory contains mixed contents of initial.pak and initial.pak\\initial.cache_block" ) );
			cmdPakPack.Handler = CommandHandler.Create<DirectoryInfo , FileInfo , bool> ( DoPakPack );
			cmdPak.Add ( cmdPakPack );


			var cmdCacheBlock = new Command ( "cache_block" , "Process cache_block files" );
			cmdCacheBlock.AddAlias ( "cb" );
			root.Add ( cmdCacheBlock );

			var cmdCacheBlockList = new Command ( "list" , "List contents of a cache_block file" );
			cmdCacheBlockList.AddArgument ( new Argument<FileInfo> ( "source" , "Path to the cache_block file" ).ExistingOnly () );
			cmdCacheBlockList.Handler = CommandHandler.Create<FileInfo> ( DoCacheBlockList );
			cmdCacheBlock.Add ( cmdCacheBlockList );

			var cmdCacheBlockUnpack = new Command ( "unpack" , "Unpack contents of a cache_block file into a directory" );
			var cmdCacheBlockUnpack_AllowMixing = new Option ( "--allow-mixing" , "Allow mixing of contents for initial.pak and initial.pak\\initial.cache_block" );
			cmdCacheBlockUnpack.AddArgument ( new Argument<FileInfo> ( "source" , "Path to the cache_block file" ).ExistingOnly () );
			cmdCacheBlockUnpack.AddArgument ( new Argument<DirectoryInfo> ( "target" , "Path to a directory that will be created to unpack into" ).NonExistingOnly ( unless: cmdCacheBlockUnpack_AllowMixing ) );
			cmdCacheBlockUnpack.AddOption ( cmdCacheBlockUnpack_AllowMixing );
			cmdCacheBlockUnpack.Handler = CommandHandler.Create<FileInfo , DirectoryInfo , bool> ( DoCacheBlockUnpack );
			cmdCacheBlock.Add ( cmdCacheBlockUnpack );

			var cmdCacheBlockPack = new Command ( "pack" , "Pack contents of a directory into a single cache_block file" );
			cmdCacheBlockPack.AddArgument ( new Argument<DirectoryInfo> ( "source" , "Path to the directory containing files" ).ExistingOnly () );
			cmdCacheBlockPack.AddArgument ( new Argument<FileInfo> ( "target" , "Path to a cache_block file that will be created" ).NonExistingOnly () );
			cmdCacheBlockPack.AddOption ( new Option ( "--mixed-cache-block" , "Indicates the source directory contains mixed contents of initial.pak and initial.pak\\initial.cache_block" ) );
			cmdCacheBlockPack.Handler = CommandHandler.Create<DirectoryInfo , FileInfo , bool> ( DoCacheBlockPack );
			cmdCacheBlock.Add ( cmdCacheBlockPack );


			var cmdLoadList = new Command ( "load_list" , "Process load_list files" );
			cmdLoadList.AddAlias ( "ll" );
			root.Add ( cmdLoadList );

			var cmdLoadListList = new Command ( "list" , "List contents of a load_list file" );
			cmdLoadListList.AddOption ( new Option ( "--compact" , "Use compact format (internal names only)" ) );
			cmdLoadListList.AddArgument ( new Argument<FileInfo> ( "source" , "Path to the load_list file" ).ExistingOnly () );
			cmdLoadListList.Handler = CommandHandler.Create<FileInfo , bool> ( DoLoadListList );
			cmdLoadList.Add ( cmdLoadListList );

			var cmdLoadListCreate = new Command ( "create-initial" , "Create load_list file using conventions for initial.pak\\pak.load_list" );
			cmdLoadListCreate.AddAlias ( "ci" );
			cmdLoadListCreate.AddArgument ( new Argument<FileInfo> ( "target" , "Path to a load_list file that will be created" ).NonExistingOnly () );
			cmdLoadListCreate.AddArgument ( new Argument<FileSystemInfo> ( "initial" , "Path to the initial.pak file or a directory with its contents" ).ExistingOnly () );
			cmdLoadListCreate.AddArgument ( new Argument<FileSystemInfo> ( "shared" , "Path to the shared.pak file or a directory with its contents" ).ExistingOnly () );
			cmdLoadListCreate.AddArgument ( new Argument<FileSystemInfo> ( "shared_sound" , "Path to the shared_sound.pak file or a directory with its contents" ).ExistingOnly () );
			cmdLoadListCreate.Handler = CommandHandler.Create<FileInfo , FileSystemInfo , FileSystemInfo , FileSystemInfo> ( DoLoadListCreate );
			cmdLoadList.Add ( cmdLoadListCreate );


			var cmdSoundList = new Command ( "sound_list" , "Process sound_list files" );
			cmdSoundList.AddAlias ( "sl" );
			root.Add ( cmdSoundList );

			var cmdSoundListList = new Command ( "list" , "List contents of a sound_list file" );
			cmdSoundListList.AddArgument ( new Argument<FileInfo> ( "source" , "Path to the sound_list file" ).ExistingOnly () );
			cmdSoundListList.Handler = CommandHandler.Create<FileInfo> ( DoSoundListList );
			cmdSoundList.Add ( cmdSoundListList );

			var cmdSoundListCreate = new Command ( "create" , "Create sound_list file" );
			cmdSoundListCreate.AddArgument ( new Argument<FileSystemInfo> ( "source" , "Path to the PAK file or a directory containing PCM files" ).ExistingOnly () );
			cmdSoundListCreate.AddArgument ( new Argument<FileInfo> ( "target" , "Path to the sound_list file" ).NonExistingOnly () );
			cmdSoundListCreate.Handler = CommandHandler.Create<FileSystemInfo , FileInfo> ( DoSoundListCreate );
			cmdSoundList.Add ( cmdSoundListCreate );

			return root.InvokeWithMiddleware ( args , CommandLineExtensions.MakePrintLicenseResourceMiddleware ( typeof ( Program ) ) );
		}

		private static void DoCacheBlockList ( FileInfo source ) {
			using ( var stream = File.OpenRead ( source.FullName ) ) {
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

		private static void DoCacheBlockUnpack ( FileInfo source , DirectoryInfo target , bool allowMixing ) {
			using ( var stream = File.OpenRead ( source.FullName ) ) {
				var reader = new CacheBlockReader ( stream );
				reader.UnpackAll ( target.FullName );
			}
		}

		private static void DoCacheBlockPack ( DirectoryInfo source , FileInfo target , bool mixedCacheBlock ) {
			var entries = CacheBlockWriter.GetFileEntries ( source.FullName , mixedCacheBlock ).OrderBy ( a => a.InternalName ).ToList ();
			using ( var stream = File.Open ( target.FullName , FileMode.CreateNew , FileAccess.Write , FileShare.Read ) ) {
				var writer = new CacheBlockWriter ( stream );
				writer.Pack ( source.FullName , entries );
			}
		}

		private static void DoPakList ( FileInfo source , LocalHeaderField[] localHeader , bool sort ) {
			ZipPakHelper.ListPak ( source.FullName , localHeader , sort );
		}

		private static void DoPakPack ( DirectoryInfo source , FileInfo target , bool mixedCacheBlock ) {
			string tmp = null;
			try {
				IEnumerable<KeyValuePair<string , string>> additionalFiles = null;
				if ( mixedCacheBlock ) {
					tmp = Path.Combine ( Path.GetTempPath () , Path.GetRandomFileName () );
					DoCacheBlockPack ( source , new FileInfo ( tmp ) , true );
					additionalFiles = new[] { new KeyValuePair<string , string> ( CacheBlockFile.InitialCacheBlockName , tmp ) };
				}
				ZipPakHelper.CreatePak ( source.FullName , target.FullName , CacheBlockFile.InitialCacheBlockDirectories , additionalFiles );
			}
			finally {
				if ( tmp != null ) {
					try {
						File.Delete ( tmp );
					}
					catch {
						//NOP
					}
				}
			}
		}

		private static void DoLoadListList ( FileInfo source , bool compact ) {
			if ( compact ) {
				ListLoadListCompact ( source.FullName );
			}
			else {
				ListLoadList ( source.FullName );
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

		private static void DoLoadListCreate ( FileInfo target , FileSystemInfo initial , FileSystemInfo shared , FileSystemInfo shared_sound ) {
			var initialContainer = FilesContainer.From ( initial.FullName );
			var sharedContainer = FilesContainer.From ( shared.FullName );
			var sharedSoundContainer = FilesContainer.From ( shared_sound.FullName );
			var initialContainerFiles = initialContainer.GetFiles ();
			var sharedContainerFiles = sharedContainer.GetFiles ();
			var sharedSoundContainerFiles = sharedSoundContainer.GetFiles ();
			LoadListFile.WriteFileNames ( target.FullName , initialContainerFiles , sharedContainerFiles , sharedSoundContainerFiles );
		}

		private static void DoSoundListList ( FileInfo source ) {
			var names = SoundListFile.ReadEntries ( source.FullName ).ToList ();
			foreach ( var name in names ) {
				Console.WriteLine ( name );
			}

			Console.WriteLine ( $"---\n{names.Count} name(s)" );

			var extensions = names.GroupBy ( a => Path.GetExtension ( a ) );
			foreach ( var item in extensions ) {
				Console.WriteLine ( $"{item.Key}: {item.Count ()}" );
			}
		}

		private static void DoSoundListCreate ( FileSystemInfo source , FileInfo target ) {
			Console.WriteLine ( $"Scanning '{source.FullName}' for .pcm files." );
			var sourceContainer = FilesContainer.From ( source.FullName );
			var names = sourceContainer.GetFiles ().Where ( a => a.EndsWith ( ".pcm" , StringComparison.OrdinalIgnoreCase ) ).ToList ();
			Console.WriteLine ( $"{names.Count} .pcm files found." );
			Console.WriteLine ( "Writing." );
			SoundListFile.WriteEntries ( target.FullName , names );
			Console.WriteLine ( "Done." );
		}

	}

}
