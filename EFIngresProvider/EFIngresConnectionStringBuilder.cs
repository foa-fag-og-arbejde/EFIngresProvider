using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Common;
using Ingres.Client;
using System.Collections;
using System.ComponentModel;
using EFIngresProvider.Helpers;

namespace EFIngresProvider
{
    public class EFIngresConnectionStringBuilder : DbConnectionStringBuilder
    {
        private static readonly Dictionary<string, object> _efIngresDefaults = new Dictionary<string, object>
        {
            { "TrimChars", false },
            { "UseIngresDate", false },
            { "JoinOPGreedy", false },
            { "JoinOPTimeout", 0 },
        };

        private static readonly Dictionary<string, List<string>> _efIngresKeys = new Dictionary<string, List<string>>
        {
            { "TrimChars", new List<string> { "TrimChars", "Trim Chars" } },
            { "UseIngresDate", new List<string> { "UseIngresDate", "Use IngresDate", "Use Ingres Date" } },
            { "JoinOPGreedy", new List<string> { "JoinOPGreedy" } },
            { "JoinOPTimeout", new List<string> { "JoinOPTimeout" } }
        };

        private static readonly Dictionary<string, string> _aliases = _efIngresKeys.SelectMany(x => x.Value.Select(y => new { Key = x.Key, Value = y }))
                                                                                   .ToDictionary(x => x.Value, x => x.Key, StringComparer.InvariantCultureIgnoreCase);

        private Dictionary<string, object> _items = _efIngresDefaults.ToDictionary(x => x.Key, x => x.Value, StringComparer.InvariantCultureIgnoreCase);

        private IngresConnectionStringBuilder _wrappedConnectionStringBuilder;

        public EFIngresConnectionStringBuilder(string connectionString)
        {
            var builder = new DbConnectionStringBuilder();
            _wrappedConnectionStringBuilder = new IngresConnectionStringBuilder();
            builder.ConnectionString = connectionString;
            foreach (var alias in builder.Keys.Cast<string>())
            {
                string key;
                if (_aliases.TryGetValue(alias, out key))
                {
                    _items[key] = builder[alias];
                }
                else
                {
                    _wrappedConnectionStringBuilder[alias] = builder[alias];
                }
            }

            ConnectionString = connectionString;
            //foreach (string key in Keys)
            //{
            //    this[key] = this[key];
            //}
        }

        public EFIngresConnectionStringBuilder()
            : this("")
        {
        }

        [Browsable(false)]
        public string IngresConnectionString
        {
            get { return _wrappedConnectionStringBuilder.ConnectionString; }
        }

        #region ConnectionString properties

        /// <summary>
        /// BlankDate=null specifies that an Ingres blank (empty) date
        /// result value is to be returned to the application as a
        /// null value.  The default is to return an Ingres blank date
        /// as a DateTime value of "9999-12-31 23:59:59".
        /// </summary>
        [Category("Locale")]
        [RefreshProperties(RefreshProperties.All)]
        [Description(
          "BlankDate=\"null\" specifies that an Ingres blank (empty) date " +
          "result value is to be returned to the application as a " +
          "null value.  The default is to return an Ingres blank date " +
          "as a DateTime value of \"9999-12-31 23:59:59\".")]
        [DisplayName("BlankDate")]
        public string BlankDate
        {
            get { return this["BlankDate"].ToString(); }
            set { this["BlankDate"] = value; }
        }  // BlankDate

        /// <summary>
        /// Specifies the .NET character encoding (e.g. ISO-8859-1) or 
        /// code page (e.g. cp1252) to be used for conversions between 
        /// Unicode and character data types.  Generally, the character 
        /// encoding is determined automatically by the data provider 
        /// from the Data Access Server installation character set.  
        /// This keyword allows an alternate character encoding to be 
        /// specified (if desired) or a valid character encoding to be 
        /// used if the data provider is unable to map the server’s character set.
        /// </summary>
        [Category("Locale")]
        [RefreshProperties(RefreshProperties.All)]
        [Description(
            "Specifies the .NET character encoding (e.g. ISO-8859-1) " +
            "or code page (e.g. cp1252) to be used for conversions " +
            "between Unicode and character data types.  Generally, " +
            "the character encoding is determined automatically by " +
            "the data provider from the Data Access Server " +
            "installation character set.  This keyword allows an " +
            "alternate character encoding to be specified (if desired) " +
            "or a valid character encoding to be used if the data provider " +
            "is unable to map the server’s character set.")]
        [DisplayName("Character Encoding")]
        public string CharacterEncoding
        {
            get { return this["Character Encoding"].ToString(); }
            set { this["Character Encoding"] = value; }
        }  // CharacterEncoding

