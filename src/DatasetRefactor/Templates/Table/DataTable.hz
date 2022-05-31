#+Table#namespace #Namespace##+#
{
    using System;
    using System.Data;
#+Dataset#
    using #Namespace#;
#+#

#+Table#
    public class #Name#DataTable : DbTable<#Name#Row>
    {
        protected override DataRow NewRowFromBuilder(DataRowBuilder builder)
        {
            return new #Name#Row(builder);
        }

        public #Name#Row New#Name#Row()
        {
            return this.NewDataRow();
        }

        public void Add#Name#Row(#Name#Row row)
        {
            this.Rows.Add(row);
        }

        public void Remove#Name#Row(#Name#Row row)
        {
            this.Rows.Remove(row);
        }
#+Actions#

        public #ReturnType# #Name#(#+Parameters##Type# #Name##!.Last#, #!##+#)
        {
            return this.FindRow(#+Parameters##Name##!.Last#, #!##+#);
        }
#+#
    }
#+#
}