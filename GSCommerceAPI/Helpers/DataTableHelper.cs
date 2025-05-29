using System.ComponentModel;
using System.Data;

namespace GSCommerceAPI.Helpers
{
    public static class DataTableHelper
    {
        public static DataTable ToDataTable<T>(IEnumerable<T> data, string? tableName = null)
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(T));
            DataTable table = new DataTable();

            if (!string.IsNullOrEmpty(tableName))
                table.TableName = tableName;

            foreach (PropertyDescriptor prop in properties)
            {
                if (!prop.PropertyType.IsAbstract)
                {
                    table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
                }
            }

            foreach (T item in data)
            {
                var row = table.NewRow();
                foreach (PropertyDescriptor prop in properties)
                {
                    if (!prop.PropertyType.IsAbstract)
                        row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                }
                table.Rows.Add(row);
            }

            return table;
        }
    }
}