        /// <summary>
        /// The time, in seconds, to wait for an attempted connection
        /// to time out if the connection has not completed.
        /// Default is 15.
        /// </summary>
        [Description("The time (in seconds) to wait for the connection attempt.  Default is 15")]
        [DisplayName("Connect Timeout")]
        public int ConnectTimeout
        {
            get { return (int)this["Connect Timeout"]; }
            set { this["Connect Timeout"] = value; }
        } // ConnectTimeout

        /// <summary>
        /// Specifies the default cursor concurrency mode, 
        /// which determines the concurrency of cursors that have 
        /// no explicitly assigned option in the command text, 
        /// e.g. "FOR UPDATE" or "FOR READONLY".
        /// </summary>
        [RefreshProperties(RefreshProperties.All)]
        [Description(
          "Specifies the default cursor concurrency mode, " +
          "which determines the concurrency of cursors that have " +
          "no explicitly assigned option in the command text, " +
          "e.g. \"FOR UPDATE\" or \"FOR READONLY\".  " +
          "Available options are: " +
          "\"readonly\" – Provides non-updateable cursors " +
          "for best performance (default); " +
          "\"update\" - Provides updateable cursors; " +
          "\"dbms\" – Concurrencly is assigned by the DBMS server")]
        [DisplayName("Cursor_Mode")]
        public string CursorMode
        {
            get { return this["Cursor_Mode"].ToString(); }
            set { this["Cursor_Mode"] = value; }
        }  // CursorMode

        /// <summary>
        /// Name of the Ingres database being connected to.
        /// If a server class is required, use syntax: dbname/server_class
        /// </summary>
        [Category("(Database)")]
        [RefreshProperties(RefreshProperties.All)]
        [Description("The database name opened or to be used upon connection.")]
        public string Database
        {
            get { return this["Database"].ToString(); }
            set { this["Database"] = value; }
        }  // Database

        /// <summary>
        /// Specifies the Ingres date format to be used by the Ingres server
        /// for date literals.  Corresponds to the Ingres environment
        /// variable II_DATE_FORMAT and is assigned the same values.
        /// </summary>
        [Category("Locale")]
        [RefreshProperties(RefreshProperties.All)]
        [Description(
          "Specifies the Ingres date format to be used by the Ingres server " +
          "for date literals.  Corresponds to the Ingres environment " +
          "variable II_DATE_FORMAT and is assigned the same values.")]
        [DisplayName("Date_format")]
        public string DateFormat
        {
            get { return this["Date_format"].ToString(); }
            set { this["Date_format"] = value; }
        }  // DateFormat

        /// <summary>
        /// The user name to be associated with the DBMS session. (Equivalent to
        /// the Ingres -u flag which can require administrator privileges).
        /// </summary>
        [RefreshProperties(RefreshProperties.All)]
        [Description("The user name to be associated with the DBMS session. " +
            "(Equivalent to the Ingres -u flag " +
            "which can require administrator privileges).")]
        [DisplayName("Dbms_user")]
        public string DbmsUser
        {
            get { return this["Dbms_user"].ToString(); }
            set { this["Dbms_user"] = value; }
        } // DbmsUser

        /// <summary>
        /// The user's DBMS password for the session. 
        /// (Equivalent to the Ingres -P flag).
        /// </summary>
        [RefreshProperties(RefreshProperties.All)]
        [PasswordPropertyText(true)]
        [Description("The user's DBMS password for the session. " +
            " (Equivalent to the Ingres -P flag).")]
        [DisplayName("Dbms_password")]
        public string DbmsPassword
        {
            get { return this["Dbms_password"].ToString(); }
            set { this["Dbms_password"] = value; }
        } // DbmsPassword

