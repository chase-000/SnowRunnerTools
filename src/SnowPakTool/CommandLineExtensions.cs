using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SnowPakTool {

	/// <summary>
	/// Extension methods to work with <see cref="System.CommandLine"/>.
	/// </summary>
	public static class CommandLineExtensions {

		public static string LicenseOptionName => "--license";


		public static Argument<DirectoryInfo> NonExistingOnly ( this Argument<DirectoryInfo> argument , Func<ArgumentResult , bool> unless = null ) {
			argument.AddValidator ( symbol =>
				unless != null && unless ( symbol ) ? null :
				symbol.Tokens
					.Select ( a => a.Value )
					.Where ( a => Directory.Exists ( a ) )
					.Select ( a => $"Directory already exists: {a}" )
					.FirstOrDefault ()
			);
			return argument;
		}

		public static Argument<DirectoryInfo> NonExistingOnly ( this Argument<DirectoryInfo> argument , Option unless ) {
			return argument.NonExistingOnly ( symbol => symbol.Parent.Children.OfType<OptionResult> ().Any ( a => unless.HasAlias ( a.Token.Value ) ) );
		}

		public static Argument<FileInfo> NonExistingOnly ( this Argument<FileInfo> argument ) {
			argument.AddValidator ( symbol =>
				symbol.Tokens
					.Select ( a => a.Value )
					.Where ( a => File.Exists ( a ) )
					.Select ( a => $"File already exists: {a}" )
					.FirstOrDefault ()
			);
			return argument;
		}

		public static Argument<IEnumerable<FileInfo>> ExistingOrWildcardOnly ( this Argument<IEnumerable<FileInfo>> argument ) {
			argument.AddValidator ( symbol =>
				symbol.Tokens
					.Select ( t => t.Value )
					.Where ( a => !IOHelpers.FileExistsOrWildcardDirectoryExists ( a ) )
					.Select ( a => $"File does not exist: {a}" )
					.FirstOrDefault ()
			);
			return argument;
		}

		public static void AddLicenseOption ( this RootCommand root ) {
			var optLicense = new Option ( LicenseOptionName , "Show licensing information" );
			root.Add ( optLicense );
		}

		public static InvocationMiddleware MakePrintLicenseResourceMiddleware ( Type programType ) {
			return MakePrintLicenseMiddleware ( () => programType.Assembly.GetManifestResourceStream ( $"{programType.Namespace}.LICENSE" ) );
		}

		public static InvocationMiddleware MakePrintLicenseMiddleware ( Func<Stream> getLicenseStream ) {
			return async ( InvocationContext context , Func<InvocationContext , Task> next ) => {
				if ( context.ParseResult.Tokens.Any ( a => a.Type == TokenType.Option && a.Value == LicenseOptionName ) ) {
					DoPrintLicense ( getLicenseStream );
				}
				else {
					await next ( context );
				}
			};
		}

		public static int InvokeWithMiddleware ( this RootCommand root , string[] args , params InvocationMiddleware[] middlewares ) {
			var builder = new CommandLineBuilder ( root );
			var parser = middlewares
				.Aggregate ( builder.UseDefaults () , ( builder , middleware ) => builder.UseMiddleware ( middleware ) )
				.Build ();
			return parser.Invoke ( args );
		}

		public static IEnumerable<FileInfo> ParseWildcards ( ArgumentResult result ) {
			foreach ( var token in result.Tokens ) {
				var location = token.Value;
				if ( location.IndexOfAny ( IOHelpers.Wildcards ) < 0 ) {
					yield return new FileInfo ( location );
				}
				else {
					var directory = Path.GetDirectoryName ( location );
					if ( directory.Length == 0 ) directory = Directory.GetCurrentDirectory ();
					var name = Path.GetFileName ( location );
					foreach ( var item in Directory.EnumerateFiles ( directory , name ) ) {
						yield return new FileInfo ( item );
					}
				}
			}
		}



		private static void DoPrintLicense ( Func<Stream> getLicenseStream ) {
			using var stream = getLicenseStream () ?? throw new InvalidOperationException ( "Can't find the license resource." );
			using var reader = new StreamReader ( stream );
			Console.WriteLine ( reader.ReadToEnd () );
		}

	}

}
