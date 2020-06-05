using System;
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



		private static void DoPrintLicense ( Func<Stream> getLicenseStream ) {
			using var stream = getLicenseStream () ?? throw new InvalidOperationException ( "Can't find the license resource." );
			using var reader = new StreamReader ( stream );
			Console.WriteLine ( reader.ReadToEnd () );
		}

	}

}
