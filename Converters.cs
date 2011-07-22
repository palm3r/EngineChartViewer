using System;
using System.Windows;
using System.Windows.Data;

namespace EngineChartViewer
{
  public class VisibilityBooleanConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (value is Visibility)
        return (Visibility)value == Visibility.Visible;
      return false;
    }
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (value is bool && !(bool)value)
        return Visibility.Collapsed;
      return Visibility.Visible;
    }
  }
}
