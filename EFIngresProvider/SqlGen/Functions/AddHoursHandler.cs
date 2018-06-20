namespace EFIngresProvider.SqlGen.Functions
{
    public class AddHoursHandler : AddDateTimeHandler
    {
        protected override IntervalBase CreateInterval(ISqlFragment number)
        {
            return new IntervalDayToSecond(number) { Hours = number };
        }
    }
}
