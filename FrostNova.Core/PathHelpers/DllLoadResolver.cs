using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.PathHelpers
{
    public static class DllLoadResolver
    {
        public static void Initialize(string subDirectoryName)
        {
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += GetDllLoadResolveHandler(subDirectoryName);

        }


        private static ResolveEventHandler GetDllLoadResolveHandler(string subDirectoryName)
        {

            var path = FrostNova.Core.PathHelpers.PathHelper.GetAbsolutePath(subDirectoryName);

            ResolveEventHandler handler = (object? sender, ResolveEventArgs e) =>
            {
                var resAsm = e.RequestingAssembly;
                if (resAsm != null)
                {
                    string[] referencedAssemblies = Directory.GetFiles(path, "*.dll");

                    foreach (string assemblyFile in referencedAssemblies)
                    {
                        AssemblyName assemblyName = AssemblyName.GetAssemblyName(assemblyFile);
                        if (e.Name == assemblyName.FullName)
                        {
                            return Assembly.LoadFrom(assemblyFile);
                        }
                    }
                }

                return null;
            };

            return handler;

        }


    }
}
