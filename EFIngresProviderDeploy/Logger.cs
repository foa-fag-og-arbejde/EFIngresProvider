using System;
using System.IO;
using System.Linq;

namespace EFIngresProviderDeploy
{
    public class Logger
    {
        public class Indentation : IDisposable
        {
            public Indentation(Logger logger)
            {
                _logger = logger;
                _logger.IndentInternal();
            }

            private Logger _logger;

            void IDisposable.Dispose()
            {
                _logger.OutdentInternal();
            }
        }

        public Logger(TextWriter writer)
        {
            _writer = writer;
            IndentationSpaceCount = 4;
        }

        private TextWriter _writer;
        private int _indentationLevel = 0;
        private string _indentation = "";
        private bool _atStartOfLine = false;

        public int IndentationSpaceCount { get; set; }

        public int IndentationLevel
        {
            get { return _indentationLevel; }
            set { _indentationLevel = Math.Max(0, value); _indentation = new string(' ', _indentationLevel * IndentationSpaceCount); }
        }

        private string IndentationInternal
        {
            get { return _indentation; }
        }

        public IDisposable Indent()
        {
            return new Indentation(this);
        }

        private void IndentInternal()
        {
            IndentationLevel += 1;
        }

        private void OutdentInternal()
        {
            IndentationLevel = Math.Max(0, _indentationLevel - 1);
        }

        private void WriteFragmentInternal(string str, bool isLine)
        {
            if (isLine)
            {
                WriteIndentation();
                _writer.WriteLine(str);
                _atStartOfLine = true;
            }
            else
            {
                if (!string.IsNullOrEmpty(str))
                {
                    WriteIndentation();
                    _writer.Write(str);
                    _atStartOfLine = false;
                }
            }
        }

        private void WriteStr(string str, bool isLine)
        {
            if (string.IsNullOrEmpty(str))
            {
                if (isLine)
                {
                    WriteFragmentInternal("", true);
                }
            }
            else
            {
                var lines = str.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                foreach (var line in lines.Take(lines.Count() - 1))
                {
                    WriteFragmentInternal(line, true);
                }
                WriteFragmentInternal(lines.Last(), isLine);
            }
        }

        private void WriteIndentation()
        {
            if (_atStartOfLine)
            {
                _writer.Write(IndentationInternal);
                _atStartOfLine = false;
            }
        }

        public void Write(string str)
        {
            WriteStr(str, false);
        }

        public void Write(string format, params object[] parameters)
        {
            Write(string.Format(format, parameters));
        }

        public void WriteLine()
        {
            _writer.WriteLine();
            _atStartOfLine = true;
        }

        public void WriteLine(string str)
        {
            WriteStr(str, true);
        }

        public void WriteLine(string format, params object[] parameters)
        {
            WriteLine(string.Format(format, parameters));
        }

        public override string ToString()
        {
            return _writer.ToString();
        }
    }
}
