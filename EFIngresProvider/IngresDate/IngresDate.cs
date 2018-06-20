using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;

namespace EFIngresProvider
{
    public enum IngresDateKind
    {
        Empty = 0,
        DateTime = 1,
        TimeSpan = 2
    }

    [Serializable]
    public struct IngresDate : IComparable, IFormattable, ISerializable, IComparable<IngresDate>, IEquatable<IngresDate>
    {
        private readonly object _value;
        public readonly IngresDateKind IngresDateKind;
        public DateTime AsDateTime { get { return ToDateTime(this); } }
        public TimeSpan AsTimeSpan { get { return ToTimeSpan(this); } }
        public bool IsTimeSpan { get { return IngresDateKind == IngresDateKind.TimeSpan; } }
        public bool IsDateTime { get { return !IsTimeSpan; } }
        // public static bool EmptyIsGreatest = true;

        public static readonly DateTime IntervalDateTimeValue = new DateTime(1000, 1, 1, 0, 0, 0);
        public static readonly DateTime IntervalMaxDateTimeValue = IntervalDateTimeValue.AddYears(500);
        public static readonly DateTime EmptyDateTimeValue = new DateTime(9999, 12, 31, 23, 59, 59);
        public static readonly IngresDate Empty = new IngresDate(EmptyDateTimeValue, EmptyDateTimeValue);

        private DateTime DateTimeValue { get { return IsEmpty ? EmptyDateTimeValue : AsDateTime; } }
        public bool IsEmpty { get { return IngresDateKind == IngresDateKind.Empty; } }

        #region Constructors

        private IngresDate(DateTime value, DateTime emptyDateTimeValue, bool convertFromUtc = false)
        {
            if (convertFromUtc && value.Kind == DateTimeKind.Utc)
            {
                value = value.ToLocalTime();
            }

            if (value >= emptyDateTimeValue.AddYears(-1))
            {
                _value = emptyDateTimeValue;
                IngresDateKind = IngresDateKind.Empty;
            }
            else if (value <= IntervalMaxDateTimeValue)
            {
                _value = value - IntervalDateTimeValue;
                IngresDateKind = IngresDateKind.TimeSpan;
            }
            else
            {
                _value = value;
                IngresDateKind = IngresDateKind.DateTime;
            }
        }

        public IngresDate(DateTime? value, bool convertFromUtc = false)
            : this(value ?? EmptyDateTimeValue, EmptyDateTimeValue, convertFromUtc)
        {
        }

        public IngresDate(TimeSpan value)
        {
            _value = value;
            IngresDateKind = IngresDateKind.TimeSpan;
        }

        #endregion

        public static IngresDate Create(object value, bool convertFromUtc = false)
        {
            if (value is IngresDate)
            {
                return (IngresDate)value;
            }
            if (value is DateTime)
            {
                return new IngresDate((DateTime)value, convertFromUtc);
            }
            if (value is TimeSpan)
            {
                return new IngresDate((TimeSpan)value);
            }
            throw new NotSupportedException();
        }

        public bool IsDate
        {
            get { return !IsEmpty && IsDateTime && AsDateTime.Date == AsDateTime; }
        }

        public static DateTime ToDateTime(IngresDate value)
        {
            if (value.IsEmpty)
            {
                return EmptyDateTimeValue;
            }
            else if (value.IsTimeSpan)
            {
                return IntervalDateTimeValue + (TimeSpan)value._value;
            }
            else
            {
                return (DateTime)value._value;
            }
        }

        public static TimeSpan ToTimeSpan(IngresDate value)
        {
            if (value.IsEmpty)
            {
                return TimeSpan.Zero;
            }
            else if (value.IsTimeSpan)
            {
                return (TimeSpan)value._value;
            }
            else
            {
                var dateTime = (DateTime)value._value;
                return dateTime - dateTime.Date;
            }
        }

        private static IEnumerable<string> FormatTimeSpanParts(TimeSpan value)
        {
            if (value == TimeSpan.Zero)
            {
                yield return "0 hours";
            }
            else
            {
                if (value.Days != 0)
                {
                    yield return string.Format("{0} days", value.Days);
                }
                if (value.Hours != 0)
                {
                    yield return string.Format("{0} hrs", value.Hours);
                }
                if (value.Minutes != 0)
                {
                    yield return string.Format("{0} mins", value.Minutes);
                }
                if (value.Seconds != 0)
                {
                    yield return string.Format("{0} secs", value.Seconds);
                }
            }
        }

        public static string Format(IngresDate value)
        {
            switch (value.IngresDateKind)
            {
                case IngresDateKind.Empty:
                    return "date('')";
                case IngresDateKind.TimeSpan:
                    return string.Format("date('{0}')", string.Join(" ", FormatTimeSpanParts(value.AsTimeSpan)));
                default:
                    var localValue = value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;
                    if (localValue.IsDate)
                    {
                        return string.Format("date('{0:yyyy_MM_dd}')", localValue.AsDateTime);
                    }
                    else
                    {
                        return string.Format("date('{0:yyyy_MM_dd HH:mm:ss}')", value.AsDateTime);
                    }
            }
        }

        #region Implicit conversions

        public static implicit operator IngresDate(DateTime value)
        {
            return new IngresDate(value);
        }

        public static implicit operator IngresDate(DateTime? value)
        {
            return new IngresDate(value);
        }

        public static implicit operator DateTime(IngresDate value)
        {
            return value.AsDateTime;
        }

        public static implicit operator IngresDate(TimeSpan value)
        {
            return new IngresDate(value);
        }

        public static implicit operator TimeSpan(IngresDate value)
        {
            return value.AsTimeSpan;
        }

        #endregion

        #region DateTime properties and methods

        //
        // Summary:
        //     Gets the number of ticks that represent the date and time of this instance.
        //
        // Returns:
        //     The number of ticks that represent the date and time of this instance. The
        //     value is between IngresDate.MinValue.Ticks and IngresDate.MaxValue.Ticks.
        public long Ticks { get { return WrapFunctions(d => d.Ticks, t => t.Ticks, long.MinValue); } }

        //
        // Summary:
        //     Represents the largest possible value of EFIngresProvider.IngresDate. This field is read-only.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     EFIngresProvider.IngresDate.MaxValue is outside the range of the current culture's default
        //     calendar or of a specified culture's default calendar.
        public static readonly IngresDate MaxDateTimeValue = new IngresDate(DateTime.MaxValue);

        //
        // Summary:
        //     Represents the smallest possible value of EFIngresProvider.IngresDate. This field is
        //     read-only.
        public static readonly IngresDate MinDateTimeValue = new IngresDate(DateTime.MinValue);

        // Summary:
        //     Gets the date component of this instance.
        //
        // Returns:
        //     A new EFIngresProvider.IngresDate with the same date as this instance, and the time value
        //     set to 12:00:00 midnight (00:00:00).
        public IngresDate Date { get { return IsEmpty ? IngresDate.Empty : new IngresDate(AsDateTime.Date); } }

        //
        // Summary:
        //     Gets the day of the month represented by this instance.
        //
        // Returns:
        //     The day component, expressed as a value between 1 and 31.
        public int Day { get { return WrapFunctions(d => d.Day, 0); } }

        //
        // Summary:
        //     Gets the day of the week represented by this instance.
        //
        // Returns:
        //     A System.DayOfWeek enumerated constant that indicates the day of the week
        //     of this EFIngresProvider.IngresDate value.
        public DayOfWeek DayOfWeek { get { return WrapFunctions(x => x.DayOfWeek, DayOfWeek.Sunday); } }

        //
        // Summary:
        //     Gets the day of the year represented by this instance.
        //
        // Returns:
        //     The day of the year, expressed as a value between 1 and 366.
        public int DayOfYear { get { return WrapFunctions(x => x.DayOfYear, 0); } }
        //
        // Summary:
        //     Gets the hour component of the date represented by this instance.
        //
        // Returns:
        //     The hour component, expressed as a value between 0 and 23.
        public int Hour { get { return WrapFunctions(x => x.Hour, 0); } }
        //
        // Summary:
        //     Gets a value that indicates whether the time represented by this instance
        //     is based on local time, Coordinated Universal Time (UTC), or neither.
        //
        // Returns:
        //     One of the System.DateTimeKind values. The default is System.DateTimeKind.Unspecified.
        public DateTimeKind Kind { get { return WrapFunctions(x => x.Kind, DateTimeKind.Unspecified); } }
        //
        // Summary:
        //     Gets the milliseconds component of the date represented by this instance.
        //
        // Returns:
        //     The milliseconds component, expressed as a value between 0 and 999.
        public int Millisecond { get { return WrapFunctions(x => x.Millisecond, 0); } }

        //
        // Summary:
        //     Gets the minute component of the date represented by this instance.
        //
        // Returns:
        //     The minute component, expressed as a value between 0 and 59.
        public int Minute { get { return WrapFunctions(x => x.Minute, 0); } }
        //
        // Summary:
        //     Gets the month component of the date represented by this instance.
        //
        // Returns:
        //     The month component, expressed as a value between 1 and 12.
        public int Month { get { return WrapFunctions(x => x.Month, 0); } }
        //
        // Summary:
        //     Gets a EFIngresProvider.IngresDate object that is set to the current date and time on
        //     this computer, expressed as the local time.
        //
        // Returns:
        //     A EFIngresProvider.IngresDate whose value is the current local date and time.
        public static IngresDate Now { get { return new IngresDate(DateTime.Now); } }
        //
        // Summary:
        //     Gets the seconds component of the date represented by this instance.
        //
        // Returns:
        //     The seconds, between 0 and 59.
        public int Second { get { return WrapFunctions(x => x.Second, 0); } }
        //
        // Summary:
        //     Gets the time of day for this instance.
        //
        // Returns:
        //     A System.TimeSpan that represents the fraction of the day that has elapsed
        //     since midnight.
        public TimeSpan TimeOfDay { get { return WrapFunctions(d => d.TimeOfDay, Fail<TimeSpan>, Fail<TimeSpan>); } }
        //
        // Summary:
        //     Gets the current date.
        //
        // Returns:
        //     A EFIngresProvider.IngresDate set to today's date, with the time component set to 00:00:00.
        public static IngresDate Today { get { return new IngresDate(DateTime.Today); } }
        //
        // Summary:
        //     Gets a EFIngresProvider.IngresDate object that is set to the current date and time on
        //     this computer, expressed as the Coordinated Universal Time (UTC).
        //
        // Returns:
        //     A EFIngresProvider.IngresDate whose value is the current UTC date and time.
        public static IngresDate UtcNow { get { return new IngresDate(DateTime.UtcNow); } }
        //
        // Summary:
        //     Gets the year component of the date represented by this instance.
        //
        // Returns:
        //     The year, between 1 and 9999.
        public int Year { get { return WrapFunctions(x => x.Year, 0); } }

