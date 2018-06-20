using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EFIngresProvider.Helpers
{
    public class IngresDateData
    {
        public static IngresDate? GetIngresDate(object data)
        {
            var ingresDateData = new IngresDateData(data);
            return ingresDateData.GetValue();
        }

        public IngresDateData(object data)
        {
            Data = data;
            IsNull = data.SqlDataIsNull();
            if (!IsNull)
            {
                // Force population of IsInterval
                StringValue = data.SqlDataGetString();
            }
            Value = data.GetWrappedField<string>("value");
            UseGmt = data.GetWrappedField<bool>("use_gmt");
            IsInterval = data.GetWrappedField<bool>("interval");
        }

        public IngresDate? GetValue()
        {
            if (IsNull)
            {
                return null;
            }
            else if (IsInterval)
            {
                TimeSpan interval;
                if (TryParseInterval(out interval))
                {
                    return new IngresDate(interval);
                }
                else
                {
                    return new IngresDate(TimeSpan.Zero);
                }
            }
            else if (string.IsNullOrWhiteSpace(Value))
            {
                return IngresDate.Empty;
            }
            var timestamp = Data.SqlDataGetTimestamp(null);
            if (timestamp.Kind == DateTimeKind.Utc && UseGmt)
            {
                timestamp = timestamp.ToLocalTime();
            }
            return new IngresDate(timestamp);
        }

        public object Data { get; private set; }
        public bool IsNull { get; private set; }
        public string Value { get; private set; }
        public bool IsInterval { get; private set; }
        public bool UseGmt { get; private set; }
        public string StringValue { get; private set; }

        private bool TryParseInterval(out TimeSpan result)
        {
            var hitKey = false;

            result = new TimeSpan(0);

            if (Value == null || Value.Length == 0)
            {
                return false;
            }

            TimeSpan span;
            if (TimeSpan.TryParse(Value, out span))
            {
                result = span;
                return true;
            }

            var negate = false;
            var unsigned_value = Value;
            if (Value.StartsWith("+"))
            {
                unsigned_value = Value.Substring(1);
            }
            else if (Value.StartsWith("-"))
            {
                unsigned_value = Value.Substring(1);
                negate = true;
            }

            var tokens = ScanDateTime(unsigned_value).ToList();
            var ticksPerYear = TimeSpan.TicksPerDay * 365;
            var ticksPerMonth = ticksPerYear / 12;

            // if "y-m" format
            if (tokens.Count == 3 && tokens[1] == "-")
            {
                if (IsNumber(tokens[0]) == false || IsNumber(tokens[2]) == false)
                {
                    return false;
                }
                result = CreateTimeSpan(long.Parse(tokens[0]) * ticksPerYear + long.Parse(tokens[2]) * ticksPerMonth, negate);
                return true;
            }

            var num = 0L;
            var ticks = 0L;
            var multiplier = 0L;
            var multiplier2 = 0L;

            foreach (string ss in tokens)
            {
                if (ss.Contains(":"))   // hh:mm:ss.mmmmmm item
                {
                    ticks += (num * TimeSpan.TicksPerDay);
                    ticks += TimeSpan.Parse(ss).Ticks;
                    num = 0L;
                    hitKey = true;  // all is well
                    continue;
                }

                if (Char.IsDigit(ss[0]))
                {
                    num = long.Parse(ss);
                    continue;
                }

                multiplier = 0L;

                string key = ss.ToLowerInvariant();
                string key1 = ss.Substring(0, 1);
                string key3 = (key.Length > 3) ? key.Substring(0, 3) : key;

                if (key1 == "y")
                {
                    multiplier = ticksPerYear;  // year
                    multiplier2 = ticksPerMonth;  // month
                }
                else if (key3 == "mon" || key3 == "mnt")
                {
                    multiplier = ticksPerMonth;  // month
                    multiplier2 = TimeSpan.TicksPerDay;  // day
                }
                else if (key1 == "d")
                {
                    multiplier = TimeSpan.TicksPerDay;  // day
                    multiplier2 = TimeSpan.TicksPerHour;  // hour
                }
                else if (key1 == "h")
                {
                    multiplier = TimeSpan.TicksPerHour;    // day
                    multiplier2 = TimeSpan.TicksPerMinute;  // minute
                }
                else if (key3 == "min")
                {
                    multiplier = TimeSpan.TicksPerMinute;  // minute
                    multiplier2 = TimeSpan.TicksPerSecond;  // second
                }
                else if (key1 == "s")
                {
                    multiplier = TimeSpan.TicksPerSecond;  // second
                    multiplier2 = 0;
                }

                if (multiplier > 0)
                {
                    ticks += (multiplier * num);
                    multiplier = 0L;
                    num = 0L;
                    hitKey = true;
                }
                continue;
            }  // end foreach (string ss in tokens)

            if (num != 0)
            {
                // if last number missing its units
                ticks += (multiplier2 * num);
            }

            if (!hitKey)
            {
                // if no Ingres keywords hit, return false
                return false;
            }

            result = CreateTimeSpan(ticks, negate);
            return true;
        }

        private TimeSpan CreateTimeSpan(long ticks, bool negate)
        {
            if (negate)
            {
                ticks = -ticks;
            }
            return new TimeSpan(ticks);
        }

        /*
        ** Name: ScanDateTime
        **
        ** Description:
        **	Scan the string and tokenize it.
        **
        ** Input:
        **	String with identifiers and numbers.
        **
        ** Output:
        **	None.
        **
        ** Returns:
        **	A collection list of strings.
        **
        ** History:
        **	17-Jan-07 (thoda04)
        **	    Created.
        */

        /// <summary>
        /// Scan the date/time/interval string and tokenize it.
        /// </summary>
        /// <param name="dateTimeString"></param>
        /// <returns>A StringCollection of token strings.</returns>
        static public IEnumerable<string> ScanDateTime(string dateTimeString)
        {
            var sbToken = new StringBuilder(15);
            var i = 0;

            // loop thru chars and build tokens
            while (i < dateTimeString.Length)
            {
                if (Char.IsWhiteSpace(dateTimeString[i]))
                {
                    i++;
                    continue;  // skip over whitespace
                }

                if (Char.IsNumber(dateTimeString[i]))
                {
                    char decimal_point = ':';
                    while ((i < dateTimeString.Length) &&
                           (Char.IsNumber(dateTimeString[i]) || dateTimeString[i] == ':' || dateTimeString[i] == decimal_point))
                    {
                        if (dateTimeString[i] == ':')
                        {
                            // if nn:nn:nn form
                            decimal_point = '.'; // enable decimal_pt
                        }
                        sbToken.Append(dateTimeString[i++]);
                    }
                }
                else if (Char.IsLetter(dateTimeString[i]))
                {
                    while ((i < dateTimeString.Length) && (Char.IsLetter(dateTimeString[i])))
                    {
                        sbToken.Append(dateTimeString[i++]);
                    }
                }
                else  // not number, not identifier, must be hyphen
                {
                    sbToken.Append(dateTimeString[i++]);
                }

                if (sbToken.Length > 0)
                {
                    yield return sbToken.ToString();
                }
                sbToken.Length = 0;
                continue;   // loop to next char for next token
            }  // end while (i < dateTimeString.Length) loop thru chars
        }

        private static bool IsNumber(string token)
        {
            return !string.IsNullOrEmpty(token) && token.All(c => Char.IsDigit(c));
        }
    }
}
