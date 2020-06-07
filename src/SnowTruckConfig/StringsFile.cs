using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SnowTruckConfig {

	/// <summary>
	/// [strings]\strings_*.str reader/writer.
	/// </summary>
	public static class StringsFile {

		//public static Regex LineRegex { get; } = new Regex ( @"^\s*(?<key>\S+)\s+""(?<value>[^""]*)""\s*$" , RegexOptions.Compiled );
		public static Regex LineRegex { get; } = new Regex ( @"^\s*(?<key>""[^""]+"")|(?<key>\S+)\s+""(?<value>[^""]*)""\s*$" , RegexOptions.Compiled );

		/// <summary>
		/// Read key/value pairs from the .str file.
		/// </summary>
		public static IEnumerable<KeyValuePair<string , string>> ReadStrings ( string location ) {
			if ( location is null ) throw new ArgumentNullException ( nameof ( location ) );
			return Generator ( location );

			static IEnumerable<KeyValuePair<string , string>> Generator ( string location ) {
				using var reader = new StreamReader ( location );
				while ( true ) {
					var line = reader.ReadLine ();
					if ( line == null ) break;
					var pair = ParseLine ( line.AsSpan () );
					if ( pair.Key == null || pair.Value == null ) continue; //ignore parsing errors
					yield return pair;
				}
			}
		}

		/// <summary>
		/// Read key/value pairs from the .str file.
		/// </summary>
		public static void WriteStrings ( string location , IEnumerable<KeyValuePair<string , string>> values , bool overwrite = false ) {
			if ( location is null ) throw new ArgumentNullException ( nameof ( location ) );
			if ( values is null ) throw new ArgumentNullException ( nameof ( values ) );

			using var stream = File.Open ( location , overwrite ? FileMode.Create : FileMode.CreateNew , FileAccess.Write , FileShare.Read );
			using var writer = new StreamWriter ( stream , Encoding.Unicode );
			foreach ( var pair in values ) {
				WriteString ( writer , pair.Key );
				writer.Write ( "\t\t\t\t" );
				WriteString ( writer , pair.Value , true );
				writer.WriteLine ();
			}
		}

		private static void WriteString ( StreamWriter writer , string str , bool forceQuotes = false ) {
			var quoted = forceQuotes || str.Length == 0 || str.Any ( char.IsWhiteSpace );
			if ( quoted ) {
				writer.Write ( '"' );
			}
			foreach ( var ch in str ) {
				switch ( ch ) {
					case '"': writer.Write ( "\\\"" ); continue;
					case '\n': writer.Write ( "\\n" ); continue;
					case '\\': writer.Write ( "\\\\" ); continue;
				}
				writer.Write ( ch );
			}
			if ( quoted ) {
				writer.Write ( '"' );
			}
		}

		public static KeyValuePair<string , string> ParseLine ( in ReadOnlySpan<char> line ) {
			var i = 0;
			i = GetString ( in line , out var key );
			var tail = line[i..];
			GetString ( in tail , out var value );
			return new KeyValuePair<string , string> ( key , value );
		}

		/// <summary>
		/// Gets possibly quoted string with backslash-escapes.
		/// </summary>
		public static int GetString ( string line , out string str ) {
			if ( line is null ) throw new ArgumentNullException ( nameof ( line ) );
			return GetString ( line.AsSpan () , out str );
		}

		/// <summary>
		/// Gets possibly quoted string with backslash-escapes.
		/// </summary>
		public static int GetString ( in ReadOnlySpan<char> line , out string str ) {
			var i = 0;

			while ( i < line.Length && char.IsWhiteSpace ( line[i] ) ) i++;

			if ( i >= line.Length ) {
				str = string.Empty;
				return i;
			}

			var inQuotes = line[i] == '"';
			if ( inQuotes ) i++;
			var start = i;
			var sb = new StringBuilder ( line.Length - start );
			while ( i < line.Length ) {
				var ch = line[i];
				if ( !inQuotes && char.IsWhiteSpace ( ch ) ) {
					str = sb.ToString ();
					return i;
				}
				if ( ch == '"' ) {
					inQuotes = !inQuotes;
					i++;
					continue;
				}
				if ( ch == '\\' ) {
					i++;
					if ( i >= line.Length ) {
						str = sb.ToString ();
						return i;
					}
					ch = line[i];
					switch ( ch ) {
						case 'n': ch = '\n'; break;
					}
				}
				sb.Append ( ch );
				i++;
			}
			str = sb.ToString ();
			return i;
		}

	}

}
