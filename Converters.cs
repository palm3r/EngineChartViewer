using System;
using System.Windows;
using System.Windows.Data;

namespace EngineChartViewer
{
  public class ExponentConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      int exponent = int.Parse(parameter as string);
      return Math.Pow((double)value, exponent);
    }
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

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

  public class DataSeriesBoostMaxConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      var ds = (DataSeries)value;
      return ds.BoostMin + (ds.BoostStep * (ds.BoostRange - 1));
    }
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  public class MaxPowerStringConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      var dp = (DataPoint)value;
      return dp != null
        ? string.Format("{0:f1} KW ({1:f1} PS / {2:f1} HP) @ {3:f0} rpm",
        dp.PowerKW, dp.PowerKW * Const.KW_PS, dp.PowerKW * Const.KW_HP, dp.RPM)
        : DependencyProperty.UnsetValue;
    }
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  public class MaxTorqueStringConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      var dp = (DataPoint)value;
      return dp != null
        ? string.Format("{0:f1} Nm ({1:f1} Kgm) @ {2:f0} rpm",
        dp.TorqueNM, dp.TorqueNM * Const.NM_KGM, dp.RPM)
        : DependencyProperty.UnsetValue;
    }
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