        /// <summary>
        /// Specifies the character that the Ingres DBMS Server is to
        /// use the comma (',') or the period ('.') to separate fractional
        /// and non-fractional parts of a number.  Default is the period ('.').
        /// </summary>
        [Category("Locale")]
        [RefreshProperties(RefreshProperties.All)]
        [Description(
          "Specifies the character that the Ingres DBMS Server is to " +
          "use the comma (',') or the period ('.') to separate fractional " +
          "and non-fractional parts of a number.  Default is the period ('.').")]
        [DisplayName("Decimal_char")]
        public string DecimalChar
        {
            get { return this["Decimal_char"].ToString(); }
            set { this["Decimal_char"] = value; }
        }  // DecimalChar

        /// <summary>
        /// If set to true and if the creation thread is within a transaction
        /// context as established by System.EnterpriseServices.ServicedComponent,
        /// the IngresConnection is automatically enlisted into the transaction
        /// context.  Default is true.
        /// </summary>
        [RefreshProperties(RefreshProperties.All)]
        [Description(
          "If set to true and if the creation thread is within a transaction " +
          "context as established by System.EnterpriseServices.ServicedComponent, " +
          "the IngresConnection is automatically enlisted into the transaction " +
          "context.  Default is true.")]
        public bool Enlist
        {
            get { return (bool)this["Enlist"]; }
            set { this["Enlist"] = value; }
        } // Enlist

        /// <summary>
        /// Group identifier that has permissions for a group of users.
        /// </summary>
        [DisplayName("Group ID")]
        public string GroupID
        {
            get { return this["Group ID"].ToString(); }
            set { this["Group ID"] = value; }
        }  // GroupID

        /// <summary>
        /// Maximum number of connections that can be in the pool.
        /// Default is 100.
        /// </summary>
        [Category("Pooling")]
        [RefreshProperties(RefreshProperties.All)]
        [Description("The maximum number of connections that can be in the pool.  Default is 100")]
        [DisplayName("Max Pool Size")]
        public int MaxPoolSize
        {
            get { return (int)this["Max Pool Size"]; }
            set { this["Max Pool Size"] = value; }
        } // MaxPoolSize

        /// <summary>
        /// Minimum number of connections that can be in the pool.
        /// Default is 0.
        /// </summary>
        [Category("Pooling")]
        [RefreshProperties(RefreshProperties.All)]
        [Description("The minimum number of connections that can be in the pool.  Default is 0")]
        [DisplayName("Min Pool Size")]
        public int MinPoolSize
        {
            get { return (int)this["Min Pool Size"]; }
            set { this["Min Pool Size"] = value; }
        } // MinPoolSize

        /// <summary>
        /// Specifies the Ingres money format to be used by the Ingres server
        /// for money literals.  Corresponds to the Ingres environment
        /// variable II_MONEY_FORMAT and is assigned the same values.
        /// </summary>
        [Category("Locale")]
        [RefreshProperties(RefreshProperties.All)]
        [Description(
          "Specifies the Ingres money format to be used by the Ingres server " +
          "for money literals.  Corresponds to the Ingres environment " +
          "variable II_MONEY_FORMAT and is assigned the same values.")]
        [DisplayName("Money_format")]
        public string MoneyFormat
        {
            get { return this["Money_format"].ToString(); }
            set { this["Money_format"] = value; }
        } // MoneyFormat

        /// <summary>
        /// Specifies the money precision to be used by the Ingres server
        /// for money literals.  Corresponds to the Ingres environment
        /// variable II_MONEY_PREC and is assigned the same values.
        /// </summary>
        [Category("Locale")]
        [RefreshProperties(RefreshProperties.All)]
        [Description(
          "Specifies the money precision to be used by the Ingres server " +
          "for money literals.  Corresponds to the Ingres environment " +
          "variable II_MONEY_PREC and is assigned the same values.")]
        [DisplayName("Money_precision")]
        public string MoneyPrecision
        {
            get { return this["Money_precision"].ToString(); }
            set { this["Money_precision"] = value; }
        } // MoneyPrecision

        /// <summary>
        /// The password to the Ingres database.
        /// </summary>
        [Category("Authentication")]
        [RefreshProperties(RefreshProperties.All)]
        [PasswordPropertyText(true)]
        [Description("The password to the database.  This value may be " +
            "case-sensitive depending on the server.")]
        public string Password
        {
            get { return this["Password"].ToString(); }
            set { this["Password"] = value; }
        } // Password

