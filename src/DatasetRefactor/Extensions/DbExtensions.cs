using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DatasetRefactor.Extensions
{
    public static class DbExtensions
    {
        public static IEnumerable<IDbCommand> GetDataCommands(this IDbDataAdapter adapter)
        {
            if (adapter is null)
            {
                return Enumerable.Empty<IDbCommand>();
            }

            var commands = new[]
            {
                adapter?.UpdateCommand,
                adapter?.InsertCommand,
                adapter?.DeleteCommand,
            };

            return commands.Where(i => i != null);
        }
    }
}
