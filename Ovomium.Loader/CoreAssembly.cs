using System.IO;
using System.Reflection;
using BepInEx;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Ovomium.Loader
{
    /// <summary>
    /// Lecture d'Ovomium.Core.dll (et de son .pdb s'il existe) puis chargement depuis ses octets sous un nom unique :
    /// deux versions portant le même nom d'assembly se confondraient (Unity associe les MonoBehaviour à leur
    /// assembly par son nom, la résolution de types par nom aussi) ; même procédé que ScriptEngine. Le fichier n'est
    /// jamais verrouillé, il peut être remplacé jeu lancé.
    /// </summary>
    internal sealed class CoreAssembly
    {
        public readonly string Path;
        private readonly byte[] m_dll;
        private readonly byte[] m_pdb;

        private CoreAssembly(string path, byte[] dll, byte[] pdb)
        {
            Path = path;
            m_dll = dll;
            m_pdb = pdb;
        }

        public static CoreAssembly Read(string path)
        {
            string pdb = System.IO.Path.ChangeExtension(path, ".pdb");
            return new CoreAssembly(path, File.ReadAllBytes(path), File.Exists(pdb) ? File.ReadAllBytes(pdb) : null);
        }

        /// <summary>Nouvelle assembly à chaque appel (nom suffixé), symboles compris si le .pdb est lisible.</summary>
        public Assembly Load()
        {
            string name = "Ovomium.Core-" + System.DateTime.Now.Ticks;
            if (m_pdb != null)
            {
                try { return LoadRenamed(name, true); }
                catch (System.Exception e) { OvomiumLoader.Log.LogWarning($"Symboles de {Path} illisibles, chargement sans : {e.Message}"); }
            }
            return LoadRenamed(name, false);
        }

        private Assembly LoadRenamed(string name, bool symbols)
        {
            using (var resolver = new DefaultAssemblyResolver())
            {
                resolver.AddSearchDirectory(Paths.ManagedPath);
                resolver.AddSearchDirectory(Paths.BepInExAssemblyDirectory);
                resolver.AddSearchDirectory(System.IO.Path.GetDirectoryName(Path));
                var reader = new ReaderParameters { AssemblyResolver = resolver };
                if (symbols)
                {
                    reader.ReadSymbols = true;
                    reader.SymbolStream = new MemoryStream(m_pdb);
                    reader.SymbolReaderProvider = new PortablePdbReaderProvider();
                }
                using (AssemblyDefinition definition = AssemblyDefinition.ReadAssembly(new MemoryStream(m_dll), reader))
                {
                    definition.Name.Name = name;
                    return Write(definition, symbols);
                }
            }
        }

        private static Assembly Write(AssemblyDefinition definition, bool symbols)
        {
            var dll = new MemoryStream();
            if (!symbols)
            {
                definition.Write(dll);
                return Assembly.Load(dll.ToArray());
            }
            var pdb = new MemoryStream();
            definition.Write(dll, new WriterParameters
            {
                WriteSymbols = true,
                SymbolStream = pdb,
                SymbolWriterProvider = new PortablePdbWriterProvider(),
            });
            return Assembly.Load(dll.ToArray(), pdb.ToArray());
        }
    }
}