        /// <summary>
        /// Indicate whether password information is returned in a get
        /// of the ConnectionString.
        /// </summary>
        [Category("Authentication")]
        [RefreshProperties(RefreshProperties.All)]
        [Description(
          "Indicate whether password information is returned in a get " +
          "of the ConnectionString.  Default is 'false'.")]
        [DisplayName("Persist Security Info")]
        public bool PersistSecurityInfo
        {
            get { return (bool)this["Persist Security Info"]; }
            set { this["Persist Security Info"] = value; }
        } // PersistSecurityInfo

        /// <summary>
        /// Enables or disables connection pooling.  By default,
        /// connection pooling is enabled (true).
        /// </summary>
        [Category("Pooling")]
        [RefreshProperties(RefreshProperties.All)]
        [Description("Enables or disables connection pooling.  By default, " +
           "connection pooling is enabled (true).")]
        public bool Pooling
        {
            get { return (bool)this["Pooling"]; }
            set { this["Pooling"] = value; }
        } // Pooling

        /// <summary>
        /// Port number on the target server machine that the
        /// Data Access Server (DAS) is listening to.  Default is "II7".
        /// </summary>
        [Category("(Server)")]
        [RefreshProperties(RefreshProperties.All)]
        [Description("The TCP port id that the Data Access Server listens to.  Default is \"II7\".")]
        [DefaultValue("II7")]
        public string Port
        {
            get { return this["Port"].ToString(); }
            set { this["Port"] = value; }
        } // Port

        /// <summary>
        /// Role identifier that has associated privileges for the role.
        /// </summary>
        [Category("Authentication")]
        [RefreshProperties(RefreshProperties.All)]
        [Description("Role idenitifier that has associated privileges for the role.")]
        [DisplayName("Role ID")]
        public string RoleID
        {
            get { return this["Role ID"].ToString(); }
            set { this["Role ID"] = value; }
        } // RoleID

        /// <summary>
        /// Role password associated with the Role ID.
        /// </summary>
        [Category("Authentication")]
        [RefreshProperties(RefreshProperties.All)]
        [PasswordPropertyText(true)]
        [Description("Role password associated with the Role ID.")]
        [DisplayName("Role Password")]
        public string RolePassword
        {
            get { return this["Role Password"].ToString(); }
            set { this["Role Password"] = value; }
        } // RolePassword

        /// <summary>
        /// If set to true, send DateTime, Date, and Time parameters as
        /// ingresdate data type rather than as ANSI timestamp_with_timezone,
        /// ANSI date, ANSI time respectively.  This option may be used for
        /// compatibility with semantic rules from older releases of Ingres.
        /// Default is false.
        /// </summary>
        [Category("Locale")]
        [RefreshProperties(RefreshProperties.All)]
        [Description(
         "If set to true, send DateTime, Date, and Time parameters as " +
         "ingresdate data type rather than as ANSI timestamp_with_timezone, " +
         "ANSI date, ANSI time respectively.  This option may be used for " +
         "compatibility with semantic rules from older releases of Ingres.  " +
         "Default is false.")]
        [DefaultValue(false)]
        [DisplayName("SendIngresDates")]
        public Boolean SendIngresDates
        {
            get { return (bool)this["SendIngresDates"]; }
            set { this["SendIngresDates"] = value; }
        }

        /// <summary>
        /// The Ingres server to connect to.
        /// </summary>
        [Category("(Server)")]
        [RefreshProperties(RefreshProperties.All)]
        [Description("The name of the Ingres server connected to.")]
        public string Server
        {
            get { return this["Server"].ToString(); }
            set { this["Server"] = value; }
        } // Server


        /// <summary>
        /// Specifies the Ingres time zone associated with the user's location.
        /// Used by the Ingres server only.  Corresponds to the Ingres environment
        /// variable II_TIMEZONE_NAME and is assigned the same values.
        /// </summary>
        [Category("Locale")]
        [RefreshProperties(RefreshProperties.All)]
        [Description(
          "Specifies the Ingres time zone associated with the user's location. " +
          "Used by the Ingres server only.  Corresponds to the Ingres environment " +
          "variable II_TIMEZONE_NAME and is assigned the same values.")]
        public string Timezone
        {
            get { return this["Timezone"].ToString(); }
            set { this["Timezone"] = value; }
        } // Timezone

