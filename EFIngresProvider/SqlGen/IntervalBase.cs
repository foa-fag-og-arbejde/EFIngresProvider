namespace EFIngresProvider.SqlGen
{
    public abstract class IntervalBase : ISqlFragment
    {
        public IntervalBase(ISqlFragment number)
        {
            Number = number;
        }

        public ISqlFragment Number { get; private set; }

        protected void WriteIntervalPart(SqlWriter writer, SqlGenerator sqlGenerator, ISqlFragment intervalPart)
        {
            if (intervalPart != null)
            {
                writer.Write("varchar(abs(");
                intervalPart.WriteSql(writer, sqlGenerator);
                writer.Write("))");
            }
            else
            {
                writer.Write("'0'");
            }
        }

        protected void WriteSign(SqlWriter writer)
        {
            writer.Write("case when ", Number, " < 0 then '-' else '' end");
        }

        public abstract void WriteSql(SqlWriter writer, SqlGenerator sqlGenerator);
    }
}
