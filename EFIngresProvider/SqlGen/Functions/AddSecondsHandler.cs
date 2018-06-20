namespace EFIngresProvider.SqlGen.Functions
{
    public class AddSecondsHandler : AddDateTimeHandler
    {
        public AddSecondsHandler()
        {
            Divisor = 1;
        }

        public int Divisor { get; set; }

        protected override IntervalBase CreateInterval(ISqlFragment number)
        {
            if (Divisor == 1)
            {
                return new IntervalDayToSecond(number) { Seconds = number };
            }
            else
            {
                return new IntervalDayToSecond(number) { Seconds = new SqlBuilder("decimal(", number, ", 20, 9) / ", Divisor) };
            }
        }
    }
}
