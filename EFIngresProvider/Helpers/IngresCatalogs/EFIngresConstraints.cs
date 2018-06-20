using System.Collections.Generic;

namespace EFIngresProvider.Helpers.IngresCatalogs
{
    public class EFIngresConstraints : CatalogHelper
    {
        protected override IEnumerable<string> DependsOn
        {
            get
            {
                yield return "EFIngresTables";
            }
        }

        protected override void CreateCatalogInternal()
        {
            DropAndCreateSessionTableAs("EFIngresConstraints", @"
                select Id                  = '[' + trim(c.schema_name) + '][' + trim(c.table_name) + '][' + trim(c.constraint_name) + ']',
                       ParentId            = '[' + trim(c.schema_name) + '][' + trim(c.table_name) + ']',
                       Name                = trim(c.constraint_name),
                       ConstraintType      = case c.constraint_type when 'U' then 'UNIQUE'
                                                                    when 'C' then 'CHECK'
                                                                    when 'R' then 'FOREIGN KEY'
                                                                    when 'P' then 'PRIMARY KEY' end,
                       IsDeferrable        = int1(0),
                       IsInitiallyDeferred = int1(0),
                       table_owner         = c.schema_name,
                       table_name          = c.table_name,
                       table_type          = 'T',
                       constraint_name     = c.constraint_name,
                       constraint_type     = c.constraint_type,
                       virtual_constraint  = 0
                  from iiconstraints c
                 where c.constraint_type in ('U', 'P', 'C', 'R')
            ");

            ExecuteSql(@"
                insert into session.EFIngresConstraints
                select Id                  = '[' + trim(t.table_owner) + '][' + trim(t.table_name) + ']-key',
                       ParentId            = '[' + trim(t.table_owner) + '][' + trim(t.table_name) + ']',
                       Name                = '[' + trim(t.table_owner) + '][' + trim(t.table_name) + ']-key',
                       ConstraintType      = 'UNIQUE',
                       IsDeferrable        = int1(0),
                       IsInitiallyDeferred = int1(0),
                       table_owner         = t.table_owner,
                       table_name          = t.table_name,
                       table_type          = t.table_type,
                       constraint_name     = 'efingres-virtual-key',
                       constraint_type     = 'U',
                       virtual_constraint  = 1
                  from session.EFIngresTables t
                 where t.table_type = 'T'
                   and t.unique_rule = 'U'
                   and exists ( select 1
                                  from session.EFIngresConstraints c
                                 where c.table_owner = t.table_owner
                                   and c.table_name  = t.table_name
                                   and c.constraint_type = 'P' )
            ");

            ExecuteSql(@"
                insert into session.EFIngresConstraints
                select Id                  = '[' + trim(t.table_owner) + '][' + trim(t.table_name) + ']-key',
                       ParentId            = '[' + trim(t.table_owner) + '][' + trim(t.table_name) + ']',
                       Name                = '[' + trim(t.table_owner) + '][' + trim(t.table_name) + ']-key',
                       ConstraintType      = 'PRIMARY KEY',
                       IsDeferrable        = int1(0),
                       IsInitiallyDeferred = int1(0),
                       table_owner         = trim(t.table_owner),
                       table_name          = trim(t.table_name),
                       table_type          = t.table_type,
                       constraint_name     = 'efingres-virtual-key',
                       constraint_type     = 'P',
                       virtual_constraint  = 1
                  from session.EFIngresTables t
                 where not exists ( select 1
                                     from session.EFIngresConstraints c
                                    where c.table_owner = t.table_owner
                                      and c.table_name  = t.table_name
                                      and c.constraint_type = 'P' )
            ");
        }
    }
}
