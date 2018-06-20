namespace EFIngresProvider.Helpers.IngresCatalogs
{
    public class EFIngresForeignKeyConstraints : CatalogHelper
    {
        protected override void CreateCatalogInternal()
        {
            DropAndCreateSessionTableAs("EFIngresForeignKeyConstraints", @"
                select Id         = '[' + trim(r.relowner) + '][' + trim(r.relid) + '][' + trim(i.consname) + ']',
                       UpdateRule = case consupdrule when 0 then 'NO ACTION'
                                                     when 1 then 'RESTRICT'
                                                     when 2 then 'CASCADE'
                                                     when 3 then 'SET NULL'
                                                     else varchar(consupdrule) end,
                       DeleteRule = case consdelrule when 0 then 'NO ACTION'
                                                     when 1 then 'RESTRICT'
                                                     when 2 then 'CASCADE'
                                                     when 3 then 'SET NULL'
                                                     else varchar(consdelrule) end
                  from iiintegrity i
                  join iirelation r on
                       r.reltid  = i.inttabbase
                   and r.reltidx = i.inttabidx
                 where mod(i.consflags, 16) = 4
            ");
        }
    }
}
