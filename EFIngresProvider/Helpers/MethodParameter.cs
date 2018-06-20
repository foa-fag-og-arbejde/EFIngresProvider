using System;

namespace EFIngresProvider.Helpers
{
    internal class MethodParameter
    {
        public MethodParameter(Type type, object value)
        {
            Type = type;
            Value = value;
        }

        public Type Type { get; private set; }
        public object Value { get; private set; }
    }

    internal class MethodParameter<T> : MethodParameter
    {
        public MethodParameter(T value)
            : base(typeof(T), value)
        {
        }
    }
}
