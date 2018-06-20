namespace EFIngresProvider.SqlGen.Functions
{
    public class AddYearsHandler : AddDateTimeHandler
    {
        protected override IntervalBase CreateInterval(ISqlFragment number)
        {
            return new IntervalYearToMonth(number) { Years = number };
        }
    }
}
