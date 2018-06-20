using System;

namespace EFIngresProviderDeploy
{
    public static class ConsoleLog
    {
        private static Logger _out = new Logger(Console.Out);

        public static IDisposable Indent()
        {
            return _out.Indent();
        }

        public static void Write(string str)
        {
            _out.Write(str);
        }

        public static void Write(string format, params object[] parameters)
        {
            _out.Write(format, parameters);
        }

        public static void WriteLine()
        {
            _out.WriteLine();
        }

        public static void WriteLine(string str)
        {
            _out.WriteLine(str);
        }

        public static void WriteLine(string format, params object[] parameters)
        {
            _out.WriteLine(format, parameters);
        }
    }
}
