namespace EFIngresProvider.SqlGen.Functions
{
    public class AddDaysHandler : AddDateTimeHandler
    {
        protected override IntervalBase CreateInterval(ISqlFragment number)
        {
            return new IntervalDayToSecond(number) { Days = number };
        }
    }
}
