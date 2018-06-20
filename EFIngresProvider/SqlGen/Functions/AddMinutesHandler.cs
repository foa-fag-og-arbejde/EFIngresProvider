namespace EFIngresProvider.SqlGen.Functions
{
    public class AddMinutesHandler : AddDateTimeHandler
    {
        protected override IntervalBase CreateInterval(ISqlFragment number)
        {
            return new IntervalDayToSecond(number) { Minutes = number };
        }
    }
}
