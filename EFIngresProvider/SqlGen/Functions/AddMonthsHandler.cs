namespace EFIngresProvider.SqlGen.Functions
{
    public class AddMonthsHandler : AddDateTimeHandler
    {
        protected override IntervalBase CreateInterval(ISqlFragment number)
        {
            return new IntervalYearToMonth(number) { Months = number };
        }
    }
}
