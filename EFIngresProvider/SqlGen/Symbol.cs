using System;
using System.Collections.Generic;
using System.Data.Metadata.Edm;

namespace EFIngresProvider.SqlGen
{
    /// <summary>
    /// <see cref="SymbolTable"/>
    /// This class represents an extent/nested select statement,
    /// or a column.
    ///
    /// The important fields are Name, Type and NewName.
    /// NewName starts off the same as Name, and is then modified as necessary.
    ///
    ///
    /// The rest are used by special symbols.
    /// e.g. NeedsRenaming is used by columns to indicate that a new name must
    /// be picked for the column in the second phase of translation.
    ///
    /// IsUnnest is used by symbols for a collection expression used as a from clause.
    /// This allows <see cref="SqlGenerator.AddFromSymbol(SqlSelectStatement, string, Symbol, bool)"/> to add the column list
    /// after the alias.
    ///
    /// </summary>
    public class Symbol : ISqlFragment
    {
        private Symbol(string name, TypeUsage type, Dictionary<string, Symbol> columns, bool outputColumnsRenamed)
        {
            Columns = columns;
            OutputColumnsRenamed = outputColumnsRenamed;
            NeedsRenaming = false;
            IsUnnest = false;
            Name = name;
            NewName = name;
            Type = type;
        }

        public Symbol(string name, TypeUsage type = null)
            : this(name, type, new Dictionary<string, Symbol>(StringComparer.OrdinalIgnoreCase), false)
        {
        }

        /// <summary>
        /// Use this constructor the symbol represents a SqlStatement with renamed output columns.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="columns"></param>
        public Symbol(string name, TypeUsage type, Dictionary<string, Symbol> columns)
            : this(name, type, columns, true)
        {
        }

        public Dictionary<string, Symbol> Columns { get; private set; }
        internal bool OutputColumnsRenamed { get; set; }
        public bool NeedsRenaming { get; set; }
        public bool IsUnnest { get; set; }
        public string Name { get; private set; }
        public string NewName { get; set; }
        public TypeUsage Type { get; set; }

        #region ISqlFragment Members

        /// <summary>
        /// Write this symbol out as a string for sql.  This is just
        /// the new name of the symbol (which could be the same as the old name).
        ///
        /// We rename columns here if necessary.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="sqlGenerator"></param>
        public void WriteSql(SqlWriter writer, SqlGenerator sqlGenerator)
        {
            if (NeedsRenaming)
            {
                string newName;
                int i = sqlGenerator.AllColumnNames[NewName];
                do
                {
                    ++i;
                    newName = SqlGenerator.Format("{0}{1}", Name, i);
                } while (sqlGenerator.AllColumnNames.ContainsKey(newName));
                sqlGenerator.AllColumnNames[NewName] = i;

                // Prevent it from being renamed repeatedly.
                NeedsRenaming = false;
                NewName = newName;

                // Add this column name to list of known names so that there are no subsequent
                // collisions
                sqlGenerator.AllColumnNames[newName] = 0;
            }
            writer.Write(SqlGenerator.QuoteIdentifier(NewName));
        }

        #endregion
    }
}