        //
        // Summary:
        //     Returns a new EFIngresProvider.IngresDate that adds the specified number of days to the
        //     value of this instance.
        //
        // Parameters:
        //   value:
        //     A number of whole and fractional days. The value parameter can be negative
        //     or positive.
        //
        // Returns:
        //     A EFIngresProvider.IngresDate whose value is the sum of the date and time represented
        //     by this instance and the number of days represented by value.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     The resulting EFIngresProvider.IngresDate is less than EFIngresProvider.IngresDate.MinValue or greater
        //     than EFIngresProvider.IngresDate.MaxValue.
        public IngresDate AddDays(double value)
        {
            return CreateIngresDate(d => d.AddDays(value), t => t.Add(TimeSpan.FromDays(value)), Empty);
        }

        //
        // Summary:
        //     Returns a new EFIngresProvider.IngresDate that adds the specified number of hours to
        //     the value of this instance.
        //
        // Parameters:
        //   value:
        //     A number of whole and fractional hours. The value parameter can be negative
        //     or positive.
        //
        // Returns:
        //     A EFIngresProvider.IngresDate whose value is the sum of the date and time represented
        //     by this instance and the number of hours represented by value.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     The resulting EFIngresProvider.IngresDate is less than EFIngresProvider.IngresDate.MinValue or greater
        //     than EFIngresProvider.IngresDate.MaxValue.
        public IngresDate AddHours(double value)
        {
            return CreateIngresDate(d => d.AddHours(value), t => t.Add(TimeSpan.FromHours(value)), Empty);
        }
        //
        // Summary:
        //     Returns a new EFIngresProvider.IngresDate that adds the specified number of milliseconds
        //     to the value of this instance.
        //
        // Parameters:
        //   value:
        //     A number of whole and fractional milliseconds. The value parameter can be
        //     negative or positive. Note that this value is rounded to the nearest integer.
        //
        // Returns:
        //     A EFIngresProvider.IngresDate whose value is the sum of the date and time represented
        //     by this instance and the number of milliseconds represented by value.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     The resulting EFIngresProvider.IngresDate is less than EFIngresProvider.IngresDate.MinValue or greater
        //     than EFIngresProvider.IngresDate.MaxValue.
        public IngresDate AddMilliseconds(double value)
        {
            return CreateIngresDate(d => d.AddMilliseconds(value), t => t.Add(TimeSpan.FromMilliseconds(value)), Empty);
        }
        //
        // Summary:
        //     Returns a new EFIngresProvider.IngresDate that adds the specified number of minutes to
        //     the value of this instance.
        //
        // Parameters:
        //   value:
        //     A number of whole and fractional minutes. The value parameter can be negative
        //     or positive.
        //
        // Returns:
        //     A EFIngresProvider.IngresDate whose value is the sum of the date and time represented
        //     by this instance and the number of minutes represented by value.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     The resulting EFIngresProvider.IngresDate is less than EFIngresProvider.IngresDate.MinValue or greater
        //     than EFIngresProvider.IngresDate.MaxValue.
        public IngresDate AddMinutes(double value)
        {
            return CreateIngresDate(d => d.AddMinutes(value), t => t.Add(TimeSpan.FromMinutes(value)), Empty);
        }
        //
        // Summary:
        //     Returns a new EFIngresProvider.IngresDate that adds the specified number of months to
        //     the value of this instance.
        //
        // Parameters:
        //   months:
        //     A number of months. The months parameter can be negative or positive.
        //
        // Returns:
        //     A EFIngresProvider.IngresDate whose value is the sum of the date and time represented
        //     by this instance and months.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     The resulting EFIngresProvider.IngresDate is less than EFIngresProvider.IngresDate.MinValue or greater
        //     than EFIngresProvider.IngresDate.MaxValue.-or- months is less than -120,000 or greater
        //     than 120,000.
        public IngresDate AddMonths(int months)
        {
            return CreateIngresDate(d => d.AddMonths(months), Fail<TimeSpan>, Empty);
        }

