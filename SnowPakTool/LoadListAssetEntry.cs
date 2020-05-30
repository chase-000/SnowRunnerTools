using System;
using System.Text.RegularExpressions;

namespace SnowPakTool {

	public class LoadListAssetEntry : LoadListEntryBase {

		private string m_InternalName;


		/// <summary>
		/// Regex used to parse internal file names.
		/// </summary>
		/// <remarks>
		/// This format differs slightly from the one in <see cref="CacheBlockFileFileEntry"/> in that it doesn't use ':' in place of an empty directory name.
		/// </remarks>
		public static Regex InternalNameRegex { get; } = new Regex ( @"^(?:<(?<ps>[^>]+)>(?<dir>\\(?:.+\\)?))?(?<fn>[^\\]+)$" , RegexOptions.Compiled );

		/// <summary>
		/// Converts file name from the internal name format that uses angled brackets into the external name format with square brackets.
		/// </summary>
		public static string InternalNameToExternalName ( string name , out string ps ) {
			ps = null;
			var match = InternalNameRegex.Match ( name );
			if ( !match.Success ) throw new ArgumentException ( $"Unexpected internal file name format: '{name}'" , nameof ( name ) );
			var psValue = match.Groups["ps"].Value;
			var dir = match.Groups["dir"].Value;
			var fn = match.Groups["fn"].Value;
			if ( psValue.IndexOfAny ( IOHelpers.InvalidNameChars ) >= 0
						|| dir.IndexOfAny ( IOHelpers.InvalidPathChars ) >= 0
						|| fn.IndexOfAny ( IOHelpers.InvalidNameChars ) >= 0 ) throw new ArgumentException ( $"Invalid characters found in internal name: '{name}'" , nameof ( name ) );

			if ( psValue.Length > 0 ) {
				ps = psValue;
				return $"[{psValue}]{dir}{fn}";
			}
			else {
				return dir + fn;
			}
		}



		public override LoadListEntryType Type => LoadListEntryType.Asset;

		public override int ExpectedStringsCount => 3;

		public string InternalName {
			get => m_InternalName;
			set {
				ExternalName = InternalNameToExternalName ( value , out var ps );
				InternalNamePs = ps;
				m_InternalName = value;
			}
		}

		public string InternalNamePs { get; private set; }

		public string ExternalName { get; private set; }

		public string Loader { get; set; }

		public string PakName { get; set; }

		public override string ToString () {
			return $"[{Index}] [{PakName}] {InternalName} ({Loader}) ({DependsOn.Length}) @0x{DependencyEntryOffset:X}/0x{StringsEntryOffset:X}";
		}

	}

}
