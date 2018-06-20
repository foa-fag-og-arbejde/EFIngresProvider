namespace EFIngresProvider.SqlGen
{
    public class IntervalDayToSecond : IntervalBase
    {
        public IntervalDayToSecond(ISqlFragment number) : base(number) { }

        public ISqlFragment Days { get; set; }
        public ISqlFragment Hours { get; set; }
        public ISqlFragment Minutes { get; set; }
        public ISqlFragment Seconds { get; set; }

        public override void WriteSql(SqlWriter writer, SqlGenerator sqlGenerator)
        {
            writer.Write("interval_dtos(");
            WriteSign(writer);
            WriteIntervalPart(writer, sqlGenerator, Days);
            writer.Write(" + ' ' + ");
            WriteIntervalPart(writer, sqlGenerator, Hours);
            writer.Write(" + ':' + ");
            WriteIntervalPart(writer, sqlGenerator, Minutes);
            writer.Write(" + ':' + ");
            WriteIntervalPart(writer, sqlGenerator, Seconds);
            writer.Write(")");
        }
    }
}
