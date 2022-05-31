#+Table#namespace #Namespace##+#
{
    using System;
    using System.Data;
#+Dataset#
    using #Namespace#;
#+#

#+Table#
    public class #Name#Row : DbRow
    {
        internal #Name#Row(DataRowBuilder builder) : base(builder)
        {
        }
    #+Columns#

        #?Caption##?PrimaryKey#
        [DbColumn(Name="#Caption#", PrimaryKey=#PrimaryKey#)]
        #?##?#
        #?Caption##!PrimaryKey#
        [DbColumn(Name="#Caption#")]
        #!##?#
        #!Caption##?PrimaryKey#
        [DbColumn(PrimaryKey=#PrimaryKey#)]
        #?##!#
        public #Type# #Name#
        {
            get => this.Get<#Type#>();
            set => this.Set(value);
        }
    #+#

    #+Columns#
        public bool Is#Name#Null() => this.HasNull();
    #+#

    #+Columns#
        public void Set#Name#Null() => this.SetNull();
    #+#
    }
#+#
}