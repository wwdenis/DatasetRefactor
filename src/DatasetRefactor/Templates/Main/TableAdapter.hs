#+Adapter#namespace #Namespace##+#
{
    using System;
    using System.Collections.Generic;
    using System.Data;
#+Dataset#
    using #Namespace#;
    using #Namespace#.#Name#;
#+#

#+Adapter#
    public partial class #Name# : DbAdapter
    {
        private static readonly Dictionary<string, string> Commands = new Dictionary<string, string>
        {
    #+Commands#
            { "#Type##Name#", @"#Text#" },
    #+#
        };

        public #Name#()
        {
            this.ConnectionString = GlobalSettings.ConnectionString;
        }

    #+Select#
        public int Fill#Suffix#(#Table#DataTable table#+Parameters#, #Type# #Name##+#)
        {
            var sql = Commands["#Type#"];
            var parameters = new Dictionary<string, object>
            {
                #+Parameters#
                { "@#Name#", #Name# },
                #+#
            };
            return this.Fill(table, sql, parameters);
        }

        public #ReturnType# GetData#Suffix#(#+Parameters##Type# #Name##!.Last#, #!##+#)
        {
            var table = new #Table#DataTable();
            this.Fill(table);
            return table;
        }
    #+#

    #+Insert#
        public int Insert(#+Parameters##Type# #Name##!.Last#, #!##+#)
        {
            var sql = Commands["#Type#"];
            var parameters = new Dictionary<string, object>
            {
                #+Parameters#
                { "@#Name#", #Name# },
                #+#
            };
            return this.Execute(sql, parameters);
        }
    #+#

    #+Delete#
        public int Delete(#+Parameters##Type# #Name##!.Last#, #!##+#)
        {
            var sql = Commands["#Type#"];
            var parameters = new Dictionary<string, object>
            {
                #+Parameters#
                { "@#Name#", #Name# }, 
                #+#
            };
            return this.Execute(sql, parameters);
        }
    #+#

    #+Update#
        public int Update(#+Parameters##Type# #Name##!.Last#, #!##+#)
        {
            var sql = Commands["#Type#"];
            var parameters = new Dictionary<string, object>
            {
                #+Parameters#
                { "@#Name#", #Name# },
                #+#
            };
            return this.Execute(sql, parameters);
        }

        public int Update(#Table#DataTable table)
        {
            var sql = Commands["#Type#"];
            return this.Update(sql, table);
        }

        public int Update(#Table#Row row)
        {
            var sql = Commands["#Type#"];
            return this.Update(sql, row);
        }
    #+#
    }
#+#
}