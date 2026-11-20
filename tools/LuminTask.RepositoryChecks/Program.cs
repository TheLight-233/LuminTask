using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;

string root = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
string configuration = args.Length > 1 ? args[1] : "Release";
string[] frameworks = ["netstandard2.1", "net8.0", "net9.0", "net10.0"];
foreach (string project in new[] { "LuminTask", "LuminTask.AsyncEx" })
{
    string folder = Path.Combine(root, "src", project);
    var file = XDocument.Load(Path.Combine(folder, project + ".csproj"));
    Require(file.Descendants("AllowUnsafeBlocks").Single().Value == "false", project + " allows unsafe C#.");
    Require(file.Descendants("TargetFrameworks").Single().Value.Split(';').SequenceEqual(frameworks), project + " target matrix changed.");
    foreach (var reference in file.Descendants("ProjectReference"))
    {
        string name = Path.GetFileNameWithoutExtension(reference.Attribute("Include")!.Value.Replace('\\', '/'));
        string? includeAssembly = (string?)reference.Attribute("ReferenceOutputAssembly") ??
            (string?)reference.Element("ReferenceOutputAssembly");
        bool analyzer = name == "LuminTask.Analyzer" && includeAssembly == "false";
        Require(analyzer || project == "LuminTask.AsyncEx" && name == "LuminTask", $"Unexpected project reference from {project} to {name}.");
    }

    foreach (string framework in frameworks)
    {
        using var stream = File.OpenRead(Path.Combine(folder, "bin", configuration, framework, project + ".dll"));
        using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader();
        foreach (var handle in metadata.AssemblyReferences)
        {
            string name = metadata.GetString(metadata.GetAssemblyReference(handle).Name);
            Require(name.StartsWith("System.", StringComparison.Ordinal) || name == "netstandard" ||
                project == "LuminTask.AsyncEx" && name == "LuminTask", $"{project} has a runtime dependency on {name}.");
        }
        foreach (var handle in metadata.MethodDefinitions)
        {
            var method = metadata.GetMethodDefinition(handle);
            Require((method.Attributes & MethodAttributes.PinvokeImpl) == 0, $"{project} contains a native import.");
        }
        foreach (var handle in metadata.TypeReferences)
        {
            string name = metadata.GetString(metadata.GetTypeReference(handle).Name);
            Require(name is not ("NativeMemory" or "NativeLibrary" or "Marshal"), $"{project} references {name}.");
        }
    }
}
Console.WriteLine("Managed runtime and dependency boundaries verified.");

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