        //
        // Summary:
        //     Returns a new EFIngresProvider.IngresDate that adds the specified number of seconds to
        //     the value of this instance.
        //
        // Parameters:
        //   value:
        //     A number of whole and fractional seconds. The value parameter can be negative
        //     or positive.
        //
        // Returns:
        //     A EFIngresProvider.IngresDate whose value is the sum of the date and time represented
        //     by this instance and the number of seconds represented by value.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     The resulting EFIngresProvider.IngresDate is less than EFIngresProvider.IngresDate.MinValue or greater
        //     than EFIngresProvider.IngresDate.MaxValue.
        public IngresDate AddSeconds(double value)
        {
            return CreateIngresDate(d => d.AddSeconds(value), t => t.Add(TimeSpan.FromSeconds(value)), Empty);
        }
        //
        // Summary:
        //     Returns a new EFIngresProvider.IngresDate that adds the specified number of ticks to
        //     the value of this instance.
        //
        // Parameters:
        //   value:
        //     A number of 100-nanosecond ticks. The value parameter can be positive or
        //     negative.
        //
        // Returns:
        //     A EFIngresProvider.IngresDate whose value is the sum of the date and time represented
        //     by this instance and the time represented by value.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     The resulting EFIngresProvider.IngresDate is less than EFIngresProvider.IngresDate.MinValue or greater
        //     than EFIngresProvider.IngresDate.MaxValue.
        public IngresDate AddTicks(long value)
        {
            return CreateIngresDate(d => d.AddTicks(value), t => t.Add(TimeSpan.FromTicks(value)), Empty);
        }
        //
        // Summary:
        //     Returns a new EFIngresProvider.IngresDate that adds the specified number of years to
        //     the value of this instance.
        //
        // Parameters:
        //   value:
        //     A number of years. The value parameter can be negative or positive.
        //
        // Returns:
        //     A EFIngresProvider.IngresDate whose value is the sum of the date and time represented
        //     by this instance and the number of years represented by value.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     value or the resulting EFIngresProvider.IngresDate is less than EFIngresProvider.IngresDate.MinValue
        //     or greater than EFIngresProvider.IngresDate.MaxValue.
        public IngresDate AddYears(int value)
        {
            return CreateIngresDate(d => d.AddYears(value), Fail<TimeSpan>, Empty);
        }
        //
        // Summary:
        //     Returns the number of days in the specified month and year.
        //
        // Parameters:
        //   year:
        //     The year.
        //
        //   month:
        //     The month (a number ranging from 1 to 12).
        //
        // Returns:
        //     The number of days in month for the specified year.For example, if month
        //     equals 2 for February, the return value is 28 or 29 depending upon whether
        //     year is a leap year.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     month is less than 1 or greater than 12.-or-year is less than 1 or greater
        //     than 9999.
        public static int DaysInMonth(int year, int month)
        {
            return DateTime.DaysInMonth(year, month);
        }
        //
        // Summary:
        //     Deserializes a 64-bit binary value and recreates an original serialized EFIngresProvider.IngresDate
        //     object.
        //
        // Parameters:
        //   dateData:
        //     A 64-bit signed integer that encodes the EFIngresProvider.IngresDate.Kind property in
        //     a 2-bit field and the EFIngresProvider.IngresDate.Ticks property in a 62-bit field.
        //
        // Returns:
        //     A EFIngresProvider.IngresDate object that is equivalent to the EFIngresProvider.IngresDate object
        //     that was serialized by the EFIngresProvider.IngresDate.ToBinary() method.
        //
        // Exceptions:
        //   System.ArgumentException:
        //     dateData is less than EFIngresProvider.IngresDate.MinValue or greater than EFIngresProvider.IngresDate.MaxValue.
        public static IngresDate FromBinary(long dateData)
        {
            return dateData == long.MinValue ? IngresDate.Empty : new IngresDate(DateTime.FromBinary(dateData));
        }
        //
        // Summary:
        //     Converts the specified Windows file time to an equivalent local time.
        //
        // Parameters:
        //   fileTime:
        //     A Windows file time expressed in ticks.
        //
        // Returns:
        //     A EFIngresProvider.IngresDate object that represents a local time equivalent to the date
        //     and time represented by the fileTime parameter.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     fileTime is less than 0 or represents a time greater than EFIngresProvider.IngresDate.MaxValue.
        public static IngresDate FromFileTime(long fileTime)
        {
            return new IngresDate(DateTime.FromFileTime(fileTime));
        }
        //
        // Summary:
        //     Converts the specified Windows file time to an equivalent UTC time.
        //
        // Parameters:
        //   fileTime:
        //     A Windows file time expressed in ticks.
        //
        // Returns:
        //     A EFIngresProvider.IngresDate object that represents a UTC time equivalent to the date
        //     and time represented by the fileTime parameter.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     fileTime is less than 0 or represents a time greater than EFIngresProvider.IngresDate.MaxValue.
        public static IngresDate FromFileTimeUtc(long fileTime)
        {
            return new IngresDate(DateTime.FromFileTimeUtc(fileTime));
        }
        //
        // Summary:
        //     Returns a EFIngresProvider.IngresDate equivalent to the specified OLE Automation Date.
        //
        // Parameters:
        //   d:
        //     An OLE Automation Date value.
        //
        // Returns:
        //     A EFIngresProvider.IngresDate that represents the same date and time as d.
        //
        // Exceptions:
        //   System.ArgumentException:
        //     The date is not a valid OLE Automation Date value.
        public static IngresDate FromOADate(double d)
        {
            return new IngresDate(DateTime.FromOADate(d));
        }
        //
        // Summary:
        //     Converts the value of this instance to all the string representations supported
        //     by the standard EFIngresProvider.IngresDate format specifiers.
        //
        // Returns:
        //     A string array where each element is the representation of the value of this
        //     instance formatted with one of the standard EFIngresProvider.IngresDate formatting specifiers.
        public string[] GetDateTimeFormats()
        {
            return WrapFunctions(x => x.GetDateTimeFormats(), new string[] { "" });
        }
        //
        // Summary:
        //     Converts the value of this instance to all the string representations supported
        //     by the specified standard EFIngresProvider.IngresDate format specifier.
        //
        // Parameters:
        //   format:
        //     A standard date and time format string. (See Standard Date and Time Format
        //     Strings.)
        //
        // Returns:
        //     A string array where each element is the representation of the value of this
        //     instance formatted with the format standard EFIngresProvider.IngresDate formatting specifier.
        //
        // Exceptions:
        //   System.FormatException:
        //     format is not a valid standard date and time format specifier character.
        public string[] GetDateTimeFormats(char format)
        {
            return WrapFunctions(x => x.GetDateTimeFormats(format), new string[] { "" });
        }
        //
        // Summary:
        //     Converts the value of this instance to all the string representations supported
        //     by the standard EFIngresProvider.IngresDate format specifiers and the specified culture-specific
        //     formatting information.
        //
        // Parameters:
        //   provider:
        //     An object that supplies culture-specific formatting information about this
        //     instance.
        //
        // Returns:
        //     A string array where each element is the representation of the value of this
        //     instance formatted with one of the standard EFIngresProvider.IngresDate formatting specifiers.
        public string[] GetDateTimeFormats(IFormatProvider provider)
        {
            return WrapFunctions(x => x.GetDateTimeFormats(provider), new string[] { "" });
        }
        //
        // Summary:
        //     Converts the value of this instance to all the string representations supported
        //     by the specified standard EFIngresProvider.IngresDate format specifier and culture-specific
        //     formatting information.
        //
        // Parameters:
        //   format:
        //     A date and time format string.
        //
        //   provider:
        //     An object that supplies culture-specific formatting information about this
        //     instance.
        //
        // Returns:
        //     A string array where each element is the representation of the value of this
        //     instance formatted with one of the standard EFIngresProvider.IngresDate formatting specifiers.
        //
        // Exceptions:
        //   System.FormatException:
        //     format is not a valid standard date and time format specifier character.
        public string[] GetDateTimeFormats(char format, IFormatProvider provider)
        {
            return WrapFunctions(x => x.GetDateTimeFormats(format, provider), new string[] { "" });
        }
        //
        // Summary:
        //     Returns the System.TypeCode for value type EFIngresProvider.IngresDate.
        //
        // Returns:
        //     The enumerated constant, System.TypeCode.IngresDate.
        public TypeCode GetTypeCode()
        {
            return WrapFunctions(x => x.GetTypeCode(), TypeCode.Empty);
        }
        //
        // Summary:
        //     Indicates whether this instance of EFIngresProvider.IngresDate is within the Daylight
        //     Saving Time range for the current time zone.
        //
        // Returns:
        //     true if EFIngresProvider.IngresDate.Kind is System.DateTimeKind.Local or System.DateTimeKind.Unspecified
        //     and the value of this instance of EFIngresProvider.IngresDate is within the Daylight
        //     Saving Time range for the current time zone. false if EFIngresProvider.IngresDate.Kind
        //     is System.DateTimeKind.Utc.
        public bool IsDaylightSavingTime()
        {
            return WrapFunctions(d => d.IsDaylightSavingTime(), false);
        }
        //
        // Summary:
        //     Returns an indication whether the specified year is a leap year.
        //
        // Parameters:
        //   year:
        //     A 4-digit year.
        //
        // Returns:
        //     true if year is a leap year; otherwise, false.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     year is less than 1 or greater than 9999.
        public static bool IsLeapYear(int year)
        {
            return DateTime.IsLeapYear(year);
        }
        //
        // Summary:
        //     Creates a new EFIngresProvider.IngresDate object that has the same number of ticks as
        //     the specified EFIngresProvider.IngresDate, but is designated as either local time, Coordinated
        //     Universal Time (UTC), or neither, as indicated by the specified System.DateTimeKind
        //     value.
        //
        // Parameters:
        //   value:
        //     A date and time.
        //
        //   kind:
        //     One of the enumeration values that indicates whether the new object represents
        //     local time, UTC, or neither.
        //
        // Returns:
        //     A new object that has the same number of ticks as the object represented
        //     by the value parameter and the System.DateTimeKind value specified by the
        //     kind parameter.
        public static IngresDate SpecifyKind(IngresDate value, DateTimeKind kind)
        {
            return value.CreateIngresDate(d => DateTime.SpecifyKind(d, kind), t => t, Empty);
        }
        //
        // Summary:
        //     Serializes the current EFIngresProvider.IngresDate object to a 64-bit binary value that
        //     subsequently can be used to recreate the EFIngresProvider.IngresDate object.
        //
        // Returns:
        //     A 64-bit signed integer that encodes the EFIngresProvider.IngresDate.Kind and EFIngresProvider.IngresDate.Ticks
        //     properties.
        public long ToBinary()
        {
            return WrapFunctions(x => x.ToBinary(), long.MinValue);
        }
        //
        // Summary:
        //     Converts the value of the current EFIngresProvider.IngresDate object to a Windows file
        //     time.
        //
        // Returns:
        //     The value of the current EFIngresProvider.IngresDate object expressed as a Windows file
        //     time.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     The resulting file time would represent a date and time before 12:00 midnight
        //     January 1, 1601 C.E. UTC.
        public long ToFileTime()
        {
            return WrapFunctions(d => d.ToFileTime(), Fail<long>, Fail<long>);
        }
        //
        // Summary:
        //     Converts the value of the current EFIngresProvider.IngresDate object to a Windows file
        //     time.
        //
        // Returns:
        //     The value of the current EFIngresProvider.IngresDate object expressed as a Windows file
        //     time.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     The resulting file time would represent a date and time before 12:00 midnight
        //     January 1, 1601 C.E. UTC.
        public long ToFileTimeUtc()
        {
            return WrapFunctions(d => d.ToFileTimeUtc(), Fail<long>, Fail<long>);
        }
        //
        // Summary:
        //     Converts the value of the current EFIngresProvider.IngresDate object to local time.
        //
        // Returns:
        //     A EFIngresProvider.IngresDate object whose EFIngresProvider.IngresDate.Kind property is System.DateTimeKind.Local,
        //     and whose value is the local time equivalent to the value of the current
        //     EFIngresProvider.IngresDate object, or EFIngresProvider.IngresDate.MaxValue if the converted value
        //     is too large to be represented by a EFIngresProvider.IngresDate object, or EFIngresProvider.IngresDate.MinValue
        //     if the converted value is too small to be represented as a EFIngresProvider.IngresDate
        //     object.
        public IngresDate ToLocalTime()
        {
            return CreateIngresDate(d => d.ToLocalTime(), t => t, Empty);
        }
        //
        // Summary:
        //     Converts the value of this instance to the equivalent OLE Automation date.
        //
        // Returns:
        //     A double-precision floating-point number that contains an OLE Automation
        //     date equivalent to the value of this instance.
        //
        // Exceptions:
        //   System.OverflowException:
        //     The value of this instance cannot be represented as an OLE Automation Date.
        public double ToOADate()
        {
            return WrapFunctions(d => d.ToOADate(), Fail<double>, Fail<double>);
        }
        //
        // Summary:
        //     Converts the value of the current EFIngresProvider.IngresDate object to Coordinated Universal
        //     Time (UTC).
        //
        // Returns:
        //     A EFIngresProvider.IngresDate object whose EFIngresProvider.IngresDate.Kind property is System.DateTimeKind.Utc,
        //     and whose value is the UTC equivalent to the value of the current EFIngresProvider.IngresDate
        //     object, or EFIngresProvider.IngresDate.MaxValue if the converted value is too large to
        //     be represented by a EFIngresProvider.IngresDate object, or EFIngresProvider.IngresDate.MinValue if
        //     the converted value is too small to be represented by a EFIngresProvider.IngresDate object.
        public IngresDate ToUniversalTime()
        {
            return CreateIngresDate(d => d.ToUniversalTime(), t => t, Empty);
        }

        #endregion

        #region TimeSpan properties and methods

        // Summary:
        //     Represents the number of ticks in 1 day. This field is constant.
        public const long TicksPerDay = TimeSpan.TicksPerDay;
        //
        // Summary:
        //     Represents the number of ticks in 1 hour. This field is constant.
        public const long TicksPerHour = TimeSpan.TicksPerHour;
        //
        // Summary:
        //     Represents the number of ticks in 1 millisecond. This field is constant.
        public const long TicksPerMillisecond = TimeSpan.TicksPerMillisecond;
        //
        // Summary:
        //     Represents the number of ticks in 1 minute. This field is constant.
        public const long TicksPerMinute = TimeSpan.TicksPerMinute;
        //
        // Summary:
        //     Represents the number of ticks in 1 second.
        public const long TicksPerSecond = TimeSpan.TicksPerSecond;

