using System.Globalization;
using System.Text.RegularExpressions;

namespace LMHull.Core.Validation;

/// <summary>
/// Quy tắc nhập liệu cho ô số: chỉ cho phép chữ số, tối đa một dấu thập phân ('.' hoặc ',')
/// và dấu '-' ở đầu khi cho phép số âm.
/// </summary>
public static class NumericTextRules
{
    /// <summary>
    /// Chuỗi có được phép xuất hiện trong ô nhập khi đang gõ không (chấp nhận trạng thái dở dang như "", "-", "12.").
    /// </summary>
    public static bool IsAcceptable(string? text, int decimalPlaces, bool allowNegative)
    {
        if (string.IsNullOrEmpty(text)) return true;

        string sign = allowNegative ? "-?" : "";
        string fraction = decimalPlaces > 0 ? $@"([.,]\d{{0,{decimalPlaces}}})?" : "";
        return Regex.IsMatch(text, $@"^{sign}\d*{fraction}$");
    }

    /// <summary>
    /// Đọc giá trị số từ chuỗi đã nhập (chấp nhận cả '.' và ',' làm dấu thập phân).
    /// </summary>
    public static bool TryParse(string? text, out double value)
    {
        value = 0.0;
        if (string.IsNullOrWhiteSpace(text) || text == "-") return false;
        return double.TryParse(text.Replace(',', '.'), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out value);
    }

    public static string Format(double value, int decimalPlaces) =>
        value.ToString("F" + decimalPlaces, CultureInfo.InvariantCulture);
}
