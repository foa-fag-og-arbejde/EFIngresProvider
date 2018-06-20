using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Data.Framework.AdoDotNet;
using Microsoft.VisualStudio.Data.Services;
using Ingres.Client;
using System.Diagnostics;
using System.Data;

namespace EFIngresDDEXProvider
{
    internal class EFIngresSourceInformation : AdoDotNetSourceInformation
    {
        public EFIngresSourceInformation()
        {
            AddProperty(SupportsAnsi92Sql, true);
            AddProperty(SupportsQuotedIdentifierParts, true);
            AddProperty(IdentifierOpenQuote, "\"");
            AddProperty(IdentifierCloseQuote, "\"");
            AddProperty(ServerSeparator, ".");
            AddProperty(CatalogSupported, true);
            AddProperty(CatalogSupportedInDml, true);
            AddProperty(SchemaSupported, true);
            AddProperty(SchemaSupportedInDml, true);
            AddProperty(SchemaSeparator, ".");
            AddProperty(ParameterPrefix, "@");
            AddProperty(ParameterPrefixInName, true);
            AddProperty(DefaultSchema);
        }

        protected override object RetrieveValue(string propertyName)
        {
            PopulateValues();
            object value;
            if (!_values.TryGetValue(propertyName, out value))
            {
                value = base.RetrieveValue(propertyName);
            }
            return value;
        }

        protected override void OnSiteChanged(EventArgs e)
        {
            _values = null;
            base.OnSiteChanged(e);
        }

