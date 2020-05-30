using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace SnowPakTool {

	public static class ZipPakHelper {

		public static string LoadListName { get; } = "pak.load_list";

		private static readonly byte[] __DummyBuffer = new byte[0];
		private const int ZipVersion = 0x14;


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
				if ( loadListExists ) {
					WriteListFile ( zipStream , sourceDirectory , listLocation );
				}
				using ( var zip = new ZipArchive ( zipStream , ZipArchiveMode.Update ) ) {
					AddFiles ( zip , sourceDirectory );
					Console.WriteLine ( "Compressing..." );
				}
			}
			Console.WriteLine ( "Done." );
		}


		private static void AddFiles ( ZipArchive zip , string sourceDirectory ) {
			var files = Directory.GetFiles ( sourceDirectory , "*" , SearchOption.AllDirectories );
			var skipped = false;
			for ( int i = 0; i < files.Length; i++ ) {
				var location = files[i];
				Console.Write ( $"\rAdding file {i + 1}/{files.Length}" );
				var relativeName = location.Substring ( sourceDirectory.Length );
				if ( !skipped && relativeName.Equals ( LoadListName , StringComparison.OrdinalIgnoreCase ) ) {
					skipped = true;
					continue;
				}
				zip.CreateEntryFromFile ( location , relativeName , CompressionLevel.Fastest );
			}
			Console.WriteLine ();
		}

		/// <summary>
		/// The list file must be the first file in the archive, stored without compression.
		/// </summary>
		/// <remarks>
		/// <see cref="ZipArchive"/> does not support real store (0) method and uses deflate instead, so it's written here manually.
		/// </remarks>
		private static void WriteListFile ( Stream zipStream , string baseLocation , string listLocation ) {
			StoreFiles ( zipStream , baseLocation , listLocation );
		}

		private static void StoreFiles ( Stream zipStream , string baseLocation , params string[] files ) {
			var count = MiscHelpers.EnsureFitsUInt16 ( files.Length );
			var localEntries = new LocalFileHeader[count];
			var relativeNames = new byte[count][];
			var offsets = new long[count];

			//local entries and actual data
			for ( int i = 0; i < count; i++ ) {
				offsets[i] = zipStream.Position;
				var location = files[i];
				var relativeName = location.Substring ( baseLocation.Length );
				using ( var sourceStream = File.OpenRead ( location ) ) {
					var size = MiscHelpers.EnsureFitsUInt32 ( sourceStream.Length );
					var relativeNameBytes = relativeNames[i] = MiscHelpers.Encoding.GetBytes ( relativeName );
					var lfh = localEntries[i] = new LocalFileHeader {
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
					zipStream.WriteValue ( LocalFileHeader.DefaultSignature );
					zipStream.WriteValue ( lfh );
					zipStream.Write ( relativeNameBytes , 0 , relativeNameBytes.Length );

					sourceStream.Position = 0;
					sourceStream.CopyTo ( zipStream );
				}
			}

			//central directory entries
			var centralDirectoryStart = MiscHelpers.EnsureFitsUInt32 ( zipStream.Position );
			for ( int i = 0; i < count; i++ ) {
				var lfh = localEntries[i];
				var cdfh = new CentralDirectoryFileHeader ( lfh ) {
					LocalOffset = MiscHelpers.EnsureFitsUInt32 ( offsets[i] ) ,
				};
				zipStream.WriteValue ( CentralDirectoryFileHeader.DefaultSignature );
				zipStream.WriteValue ( cdfh );
				var nameBytes = relativeNames[i];
				zipStream.Write ( nameBytes , 0 , nameBytes.Length );
			}

			//central directory end
			var centralDirectoryEnd = MiscHelpers.EnsureFitsUInt32 ( zipStream.Position );
			var eocd = new EndOfCentralDirectory {
				DiskNumber = 0 ,
				CentralDirectoryDiskNumber = 0 ,
				DiskRecords = count ,
				TotalRecords = count ,
				CentralDirectorySize = centralDirectoryEnd - centralDirectoryStart ,
				CentralDirectoryOffset = centralDirectoryStart ,
				CommentLength = 0 ,
			};
			zipStream.WriteValue ( EndOfCentralDirectory.DefaultSignature );
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

			public ushort VersionNeeded;
			public ushort Flags;
			public ushort Compression;
			public ushort Time;
			public ushort Date;
			public int Crc32;
			public uint CompressedSize;
			public uint UncompressedSize;
			public ushort NameLength;
			public ushort ExtraLength;

		}


		[StructLayout ( LayoutKind.Sequential , Pack = 1 )]
		public struct CentralDirectoryFileHeader {

			public const int DefaultSignature = 0x02014B50;

			public ushort VersionMadeBy;
			public ushort VersionNeeded;
			public ushort Flags;
			public ushort Compression;
			public ushort Time;
			public ushort Date;
			public int Crc32;
			public uint CompressedSize;
			public uint UncompressedSize;
			public ushort NameLength;
			public ushort ExtraLength;
			public ushort CommentLength;
			public ushort DiskNumber;
			public ushort InternalAttributes;
			public uint ExternalAttributes;
			public uint LocalOffset;

			public CentralDirectoryFileHeader ( LocalFileHeader header ) {
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

			public ushort DiskNumber;
			public ushort CentralDirectoryDiskNumber;
			public ushort DiskRecords;
			public ushort TotalRecords;
			public uint CentralDirectorySize;
			public uint CentralDirectoryOffset;
			public ushort CommentLength;

		}

	}

}
