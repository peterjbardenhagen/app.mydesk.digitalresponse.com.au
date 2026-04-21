using System.Data;

namespace MyDesk.Shared.Services;

internal static class DataTableExtensions
{
    /// <summary>
    /// Maps each row of a DataTable to T using the provided mapper function.
    /// Avoids the need for System.Data.DataSetExtensions package.
    /// </summary>
    public static List<T> Map<T>(this DataTable dt, Func<DataRow, T> mapper)
    {
        var result = new List<T>(dt.Rows.Count);
        foreach (DataRow row in dt.Rows)
        {
            result.Add(mapper(row));
        }
        return result;
    }

    /// <summary>
    /// Returns the rows as IEnumerable<DataRow> for LINQ scenarios.
    /// </summary>
    public static IEnumerable<DataRow> Rows(this DataTable dt)
    {
        foreach (DataRow row in dt.Rows)
        {
            yield return row;
        }
    }
}
