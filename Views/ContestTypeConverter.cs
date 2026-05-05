using Avalonia.Data.Converters;
using HamBusLog.ViewModels;
using System.Globalization;

namespace HamBusLog.Views;

public sealed class ContestTypeConverter : IValueConverter
{
    public static readonly ContestTypeConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is ContestType ct
            ? ContestCatalog.Get(ct).DisplayName
            : value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

