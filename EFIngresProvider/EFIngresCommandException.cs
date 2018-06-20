using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace EFIngresProvider
{
    public class EFIngresCommandException : EFIngresException
    {
        public EFIngresCommandException(EFIngresCommand command)
            : base()
        {
            Init(command);
        }

        public EFIngresCommandException(EFIngresCommand command, string message)
            : base(message)
        {
            Init(command);
        }

        public EFIngresCommandException(EFIngresCommand command, string message, Exception innerException)
            : base(message, innerException)
        {
            Init(command);
        }

        private void Init(EFIngresCommand command)
        {
            if (command != null)
            {
                CommandType = command.CommandType;
                CommandText = command.CommandText;
                Parameters = command.Parameters.Cast<IDbDataParameter>().ToList();
                ModifiedCommandText = command.ModifiedCommandText;
                ModifiedParameters = command.ModifiedParameters.Cast<IDbDataParameter>().ToList();
            }
        }

        public string CommandText { get; private set; }
        public CommandType CommandType { get; private set; }
        public IEnumerable<IDbDataParameter> Parameters { get; private set; }
        public string ModifiedCommandText { get; private set; }
        public IEnumerable<IDbDataParameter> ModifiedParameters { get; private set; }

        private string FormatValue(object value)
        {
            if (value == null)
            {
                return "null";
            }
            if (value is string)
            {
                return string.Format(@"""{0}""", value);
            }
            if (value is char)
            {
                return string.Format(@"'{0}'", value);
            }
            if (value is DateTime)
            {
                var dtValue = (DateTime)value;
                return string.Format(@"{0:dd.MM.yyyy HH:mm:ss.ffff}", dtValue.Kind == DateTimeKind.Utc ? dtValue.ToLocalTime() : dtValue);
            }
            return string.Format(@"{0}", value);
        }
        
        public override string Message
        {
            get
            {
                var msg = new StringBuilder();

                if (!string.IsNullOrWhiteSpace(base.Message))
                {
                    msg.AppendLine(base.Message);
                }

                if (!string.IsNullOrWhiteSpace(CommandText))
                {
                    msg.AppendLine("Query:");
                    msg.AppendLine(CommandText);

                    if (Parameters.Any())
                    {
                        msg.AppendLine("Parameters:");
                        foreach (var param in Parameters)
                        {
                            msg.AppendLine(string.Format("{0} = {1}", param.ParameterName, FormatValue(param.Value)));
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(ModifiedCommandText) && ModifiedCommandText != CommandText)
                {
                    msg.AppendLine("Modified query:");
                    msg.AppendLine(ModifiedCommandText);

                    if (ModifiedParameters.Any())
                    {
                        msg.AppendLine("Modified parameters:");
                        foreach (var param in ModifiedParameters)
                        {
                            msg.AppendLine(string.Format("{0} = {1}", param.ParameterName, FormatValue(param.Value)));
                        }
                    }
                }

                return msg.ToString();
            }
        }
    }
}