        /// <summary>
        /// The name of the authorized user connecting to the DBMS Server.
        /// This value may be case-sensitive depending on the targer server.
        /// </summary>
        [Category("Authentication")]
        [RefreshProperties(RefreshProperties.All)]
        [Description("The name of the authorized user connecting to the DBMS Server. " +
           "This value may be case-sensitive depending on the targer server.")]
        [DisplayName("User ID")]
        public string UserID
        {
            get { return this["User ID"].ToString(); }
            set { this["User ID"] = value; }
        }

        /// <summary>
        /// Allows the .NET application to control the portions of the vnode
        /// information that are used to establish the connection to the remote
        /// DBMS server through the Ingres DAS server.
        /// Valid options are: 
        ///    "connect" - Only the vnode connection information is used (default).
        ///    "login"   - Both the vnode connection and login information is used.
        /// </summary>
        [RefreshProperties(RefreshProperties.All)]
        [Description(
         "Allows the .NET application to control the portions of the vnode " +
         "information that are used to establish the connection to the remote " +
         "DBMS server through the Ingres DAS server. " +
           "Valid options are: " +
            "\"connect\" - Only the vnode connection information is used (default). " +
            "\"login\"   - Both the vnode connection and login information is used.")]
        [DisplayName("Vnode_usage")]
        public string VnodeUsage
        {
            get { return this["Vnode_usage"].ToString(); }
            set { this["Vnode_usage"] = value; }
        } // VnodeUsage

        /// <summary>
        /// If set to true, any char and nchar columns retrieved will have trailing blanks removed
        /// </summary>
        [RefreshProperties(RefreshProperties.All)]
        [Description("If set to true, any char and nchar columns retrieved will have trailing blanks removed")]
        [DisplayName("Trim Chars")]
        [DefaultValue(false)]
        public bool TrimChars
        {
            get { return (bool)this["TrimChars"]; }
            set { this["TrimChars"] = value; }
        } // TrimChars

        /// <summary>
        /// If set to true, Ingres dates are retrieved as EFIngresProvider.IngresDate, in stead og System.DateTime.
        /// This does NOT work with entities - it only works when using EFIngresProvider.EFIngresCommand directly.
        /// </summary>
        [RefreshProperties(RefreshProperties.All)]
        [Description("If set to true, Ingres dates are retrieved as EFIngresProvider.IngresDate, in stead og System.DateTime. " +
                     "This does NOT work with entities - it only works when using EFIngresProvider.EFIngresCommand directly.")]
        [DisplayName("Use Ingres Date")]
        [DefaultValue(false)]
        public bool UseIngresDate
        {
            get { return (bool)this["UseIngresDate"]; }
            set { this["UseIngresDate"] = value; }
        } // UseIngresDate

        /// <summary>
        /// If set to true, the connection will run SET JOINOP GREEDY every time it is opened.
        /// </summary>
        [RefreshProperties(RefreshProperties.All)]
        [Description("If set to true, the connection will run SET JOINOP GREEDY every time it is opened.")]
        [DisplayName("Join OP Greedy")]
        [DefaultValue(false)]
        public bool JoinOPGreedy
        {
            get { return (bool)this["JoinOPGreedy"]; }
            set { this["JoinOPGreedy"] = value; }
        } // JoinOPGreedy

        /// <summary>
        /// If set to a value greater than 0, the connection will run SET JOINOP TIMEOUT {value} every time it is opened.
        /// </summary>
        [RefreshProperties(RefreshProperties.All)]
        [Description("If set to a value greater than 0, the connection will run SET JOINOP TIMEOUT {value} every time it is opened.")]
        [DisplayName("Join OP Timeout")]
        [DefaultValue(0)]
        public int JoinOPTimeout
        {
            get { return (int)this["JoinOPTimeout"]; }
            set { this["JoinOPTimeout"] = value; }
        } // JoinOPTimeout

        #endregion

        #region DbConnectionStringBuilder methods

        public override bool IsFixedSize
        {
            get { return true; }
        }

