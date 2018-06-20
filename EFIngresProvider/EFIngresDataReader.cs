using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Common;
using System.Data;
using System.Collections;
using EFIngresProvider.Helpers;
using Ingres.Client;
using System.ComponentModel;

namespace EFIngresProvider
{
    public class EFIngresDataReader : DbDataReader
    {
        private IngresDataReader _ingresDataReader;
        private CommandBehavior _commandBehavior;
        private object _resultset;
        private object _rsmd;
        private List<IngresDesc> _desc;
        private object[] _valueList;
        public bool UseIngresDate { get; set; }
        public bool TrimChars { get; set; }

        public EFIngresDataReader(IngresDataReader ingresDataReader, CommandBehavior commandBehavior)
        {
            _ingresDataReader = ingresDataReader;
            _commandBehavior = commandBehavior;
            _resultset = _ingresDataReader.GetWrappedField("_resultset");
            _rsmd = _resultset.GetWrappedField("rsmd");
            _desc = _rsmd.GetWrappedField<Array>("desc").Cast<object>().Select(x => new IngresDesc(x)).ToList();
            _valueList = new object[FieldCount];
        }

        public override void Close()
        {
            _ingresDataReader.Close();
        }

        public override int Depth
        {
            get { return _ingresDataReader.Depth; }
        }

        public override int FieldCount
        {
            get { return _ingresDataReader.FieldCount; }
        }

        private T GetValue<T>(int ordinal)
        {
            object obj = GetValue(ordinal);  // check reader is open and get object

            // No conversions are performed and
            // InvalidCastException is thrown if not right type already.
            try { return (T)obj; } // try returning data with the right cast
            catch (InvalidCastException)
            {
                throw new InvalidCastException(SpecifiedCastIsNotValid(obj, ordinal));
            }
        }

        public override bool GetBoolean(int ordinal)
        {
            return GetValue<bool>(ordinal);
        }

        public override byte GetByte(int ordinal)
        {
            return _ingresDataReader.GetByte(ordinal);
        }

        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            return _ingresDataReader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
        }

