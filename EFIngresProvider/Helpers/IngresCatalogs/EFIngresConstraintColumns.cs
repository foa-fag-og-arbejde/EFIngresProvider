using System.Collections.Generic;

namespace EFIngresProvider.Helpers.IngresCatalogs
{
    public class EFIngresConstraintColumns : CatalogHelper
    {
        protected override IEnumerable<string> DependsOn
        {
            get
            {
                yield return "EFIngresConstraints";
                yield return "EFIngresTableColumns";
            }
        }

        protected override void CreateCatalogInternal()
        {
            DropAndCreateSessionTableAs("EFIngresConstraintColumns", @"
                select ConstraintId    = '[' + trim(schema_name) + '][' + trim(table_name) + '][' + trim(constraint_name) + ']',
                       ColumnId        = '[' + trim(schema_name) + '][' + trim(table_name) + '][' + trim(column_name)     + ']',
                       constraint_name = constraint_name,
                       table_owner     = schema_name,
                       table_name      = table_name,
                       column_name     = column_name
                  from iikeys
                union
                select ConstraintId    = c.Id,
                       ColumnId        = '[' + trim(c.table_owner) + '][' + trim(c.table_name) + '][' + trim(k.column_name) + ']',
                       constraint_name = c.constraint_name,
                       table_owner     = c.table_owner,
                       table_name      = c.table_name,
                       column_name     = k.column_name
                  from session.EFIngresConstraints c
                  join iikey_columns k on
                       k.table_owner = c.table_owner
                   and k.table_name  = c.table_name
                 where c.constraint_type     = 'P'
                   and c.virtual_constraint  = 1
                union
                select ConstraintId    = c.Id,
                       ColumnId        = k.Id,
                       constraint_name = c.constraint_name,
                       table_owner     = c.table_owner,
                       table_name      = c.table_name,
                       column_name     = k.column_name
                  from session.EFIngresConstraints c
                  join session.EFIngresTableColumns k on
                       k.table_owner     = c.table_owner
                   and k.table_name      = c.table_name
                   and k.column_sequence = 1
                 where c.constraint_type    = 'P'
                   and c.virtual_constraint = 1
                   and c.table_type         = 'T'
            ");

            ExecuteSql(@"
                insert into session.EFIngresConstraintColumns
                select ConstraintId    = c.Id,
                       ColumnId        = '[' + trim(c.table_owner) + '][' + trim(c.table_name) + '][' + trim(k.column_name) + ']',
                       constraint_name = c.constraint_name,
                       table_owner     = c.table_owner,
                       table_name      = c.table_name,
                       column_name     = k2.column_name
                  from session.EFIngresConstraints c
                  join session.EFIngresConstraintColumns k on
                       k.table_owner     = c.table_owner
                   and k.table_name      = 'k_' + shift(c.table_name, -2)
                  join session.EFIngresTableColumns k2 on
                       k2.table_owner     = c.table_owner
                   and k2.table_name      = c.table_name
                   and k2.column_name     = k.column_name
                   and k2.IsNullable      = 0
                 where c.constraint_type     = 'P'
                   and c.virtual_constraint  = 1
                   and c.table_type          = 'V'
                   and left(c.table_name, 2) = 'l_'
                   and not exists ( select 1
                                      from session.EFIngresConstraintColumns cc
                                     where cc.ConstraintId = c.Id )
            ");

            ExecuteSql(@"
                insert into session.EFIngresConstraintColumns
                select ConstraintId    = c.Id,
                       ColumnId        = k.Id,
                       constraint_name = c.constraint_name,
                       table_owner     = c.table_owner,
                       table_name      = c.table_name,
                       column_name     = k.column_name
                  from session.EFIngresConstraints c
                  join session.EFIngresTableColumns k on
                       k.table_owner     = c.table_owner
                   and k.table_name      = c.table_name
                   and k.IsNullable      = 0
                 where c.constraint_type    = 'P'
                   and c.virtual_constraint = 1
                   and c.table_type         = 'V'
                   and not exists ( select 1
                                      from session.EFIngresConstraintColumns cc
                                     where cc.ConstraintId = c.Id )
            ");
        }
    }
}