        private IEnumerable<object> GetKeys()
        {
            foreach (var key in _items.Keys)
            {
                yield return key;
            }
            foreach (var key in _wrappedConnectionStringBuilder.Keys)
            {
                yield return key;
            }
        }

        private IEnumerable<object> GetValues()
        {
            foreach (var value in _items.Values)
            {
                yield return value;
            }
            foreach (var value in _wrappedConnectionStringBuilder.Values)
            {
                yield return value;
            }
        }

        public override ICollection Keys
        {
            get { return GetKeys().ToArray(); }
        }

        public override ICollection Values
        {
            get { return GetValues().ToArray(); }
        }

        public override object this[string keyword]
        {
            get
            {
                string key;
                if (_aliases.TryGetValue(keyword, out key))
                {
                    object value;
                    if (_items.TryGetValue(key, out value))
                    {
                        return value;
                    }
                    return _efIngresDefaults[key];
                }
                else
                {
                    return _wrappedConnectionStringBuilder[keyword];
                }
            }
            set
            {
                string key;
                if ((value == null) || string.IsNullOrWhiteSpace(value.ToString()))
                {
                    Remove(keyword);
                }
                else if (_aliases.TryGetValue(keyword, out key))
                {
                    var nativeValue = value;  // assume no conversion needed
                    var sourceTypeCode = Type.GetTypeCode(value.GetType());
                    var targetTypeCode = Type.GetTypeCode(_efIngresDefaults[key].GetType());

                    if (targetTypeCode != sourceTypeCode)
                    {
                        switch (targetTypeCode)
                        {
                            case TypeCode.Int32:
                                nativeValue = Int32.Parse(value.ToString());
                                break;
                            case TypeCode.Boolean:
                                var valueString = value.ToString().Trim();
                                var valueLower = valueString.ToLowerInvariant();

                                // Catch yes/no literals that Boolean.Parse doesn't
                                if (valueLower == "yes" || valueLower == "1")
                                {
                                    nativeValue = true;
                                    value = "true";
                                    break;
                                }
                                if (valueLower == "no" || valueLower == "0")
                                {
                                    nativeValue = false;
                                    value = "false";
                                    break;
                                }
                                nativeValue = Boolean.Parse(valueString);
                                break;
                            case TypeCode.String:
                                nativeValue = value.ToString();
                                break;
                            default:
                                throw new System.InvalidOperationException(
                                    "EFIngresConnectionStringBuilder internal error: " +
                                    "unexpected Type for keyword '" + keyword + "'.");
                        }
                    }
                    base[key] = nativeValue;
                    _items[key] = nativeValue;
                }
                else
                {
                    var keyOrdinal = _wrappedConnectionStringBuilder.TryGetOrdinal(keyword);
                    key = _wrappedConnectionStringBuilder.Keys.Cast<string>().ElementAt(keyOrdinal);
                    _wrappedConnectionStringBuilder[key] = value;
                    base[key] = value;
                }
            }
        }

        public override void Clear()
        {
            _items.Clear();
            _wrappedConnectionStringBuilder.Clear();
        }

        public override bool ContainsKey(string keyword)
        {
            string key;
            return _aliases.TryGetValue(keyword, out key) || _wrappedConnectionStringBuilder.ContainsKey(keyword);
        }

        public override bool Remove(string keyword)
        {
            string key;
            if (_aliases.TryGetValue(keyword, out key))
            {
                return _items.Remove(key);
            }
            else
            {
                return _wrappedConnectionStringBuilder.Remove(keyword);
            }
        }

        public override bool ShouldSerialize(string keyword)
        {
            string key;
            if (_aliases.TryGetValue(keyword, out key))
            {
                return base.ShouldSerialize(key);
            }
            else
            {
                return _wrappedConnectionStringBuilder.ShouldSerialize(keyword);
            }
        }

        public override int Count
        {
            get { return _items.Count + _wrappedConnectionStringBuilder.Count; }
        }

        public override bool TryGetValue(string keyword, out object value)
        {
            string key;
            if (_aliases.TryGetValue(keyword, out key))
            {
                return _items.TryGetValue(key, out value);
            }
            else
            {
                return _wrappedConnectionStringBuilder.TryGetValue(keyword, out value);
            }
        }

        #endregion
    }
}
