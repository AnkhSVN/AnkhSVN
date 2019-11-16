using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Ankh.UI;
using Ankh.WpfUI;

namespace Ankh.WpfPackage
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Description(AnkhId.WpfPackageDescription)]
    [CLSCompliant(false)]
    [Guid(AnkhId.WpfPackageId)]
    [ProvideAutoLoad(AnkhId.AnkhServicesAvailable, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class AnkhSvnWpfPackage : AsyncPackage
    {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public AnkhSvnWpfPackage()
        {
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override async System.Threading.Tasks.Task InitializeAsync(System.Threading.CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            AnkhRuntime runtime = null;
            var ankhPackage = await GetServiceAsync(typeof(IAnkhPackage)) as IAnkhPackage;
            
            if (ankhPackage != null)
                runtime = AnkhRuntime.Get(ankhPackage);

            if (runtime != null)
            {
                runtime.AddModule(new AnkhWpfModule(runtime));
                runtime.AddModule(new AnkhWpfUIModule(runtime));
            }
            else
                Trace.WriteLine(string.Format("Failed to initialize {0}, because the Ankh Runtime is not available", typeof(AnkhSvnWpfPackage).FullName));
        }
    }
}
