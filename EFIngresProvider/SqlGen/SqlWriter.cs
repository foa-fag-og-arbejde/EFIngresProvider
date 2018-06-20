using System;
using System.Text;
using System.IO;
using System.Globalization;

namespace EFIngresProvider.SqlGen
{
    /// <summary>
    /// This extends StringWriter primarily to add the ability to add an indent
    /// to each line that is written out.
    /// </summary>
    public class SqlWriter : StringWriter
    {
        /// <summary>
        /// The number of tabs to be added at the beginning of each new line.
        /// </summary>
        private int IndentCount { get; set; }

        internal int IndentSpaceCount { get; set; }

        bool atBeginningOfLine = true;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="b"></param>
        public SqlWriter(StringBuilder b)
            : base(b, CultureInfo.InvariantCulture)
        {
            // We start at -1, since the first select statement will increment it to 0.
            IndentCount = -1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="b"></param>
        public SqlWriter()
            : this(new StringBuilder(1024))
        {
            // We start at -1, since the first select statement will increment it to 0.
            IndentCount = -1;
        }

        public IDisposable Indent()
        {
            return new IndentHandler(this);
        }

        /// <summary>
        /// Reset atBeginningofLine if we detect the newline string.
        /// <see cref="SqlBuilder.AppendLine"/>
        /// Add as many tabs as the value of indent if we are at the 
        /// beginning of a line.
        /// </summary>
        /// <param name="value"></param>
        public override void Write(string value)
        {
            if (value == "\r\n")
            {
                base.WriteLine();
                atBeginningOfLine = true;
            }
            else
            {
                if (atBeginningOfLine)
                {
                    if (IndentCount > 0)
                    {
                        base.Write(new string(' ', IndentCount * IndentSpaceCount));
                    }
                    atBeginningOfLine = false;
                }
                base.Write(value);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override void WriteLine()
        {
            base.WriteLine();
            atBeginningOfLine = true;
        }

        private class IndentHandler : IDisposable
        {
            private SqlWriter _sqlWriter;
            private int _indentCount;

            public IndentHandler(SqlWriter sqlWriter)
            {
                _sqlWriter = sqlWriter;
                _indentCount = _sqlWriter.IndentCount;
            }

            public void Dispose()
            {
                _sqlWriter.IndentCount = _indentCount;
            }
        }
    }
}
