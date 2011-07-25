using System;
using System.Windows;
using System.Windows.Data;

namespace EngineChartViewer
{
  public class EnumBooleanConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      var valueName = parameter as string;
      if (valueName == null || !Enum.IsDefined(value.GetType(), value))
        return DependencyProperty.UnsetValue;
      var valueObject = Enum.Parse(value.GetType(), valueName);
      return valueObject.Equals(value);
    }
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      var valueName = parameter as string;
      if (valueName == null)
        return DependencyProperty.UnsetValue;
      return Enum.Parse(targetType, valueName);
    }
  }

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

  public class KW_PS_Converter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return (double)value * Const.KW_PS;
    }
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return (double)value / Const.KW_PS;
    }
  }

  public class KW_HP_Converter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return (double)value * Const.KW_HP;
    }
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return (double)value / Const.KW_HP;
    }
  }

  public class NM_KGM_Converter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return (double)value * Const.NM_KGM;
    }
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return (double)value / Const.NM_KGM;
    }
  }
}
