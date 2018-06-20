using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Common;
using System.Reflection;
using System.Data;
using System.Collections;

namespace EFIngresProvider.Helpers.IngresCatalogs
{
    public abstract class ObjectReader : DbDataReader
    {
        public static ObjectReader<T> GreateReader<T>(IEnumerable<T> objects)
        {
            return new ObjectReader<T>(objects);
        }
    }

    public class ObjectReader<T> : ObjectReader, IEnumerable<ObjectReader<T>>
    {
        private IEnumerator<T> _objectEnumerator;
        private IEnumerable<T> _objects;
        private static readonly PropertyInfo[] _properties = typeof(T).GetProperties();
        private int _recordsAffected = 0;
        private bool _closed = false;

        public ObjectReader(IEnumerable<T> objects)
        {
            _objects = objects;
            _objectEnumerator = objects.GetEnumerator();
        }

        public override void Close()
        {
            _closed = true;
        }

        public override int Depth
        {
            get { return 0; }
        }

        public override int FieldCount
        {
            get { return _properties.Length; }
        }

        public override bool GetBoolean(int ordinal)
        {
            return Convert.ToBoolean(this[ordinal]);
        }

        public override byte GetByte(int ordinal)
        {
            return Convert.ToByte(this[ordinal]);
        }

        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public override char GetChar(int ordinal)
        {
            return Convert.ToChar(this[ordinal]);
        }

        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public override string GetDataTypeName(int ordinal)
        {
            return _properties[ordinal].PropertyType.Name;
        }

        public override DateTime GetDateTime(int ordinal)
        {
            return Convert.ToDateTime(this[ordinal]);
        }

        public override decimal GetDecimal(int ordinal)
        {
            return Convert.ToDecimal(this[ordinal]);
        }

        public override double GetDouble(int ordinal)
        {
            return Convert.ToDouble(this[ordinal]);
        }

        public override IEnumerator GetEnumerator()
        {
            return new ObjectReaderEnumerator(this);
        }

        public override Type GetFieldType(int ordinal)
        {
            return _properties[ordinal].PropertyType;
        }

        public override float GetFloat(int ordinal)
        {
            return Convert.ToSingle(this[ordinal]);
        }

        public override Guid GetGuid(int ordinal)
        {
            if (this[ordinal] is Guid)
            {
                return (Guid)this[ordinal];
            }
            return new Guid(GetString(ordinal));
        }

        public override short GetInt16(int ordinal)
        {
            return Convert.ToInt16(this[ordinal]);
        }

        public override int GetInt32(int ordinal)
        {
            return Convert.ToInt32(this[ordinal]);
        }

        public override long GetInt64(int ordinal)
        {
            return Convert.ToInt64(this[ordinal]);
        }

        public override string GetName(int ordinal)
        {
            return _properties[ordinal].Name;
        }

        public override int GetOrdinal(string name)
        {
            for (var ordinal = 0; ordinal < FieldCount; ordinal++)
            {
                if (GetName(ordinal) == name)
                {
                    return ordinal;
                }
            }
            throw new IndexOutOfRangeException();
        }

        public override DataTable GetSchemaTable()
        {
            throw new NotImplementedException();
        }

        public override string GetString(int ordinal)
        {
            return Convert.ToString(this[ordinal]);
        }

        public override object GetValue(int ordinal)
        {
            return this[ordinal];
        }

        public override int GetValues(object[] values)
        {
            int count = 0;
            for (int i = 0; i < FieldCount && i < values.Length; i++)
            {
                count += 1;
                values[i] = this[i];
            }
            return count;
        }

        public override bool HasRows
        {
            get { return _objects.Any(); }
        }

        public override bool IsClosed
        {
            get { return _closed; }
        }

        public override bool IsDBNull(int ordinal)
        {
            return this[ordinal] is DBNull;
        }

        public override bool NextResult()
        {
            throw new NotImplementedException();
        }

        public override bool Read()
        {
            if (_objectEnumerator.MoveNext())
            {
                _recordsAffected += 1;
                return true;
            }
            return false;
        }

        public override int RecordsAffected
        {
            get { return _recordsAffected; }
        }

        public override object this[string name]
        {
            get { return this[GetOrdinal(name)]; }
        }

        public override object this[int ordinal]
        {
            get { return _properties[ordinal].GetValue(_objectEnumerator.Current, new object[] { }); }
        }

        public IEnumerable<string> FieldNames
        {
            get
            {
                for (var i = 0; i < FieldCount; i++)
                {
                    yield return GetName(i);
                }
            }
        }

        public IEnumerable<object> Values
        {
            get
            {
                for (var i = 0; i < FieldCount; i++)
                {
                    yield return this[i];
                }
            }
        }

        private class ObjectReaderEnumerator : IEnumerator<ObjectReader<T>>
        {
            private ObjectReader<T> _reader;

            public ObjectReaderEnumerator(ObjectReader<T> reader)
            {
                _reader = reader;
            }
            
            public ObjectReader<T> Current
            {
                get { return _reader; }
            }

            public void Dispose()
            {
                _reader = null;
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                return _reader.Read();
            }

            public void Reset()
            {
                _reader._objectEnumerator.Reset();
            }
        }

        IEnumerator<ObjectReader<T>> IEnumerable<ObjectReader<T>>.GetEnumerator()
        {
            return new ObjectReaderEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
