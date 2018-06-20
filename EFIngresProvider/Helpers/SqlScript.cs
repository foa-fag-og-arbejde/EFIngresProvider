using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Common;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;

namespace EFIngresProvider.Helpers
{
    public class SqlScript
    {
        public class Statement
        {
            public string Sql { get; set; }
            public bool IgnoreErrors { get; set; }
        }

        public static void ExecuteScript(DbConnection connection, string script, bool ignoreErrors = false, Action<Statement> beforeExecute = null)
        {
            ExecuteScript(connection, ReadScript(script), ignoreErrors, beforeExecute);
        }

        public static void ExecuteScript(DbConnection connection, Stream stream, bool ignoreErrors = false, Action<Statement> beforeExecute = null)
        {
            ExecuteScript(connection, ReadScript(stream), ignoreErrors, beforeExecute);
        }

        public static void ExecuteScriptFile(DbConnection connection, string filename, bool ignoreErrors = false, Action<Statement> beforeExecute = null)
        {
            ExecuteScript(connection, ReadScriptFromFile(Assembly.GetCallingAssembly(), filename), ignoreErrors, beforeExecute);
        }

        public static void ExecuteScript(DbConnection connection, IEnumerable<string> statements, bool ignoreErrors = false, Action<Statement> beforeExecute = null)
        {
            using (var cmd = connection.CreateCommand())
            {
                foreach (var statement in statements.Select(x => new Statement { Sql = x, IgnoreErrors = ignoreErrors }))
                {
                    if (beforeExecute != null)
                    {
                        beforeExecute(statement);
                    }
                    cmd.CommandText = statement.Sql;
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch
                    {
                        if (!statement.IgnoreErrors)
                        {
                            throw;
                        }
                    }
                }
            }
        }

        public static IEnumerable<string> ReadScript(string script)
        {
            return SplitScript(script);
        }

        public static IEnumerable<string> ReadScript(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                return ReadScript(reader.ReadToEnd());
            }
        }

        public static IEnumerable<string> ReadScriptFromFile(Assembly assembly, string filename)
        {
            using (var reader = new StreamReader(GetPath(assembly, filename)))
            {
                return ReadScript(reader.ReadToEnd());
            }
        }

        public static string GetPath(Assembly assembly, string filename)
        {
            return Path.Combine(Path.GetDirectoryName(assembly.Location), filename);
        }

        public class SqlPart
        {
            public SqlPart(string type, string value)
                : this(type, value, value)
            {
            }

            public SqlPart(string type, string value, string lexeme)
            {
                Type = type;
                Value = value;
                Lexeme = lexeme;
            }

            public string Type { get; private set; }
            public string Value { get; private set; }
            public string Lexeme { get; private set; }

            public override string ToString()
            {
                return string.Format("{0,-12}: {1}", Type, Lexeme);
            }
        }

        public class SqlRegex
        {
            private List<Tuple<string, string>> _tokenReList = new List<Tuple<string, string>>();

            public void AddToken(string name, string re)
            {
                _tokenReList.Add(Tuple.Create(name, re));
            }

            public IEnumerable<string> TokenStrings
            {
                get
                {
                    foreach (var token in _tokenReList)
                    {
                        yield return string.Format(@"(?<{0}>{1})", token.Item1, token.Item2);
                    }
                }
            }

            public Regex Regex
            {
                get { return new Regex(string.Join("|", TokenStrings), RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled); }
            }
        }

        private static IEnumerable<string> SplitScript(string sql)
        {
            var statement = "";
            var inProcedure = false;
            var beginCount = 0;

            foreach (var part in ParseSql(sql))
            {
                switch (part.Type)
                {
                    case "Comment":
                        continue;
                    case "Semicolon":
                        if (inProcedure)
                        {
                            break;
                        }
                        else
                        {
                            yield return statement.Trim();
                            statement = "";
                            inProcedure = false;
                            beginCount = 0;
                            continue;
                        }
                    case "CreateProcedure":
                        inProcedure = true;
                        break;
                    case "Begin":
                        if (inProcedure)
                        {
                            beginCount += 1;
                        }
                        break;
                    case "End":
                        if (inProcedure)
                        {
                            beginCount -= 1;
                            if (beginCount <= 0)
                            {
                                inProcedure = false;
                                beginCount = 0;
                            }
                        }
                        break;
                }
                statement += part.Lexeme;
            }
            if (!string.IsNullOrWhiteSpace(statement))
            {
                yield return statement.Trim();
            }
        }

        private static IEnumerable<SqlPart> ParseSql(string sql)
        {
            var sqlRe = new SqlRegex();
            sqlRe.AddToken("Comment", @"\/\*.*?\*\/");
            sqlRe.AddToken("String", @"'[^']*?'");
            sqlRe.AddToken("Semicolon", @";");
            sqlRe.AddToken("CreateProcedure", @"\bCREATE\s+PROCEDURE\b");
            sqlRe.AddToken("Begin", @"\bBEGIN\b");
            sqlRe.AddToken("End", @"\bEND\b");
            sqlRe.AddToken("Word", @"\b[a-zA-Z]\w*\b");
            sqlRe.AddToken("Whitespace", @"\s+");

            var re = sqlRe.Regex;

            var groupNames = re.GetGroupNames().Skip(1);

            var start = 0;
            foreach (Match match in re.Matches(sql))
            {
                if (match.Index > start)
                {
                    yield return new SqlPart("Sql", sql.Substring(start, match.Index - start));
                }

                yield return groupNames.Select(x => new { Name = x, Group = match.Groups[x] })
                                       .Where(x => x.Group.Success)
                                       .Select(x => new SqlPart(x.Name, x.Group.Value, match.Value))
                                       .Single();

                start = match.Index + match.Value.Length;
            }
            if (start < sql.Length)
            {
                yield return new SqlPart("Sql", sql.Substring(start));
            }
        }
    }
}
