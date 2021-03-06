﻿using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SnowPakTool {

	public class LoadListAssetEntry : LoadListEntryBase {

		/// <summary>
		/// Regex used to parse internal file names.
		/// </summary>
		/// <remarks>
		/// This format differs slightly from the one in <see cref="CacheBlockFileFileEntry"/> in that it doesn't use ':' in place of an empty directory name.
		/// </remarks>
		public static Regex InternalNameRegex { get; } = new Regex ( @"^(?:<(?<ps>[^>]+)>(?<dir>\\(?:.+\\)?))?(?<fn>[^\\]+)$" , RegexOptions.Compiled );

		/// <summary>
		/// Regex used to parse external file names.
		/// </summary>
		public static Regex ExternalNameRegex { get; } = new Regex ( @"^(?:\[(?<ps>[^\]]+)\](?<dir>\\(?:.+\\)?))?(?<fn>[^\\]+)$" , RegexOptions.Compiled );



		/// <summary>
		/// Converts file name from the internal name format that uses angled brackets into the external name format with square brackets.
		/// </summary>
		public static string InternalNameToExternalName ( string name , out string ps ) {
			var match = InternalNameRegex.Match ( name );
			if ( !match.Success ) throw new ArgumentException ( $"Unexpected internal file name format: '{name}'" , nameof ( name ) );
			ps = match.Groups["ps"].Value;
			var dir = match.Groups["dir"].Value;
			var fn = match.Groups["fn"].Value;
			if ( ps.IndexOfAny ( IOHelpers.InvalidNameChars ) >= 0
						|| dir.IndexOfAny ( IOHelpers.InvalidPathChars ) >= 0
						|| fn.IndexOfAny ( IOHelpers.InvalidNameChars ) >= 0 ) throw new ArgumentException ( $"Invalid characters found in internal name: '{name}'" , nameof ( name ) );

			return ps.Length > 0 ? $"[{ps}]{dir}{fn}" : dir + fn;
		}

		/// <summary>
		/// Converts file name from the external (file system) name format into the internal one.
		/// </summary>
		public static string ExternalNameToInternalName ( string name ) {
			var match = ExternalNameRegex.Match ( name );
			if ( !match.Success ) throw new ArgumentException ( $"Unexpected external file name format: '{name}'" , nameof ( name ) );
			var ps = match.Groups["ps"].Value;
			var dir = match.Groups["dir"].Value;
			var fn = match.Groups["fn"].Value;
			var sb = new StringBuilder ( name.Length + 4 );
			if ( ps.Length > 0 ) {
				sb.Append ( '<' );
				sb.Append ( ps );
				sb.Append ( '>' );
			}
			if ( dir.Length > 0 ) {
				sb.Append ( dir );
			}
			sb.Append ( fn );
			return sb.ToString ();
		}


		public override LoadListEntryType Type => LoadListEntryType.Asset;
		public string InternalName { get; set; }
		public string InternalNamePs { get; set; }
		public string ExternalName { get; set; }
		public string Loader { get; set; }
		public string PakName { get; set; }
		public string Json { get; set; }

		public override int StringsCount => Json == null ? 3 : 4;


		public override bool IsValidStringsCount ( int count ) {
			return count == 3 || count == 4;
		}

		public override void LoadStrings ( string[] strings ) {
			if ( strings is null ) throw new ArgumentNullException ( nameof ( strings ) );
			if ( !IsValidStringsCount ( strings.Length ) ) throw new NotSupportedException ();

			InternalName = strings[0];
			ExternalName = InternalNameToExternalName ( InternalName , out var ps );
			InternalNamePs = ps;
			Loader = strings[1];
			PakName = strings[2];
			if ( strings.Length >= 4 ) {
				Json = strings[3];
			}
		}

		public override void WriteStrings ( Stream stream ) {
			base.WriteStrings ( stream );
			stream.WriteLength32String ( InternalName );
			stream.WriteLength32String ( Loader );
			stream.WriteLength32String ( PakName );
			if ( Json != null ) {
				stream.WriteLength32String ( Json );
			}
		}

		public override string ToString () {
			return $"[{Index}] [{PakName}] {InternalName} ({Loader}) ({DependsOn?.Length ?? 0}) @0x{DependencyEntryOffset:X}/0x{StringsEntryOffset:X}";
		}

	}

}
