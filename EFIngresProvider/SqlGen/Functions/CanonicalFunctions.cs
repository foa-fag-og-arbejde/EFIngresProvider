using System;
using System.Collections.Generic;
using System.Data.Common.CommandTrees;

namespace EFIngresProvider.SqlGen.Functions
{
    public static class CanonicalFunctions
    {
        private static readonly Dictionary<string, FunctionHandler> _canonicalFunctions = InitializeCanonicalFunctions();
        private static readonly FunctionHandler _defaultCanonicalFunction = new DefaultFunctionHandler();

        /// <summary>
        /// All canonical functions and their handlers
        /// </summary>
        private static Dictionary<string, FunctionHandler> InitializeCanonicalFunctions()
        {
            var functions = new Dictionary<string, FunctionHandler>(StringComparer.Ordinal);

            // Compare Functions
            functions.Add("LessThan", new CompareFunctionHandler("<"));
            functions.Add("LessThanOrEqual", new CompareFunctionHandler("<="));
            functions.Add("GreaterThan", new CompareFunctionHandler(">"));
            functions.Add("GreaterThanOrEqual", new CompareFunctionHandler(">="));

            // String Canonical Functions
            functions.Add("Concat", new NamedFunctionHandler("concat"));
            functions.Add("Contains", new ContainsHandler());
            functions.Add("EndsWith", new EndsWithHandler());
            functions.Add("IndexOf", new IndexOfHandler());
            functions.Add("Left", new NamedFunctionHandler("left"));
            functions.Add("Length", new NamedFunctionHandler("length"));
            functions.Add("LTrim", new NamedFunctionHandler("ltrim"));
            functions.Add("Replace", new NamedFunctionHandler("replace"));
            functions.Add("Reverse", new NotSupportedHandler());
            functions.Add("Right", new NamedFunctionHandler("right"));
            functions.Add("RTrim", new NamedFunctionHandler("rtrim"));
            functions.Add("Substring", new SubstringHandler());
            functions.Add("StartsWith", new StartsWithHandler());
            functions.Add("ToLower", new NamedFunctionHandler("lowercase"));
            functions.Add("ToUpper", new NamedFunctionHandler("uppercase"));
            functions.Add("Trim", new NamedFunctionHandler("trim"));
            functions.Add("Like", new LikeFunctionHandler());

            // Math Canonical Functions
            functions.Add("Abs", new NamedFunctionHandler("abs"));
            functions.Add("Ceiling", new NamedFunctionHandler("ceiling"));
            functions.Add("Floor", new NamedFunctionHandler("floor"));
            functions.Add("Power", new NamedFunctionHandler("power"));
            functions.Add("Round", new RoundHandler());
            functions.Add("Truncate", new NamedFunctionHandler("truncate"));

            // Date and Time Canonical Functions
            functions.Add("AddNanoseconds", new AddSecondsHandler { Divisor = 1000000000 });
            functions.Add("AddMicroseconds", new AddSecondsHandler { Divisor = 1000000 });
            functions.Add("AddMilliseconds", new AddSecondsHandler { Divisor = 1000 });
            functions.Add("AddSeconds", new AddSecondsHandler());
            functions.Add("AddMinutes", new AddMinutesHandler());
            functions.Add("AddHours", new AddHoursHandler());
            functions.Add("AddDays", new AddDaysHandler());
            functions.Add("AddMonths", new AddMonthsHandler());
            functions.Add("AddYears", new AddYearsHandler());
            functions.Add("CreateDateTime", new CreateDateTimeHandler());
            functions.Add("CreateDateTimeOffset", new CreateDateTimeOffsetHandler());
            functions.Add("CreateTime", new CreateTimeHandler());
            functions.Add("CurrentDateTime", new NamedFunctionHandler("local_timestamp"));
            functions.Add("CurrentDateTimeOffset", new NamedFunctionHandler("current_timestamp"));
            functions.Add("CurrentUtcDateTime", new NotSupportedHandler());

            functions.Add("Year", new DefaultFunctionHandler());
            functions.Add("Month", new DefaultFunctionHandler());
            functions.Add("Day", new DefaultFunctionHandler());
            functions.Add("Hour", new DefaultFunctionHandler());
            functions.Add("Minute", new DefaultFunctionHandler());
            functions.Add("Second", new DefaultFunctionHandler());
            functions.Add("Millisecond", new DefaultFunctionHandler());

            functions.Add("DayOfYear", new DayOfYearHandler());
            functions.Add("DiffNanoseconds", new DiffSecondFractionsHandler { Factor = 1000000000 });
            functions.Add("DiffMicroseconds", new DiffSecondFractionsHandler { Factor = 1000000 });
            functions.Add("DiffMilliseconds", new DiffSecondFractionsHandler { Factor = 1000 });
            functions.Add("DiffSeconds", new DiffTimeHandler { Unit = "second" });
            functions.Add("DiffMinutes", new DiffTimeHandler { Unit = "minute" });
            functions.Add("DiffHours", new DiffTimeHandler { Unit = "hour" });
            functions.Add("DiffDays", new DiffTimeHandler { Unit = "day" });
            functions.Add("DiffMonths", new DiffTimeHandler { Unit = "month" });
            functions.Add("DiffYears", new DiffTimeHandler { Unit = "year" });
            functions.Add("GetTotalOffsetMinutes", new NotSupportedHandler());
            functions.Add("TruncateTime", new TruncateTimeHandler());
            functions.Add("AddTime", new AddTimeFunctionHandler());
            functions.Add("SubtractTime", new SubtractTimeFunctionHandler());

            // Bitwise Canonical Functions
            functions.Add("BitWiseAnd", new NamedFunctionHandler("bit_and"));
            functions.Add("BitWiseNot", new NamedFunctionHandler("bit_not"));
            functions.Add("BitWiseOr", new NamedFunctionHandler("bit_or"));
            functions.Add("BitWiseXor", new NamedFunctionHandler("bit_xor"));

            // Other Canonical Functions
            functions.Add("NewGuid", new NotSupportedHandler());

            return functions;
        }

        /// <summary>
        /// Dispatches the special function processing to the appropriate handler
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static ISqlFragment Handle(SqlGenerator sqlGenerator, DbFunctionExpression e)
        {
            FunctionHandler function;
            if (_canonicalFunctions.TryGetValue(e.Function.Name, out function))
            {
                return function.HandleFunction(sqlGenerator, e);
            }
            return _defaultCanonicalFunction.HandleFunction(sqlGenerator, e);
        }

    }
}