        public override char GetChar(int ordinal)
        {
            return GetValue<char>(ordinal);
        }

        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            return _ingresDataReader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
        }

        public override string GetDataTypeName(int ordinal)
        {
            return _ingresDataReader.GetDataTypeName(ordinal);
        }

        public override DateTime GetDateTime(int ordinal)
        {
            return GetValue<DateTime>(ordinal);
        }

        public override decimal GetDecimal(int ordinal)
        {
            return GetValue<Decimal>(ordinal);
        }

        public override double GetDouble(int ordinal)
        {
            return GetValue<double>(ordinal);
        }

        public override Type GetFieldType(int ordinal)
        {
            if (UseIngresDate && _desc[ordinal].SqlType == IngresType.IngresDate)
            {
                return typeof(IngresDate);
            }
            return _ingresDataReader.GetFieldType(ordinal);
        }

        public override float GetFloat(int ordinal)
        {
            return GetValue<float>(ordinal);
        }

        public override Guid GetGuid(int ordinal)
        {
            return GetValue<Guid>(ordinal);
        }

        public virtual IngresDate GetIngresDate(int ordinal)
        {
            return GetValue<IngresDate>(ordinal);
        }

        public override short GetInt16(int ordinal)
        {
            return GetValue<short>(ordinal);
        }

        public override int GetInt32(int ordinal)
        {
            return GetValue<int>(ordinal);
        }

        public override long GetInt64(int ordinal)
        {
            return GetValue<long>(ordinal);
        }

        public override string GetName(int ordinal)
        {
            return _ingresDataReader.GetName(ordinal);
        }

        public override int GetOrdinal(string name)
        {
            return _ingresDataReader.GetOrdinal(name);
        }

        public override DataTable GetSchemaTable()
        {
            return _ingresDataReader.GetSchemaTable();
        }

        public override string GetString(int ordinal)
        {
            return GetValue<string>(ordinal);
        }

        private object GetValueInternal(int ordinal)
        {
            switch (_desc[ordinal].SqlType)
            {
                case IngresType.IngresDate:
                    var ingresDate = IngresDateData.GetIngresDate(_resultset.AdvanRsltColumnDataValue(ordinal));
                    if (ingresDate == null)
                    {
                        return DBNull.Value;
                    }
                    if (UseIngresDate)
                    {
                        return ingresDate;
                    }
                    return ingresDate.Value.AsDateTime;
                case IngresType.Char:
                case IngresType.NChar:
                    if (TrimChars)
                    {
                        var value = (string)_ingresDataReader.GetValue(ordinal);
                        if (value != null)
                        {
                            return value.TrimEnd();
                        }
                        return value;
                    }
                    break;
                case IngresType.TinyInt:
                    var byteValue = _ingresDataReader.GetValue(ordinal);
                    return Convert.ToSByte(byteValue);
            }
            return _ingresDataReader.GetValue(ordinal);
        }

        public override object GetValue(int ordinal)
        {
            if (_valueList[ordinal] == null)
            {
                _valueList[ordinal] = GetValueInternal(ordinal);
            }
            return _valueList[ordinal];
        }

        public override int GetValues(object[] values)
        {
            int i = 0;

            for (i = 0; i < values.Length && i < FieldCount; i++)
            {
                values[i] = GetValue(i);
            }

            return i;
        }

        public override bool HasRows
        {
            get { return _ingresDataReader.HasRows; }
        }

        public override bool IsClosed
        {
            get { return _ingresDataReader.IsClosed; }
        }

        public override bool IsDBNull(int ordinal)
        {
            return _ingresDataReader.IsDBNull(ordinal);
        }

        public override bool NextResult()
        {
            return _ingresDataReader.NextResult();
        }

        public override bool Read()
        {
            for (var i = 0; i < FieldCount; i++)
            {
                _valueList[i] = null;
            }
            return _ingresDataReader.Read();
        }

        public override int RecordsAffected
        {
            get { return _ingresDataReader.RecordsAffected; }
        }

        public override object this[string name]
        {
            get { return GetValue(GetOrdinal(name)); }
        }

        public override object this[int ordinal]
        {
            get { return GetValue(ordinal); }
        }

        public override IEnumerator GetEnumerator()
        {
            return new DbEnumerator(this, (_commandBehavior & CommandBehavior.CloseConnection) != 0 ? true : false);
        }

        /// <summary>
        /// Format 'InvalidCastException' msg with more information.
        /// </summary>
        [Description("Format 'InvalidCastException' msg with more information.")]
        private string SpecifiedCastIsNotValid(Object obj, int ordinal)
        {
            string s;

            if (obj == null)
            {
                s = "<null>";
            }
            else
            {
                s = "Type." + obj.GetType().ToString();
            }
            //	s = "TypeCode." + Type.GetTypeCode(obj.GetType()).ToString();
            return String.Format("Specified cast is not valid for object of {0} at column ordinal {1} ({2}).", s, ordinal, GetFieldType(ordinal).ToString());
        }

        private class IngresDesc
        {
            public const byte MSG_DSC_NULL = 0x01; // Nullable

            public IngresDesc(object desc)
            {
                SqlType = desc.GetWrappedField<IngresType>("sql_type");
                DbmsType = desc.GetWrappedField<short>("dbms_type");
                Length = desc.GetWrappedField<short>("length");
                Precision = desc.GetWrappedField<byte>("precision");
                Scale = desc.GetWrappedField<byte>("scale");
                Flags = desc.GetWrappedField<byte>("flags");
                Name = desc.GetWrappedField<string>("name");
                IsNullable = (Flags & MSG_DSC_NULL) != 0;

                if (DbmsType == 3) // IngresDate
                {
                    SqlType = IngresType.IngresDate;
                }
            }

            public IngresType SqlType { get; private set; }
            public short DbmsType { get; private set; }
            public short Length { get; private set; }
            public byte Precision { get; private set; }
            public byte Scale { get; private set; }
            public byte Flags { get; private set; }
            public string Name { get; private set; }
            public bool IsNullable { get; set; }
        } // struct Desc
    }
}
