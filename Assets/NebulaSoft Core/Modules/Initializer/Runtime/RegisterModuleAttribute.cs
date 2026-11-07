using System;

namespace NebulaSoft
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class RegisterModuleAttribute : Attribute
    {
        public string Path { get; private set; }
        public bool Core { get; private set; }
        public int Order { get; private set; }

        public RegisterModuleAttribute(string path, bool core = false, int order = 0)
        {
            Path = path;
            Core = core;
            Order = order;
        }
    }
}