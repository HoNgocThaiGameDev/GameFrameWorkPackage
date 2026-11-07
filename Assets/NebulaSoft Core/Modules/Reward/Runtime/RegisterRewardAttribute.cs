using System;

namespace NebulaSoft
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class RegisterRewardAttribute : Attribute
    {
        public Type ViewType { get; }

        public RegisterRewardAttribute(Type viewType)
        {
            ViewType = viewType;
        }
    }
}
