using Metaheuristic_system.ReflectionRequiredInterfaces;
using Metaheuristic_system.Validators;
using System.Runtime.Loader;
using System.Reflection;

namespace Metaheuristic_system.Reflection
{
    public class FileLoader
    {
        private readonly string path;
        public Assembly file;

        public FileLoader(string path)
        {
            this.path = path;
        }

        public void Load() {
            var assemblyLoadContext = new AssemblyLoadContext("TemporaryAssemblyLoadContext");
            try
            {
                var assemblyBytes = File.ReadAllBytes(path);
                file = assemblyLoadContext.LoadFromStream(new MemoryStream(assemblyBytes));
            }
            catch (Exception ex) 
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
