using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SnowPakTool.Zip;

namespace SnowPakTool {

	public static class ZipPakHelper {

		public static string LoadListName => "pak.load_list";
		public static ushort ZipVersion => 0x2D; //default to 4.5/ZIP64 even if it's not used


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

			using ( var zipStream = File.Open ( pakLocation , FileMode.CreateNew , FileAccess.ReadWrite , FileShare.Read ) ) {
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
			var isZip64 = false;

			var count = files.Count;
			var isZip64Count = !GetLegacyUInt16 ( count , out var legacyCount );
			isZip64 = isZip64 || isZip64Count;

			var relativeNames = new byte[count][];
			var offsets = new long[count];
			var methods = new CompressionMethod[count];
			var times = new ushort[count];
			var dates = new ushort[count];
			var crc32s = new int[count];
			var compressedLengths = new long[count];
			var uncompressedLengths = new long[count];

			//local entries and actual data
			Console.Write ( "Writing files." );
			for ( int i = 0; i < count; i++ ) {
				Console.Write ( $"\rWriting files: {i + 1}/{count}" );

				offsets[i] = zipStream.Position;
				var location = files[i].Value;
				var relativeName = files[i].Key;
				var relativeNameBytes = relativeNames[i] = MiscHelpers.Encoding.GetBytes ( relativeName );

				using ( var file = File.OpenRead ( location ) ) {
					var dateTime = File.GetLastWriteTime ( file.Name );

					var uncompressedLength = uncompressedLengths[i] = file.Length;
					var compressedLength = compressedLengths[i] = file.Length;
					var isZip64CompressedLength = !GetLegacyUInt32 ( compressedLength , out var legacyCompressedLength );
					var isZip64UncompressedLength = !GetLegacyUInt32 ( uncompressedLength , out var legacyUncompressedLength );
					isZip64 = isZip64 || isZip64CompressedLength || isZip64UncompressedLength;

					var header = new LocalHeader {
						Signature = LocalHeader.DefaultSignature ,
						VersionNeeded = ZipVersion ,
						Flags = 0 ,
						Compression = methods[i] = CompressionMethod.Store ,
						Time = times[i] = MiscHelpers.GetDosTime ( dateTime ) ,
						Date = dates[i] = MiscHelpers.GetDosDate ( dateTime ) ,
						Crc32 = crc32s[i] = file.ComputeCrc32 ( uncompressedLength ) ,
						CompressedSize = legacyCompressedLength ,
						UncompressedSize = legacyUncompressedLength ,
						NameLength = MiscHelpers.EnsureFitsUInt16 ( relativeNameBytes.Length ) ,
						ExtraLength = 0 ,
					};

					if ( isZip64CompressedLength || isZip64UncompressedLength ) {
						header.ExtraLength = 4;
						if ( isZip64CompressedLength ) header.ExtraLength += 8;
						if ( isZip64UncompressedLength ) header.ExtraLength += 8;
					}

					zipStream.WriteValue ( header );
					zipStream.Write ( relativeNameBytes , 0 , relativeNameBytes.Length );

					//zip64 extra
					if ( isZip64CompressedLength || isZip64UncompressedLength ) {
						WriteExtraZip64Header ( zipStream , header.ExtraLength );
						if ( isZip64UncompressedLength ) zipStream.WriteValue ( uncompressedLengths[i] );
						if ( isZip64CompressedLength ) zipStream.WriteValue ( compressedLengths[i] );
					}

					file.Position = 0;
					file.CopyTo ( zipStream );
				}
			}
			Console.WriteLine ();

			var directoryStart = zipStream.Position;
			var isZip64DirectoryStart = !GetLegacyUInt32 ( directoryStart , out var legacyDirectoryStart );
			isZip64 = isZip64 || isZip64DirectoryStart;

			//central directory entries
			Console.Write ( "Writing central directory." );
			for ( int i = 0; i < count; i++ ) {
				Console.Write ( $"\rWriting central directory: {i + 1}/{count}" );

				var isZip64CompressedLength = !GetLegacyUInt32 ( compressedLengths[i] , out var legacyCompressedLength );
				var isZip64UncompressedLength = !GetLegacyUInt32 ( uncompressedLengths[i] , out var legacyUncompressedLength );
				var isZip64LocalOffset = !GetLegacyUInt32 ( offsets[i] , out var legacyOffset );
				isZip64 = isZip64 || isZip64CompressedLength || isZip64UncompressedLength || isZip64LocalOffset;

				var relativeName = relativeNames[i];
				var header = new DirectoryHeader {
					Signature = DirectoryHeader.DefaultSignature ,
					VersionMadeBy = ZipVersion ,
					VersionNeeded = ZipVersion ,
					Flags = 0 ,
					Compression = methods[i] ,
					Time = times[i] ,
					Date = dates[i] ,
					Crc32 = crc32s[i] ,
					CompressedSize = legacyCompressedLength ,
					UncompressedSize = legacyUncompressedLength ,
					NameLength = (ushort) relativeName.Length , //gets checked when writing local headers
					ExtraLength = 0 ,
					CommentLength = 0 ,
					DiskNumber = 0 ,
					InternalAttributes = 0 ,
					ExternalAttributes = 0 ,
					LocalHeaderOffset = legacyOffset ,
				};

				if ( isZip64CompressedLength || isZip64UncompressedLength || isZip64LocalOffset ) {
					header.ExtraLength = 4;
					if ( isZip64CompressedLength ) header.ExtraLength += 8;
					if ( isZip64UncompressedLength ) header.ExtraLength += 8;
					if ( isZip64LocalOffset ) header.ExtraLength += 8;
				}

				zipStream.WriteValue ( header );
				zipStream.Write ( relativeName , 0 , relativeName.Length );

				//zip64 extra
				if ( header.ExtraLength > 0 ) {
					WriteExtraZip64Header ( zipStream , header.ExtraLength );
					if ( isZip64UncompressedLength ) zipStream.WriteValue ( uncompressedLengths[i] );
					if ( isZip64CompressedLength ) zipStream.WriteValue ( compressedLengths[i] );
					if ( isZip64LocalOffset ) zipStream.WriteValue ( offsets[i] );
				}
			}
			Console.WriteLine ();

			var directoryEnd = zipStream.Position;
			var directorySize = directoryEnd - directoryStart;
			var isZip64DirectorySize = !GetLegacyUInt32 ( directorySize , out var legacyDirectorySize );
			isZip64 = isZip64 || isZip64DirectorySize;

			if ( isZip64 ) {
				var eod64 = new EndOfDirectory64 {
					Signature = EndOfDirectory64.DefaultSignature ,
					Size = MiscHelpers.SizeOf<EndOfDirectory64> () - 4 - 8 ,
					VersionMadeBy = ZipVersion ,
					VersionNeeded = ZipVersion ,
					DiskNumber = 0 ,
					DirectoryDiskNumber = 0 ,
					DiskRecords = (ulong) count ,
					TotalRecords = (ulong) count ,
					DirectorySize = (ulong) directorySize ,
					DirectoryOffset = (ulong) directoryStart ,
				};
				zipStream.WriteValue ( eod64 );
				var locator64 = new EndOfDirectory64Locator {
					Signature = EndOfDirectory64Locator.DefaultSignature ,
					EndOfDirectory64DiskNumber = 0 ,
					EndOfDirectory64Offset = (ulong) directoryEnd ,
					TotalDisks = 1 ,
				};
				zipStream.WriteValue ( locator64 );
			}

			//central directory end
			var eod = new EndOfDirectory {
				Signature = EndOfDirectory.DefaultSignature ,
				DiskNumber = 0 ,
				DirectoryDiskNumber = 0 ,
				DiskRecords = legacyCount ,
				TotalRecords = legacyCount ,
				DirectorySize = legacyDirectorySize ,
				DirectoryOffset = legacyDirectoryStart ,
				CommentLength = 0 ,
			};
			zipStream.WriteValue ( eod );
		}

		private static void WriteExtraZip64Header ( Stream stream , ushort extraLength ) {
			var extraHeader = new ExtensibleExtraHeader {
				Id = ExtensibleExtraId.Zip64 ,
				Size = (ushort) ( extraLength - MiscHelpers.SizeOf<ExtensibleExtraHeader> () ) ,
			};
			stream.WriteValue ( extraHeader );
		}

		private static bool GetLegacyUInt32 ( long value , out uint legacy ) {
			if ( value < 0 ) throw new ArgumentOutOfRangeException ( nameof ( value ) );
			if ( value >= uint.MaxValue ) {
				legacy = uint.MaxValue;
				return false;
			}
			else {
				legacy = (uint) value;
				return true;
			}
		}

		private static bool GetLegacyUInt16 ( int value , out ushort legacy ) {
			if ( value < 0 ) throw new ArgumentOutOfRangeException ( nameof ( value ) );
			if ( value >= ushort.MaxValue ) {
				legacy = ushort.MaxValue;
				return false;
			}
			else {
				legacy = (ushort) value;
				return true;
			}
		}

	}

}
