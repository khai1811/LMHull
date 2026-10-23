using System;
using Eto.Forms;

namespace LMHull.Rhino.UI.Common;

/// <summary>
/// Chọn file: [Title] [TextBox ReadOnly] [Browse...].
/// </summary>
public class FilePickerInput : Panel
{
    private readonly TextBox _txtPath;
    private readonly string _filterName;
    private readonly string[] _extensions;

    public event EventHandler<EventArgs>? FilePathChanged;

    /// <param name="extensions">Danh sách extension, ví dụ ".3dm", ".csv". Bỏ trống = mọi file.</param>
    public FilePickerInput(string title, string filterName = "All files", params string[] extensions)
    {
        _filterName = filterName;
        _extensions = extensions.Length > 0 ? extensions : new[] { ".*" };

        _txtPath = new TextBox { ReadOnly = true };
        var btnBrowse = new Button { Text = "Browse..." };
        btnBrowse.Click += OnBrowseClicked;

        Padding = UiMetrics.Padding;
        Content = new TableLayout
        {
            Spacing = UiMetrics.Spacing,
            Rows =
            {
                new TableRow(
                    new Label { Text = title, Width = UiMetrics.LabelWidth, VerticalAlignment = VerticalAlignment.Center },
                    new TableCell(_txtPath, true),
                    btnBrowse)
            }
        };
    }

    public string FilePath
    {
        get => _txtPath.Text;
        set
        {
            value ??= string.Empty;
            if (_txtPath.Text == value) return;
            _txtPath.Text = value;
            FilePathChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// true: nút Browse mở hộp thoại chọn thư mục thay vì chọn tệp.
    /// </summary>
    public bool SelectFolder { get; set; }

    private void OnBrowseClicked(object? sender, EventArgs e)
    {
        if (SelectFolder)
        {
            using var folderDialog = new SelectFolderDialog { Directory = FilePath };
            if (folderDialog.ShowDialog(this) == DialogResult.Ok) FilePath = folderDialog.Directory;
            return;
        }

        using var dialog = new OpenFileDialog { MultiSelect = false };
        dialog.Filters.Add(new FileFilter(_filterName, _extensions));

        if (dialog.ShowDialog(this) == DialogResult.Ok)
        {
            FilePath = dialog.FileName;
        }
    }
}