        private Dictionary<string, object> _values = null;
        private void PopulateValues()
        {
            if (_values == null)
            {
                _values = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
                if (Site.State != DataConnectionState.Open)
                {
                    Site.Open();
                }
                //var conn = Connection as EFIngresConnection;
                var conn = Connection;
                Debug.Assert(conn != null, "Invalid provider object.");
                if (conn != null)
                {
                    var comm = conn.CreateCommand();
                    try
                    {
                        comm.CommandText = @"
                            select info_autocommit_state       = dbmsinfo('autocommit_state'),
                                   info__bintim                = dbmsinfo('_bintim'),
                                   info__bio_cnt               = dbmsinfo('_bio_cnt'),
                                   info_cache_dynamic          = dbmsinfo('cache_dynamic'),
                                   info_charset                = dbmsinfo('charset'),
                                   info_collation              = dbmsinfo('collation'),
                                   info_connect_time_limit     = dbmsinfo('connect_time_limit'),
                                   info_create_procedure       = dbmsinfo('create_procedure'),
                                   info_create_table           = dbmsinfo('create_table'),
                                   info__cpu_ms                = dbmsinfo('_cpu_ms'),
                                   info_current_priv_mask      = dbmsinfo('current_priv_mask'),
                                   info_cursor_default_mode    = dbmsinfo('cursor_default_mode'),
                                   info_cursor_update_mode     = dbmsinfo('cursor_update_mode'),
                                   info_database               = dbmsinfo('database'),
                                   info_datatype_major_level   = dbmsinfo('datatype_major_level'),
                                   info_datatype_minor_level   = dbmsinfo('datatype_minor_level'),
                                   info_date_format            = dbmsinfo('date_format'),
                                   info_dba                    = dbmsinfo('dba'),
                                   info_db_admin               = dbmsinfo('db_admin'),
                                   info_db_cluster_node        = dbmsinfo('db_cluster_node'),
                                   info_db_count               = dbmsinfo('db_count'),
                                   info_db_real_user_case      = dbmsinfo('db_real_user_case'),
                                   info_dbms_bio               = dbmsinfo('dbms_bio'),
                                   info_dbms_cpu               = dbmsinfo('dbms_cpu'),
                                   info_dbms_dio               = dbmsinfo('dbms_dio'),
                                   info_db_delimited_case      = dbmsinfo('db_delimited_case'),
                                   info_db_name_case           = dbmsinfo('db_name_case'),
                                   info_db_privileges          = dbmsinfo('db_privileges'),
                                   info_db_tran_id             = dbmsinfo('db_tran_id'),
                                   info_decimal_format         = dbmsinfo('decimal_format'),
                                   info__dio_cnt               = dbmsinfo('_dio_cnt'),
                                   info__et_sec                = dbmsinfo('_et_sec'),
                                   info_flatten_aggregate      = dbmsinfo('flatten_aggregate'),
                                   info_flatten_singleton      = dbmsinfo('flatten_singleton'),
                                   info_group                  = dbmsinfo('group'),
                                   info_idle_time_limit        = dbmsinfo('idle_time_limit'),
                                   info_ima_server             = dbmsinfo('ima_server'),
                                   info_ima_session            = dbmsinfo('ima_session'),
                                   info_ima_vnode              = dbmsinfo('ima_vnode'),
                                   info_initial_user           = dbmsinfo('initial_user'),
                                   info_language               = dbmsinfo('language'),
                                   info_lockmode               = dbmsinfo('lockmode'),
                                   info_lp64                   = dbmsinfo('lp64'),
                                   info_maxconnect             = dbmsinfo('maxconnect'),
                                   info_maxcost                = dbmsinfo('maxcost'),
                                   info_maxcpu                 = dbmsinfo('maxcpu'),
                                   info_maxidle                = dbmsinfo('maxidle'),
                                   info_maxio                  = dbmsinfo('maxio'),
                                   info_maxquery               = dbmsinfo('maxquery'),
                                   info_maxrow                 = dbmsinfo('maxrow'),
                                   info_maxpage                = dbmsinfo('maxpage'),
                                   info_max_page_size          = dbmsinfo('max_page_size'),
                                   info_max_priv_mask          = dbmsinfo('max_priv_mask'),
                                   info_max_tup_len            = dbmsinfo('max_tup_len'),
                                   info_money_format           = dbmsinfo('money_format'),
                                   info_money_prec             = dbmsinfo('money_prec'),
                                   info_on_error_state         = dbmsinfo('on_error_state'),
                                   info_open_count             = dbmsinfo('open_count'),
                                   info_page_size_2k           = dbmsinfo('page_size_2k'),
                                   info_page_size_4k           = dbmsinfo('page_size_4k'),
                                   info_page_size_8k           = dbmsinfo('page_size_8k'),
                                   info_page_size_16k          = dbmsinfo('page_size_16k'),
                                   info_page_size_32k          = dbmsinfo('page_size_32k'),
                                   info_page_size_64k          = dbmsinfo('page_size_64k'),
                                   info_pagetype_v1            = dbmsinfo('pagetype_v1'),
                                   info_pagetype_v2            = dbmsinfo('pagetype_v2'),
                                   info_pagetype_v3            = dbmsinfo('pagetype_v3'),
                                   info_pagetype_v4            = dbmsinfo('pagetype_v4'),
                                   info__pfault_cnt            = dbmsinfo('_pfault_cnt'),
                                   info_query_cost_limit       = dbmsinfo('query_cost_limit'),
                                   info_query_cpu_limit        = dbmsinfo('query_cpu_limit'),
                                   info_query_flatten          = dbmsinfo('query_flatten'),
                                   info_query_io_limit         = dbmsinfo('query_io_limit'),
                                   info_query_language         = dbmsinfo('query_language'),
                                   info_query_page_limit       = dbmsinfo('query_page_limit'),
                                   info_query_row_limit        = dbmsinfo('query_row_limit'),
                                   info_role                   = dbmsinfo('role'),
                                   info_security_audit_log     = dbmsinfo('security_audit_log'),
                                   info_security_audit_state   = dbmsinfo('security_audit_state'),
                                   info_security_priv          = dbmsinfo('security_priv'),
                                   info_select_syscat          = dbmsinfo('select_syscat'),
                                   info_server_class           = dbmsinfo('server_class'),
                                   info_session_id             = dbmsinfo('session_id'),
                                   info_session_priority       = dbmsinfo('session_priority'),
                                   info_session_priority_limit = dbmsinfo('session_priority_limit'),
                                   info_session_user           = dbmsinfo('session_user'),
                                   info_system_user            = dbmsinfo('system_user'),
                                   info_table_statistics       = dbmsinfo('table_statistics'),
                                   info_terminal               = dbmsinfo('terminal'),
                                   info_timeout_abort          = dbmsinfo('timeout_abort'),
                                   info_transaction_state      = dbmsinfo('transaction_state'),
                                   info_tup_len_2k             = dbmsinfo('tup_len_2k'),
                                   info_tup_len_4k             = dbmsinfo('tup_len_4k'),
                                   info_tup_len_8k             = dbmsinfo('tup_len_8k'),
                                   info_tup_len_16k            = dbmsinfo('tup_len_16k'),
                                   info_tup_len_32k            = dbmsinfo('tup_len_32k'),
                                   info_tup_len_64k            = dbmsinfo('tup_len_64k'),
                                   info_ucollation             = dbmsinfo('ucollation'),
                                   info_unicode_level          = dbmsinfo('unicode_level'),
                                   info_unicode_normalization  = dbmsinfo('unicode_normalization'),
                                   info_update_rowcnt          = dbmsinfo('update_rowcnt'),
                                   info_update_syscat          = dbmsinfo('update_syscat'),
                                   info_username               = dbmsinfo('username'),
                                   info__version               = dbmsinfo('_version')
                        ";

                        using (var reader = comm.ExecuteReader(CommandBehavior.SequentialAccess))
                        {
                            if (reader.Read())
                            {
                                _values[DefaultSchema] = (string)reader["info_dba"];
                            }
                        }
                    }
                    catch (IngresException)
                    {
                        // We let the base class apply default behavior
                    }
                }
            }
        }
    }
}
