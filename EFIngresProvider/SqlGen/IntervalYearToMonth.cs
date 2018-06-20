namespace EFIngresProvider.SqlGen
{
    public class IntervalYearToMonth : IntervalBase
    {
        public IntervalYearToMonth(ISqlFragment number) : base(number) { }

        public ISqlFragment Years { get; set; }
        public ISqlFragment Months { get; set; }

        public override void WriteSql(SqlWriter writer, SqlGenerator sqlGenerator)
        {
            writer.Write("interval_ytom(");
            WriteSign(writer);
            WriteIntervalPart(writer, sqlGenerator, Years);
            writer.Write(" + '-' + ");
            WriteIntervalPart(writer, sqlGenerator, Months);
            writer.Write(")");
        }
    }
}
