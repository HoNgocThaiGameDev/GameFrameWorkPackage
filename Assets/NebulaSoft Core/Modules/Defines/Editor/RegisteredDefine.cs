namespace NebulaSoft
{
    public class RegisteredDefine
    {
        public readonly string Define;
        public readonly string AssemblyType;
        public readonly string FilePath;

        public RegisteredDefine(string define, string assemblyType, string filePath)
        {
            Define = define;
            AssemblyType = assemblyType;
            FilePath = filePath;
        }

        public RegisteredDefine(DefineAttribute defineAttribute)
        {
            Define = defineAttribute.Define;
            AssemblyType = defineAttribute.AssemblyType;
            FilePath = defineAttribute.FilePath;
        }
    }
}