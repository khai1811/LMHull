namespace LMHull.Core.Validation;

public static class StructuralValidator
{
    public static ValidationResult ValidatePlate(double thicknessMm, string material)
    {
        var result = new ValidationResult();

        if (thicknessMm < 3.0)
            result.AddError("Chiều dày tôn danh định không được nhỏ hơn 3.0 mm (tiêu chuẩn đóng tàu chống biến dạng khi hàn vỏ).");

        if (thicknessMm > 120.0)
            result.AddError("Chiều dày tôn không được vượt quá 120.0 mm đối với kết cấu thân vỏ thông thường.");

        if (string.IsNullOrWhiteSpace(material))
            result.AddError("Vật liệu kết cấu bắt buộc phải được chọn.");

        return result;
    }
}