using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SnowPakTool.Zip;

namespace SnowPakTool {

	public static class ZipPakHelper {

		public static string LoadListName { get; } = "pak.load_list";

		private static readonly byte[] __DummyBuffer = new byte[0];
		private const int ZipVersion = 0x14;


		/// <remarks>
		/// <see cref="System.IO.Compression.ZipArchive"/> does not support real store (0) method and uses deflate instead.
		/// This can be worked around by first creating an archive with just the pak.load_list, and then updating it,
		/// but the update mode runs out of memory with large archives, so it's either large archives without pak.load_list,
		/// or an ordered with one, but only of a certain size. More than that, large archives that it creates (shared_textures.pak)
		/// can't be read properly by the game, causing texture corruption inside garage.
		/// </remarks>
		public static void CreatePak ( string sourceDirectory , string pakLocation ) {
			if ( sourceDirectory is null ) throw new ArgumentNullException ( nameof ( sourceDirectory ) );
			if ( pakLocation is null ) throw new ArgumentNullException ( nameof ( pakLocation ) );

			Console.WriteLine ( "Preparing ordered zip file..." );

			sourceDirectory = IOHelpers.NormalizeDirectory ( sourceDirectory );
			if ( !Directory.Exists ( sourceDirectory ) ) throw new IOException ( $"Source directory '{sourceDirectory}' does not exist." );

			var listLocation = Path.Combine ( sourceDirectory , LoadListName );
			var loadListExists = File.Exists ( listLocation );
			if ( !loadListExists ) {
				Console.WriteLine ( $"WARNING: List file '{listLocation}' does not exist." );
			}

			using ( var zipStream = File.Open ( pakLocation , FileMode.CreateNew , FileAccess.ReadWrite , FileShare.None ) ) {
				var files = GetFiles ( sourceDirectory );
				StoreFiles ( zipStream , files );
			}
			Console.WriteLine ( "Done." );
		}


		private static List<KeyValuePair<string , string>> GetFiles ( string location ) {
			location = IOHelpers.NormalizeDirectory ( location );
			var files = Directory.EnumerateFiles ( location , "*" , SearchOption.AllDirectories );
			return files
				.Select ( a => new KeyValuePair<string , string> ( a.Substring ( location.Length ) , a ) )
				.OrderBy ( a => a.Key , PakableFileNameComparer.Instance ) //The list file must be the first file in the archive, stored without compression.
				.ToList ()
				;
		}

		private static void StoreFiles ( Stream zipStream , IReadOnlyList<KeyValuePair<string , string>> files ) {
			var count = MiscHelpers.EnsureFitsUInt16 ( files.Count );
			var localHeaders = new LocalHeader[count];
			var relativeNames = new byte[count][];
			var offsets = new long[count];

			//local entries and actual data
			Console.Write ( "Writing files." );
			for ( int i = 0; i < count; i++ ) {
				Console.Write ( $"\rWriting files: {i + 1}/{count}" );
				offsets[i] = zipStream.Position;
				var location = files[i].Value;
				var relativeName = files[i].Key;
				using ( var sourceStream = File.OpenRead ( location ) ) {
					var dateTime = File.GetLastWriteTime ( location );
					var size = MiscHelpers.EnsureFitsUInt32 ( sourceStream.Length );
					var relativeNameBytes = relativeNames[i] = MiscHelpers.Encoding.GetBytes ( relativeName );
					var header = localHeaders[i] = new LocalHeader {
						Signature = LocalHeader.DefaultSignature ,
						VersionNeeded = ZipVersion ,
						Flags = 0 ,
						Compression = 0 ,
						Time = MiscHelpers.GetDosTime ( dateTime ) ,
						Date = MiscHelpers.GetDosDate ( dateTime ) ,
						Crc32 = ComputeCrc32 ( sourceStream , size ) ,
						CompressedSize = size ,
						UncompressedSize = size ,
						NameLength = MiscHelpers.EnsureFitsUInt16 ( relativeNameBytes.Length ) ,
						ExtraLength = 0 ,
					};
					zipStream.WriteValue ( header );
					zipStream.Write ( relativeNameBytes , 0 , relativeNameBytes.Length );

					sourceStream.Position = 0;
					sourceStream.CopyTo ( zipStream );
				}
			}
			Console.WriteLine ();

			//central directory entries
			Console.Write ( "Writing central directory." );
			var directoryStart = MiscHelpers.EnsureFitsUInt32 ( zipStream.Position );
			for ( int i = 0; i < count; i++ ) {
				Console.Write ( $"\rWriting central directory: {i + 1}/{count}" );
				var header = new DirectoryHeader ( localHeaders[i] ) {
					LocalHeaderOffset = MiscHelpers.EnsureFitsUInt32 ( offsets[i] ) ,
				};
				zipStream.WriteValue ( header );
				var nameBytes = relativeNames[i];
				zipStream.Write ( nameBytes , 0 , nameBytes.Length );
			}
			Console.WriteLine ();

			//central directory end
			var directoryEnd = MiscHelpers.EnsureFitsUInt32 ( zipStream.Position );
			var eod = new EndOfDirectory {
				Signature = EndOfDirectory.DefaultSignature ,
				DiskNumber = 0 ,
				DirectoryDiskNumber = 0 ,
				DiskRecords = count ,
				TotalRecords = count ,
				DirectorySize = directoryEnd - directoryStart ,
				DirectoryOffset = directoryStart ,
				CommentLength = 0 ,
			};
			zipStream.WriteValue ( eod );
		}

		public static int ComputeCrc32 ( this Stream stream , long length ) {
			using var hasher = new Crc32Managed ();
			stream.ProcessChunked ( length , ( buffer , read ) => hasher.TransformBlock ( buffer , 0 , read , buffer , 0 ) );
			hasher.TransformFinalBlock ( __DummyBuffer , 0 , 0 );
			return ( hasher.Hash[0] << 24 ) | ( hasher.Hash[1] << 16 ) | ( hasher.Hash[2] << 8 ) | hasher.Hash[3];
		}

	}

}
