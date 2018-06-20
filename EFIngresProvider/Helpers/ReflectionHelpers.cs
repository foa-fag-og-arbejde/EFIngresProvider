using System;
using System.Linq;
using System.Reflection;

namespace EFIngresProvider.Helpers
{
    internal static class ReflectionHelpers
    {
        private const BindingFlags MethodBindingFlags = BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        internal static MethodInfo GetWrappedMethod(this Type type, string name, params Type[] paramTypes)
        {
            if (type == null)
            {
                throw new MissingMethodException();
            }

            foreach (var method in type.GetMethods(MethodBindingFlags).Where(m => m.Name == name))
            {
                var parameters = method.GetParameters();
                if (parameters.Length == paramTypes.Length)
                {
                    var isMatch = true;
                    for (var i = 0; isMatch && i < paramTypes.Length; i++)
                    {
                        isMatch = isMatch && parameters[i].ParameterType == paramTypes[i];
                    }
                    if (isMatch)
                    {
                        return method;
                    }
                }
            }

            //var method = type.GetMethod(name, MethodBindingFlags, null, paramTypes, null);
            //if (method != null)
            //{
            //    return method;
            //}

            return GetWrappedMethod(type.BaseType, name, paramTypes);
        }

        //private static object InvokeWrappedMethod(Type type, object obj, string name, MethodParameter[] parameters)
        //{
        //    var method = GetWrappedMethod(type, name, parameters.Select(p => p.Type).ToArray());
        //    return method.Invoke(obj, parameters.Select(p => p.Value).ToArray());
        //}

        //internal static object InvokeWrappedMethod(this object obj, string name, params MethodParameter[] parameters)
        //{
        //    return InvokeWrappedMethod(obj.GetType(), obj, name, parameters);
        //}

        //internal static T InvokeWrappedMethod<T>(this object obj, string name, params MethodParameter[] parameters)
        //{
        //    return (T)InvokeWrappedMethod(obj.GetType(), obj, name, parameters);
        //}

        internal static object GetWrappedField(this object obj, string name)
        {
            return obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(obj);
        }

        internal static T GetWrappedField<T>(this object obj, string name)
        {
            return (T)obj.GetWrappedField(name);
        }

        internal static void SetWrappedField(this object obj, string name, object value)
        {
            obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(obj, value);
        }

        internal static object GetWrappedProperty(this object obj, string name)
        {
            return obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(obj, new object[] { });
        }

        internal static T GetWrappedProperty<T>(this object obj, string name)
        {
            return (T)obj.GetWrappedProperty(name);
        }

        internal static void SetWrappedProperty(this object obj, string name, object value)
        {
            obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(obj, value, new object[] { });
        }
    }
}
