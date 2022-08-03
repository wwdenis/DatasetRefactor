using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using DatasetRefactor.Extensions;

namespace DatasetRefactor.Infrastructure
{
    internal sealed class SqlManager : IDisposable
    {
        private readonly Component component;

        private SqlManager(Component component, string tableName)
        {
            this.component = component;
            this.TableName = tableName;
        }

        public void Dispose()
        {
            this.component.Dispose();
        }

        public string TableName { get; }

        public IDbDataAdapter Adapter => this.component.GetPropertyValue<IDbDataAdapter>("Adapter");

        public IDbCommand[] Commands => this.component.GetPropertyValue<IDbCommand[]>("CommandCollection");

        public IDbCommand GetLastCalled()
        {
            return this
                .GetAllCommands()
                .FirstOrDefault(i => i.Parameters.OfType<IDbDataParameter>().Any(p => p.Value != null));
        }

        public void CallMethod(MethodInfo method)
        {
            this.ResetAllCommands();
            this.component.InvokeDefault(method);
        }

        public static SqlManager Create(Type adapterType)
        {
            var component = Activator.CreateInstance(adapterType) as Component;
            component.InvokeDefault("InitCommandCollection");

            var tableName = adapterType.Name.Replace("TableAdapter", string.Empty);
            var manager = new SqlManager(component, tableName);

            manager.ResetAllCommands();

            return manager;
        }

        private IEnumerable<IDbCommand> GetAllCommands()
        {
            var selectCommands = this.Commands;
            var dataCommands = this.GetDataCommands();
            return selectCommands
                .Union(dataCommands)
                .Where(i => i != null);
        }

        private void ResetAllCommands()
        {
            if (this.Adapter is not null)
            {
                this.Adapter.SelectCommand = null;
            }
            
            foreach (var cmd in this.GetAllCommands())
            {
                cmd.Connection = null;

                foreach (IDbDataParameter param in cmd.Parameters)
                {
                    param.Value = null;
                }
            }
        }

        private IEnumerable<IDbCommand> GetDataCommands()
        {
            if (this.Adapter is null)
            {
                return Enumerable.Empty<IDbCommand>();
            }

            var commands = new[]
            {
                this.Adapter.UpdateCommand,
                this.Adapter.InsertCommand,
                this.Adapter.DeleteCommand,
            };

            return commands.Where(i => i != null);
        }
    }
}
