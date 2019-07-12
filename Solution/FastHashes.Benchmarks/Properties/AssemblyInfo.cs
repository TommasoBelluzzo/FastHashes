#region Using Directives
using System;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
#endregion

#region Information
[assembly: AssemblyCompany("Tommaso Belluzzo")]

#if (DEBUG)
[assembly: AssemblyConfiguration("Debug Build")]
#else
[assembly: AssemblyConfiguration("Release Build")]
#endif

[assembly: AssemblyCopyright("Copyright ©2017 Tommaso Belluzzo")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyProduct("FastHashes.Benchmarks")]
[assembly: AssemblyTitle("Benchmarks for the FastHashes library.")]
[assembly: AssemblyTrademark("")]
#endregion

#region Settings
[assembly: CLSCompliant(false)]
[assembly: ComVisible(false)]
[assembly: Guid("80642F96-FB82-423A-80A4-DFCD601E47E4")]
[assembly: NeutralResourcesLanguage("en")]
#endregion

#region Version
[assembly: AssemblyFileVersion("1.7.1.0")]
[assembly: AssemblyInformationalVersion("1.7.1.0")]
[assembly: AssemblyVersion("1.7.1.0")]
#endregion