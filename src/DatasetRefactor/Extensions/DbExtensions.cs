using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace DatasetRefactor.Extensions
{
    public static class DbExtensions
    {
        public static IDictionary<string, string> ToParameterDictionary(this IDbCommand command)
        {
            const string ReturnValueParam = "RETURN_VALUE";

            return command
                .Parameters
                .OfType<SqlParameter>()
                .Where(i => i.ParameterName != ReturnValueParam && i.Direction != ParameterDirection.ReturnValue)
                .ToDictionary(k => k.ParameterName.TrimStart('@'), v => v.SqlDbType.ToClrName());
        }

        public static string ToClrName(this SqlDbType type)
        {
            return type.ToClrType().GetCsName();
        }

        public static Type ToClrType(this SqlDbType type)
        {
            return type switch
            {
                SqlDbType.Int => typeof(int),
                SqlDbType.Bit => typeof(bool),
                SqlDbType.Float => typeof(double),
                SqlDbType.Real => typeof(float),
                SqlDbType.SmallInt => typeof(short),
                SqlDbType.TinyInt => typeof(byte),
                SqlDbType.BigInt => typeof(long),
                SqlDbType.Structured    => typeof(DataTable),
                SqlDbType.DateTimeOffset  => typeof(DateTimeOffset),
                SqlDbType.UniqueIdentifier => typeof(Guid),
                SqlDbType.Variant or SqlDbType.Udt => typeof(object),
                SqlDbType.Decimal or SqlDbType.Money or SqlDbType.SmallMoney => typeof(decimal),
                SqlDbType.Binary or SqlDbType.Image or SqlDbType.Timestamp or SqlDbType.VarBinary => typeof(byte[]),
                SqlDbType.DateTime or SqlDbType.SmallDateTime or SqlDbType.Date or SqlDbType.Time or SqlDbType.DateTime2 => typeof(DateTime),
                SqlDbType.Char or SqlDbType.NChar or SqlDbType.NText or SqlDbType.NVarChar or SqlDbType.Text or SqlDbType.VarChar or SqlDbType.Xml => typeof(string),
                _ => throw new ArgumentOutOfRangeException(nameof(type)),
            };
        }
    }
}