        // Summary:
        //     Represents the maximum System.IngresDate value. This field is read-only.
        public static readonly IngresDate MaxTimeSpanValue = new IngresDate(TimeSpan.MaxValue);
        //
        // Summary:
        //     Represents the minimum System.IngresDate value. This field is read-only.
        public static readonly IngresDate MinTimeSpanValue = new IngresDate(TimeSpan.MinValue);
        //
        // Summary:
        //     Represents the zero System.TimeSpan value. This field is read-only.
        public static readonly IngresDate Zero = new IngresDate(TimeSpan.Zero);

        // Summary:
        //     Gets the days component of the time interval represented by the current System.TimeSpan
        //     structure.
        //
        // Returns:
        //     The day component of this instance. The return value can be positive or negative.
        public int Days { get { return WrapFunctions(Fail<int>, t => t.Days, 0); } }
        //
        // Summary:
        //     Gets the hours component of the time interval represented by the current
        //     System.TimeSpan structure.
        //
        // Returns:
        //     The hour component of the current System.TimeSpan structure. The return value
        //     ranges from -23 through 23.
        public int Hours { get { return WrapFunctions(Fail<int>, t => t.Hours, 0); } }
        //
        // Summary:
        //     Gets the milliseconds component of the time interval represented by the current
        //     System.TimeSpan structure.
        //
        // Returns:
        //     The millisecond component of the current System.TimeSpan structure. The return
        //     value ranges from -999 through 999.
        public int Milliseconds { get { return WrapFunctions(Fail<int>, t => t.Milliseconds, 0); } }
        //
        // Summary:
        //     Gets the minutes component of the time interval represented by the current
        //     System.TimeSpan structure.
        //
        // Returns:
        //     The minute component of the current System.TimeSpan structure. The return
        //     value ranges from -59 through 59.
        public int Minutes { get { return WrapFunctions(Fail<int>, t => t.Minutes, 0); } }
        //
        // Summary:
        //     Gets the seconds component of the time interval represented by the current
        //     System.TimeSpan structure.
        //
        // Returns:
        //     The second component of the current System.TimeSpan structure. The return
        //     value ranges from -59 through 59.
        public int Seconds { get { return WrapFunctions(Fail<int>, t => t.Seconds, 0); } }
        //
        // Summary:
        //     Gets the value of the current System.TimeSpan structure expressed in whole
        //     and fractional days.
        //
        // Returns:
        //     The total number of days represented by this instance.
        public double TotalDays { get { return WrapFunctions(Fail<double>, t => t.TotalDays, 0); } }
        //
        // Summary:
        //     Gets the value of the current System.TimeSpan structure expressed in whole
        //     and fractional hours.
        //
        // Returns:
        //     The total number of hours represented by this instance.
        public double TotalHours { get { return WrapFunctions(Fail<double>, t => t.TotalHours, 0); } }
        //
        // Summary:
        //     Gets the value of the current System.TimeSpan structure expressed in whole
        //     and fractional milliseconds.
        //
        // Returns:
        //     The total number of milliseconds represented by this instance.
        public double TotalMilliseconds { get { return WrapFunctions(Fail<double>, t => t.TotalMilliseconds, 0); } }
        //
        // Summary:
        //     Gets the value of the current System.TimeSpan structure expressed in whole
        //     and fractional minutes.
        //
        // Returns:
        //     The total number of minutes represented by this instance.
        public double TotalMinutes { get { return WrapFunctions(Fail<double>, t => t.TotalMinutes, 0); } }
        //
        // Summary:
        //     Gets the value of the current System.TimeSpan structure expressed in whole
        //     and fractional seconds.
        //
        // Returns:
        //     The total number of seconds represented by this instance.
        public double TotalSeconds { get { return WrapFunctions(Fail<double>, t => t.TotalSeconds, 0); } }

        //
        // Summary:
        //     Returns a new System.TimeSpan object whose value is the absolute value of
        //     the current System.TimeSpan object.
        //
        // Returns:
        //     A new object whose value is the absolute value of the current System.TimeSpan
        //     object.
        //
        // Exceptions:
        //   System.OverflowException:
        //     The value of this instance is System.TimeSpan.MinValue.
        public IngresDate Duration()
        {
            return CreateIngresDate(Fail<DateTime>, t => t.Duration(), Fail<IngresDate>);
        }

        //
        // Summary:
        //     Returns a System.TimeSpan that represents a specified number of days, where
        //     the specification is accurate to the nearest millisecond.
        //
        // Parameters:
        //   value:
        //     A number of days, accurate to the nearest millisecond.
        //
        // Returns:
        //     An object that represents value.
        //
        // Exceptions:
        //   System.OverflowException:
        //     value is less than System.TimeSpan.MinValue or greater than System.TimeSpan.MaxValue.
        //     -or-value is System.Double.PositiveInfinity.-or-value is System.Double.NegativeInfinity.
        //
        //   System.ArgumentException:
        //     value is equal to System.Double.NaN.
        public static IngresDate FromDays(double value)
        {
            return new IngresDate(TimeSpan.FromDays(value));
        }
        //
        // Summary:
        //     Returns a System.TimeSpan that represents a specified number of hours, where
        //     the specification is accurate to the nearest millisecond.
        //
        // Parameters:
        //   value:
        //     A number of hours accurate to the nearest millisecond.
        //
        // Returns:
        //     An object that represents value.
        //
        // Exceptions:
        //   System.OverflowException:
        //     value is less than System.TimeSpan.MinValue or greater than System.TimeSpan.MaxValue.
        //     -or-value is System.Double.PositiveInfinity.-or-value is System.Double.NegativeInfinity.
        //
        //   System.ArgumentException:
        //     value is equal to System.Double.NaN.
        public static IngresDate FromHours(double value)
        {
            return new IngresDate(TimeSpan.FromHours(value));
        }
        //
        // Summary:
        //     Returns a System.TimeSpan that represents a specified number of milliseconds.
        //
        // Parameters:
        //   value:
        //     A number of milliseconds.
        //
        // Returns:
        //     An object that represents value.
        //
        // Exceptions:
        //   System.OverflowException:
        //     value is less than System.TimeSpan.MinValue or greater than System.TimeSpan.MaxValue.-or-value
        //     is System.Double.PositiveInfinity.-or-value is System.Double.NegativeInfinity.
        //
        //   System.ArgumentException:
        //     value is equal to System.Double.NaN.
        public static IngresDate FromMilliseconds(double value)
        {
            return new IngresDate(TimeSpan.FromMilliseconds(value));
        }
        //
        // Summary:
        //     Returns a System.TimeSpan that represents a specified number of minutes,
        //     where the specification is accurate to the nearest millisecond.
        //
        // Parameters:
        //   value:
        //     A number of minutes, accurate to the nearest millisecond.
        //
        // Returns:
        //     An object that represents value.
        //
        // Exceptions:
        //   System.OverflowException:
        //     value is less than System.TimeSpan.MinValue or greater than System.TimeSpan.MaxValue.-or-value
        //     is System.Double.PositiveInfinity.-or-value is System.Double.NegativeInfinity.
        //
        //   System.ArgumentException:
        //     value is equal to System.Double.NaN.
        public static IngresDate FromMinutes(double value)
        {
            return new IngresDate(TimeSpan.FromMinutes(value));
        }
        //
        // Summary:
        //     Returns a System.TimeSpan that represents a specified number of seconds,
        //     where the specification is accurate to the nearest millisecond.
        //
        // Parameters:
        //   value:
        //     A number of seconds, accurate to the nearest millisecond.
        //
        // Returns:
        //     An object that represents value.
        //
        // Exceptions:
        //   System.OverflowException:
        //     value is less than System.TimeSpan.MinValue or greater than System.TimeSpan.MaxValue.-or-value
        //     is System.Double.PositiveInfinity.-or-value is System.Double.NegativeInfinity.
        //
        //   System.ArgumentException:
        //     value is equal to System.Double.NaN.
        public static IngresDate FromSeconds(double value)
        {
            return new IngresDate(TimeSpan.FromSeconds(value));
        }
        //
        // Summary:
        //     Returns a System.TimeSpan that represents a specified time, where the specification
        //     is in units of ticks.
        //
        // Parameters:
        //   value:
        //     A number of ticks that represent a time.
        //
        // Returns:
        //     An object that represents value.
        public static IngresDate FromTicks(long value)
        {
            return new IngresDate(TimeSpan.FromTicks(value));
        }
        //
        // Summary:
        //     Returns a System.TimeSpan whose value is the negated value of this instance.
        //
        // Returns:
        //     The same numeric value as this instance, but with the opposite sign.
        //
        // Exceptions:
        //   System.OverflowException:
        //     The negated value of this instance cannot be represented by a System.TimeSpan;
        //     that is, the value of this instance is System.TimeSpan.MinValue.
        public IngresDate Negate()
        {
            return CreateIngresDate(Fail<DateTime>, t => t.Negate(), Fail<IngresDate>);
        }

        #endregion

        #region Arithmatic

        //
        // Summary:
        //     Returns the specified instance of System.TimeSpan.
        //
        // Parameters:
        //   t:
        //     The time interval to return.
        //
        // Returns:
        //     The time interval specified by t.
        public static IngresDate operator +(IngresDate t)
        {
            return t;
        }

        // Summary:
        //     Returns a System.TimeSpan whose value is the negated value of the specified
        //     instance.
        //
        // Parameters:
        //   t:
        //     The time interval to be negated.
        //
        // Returns:
        //     An object that has the same numeric value as this instance, but the opposite
        //     sign.
        //
        // Exceptions:
        //   System.OverflowException:
        //     The negated value of this instance cannot be represented by a System.TimeSpan;
        //     that is, the value of this instance is System.TimeSpan.MinValue.
        public static IngresDate operator -(IngresDate t)
        {
            return t.Negate();
        }

