using System.Data;
using System.Text.Json;

namespace Accounting.Desktop.Helpers;

internal static class JsonGridHelper
{
    public static DataView? JsonArrayToView(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Array)
                return null;
            var table = new DataTable();
            var arr = root.EnumerateArray().ToList();
            if (arr.Count == 0)
                return table.DefaultView;

            foreach (var prop in arr[0].EnumerateObject())
                table.Columns.Add(prop.Name, typeof(string));

            foreach (var el in arr)
            {
                var row = table.NewRow();
                foreach (var prop in el.EnumerateObject())
                {
                    var v = prop.Value;
                    row[prop.Name] = v.ValueKind switch
                    {
                        JsonValueKind.String => v.GetString() ?? "",
                        JsonValueKind.Number => v.GetRawText(),
                        JsonValueKind.True => "True",
                        JsonValueKind.False => "False",
                        JsonValueKind.Null => "",
                        _ => v.GetRawText()
                    };
                }
                table.Rows.Add(row);
            }

            return table.DefaultView;
        }
        catch
        {
            return null;
        }
    }
}
