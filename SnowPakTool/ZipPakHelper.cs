using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

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
			var localEntries = new LocalFileHeader[count];
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
					var size = MiscHelpers.EnsureFitsUInt32 ( sourceStream.Length );
					var relativeNameBytes = relativeNames[i] = MiscHelpers.Encoding.GetBytes ( relativeName );
					var lfh = localEntries[i] = new LocalFileHeader {
						Signature = LocalFileHeader.DefaultSignature ,
						VersionNeeded = ZipVersion ,
						Flags = 0 ,
						Compression = 0 ,
						Time = 0 ,
						Date = 0 ,
						Crc32 = ComputeCrc32 ( sourceStream , size ) ,
						CompressedSize = size ,
						UncompressedSize = size ,
						NameLength = MiscHelpers.EnsureFitsUInt16 ( relativeNameBytes.Length ) ,
						ExtraLength = 0 ,
					};
					zipStream.WriteValue ( lfh );
					zipStream.Write ( relativeNameBytes , 0 , relativeNameBytes.Length );

					sourceStream.Position = 0;
					sourceStream.CopyTo ( zipStream );
				}
			}
			Console.WriteLine ();

			//central directory entries
			Console.Write ( "Writing central directory." );
			var centralDirectoryStart = MiscHelpers.EnsureFitsUInt32 ( zipStream.Position );
			for ( int i = 0; i < count; i++ ) {
				Console.Write ( $"\rWriting central directory: {i + 1}/{count}" );
				var lfh = localEntries[i];
				var cdfh = new CentralDirectoryFileHeader ( lfh ) {
					LocalOffset = MiscHelpers.EnsureFitsUInt32 ( offsets[i] ) ,
				};
				zipStream.WriteValue ( cdfh );
				var nameBytes = relativeNames[i];
				zipStream.Write ( nameBytes , 0 , nameBytes.Length );
			}
			Console.WriteLine ();

			//central directory end
			var centralDirectoryEnd = MiscHelpers.EnsureFitsUInt32 ( zipStream.Position );
			var eocd = new EndOfCentralDirectory {
				Signature = EndOfCentralDirectory.DefaultSignature ,
				DiskNumber = 0 ,
				CentralDirectoryDiskNumber = 0 ,
				DiskRecords = count ,
				TotalRecords = count ,
				CentralDirectorySize = centralDirectoryEnd - centralDirectoryStart ,
				CentralDirectoryOffset = centralDirectoryStart ,
				CommentLength = 0 ,
			};
			zipStream.WriteValue ( eocd );
		}

		public static int ComputeCrc32 ( this Stream stream , long length ) {
			using var hasher = new Crc32Managed ();
			stream.ProcessChunked ( length , ( buffer , read ) => hasher.TransformBlock ( buffer , 0 , read , buffer , 0 ) );
			hasher.TransformFinalBlock ( __DummyBuffer , 0 , 0 );
			return ( hasher.Hash[0] << 24 ) | ( hasher.Hash[1] << 16 ) | ( hasher.Hash[2] << 8 ) | hasher.Hash[3];
		}



		[StructLayout ( LayoutKind.Sequential , Pack = 1 )]
		public struct LocalFileHeader {

			public const int DefaultSignature = 0x04034B50;

			// +00 4
			public int Signature;
			// +04 2
			public ushort VersionNeeded;
			// +06 2
			public ushort Flags;
			// +08 2
			public ushort Compression;
			// +0A 2
			public ushort Time;
			// +0C 2
			public ushort Date;
			// +0E 4
			public int Crc32;
			// +12 4
			public uint CompressedSize;
			// +16 4
			public uint UncompressedSize;
			// +1A 2
			public ushort NameLength;
			// +1C 2
			public ushort ExtraLength;
			// +1E

		}


		[StructLayout ( LayoutKind.Sequential , Pack = 1 )]
		public struct CentralDirectoryFileHeader {

			public const int DefaultSignature = 0x02014B50;

			// +00 4
			public int Signature;
			// +04 2
			public ushort VersionMadeBy;
			// +06 2
			public ushort VersionNeeded;
			// +08 2
			public ushort Flags;
			// +0A 2
			public ushort Compression;
			// +0C 2
			public ushort Time;
			// +0E 2
			public ushort Date;
			// +10 4
			public int Crc32;
			// +14 4
			public uint CompressedSize;
			// +18 4
			public uint UncompressedSize;
			// +1C 2
			public ushort NameLength;
			// +1E 2
			public ushort ExtraLength;
			// +20 2
			public ushort CommentLength;
			// +22 2
			public ushort DiskNumber;
			// +24 2
			public ushort InternalAttributes;
			// +26 4
			public uint ExternalAttributes;
			// +2A 4
			public uint LocalOffset;
			// +2E

			public CentralDirectoryFileHeader ( LocalFileHeader header ) {
				Signature = DefaultSignature;
				VersionMadeBy = header.VersionNeeded;
				VersionNeeded = header.VersionNeeded;
				Flags = header.Flags;
				Compression = header.Compression;
				Time = header.Time;
				Date = header.Date;
				Crc32 = header.Crc32;
				CompressedSize = header.CompressedSize;
				UncompressedSize = header.UncompressedSize;
				NameLength = header.NameLength;
				ExtraLength = 0;
				CommentLength = 0;
				DiskNumber = 0;
				InternalAttributes = 0;
				ExternalAttributes = 0;
				LocalOffset = 0;
			}

		}


		[StructLayout ( LayoutKind.Sequential , Pack = 1 )]
		public struct EndOfCentralDirectory {

			public const int DefaultSignature = 0x06054B50;

			// +00 4
			public int Signature;
			// +04 2
			public ushort DiskNumber;
			// +06 2
			public ushort CentralDirectoryDiskNumber;
			// +08 2
			public ushort DiskRecords;
			// +0A 2
			public ushort TotalRecords;
			// +0C 4
			public uint CentralDirectorySize;
			// +10 4
			public uint CentralDirectoryOffset;
			// +14 2
			public ushort CommentLength;
			// +16

		}

	}

}