        // Summary:
        //     Subtracts a specified date and time from another specified date and time
        //     and returns a time interval.
        //
        // Parameters:
        //   d1:
        //     A EFIngresProvider.IngresDate (the minuend).
        //
        //   d2:
        //     A EFIngresProvider.IngresDate (the subtrahend).
        //
        // Returns:
        //     A System.TimeSpan that is the time interval between d1 and d2; that is, d1
        //     minus d2.
        public static IngresDate operator -(IngresDate d1, IngresDate d2)
        {
            return d1.Subtract(d2);
        }

        //
        // Summary:
        //     Subtracts a specified time interval from a specified date and time and returns
        //     a new date and time.
        //
        // Parameters:
        //   d:
        //     A EFIngresProvider.IngresDate.
        //
        //   t:
        //     A System.TimeSpan.
        //
        // Returns:
        //     A EFIngresProvider.IngresDate whose value is the value of d minus the value of t.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     The resulting EFIngresProvider.IngresDate is less than EFIngresProvider.IngresDate.MinValue or greater
        //     than EFIngresProvider.IngresDate.MaxValue.
        public static IngresDate operator -(IngresDate d, TimeSpan t)
        {
            return d.Subtract(t);
        }

        //
        // Summary:
        //     Determines whether two specified instances of EFIngresProvider.IngresDate are not equal.
        //
        // Parameters:
        //   d1:
        //     A EFIngresProvider.IngresDate.
        //
        //   d2:
        //     A EFIngresProvider.IngresDate.
        //
        // Returns:
        //     true if d1 and d2 do not represent the same date and time; otherwise, false.
        public static bool operator !=(IngresDate d1, IngresDate d2)
        {
            return !Equals(d1, d2);
        }

        public static bool operator !=(DateTime d1, IngresDate d2)
        {
            return !Equals(d1, d2);
        }

        public static bool operator !=(IngresDate d1, DateTime d2)
        {
            return !Equals(d1, d2);
        }

        public static bool operator !=(TimeSpan d1, IngresDate d2)
        {
            return !Equals(d1, d2);
        }

        public static bool operator !=(IngresDate d1, TimeSpan d2)
        {
            return !Equals(d1, d2);
        }

        //
        // Summary:
        //     Adds a specified time interval to a specified date and time, yielding a new
        //     date and time.
        //
        // Parameters:
        //   d:
        //     A EFIngresProvider.IngresDate.
        //
        //   t:
        //     A System.TimeSpan.
        //
        // Returns:
        //     A EFIngresProvider.IngresDate that is the sum of the values of d and t.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     The resulting EFIngresProvider.IngresDate is less than EFIngresProvider.IngresDate.MinValue or greater
        //     than EFIngresProvider.IngresDate.MaxValue.
        public static IngresDate operator +(IngresDate d, TimeSpan t)
        {
            return d.Add(t);
        }

        public static IngresDate operator +(IngresDate d, IngresDate t)
        {
            return d.Add(t);
        }

        //
        // Summary:
        //     Determines whether one specified EFIngresProvider.IngresDate is less than another specified
        //     EFIngresProvider.IngresDate.
        //
        // Parameters:
        //   t1:
        //     A EFIngresProvider.IngresDate.
        //
        //   t2:
        //     A EFIngresProvider.IngresDate.
        //
        // Returns:
        //     true if t1 is less than t2; otherwise, false.
        public static bool operator <(IngresDate t1, IngresDate t2)
        {
            return Compare(t1, t2) < 0;
        }

        public static bool operator <(DateTime t1, IngresDate t2)
        {
            return Compare(t1, t2) < 0;
        }

        public static bool operator <(IngresDate t1, DateTime t2)
        {
            return Compare(t1, t2) < 0;
        }

        public static bool operator <(TimeSpan t1, IngresDate t2)
        {
            return Compare(t1, t2) < 0;
        }

        public static bool operator <(IngresDate t1, TimeSpan t2)
        {
            return Compare(t1, t2) < 0;
        }

        //
        // Summary:
        //     Determines whether one specified EFIngresProvider.IngresDate is less than or equal to
        //     another specified EFIngresProvider.IngresDate.
        //
        // Parameters:
        //   t1:
        //     A EFIngresProvider.IngresDate.
        //
        //   t2:
        //     A EFIngresProvider.IngresDate.
        //
        // Returns:
        //     true if t1 is less than or equal to t2; otherwise, false.
        public static bool operator <=(IngresDate t1, IngresDate t2)
        {
            return Compare(t1, t2) <= 0;
        }

        public static bool operator <=(DateTime t1, IngresDate t2)
        {
            return Compare(t1, t2) <= 0;
        }

        public static bool operator <=(IngresDate t1, DateTime t2)
        {
            return Compare(t1, t2) <= 0;
        }

        public static bool operator <=(TimeSpan t1, IngresDate t2)
        {
            return Compare(t1, t2) <= 0;
        }

        public static bool operator <=(IngresDate t1, TimeSpan t2)
        {
            return Compare(t1, t2) <= 0;
        }

        //
        // Summary:
        //     Determines whether two specified instances of EFIngresProvider.IngresDate are equal.
        //
        // Parameters:
        //   d1:
        //     A EFIngresProvider.IngresDate.
        //
        //   d2:
        //     A EFIngresProvider.IngresDate.
        //
        // Returns:
        //     true if d1 and d2 represent the same date and time; otherwise, false.
        public static bool operator ==(IngresDate d1, IngresDate d2)
        {
            return Equals(d1, d2);
        }

        public static bool operator ==(DateTime d1, IngresDate d2)
        {
            return Equals(d1, d2);
        }

        public static bool operator ==(IngresDate d1, DateTime d2)
        {
            return Equals(d1, d2);
        }

        public static bool operator ==(TimeSpan d1, IngresDate d2)
        {
            return Equals(d1, d2);
        }

        public static bool operator ==(IngresDate d1, TimeSpan d2)
        {
            return Equals(d1, d2);
        }

        //
        // Summary:
        //     Determines whether one specified EFIngresProvider.IngresDate is greater than another
        //     specified EFIngresProvider.IngresDate.
        //
        // Parameters:
        //   t1:
        //     A EFIngresProvider.IngresDate.
        //
        //   t2:
        //     A EFIngresProvider.IngresDate.
        //
        // Returns:
        //     true if t1 is greater than t2; otherwise, false.
        public static bool operator >(IngresDate t1, IngresDate t2)
        {
            return Compare(t1, t2) > 0;
        }

        public static bool operator >(DateTime t1, IngresDate t2)
        {
            return Compare(t1, t2) > 0;
        }

        public static bool operator >(IngresDate t1, DateTime t2)
        {
            return Compare(t1, t2) > 0;
        }

        public static bool operator >(TimeSpan t1, IngresDate t2)
        {
            return Compare(t1, t2) > 0;
        }

        public static bool operator >(IngresDate t1, TimeSpan t2)
        {
            return Compare(t1, t2) > 0;
        }

        //
        // Summary:
        //     Determines whether one specified EFIngresProvider.IngresDate is greater than or equal
        //     to another specified EFIngresProvider.IngresDate.
        //
        // Parameters:
        //   t1:
        //     A EFIngresProvider.IngresDate.
        //
        //   t2:
        //     A EFIngresProvider.IngresDate.
        //
        // Returns:
        //     true if t1 is greater than or equal to t2; otherwise, false.
        public static bool operator >=(IngresDate t1, IngresDate t2)
        {
            return Compare(t1, t2) >= 0;
        }

        public static bool operator >=(DateTime t1, IngresDate t2)
        {
            return Compare(t1, t2) >= 0;
        }

        public static bool operator >=(IngresDate t1, DateTime t2)
        {
            return Compare(t1, t2) >= 0;
        }

        public static bool operator >=(TimeSpan t1, IngresDate t2)
        {
            return Compare(t1, t2) >= 0;
        }

        public static bool operator >=(IngresDate t1, TimeSpan t2)
        {
            return Compare(t1, t2) >= 0;
        }

        // Summary:
        //     Returns a new EFIngresProvider.IngresDate that adds the value of the specified System.TimeSpan
        //     to the value of this instance.
        //
        // Parameters:
        //   value:
        //     A System.TimeSpan object that represents a positive or negative time interval.
        //
        // Returns:
        //     A EFIngresProvider.IngresDate whose value is the sum of the date and time represented
        //     by this instance and the time interval represented by value.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     The resulting EFIngresProvider.IngresDate is less than EFIngresProvider.IngresDate.MinValue or greater
        //     than EFIngresProvider.IngresDate.MaxValue.
        public IngresDate Add(TimeSpan value)
        {
            return CreateIngresDate(d => d.Add(value), t => t.Add(value), Empty);
        }

        public IngresDate Add(IngresDate value)
        {
            if (value.IsTimeSpan)
            {
                return Add(value.AsTimeSpan);
            }
            return Fail<IngresDate>();
        }

        //
        // Summary:
        //     Subtracts the specified date and time from this instance.
        //
        // Parameters:
        //   value:
        //     An instance of EFIngresProvider.IngresDate.
        //
        // Returns:
        //     A System.TimeSpan interval equal to the date and time represented by this
        //     instance minus the date and time represented by value.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     The result is less than EFIngresProvider.IngresDate.MinValue or greater than EFIngresProvider.IngresDate.MaxValue.
        public IngresDate Subtract(IngresDate value)
        {
            if (value.IsEmpty)
            {
                return this;
            }
            else if (value.IsDateTime)
            {
                return Subtract(value.AsDateTime);
            }
            else
            {
                return Subtract(value.AsTimeSpan);
            }
        }

