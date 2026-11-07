using System;

namespace NebulaSoft
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class DefineAttribute : Attribute
    {
        public string Define { get; private set; }
        public string AssemblyType { get; private set; }
        public string FilePath { get; private set; }

        public DefineAttribute(string define, string assemblyType = "", string filePath = "")
        {
            Define = define;

            AssemblyType = assemblyType;
            FilePath = filePath;
        }
    }
}
