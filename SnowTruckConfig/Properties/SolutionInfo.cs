using System.Reflection;

[assembly: AssemblyCopyright ( "Copyright © 2020 chase <s.chubukov@protonmail.com>" )]

#if DEBUG
[assembly: AssemblyConfiguration ( "DEBUG" )]
#else
[assembly: AssemblyConfiguration ( "RELEASE" )]
#endif