        //
        // Summary:
        //     Subtracts the specified duration from this instance.
        //
        // Parameters:
        //   value:
        //     An instance of System.TimeSpan.
        //
        // Returns:
        //     A EFIngresProvider.IngresDate equal to the date and time represented by this instance
        //     minus the time interval represented by value.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     The result is less than EFIngresProvider.IngresDate.MinValue or greater than EFIngresProvider.IngresDate.MaxValue.
        public IngresDate Subtract(TimeSpan value)
        {
            return CreateIngresDate(d => d.Subtract(value), t => t.Subtract(value), Empty);
        }

        public IngresDate Subtract(DateTime value)
        {
            return CreateIngresDate(d => d.Subtract(value), Fail<DateTime>, Empty);
        }

        #endregion

        #region Compare

        //
        // Summary:
        //     Compares two instances of EFIngresProvider.IngresDate and returns an integer that indicates
        //     whether the first instance is earlier than, the same as, or later than the
        //     second instance.
        //
        // Parameters:
        //   t1:
        //     The first EFIngresProvider.IngresDate.
        //
        //   t2:
        //     The second EFIngresProvider.IngresDate.
        //
        // Returns:
        //     A signed number indicating the relative values of t1 and t2.Value Type Condition
        //     Less than zero t1 is earlier than t2. Zero t1 is the same as t2. Greater
        //     than zero t1 is later than t2.
        public static int Compare(IngresDate t1, IngresDate t2)
        {
            if (t1.IsEmpty || t2.IsEmpty)
            {
                if (!t1.IsEmpty)
                {
                    return -1;
                }
                else if (!t2.IsEmpty)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            else if (t1.IsDateTime && t2.IsDateTime)
            {
                return DateTime.Compare(t1.DateTimeValue, t2.DateTimeValue);
            }
            else if (t1.IsTimeSpan && t2.IsTimeSpan)
            {
                return TimeSpan.Compare(t1.AsTimeSpan, t2.AsTimeSpan);
            }
            else if (t1.IsDateTime && t2.IsTimeSpan)
            {
                return 1;
            }
            else // if (t1.IsTimeSpan && t2.IsDateTime)
            {
                return -1;
            }
        }

        //
        // Summary:
        //     Compares the value of this instance to a specified EFIngresProvider.IngresDate value
        //     and returns an integer that indicates whether this instance is earlier than,
        //     the same as, or later than the specified EFIngresProvider.IngresDate value.
        //
        // Parameters:
        //   value:
        //     A EFIngresProvider.IngresDate object to compare.
        //
        // Returns:
        //     A signed number indicating the relative values of this instance and the value
        //     parameter.Value Description Less than zero This instance is earlier than
        //     value. Zero This instance is the same as value. Greater than zero This instance
        //     is later than value.
        public int CompareTo(IngresDate value)
        {
            return Compare(this, value);
        }

        //
        // Summary:
        //     Compares the value of this instance to a specified object that contains a
        //     specified EFIngresProvider.IngresDate value, and returns an integer that indicates whether
        //     this instance is earlier than, the same as, or later than the specified EFIngresProvider.IngresDate
        //     value.
        //
        // Parameters:
        //   value:
        //     A boxed EFIngresProvider.IngresDate object to compare, or null.
        //
        // Returns:
        //     A signed number indicating the relative values of this instance and value.Value
        //     Description Less than zero This instance is earlier than value. Zero This
        //     instance is the same as value. Greater than zero This instance is later than
        //     value, or value is null.
        //
        // Exceptions:
        //   System.ArgumentException:
        //     value is not a EFIngresProvider.IngresDate.
        public int CompareTo(object value)
        {
            if (value is IngresDate)
            {
                return CompareTo((IngresDate)value);
            }
            else if (value is DateTime)
            {
                return CompareTo((DateTime)value);
            }
            else if (value is TimeSpan)
            {
                return CompareTo((TimeSpan)value);
            }
            else if (value == null)
            {
                return 1;
            }
            return Fail<int>();
        }

        //
        // Summary:
        //     Returns a value indicating whether this instance is equal to the specified
        //     EFIngresProvider.IngresDate instance.
        //
        // Parameters:
        //   value:
        //     A EFIngresProvider.IngresDate instance to compare to this instance.
        //
        // Returns:
        //     true if the value parameter equals the value of this instance; otherwise,
        //     false.
        public bool Equals(IngresDate value)
        {
            return Equals(this, value);
        }

        //
        // Summary:
        //     Returns a value indicating whether this instance is equal to a specified
        //     object.
        //
        // Parameters:
        //   value:
        //     An object to compare to this instance.
        //
        // Returns:
        //     true if value is an instance of EFIngresProvider.IngresDate and equals the value of this
        //     instance; otherwise, false.
        public override bool Equals(object value)
        {
            if (value is IngresDate)
            {
                return Equals((IngresDate)value);
            }
            else if (value is DateTime)
            {
                return Equals((DateTime)value);
            }
            else if (value is TimeSpan)
            {
                return Equals((TimeSpan)value);
            }
            return false;
        }
        //
        // Summary:
        //     Returns a value indicating whether two instances of EFIngresProvider.IngresDate are equal.
        //
        // Parameters:
        //   t1:
        //     The first EFIngresProvider.IngresDate instance.
        //
        //   t2:
        //     The second EFIngresProvider.IngresDate instance.
        //
        // Returns:
        //     true if the two EFIngresProvider.IngresDate values are equal; otherwise, false.
        public static bool Equals(IngresDate t1, IngresDate t2)
        {
            if (t1.IsEmpty || t2.IsEmpty)
            {
                return t1.IsEmpty && t2.IsEmpty;
            }
            else if (t1.IsDateTime && t2.IsDateTime)
            {
                return DateTime.Equals(t1.DateTimeValue, t2.DateTimeValue);
            }
            else if (t1.IsTimeSpan && t2.IsTimeSpan)
            {
                return TimeSpan.Equals(t1.AsTimeSpan, t2.AsTimeSpan);
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region ToString

        //
        // Summary:
        //     Converts the value of the current EFIngresProvider.IngresDate object to its equivalent
        //     string representation.
        //
        // Returns:
        //     A string representation of the value of the current EFIngresProvider.IngresDate object.
        public override string ToString()
        {
            return WrapFunctions(d => d.ToString(), t => t.ToString(), "");
        }
        //
        // Summary:
        //     Converts the value of the current EFIngresProvider.IngresDate object to its equivalent
        //     string representation using the specified culture-specific format information.
        //
        // Parameters:
        //   provider:
        //     An System.IFormatProvider that supplies culture-specific formatting information.
        //
        // Returns:
        //     A string representation of value of the current EFIngresProvider.IngresDate object as
        //     specified by provider.
        public string ToString(IFormatProvider provider)
        {
            return WrapFunctions(d => d.ToString(provider), t => t.ToString(), "");
        }
        //
        // Summary:
        //     Converts the value of the current EFIngresProvider.IngresDate object to its equivalent
        //     string representation using the specified format.
        //
        // Parameters:
        //   format:
        //     A standard or custom date and time format string.
        //
        // Returns:
        //     A string representation of value of the current EFIngresProvider.IngresDate object as
        //     specified by format.
        //
        // Exceptions:
        //   System.FormatException:
        //     The length of format is 1, and it is not one of the format specifier characters
        //     defined for System.Globalization.DateTimeFormatInfo.-or- format does not
        //     contain a valid custom format pattern.
        public string ToString(string format)
        {
            return WrapFunctions(d => d.ToString(format), t => t.ToString(format), "");
        }
        //
        // Summary:
        //     Converts the value of the current EFIngresProvider.IngresDate object to its equivalent
        //     string representation using the specified format and culture-specific format
        //     information.
        //
        // Parameters:
        //   format:
        //     A standard or custom date and time format string.
        //
        //   provider:
        //     An object that supplies culture-specific formatting information.
        //
        // Returns:
        //     A string representation of value of the current EFIngresProvider.IngresDate object as
        //     specified by format and provider.
        //
        // Exceptions:
        //   System.FormatException:
        //     The length of format is 1, and it is not one of the format specifier characters
        //     defined for System.Globalization.DateTimeFormatInfo.-or- format does not
        //     contain a valid custom format pattern.
        public string ToString(string format, IFormatProvider provider)
        {
            return WrapFunctions(d => d.ToString(format, provider), t => t.ToString(format, provider), "");
        }

        //
        // Summary:
        //     Converts the value of the current EFIngresProvider.IngresDate object to its equivalent
        //     short date string representation.
        //
        // Returns:
        //     A string that contains the short date string representation of the current
        //     EFIngresProvider.IngresDate object.
        public string ToShortDateString()
        {
            return WrapFunctions(d => d.ToShortDateString(), t => t.ToString(), "");
        }
        //
        // Summary:
        //     Converts the value of the current EFIngresProvider.IngresDate object to its equivalent
        //     short time string representation.
        //
        // Returns:
        //     A string that contains the short time string representation of the current
        //     EFIngresProvider.IngresDate object.
        public string ToShortTimeString()
        {
            return WrapFunctions(d => d.ToShortTimeString(), t => t.ToString(), "");
        }

        //
        // Summary:
        //     Converts the value of the current EFIngresProvider.IngresDate object to its equivalent
        //     long date string representation.
        //
        // Returns:
        //     A string that contains the long date string representation of the current
        //     EFIngresProvider.IngresDate object.
        public string ToLongDateString()
        {
            return WrapFunctions(d => d.ToLongDateString(), t => t.ToString(), "");
        }

        //
        // Summary:
        //     Converts the value of the current EFIngresProvider.IngresDate object to its equivalent
        //     long time string representation.
        //
        // Returns:
        //     A string that contains the long time string representation of the current
        //     EFIngresProvider.IngresDate object.
        public string ToLongTimeString()
        {
            return WrapFunctions(d => d.ToLongTimeString(), t => t.ToString(), "");
        }

        #endregion

        #region GetHashCode

        //
        // Summary:
        //     Returns the hash code for this instance.
        //
        // Returns:
        //     A 32-bit signed integer hash code.
        public override int GetHashCode()
        {
            return WrapFunctions(d => d.GetHashCode(), t => t.GetHashCode(), 0);
        }

        #endregion

        #region Parse

        //
        // Summary:
        //     Converts the specified string representation of a date and time to its EFIngresProvider.IngresDate
        //     equivalent.
        //
        // Parameters:
        //   s:
        //     A string containing a date and time to convert.
        //
        // Returns:
        //     A EFIngresProvider.IngresDate equivalent to the date and time contained in s.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     s is null.
        //
        //   System.FormatException:
        //     s does not contain a valid string representation of a date and time.
        public static IngresDate Parse(string s)
        {
            IngresDate result;
            if (TryParse(s, out result))
            {
                return result;
            }
            return new IngresDate(DateTime.Parse(s));
        }
        //
        // Summary:
        //     Converts the specified string representation of a date and time to its EFIngresProvider.IngresDate
        //     equivalent using the specified culture-specific format information.
        //
        // Parameters:
        //   s:
        //     A string containing a date and time to convert.
        //
        //   provider:
        //     An object that supplies culture-specific format information about s.
        //
        // Returns:
        //     A EFIngresProvider.IngresDate equivalent to the date and time contained in s as specified
        //     by provider.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     s is null.
        //
        //   System.FormatException:
        //     s does not contain a valid string representation of a date and time.
        public static IngresDate Parse(string s, IFormatProvider provider)
        {
            return Parse(s, provider, DateTimeStyles.None);
        }
        //
        // Summary:
        //     Converts the specified string representation of a date and time to its EFIngresProvider.IngresDate
        //     equivalent using the specified culture-specific format information and formatting
        //     style.
        //
        // Parameters:
        //   s:
        //     A string containing a date and time to convert.
        //
        //   provider:
        //     An object that supplies culture-specific formatting information about s.
        //
        //   styles:
        //     A bitwise combination of the enumeration values that indicates the style
        //     elements that can be present in s for the parse operation to succeed and
        //     that defines how to interpret the parsed date in relation to the current
        //     time zone or the current date. A typical value to specify is System.Globalization.DateTimeStyles.None.
        //
        // Returns:
        //     A EFIngresProvider.IngresDate equivalent to the date and time contained in s as specified
        //     by provider and styles.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     s is null.
        //
        //   System.FormatException:
        //     s does not contain a valid string representation of a date and time.
        //
        //   System.ArgumentException:
        //     styles contains an invalid combination of System.Globalization.DateTimeStyles
        //     values. For example, both System.Globalization.DateTimeStyles.AssumeLocal
        //     and System.Globalization.DateTimeStyles.AssumeUniversal.
        public static IngresDate Parse(string s, IFormatProvider provider, DateTimeStyles dateTimeStyle)
        {
            IngresDate result;
            if (TryParse(s, provider, dateTimeStyle, out result))
            {
                return result;
            }
            return new IngresDate(DateTime.Parse(s, provider, dateTimeStyle));
        }
        //
        // Summary:
        //     Converts the specified string representation of a date and time to its EFIngresProvider.IngresDate
        //     equivalent using the specified format and culture-specific format information.
        //     The format of the string representation must match the specified format exactly.
        //
        // Parameters:
        //   s:
        //     A string that contains a date and time to convert.
        //
        //   format:
        //     A format specifier that defines the required format of s.
        //
        //   provider:
        //     An object that supplies culture-specific format information about s.
        //
        // Returns:
        //     A EFIngresProvider.IngresDate equivalent to the date and time contained in s as specified
        //     by format and provider.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     s or format is null.
        //
        //   System.FormatException:
        //     s or format is an empty string. -or- s does not contain a date and time that
        //     corresponds to the pattern specified in format. -or-The hour component and
        //     the AM/PM designator in s do not agree.
        public static IngresDate ParseExact(string s, string format, IFormatProvider provider)
        {
            return ParseExact(s, new string[] { format }, provider, DateTimeStyles.None, TimeSpanStyles.None);
        }
        //
        // Summary:
        //     Converts the specified string representation of a date and time to its EFIngresProvider.IngresDate
        //     equivalent using the specified format, culture-specific format information,
        //     and style. The format of the string representation must match the specified
        //     format exactly or an exception is thrown.
        //
        // Parameters:
        //   s:
        //     A string containing a date and time to convert.
        //
        //   format:
        //     A format specifier that defines the required format of s.
        //
        //   provider:
        //     An object that supplies culture-specific formatting information about s.
        //
        //   style:
        //     A bitwise combination of the enumeration values that provides additional
        //     information about s, about style elements that may be present in s, or about
        //     the conversion from s to a EFIngresProvider.IngresDate value. A typical value to specify
        //     is System.Globalization.DateTimeStyles.None.
        //
        // Returns:
        //     A EFIngresProvider.IngresDate equivalent to the date and time contained in s as specified
        //     by format, provider, and style.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     s or format is null.
        //
        //   System.FormatException:
        //     s or format is an empty string. -or- s does not contain a date and time that
        //     corresponds to the pattern specified in format. -or-The hour component and
        //     the AM/PM designator in s do not agree.
        //
        //   System.ArgumentException:
        //     style contains an invalid combination of System.Globalization.DateTimeStyles
        //     values. For example, both System.Globalization.DateTimeStyles.AssumeLocal
        //     and System.Globalization.DateTimeStyles.AssumeUniversal.
        public static IngresDate ParseExact(string s, string format, IFormatProvider provider, DateTimeStyles style)
        {
            return ParseExact(s, new string[] { format }, provider, style, TimeSpanStyles.None);
        }

        public static IngresDate ParseExact(string s, string format, IFormatProvider provider, TimeSpanStyles style)
        {
            return ParseExact(s, new string[] { format }, provider, DateTimeStyles.None, style);
        }

        //
        // Summary:
        //     Converts the specified string representation of a date and time to its EFIngresProvider.IngresDate
        //     equivalent using the specified array of formats, culture-specific format
        //     information, and style. The format of the string representation must match
        //     at least one of the specified formats exactly or an exception is thrown.
        //
        // Parameters:
        //   s:
        //     A string containing one or more dates and times to convert.
        //
        //   formats:
        //     An array of allowable formats of s.
        //
        //   provider:
        //     An System.IFormatProvider that supplies culture-specific format information
        //     about s.
        //
        //   style:
        //     A bitwise combination of System.Globalization.DateTimeStyles values that
        //     indicates the permitted format of s. A typical value to specify is System.Globalization.DateTimeStyles.None.
        //
        // Returns:
        //     A EFIngresProvider.IngresDate equivalent to the date and time contained in s as specified
        //     by formats, provider, and style.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     s or formats is null.
        //
        //   System.FormatException:
        //     s is an empty string. -or- an element of formats is an empty string. -or-
        //     s does not contain a date and time that corresponds to any element of formats.
        //     -or-The hour component and the AM/PM designator in s do not agree.
        //
        //   System.ArgumentException:
        //     style contains an invalid combination of System.Globalization.DateTimeStyles
        //     values. For example, both System.Globalization.DateTimeStyles.AssumeLocal
        //     and System.Globalization.DateTimeStyles.AssumeUniversal.
        public static IngresDate ParseExact(string s, string[] formats, IFormatProvider provider, DateTimeStyles style)
        {
            return ParseExact(s, formats, provider, style, TimeSpanStyles.None);
        }

        public static IngresDate ParseExact(string s, string[] formats, IFormatProvider provider, TimeSpanStyles style)
        {
            return ParseExact(s, formats, provider, DateTimeStyles.None, style);
        }

        public static IngresDate ParseExact(string s, string[] formats, IFormatProvider provider, DateTimeStyles dateTimeStyle, TimeSpanStyles timeSpanStyles)
        {
            IngresDate result;
            if (TryParseExact(s, formats, provider, dateTimeStyle, timeSpanStyles, out result))
            {
                return result;
            }
            return new IngresDate(DateTime.ParseExact(s, formats, provider, dateTimeStyle));
        }

        //
        // Summary:
        //     Converts the specified string representation of a date and time to its EFIngresProvider.IngresDate
        //     equivalent and returns a value that indicates whether the conversion succeeded.
        //
        // Parameters:
        //   s:
        //     A string containing a date and time to convert.
        //
        //   result:
        //     When this method returns, contains the EFIngresProvider.IngresDate value equivalent to
        //     the date and time contained in s, if the conversion succeeded, or EFIngresProvider.IngresDate.MinValue
        //     if the conversion failed. The conversion fails if the s parameter is null,
        //     is an empty string (""), or does not contain a valid string representation
        //     of a date and time. This parameter is passed uninitialized.
        //
        // Returns:
        //     true if the s parameter was converted successfully; otherwise, false.
        public static bool TryParse(string s, out IngresDate result)
        {
            result = IngresDate.Empty;
            if (string.IsNullOrWhiteSpace(s))
            {
                result = IngresDate.Empty;
                return true;
            }
            DateTime dateTimeValue;
            if (DateTime.TryParse(s, out dateTimeValue))
            {
                result = new IngresDate(dateTimeValue);
                return true;
            }
            TimeSpan timeSpanValue;
            if (TimeSpan.TryParse(s, out timeSpanValue))
            {
                result = new IngresDate(timeSpanValue);
                return true;
            }
            return false;
        }
        //
        // Summary:
        //     Converts the specified string representation of a date and time to its EFIngresProvider.IngresDate
        //     equivalent using the specified culture-specific format information and formatting
        //     style, and returns a value that indicates whether the conversion succeeded.
        //
        // Parameters:
        //   s:
        //     A string containing a date and time to convert.
        //
        //   provider:
        //     An object that supplies culture-specific formatting information about s.
        //
        //   styles:
        //     A bitwise combination of enumeration values that defines how to interpret
        //     the parsed date in relation to the current time zone or the current date.
        //     A typical value to specify is System.Globalization.DateTimeStyles.None.
        //
        //   result:
        //     When this method returns, contains the EFIngresProvider.IngresDate value equivalent to
        //     the date and time contained in s, if the conversion succeeded, or EFIngresProvider.IngresDate.MinValue
        //     if the conversion failed. The conversion fails if the s parameter is null,
        //     is an empty string (""), or does not contain a valid string representation
        //     of a date and time. This parameter is passed uninitialized.
        //
        // Returns:
        //     true if the s parameter was converted successfully; otherwise, false.
        //
        // Exceptions:
        //   System.ArgumentException:
        //     styles is not a valid System.Globalization.DateTimeStyles value.-or-styles
        //     contains an invalid combination of System.Globalization.DateTimeStyles values
        //     (for example, both System.Globalization.DateTimeStyles.AssumeLocal and System.Globalization.DateTimeStyles.AssumeUniversal).
        //
        //   System.NotSupportedException:
        //     provider is a neutral culture and cannot be used in a parsing operation.
        public static bool TryParse(string s, IFormatProvider provider, DateTimeStyles styles, out IngresDate result)
        {
            result = IngresDate.Empty;
            if (string.IsNullOrWhiteSpace(s))
            {
                result = IngresDate.Empty;
                return true;
            }
            DateTime dateTimeValue;
            if (DateTime.TryParse(s, provider, styles, out dateTimeValue))
            {
                result = new IngresDate(dateTimeValue);
                return true;
            }
            TimeSpan timeSpanValue;
            if (TimeSpan.TryParse(s, provider, out timeSpanValue))
            {
                result = new IngresDate(timeSpanValue);
                return true;
            }
            return false;
        }

        //
        // Summary:
        //     Converts the specified string representation of a date and time to its EFIngresProvider.IngresDate
        //     equivalent using the specified format, culture-specific format information,
        //     and style. The format of the string representation must match the specified
        //     format exactly. The method returns a value that indicates whether the conversion
        //     succeeded.
        //
        // Parameters:
        //   s:
        //     A string containing a date and time to convert.
        //
        //   format:
        //     The required format of s.
        //
        //   provider:
        //     An System.IFormatProvider object that supplies culture-specific formatting
        //     information about s.
        //
        //   style:
        //     A bitwise combination of one or more enumeration values that indicate the
        //     permitted format of s.
        //
        //   result:
        //     When this method returns, contains the EFIngresProvider.IngresDate value equivalent to
        //     the date and time contained in s, if the conversion succeeded, or EFIngresProvider.IngresDate.MinValue
        //     if the conversion failed. The conversion fails if either the s or format
        //     parameter is null, is an empty string, or does not contain a date and time
        //     that correspond to the pattern specified in format. This parameter is passed
        //     uninitialized.
        //
        // Returns:
        //     true if s was converted successfully; otherwise, false.
        //
        // Exceptions:
        //   System.ArgumentException:
        //     styles is not a valid System.Globalization.DateTimeStyles value.-or-styles
        //     contains an invalid combination of System.Globalization.DateTimeStyles values
        //     (for example, both System.Globalization.DateTimeStyles.AssumeLocal and System.Globalization.DateTimeStyles.AssumeUniversal).
        public static bool TryParseExact(string s, string format, IFormatProvider provider, DateTimeStyles style, out IngresDate result)
        {
            return TryParseExact(s, new string[] { format }, provider, style, TimeSpanStyles.None, out result);
        }

        public static bool TryParseExact(string s, string format, IFormatProvider provider, TimeSpanStyles style, out IngresDate result)
        {
            return TryParseExact(s, new string[] { format }, provider, DateTimeStyles.None, style, out result);
        }

        //
        // Summary:
        //     Converts the specified string representation of a date and time to its EFIngresProvider.IngresDate
        //     equivalent using the specified array of formats, culture-specific format
        //     information, and style. The format of the string representation must match
        //     at least one of the specified formats exactly. The method returns a value
        //     that indicates whether the conversion succeeded.
        //
        // Parameters:
        //   s:
        //     A string containing one or more dates and times to convert.
        //
        //   formats:
        //     An array of allowable formats of s.
        //
        //   provider:
        //     An object that supplies culture-specific format information about s.
        //
        //   style:
        //     A bitwise combination of enumeration values that indicates the permitted
        //     format of s. A typical value to specify is System.Globalization.DateTimeStyles.None.
        //
        //   result:
        //     When this method returns, contains the EFIngresProvider.IngresDate value equivalent to
        //     the date and time contained in s, if the conversion succeeded, or EFIngresProvider.IngresDate.MinValue
        //     if the conversion failed. The conversion fails if s or formats is null, s
        //     or an element of formats is an empty string, or the format of s is not exactly
        //     as specified by at least one of the format patterns in formats. This parameter
        //     is passed uninitialized.
        //
        // Returns:
        //     true if the s parameter was converted successfully; otherwise, false.
        //
        // Exceptions:
        //   System.ArgumentException:
        //     styles is not a valid System.Globalization.DateTimeStyles value.-or-styles
        //     contains an invalid combination of System.Globalization.DateTimeStyles values
        //     (for example, both System.Globalization.DateTimeStyles.AssumeLocal and System.Globalization.DateTimeStyles.AssumeUniversal).
        public static bool TryParseExact(string s, string[] formats, IFormatProvider provider, DateTimeStyles style, out IngresDate result)
        {
            return TryParseExact(s, formats, provider, style, TimeSpanStyles.None, out result);
        }

        public static bool TryParseExact(string s, string[] formats, IFormatProvider provider, TimeSpanStyles style, out IngresDate result)
        {
            return TryParseExact(s, formats, provider, DateTimeStyles.None, style, out result);
        }

        public static bool TryParseExact(string s, string[] formats, IFormatProvider provider, DateTimeStyles dateTimeStyle, TimeSpanStyles timeSpanStyles, out IngresDate result)
        {
            result = IngresDate.Empty;
            if (string.IsNullOrWhiteSpace(s))
            {
                result = IngresDate.Empty;
                return true;
            }
            DateTime dateTimeValue;
            if (DateTime.TryParseExact(s, formats, provider, dateTimeStyle, out dateTimeValue))
            {
                result = new IngresDate(dateTimeValue);
                return true;
            }
            TimeSpan timeSpanValue;
            if (TimeSpan.TryParseExact(s, formats, provider, timeSpanStyles, out timeSpanValue))
            {
                result = new IngresDate(timeSpanValue);
                return true;
            }
            return false;
        }

        #endregion

        #region Helper methods

        private static T Fail<T>()
        {
            throw new NotSupportedException();
        }

        private static T Fail<T>(DateTime d)
        {
            return Fail<T>();
        }

        private static T Fail<T>(TimeSpan t)
        {
            return Fail<T>();
        }

        private T WrapFunctions<T>(Func<DateTime, T> dateTimeFunction, Func<TimeSpan, T> timeSpanFunction, Func<T> emptyFunction)
        {
            switch (IngresDateKind)
            {
                case IngresDateKind.DateTime: return dateTimeFunction(AsDateTime);
                case IngresDateKind.TimeSpan: return timeSpanFunction(AsTimeSpan);
                default: return emptyFunction();
            }
        }

        private T WrapFunctions<T>(Func<DateTime, T> dateTimeFunction, Func<TimeSpan, T> timeSpanFunction, T defaultValue)
        {
            return WrapFunctions(dateTimeFunction, timeSpanFunction, () => defaultValue);
        }

        private T WrapFunctions<T>(Func<DateTime, T> dateTimeFunction, T defaultValue)
        {
            return WrapFunctions(dateTimeFunction, Fail<T>, defaultValue);
        }

        private T WrapFunctions<T>(Func<TimeSpan, T> timeSpanFunction, T defaultValue)
        {
            return WrapFunctions(Fail<T>, timeSpanFunction, defaultValue);
        }

        private IngresDate CreateIngresDate(Func<DateTime, DateTime> dateTimeFunction, Func<TimeSpan, TimeSpan> timeSpanFunction, Func<IngresDate> emptyFunction)
        {
            return WrapFunctions(x => new IngresDate(dateTimeFunction(x)), x => new IngresDate(timeSpanFunction(x)), emptyFunction);
        }

        private IngresDate CreateIngresDate(Func<DateTime, TimeSpan> dateTimeFunction, Func<TimeSpan, DateTime> timeSpanFunction, Func<IngresDate> emptyFunction)
        {
            return WrapFunctions(x => new IngresDate(dateTimeFunction(x)), x => new IngresDate(timeSpanFunction(x)), emptyFunction);
        }

        private IngresDate CreateIngresDate(Func<DateTime, DateTime> dateTimeFunction, Func<TimeSpan, TimeSpan> timeSpanFunction, IngresDate defaultValue)
        {
            return CreateIngresDate(dateTimeFunction, timeSpanFunction, () => defaultValue);
        }

        private IngresDate CreateIngresDate(Func<DateTime, TimeSpan> dateTimeFunction, Func<TimeSpan, DateTime> timeSpanFunction, IngresDate defaultValue)
        {
            return CreateIngresDate(dateTimeFunction, timeSpanFunction, () => defaultValue);
        }

        #endregion

        #region ISerializable methods

        private IngresDate(SerializationInfo info, StreamingContext context)
        {
            IngresDateKind = (IngresDateKind)info.GetInt32("IngresDateKind");
            switch (IngresDateKind)
            {
                case IngresDateKind.Empty:
                    _value = EmptyDateTimeValue;
                    break;
                case IngresDateKind.DateTime:
                    _value = info.GetDateTime("Value");
                    break;
                case IngresDateKind.TimeSpan:
                    _value = info.GetValue("Value", typeof(TimeSpan));
                    break;
                default:
                    _value = null;
                    break;
            }
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            info.AddValue("IngresDateKind", (int)IngresDateKind);
            info.AddValue("Value", this._value);
        }

        #endregion
    }
}
