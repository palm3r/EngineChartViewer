using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Visiblox.Charts;

namespace EngineChartViewer
{
  public partial class MainWindow : Window, INotifyPropertyChanged
  {
    public static readonly ICommand SaveAsImageCommand = new RoutedUICommand("Save as Image",
      "SaveAsImageCommand", typeof(MainWindow));

    public DataCollection DataSource
    {
      get
      {
        return this.FindResource("DataSource") as DataCollection;
      }
    }

    public MainWindow()
    {
      InitializeComponent();
      DataContext = this;

      chart.Behaviour = new ZoomBehaviour();
      chart.Series[0].YAxis = chart.SecondaryYAxis;
      chart.Series[1].YAxis = chart.SecondaryYAxis;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
      Properties.Settings.Default.Save();
      base.OnClosing(e);
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
      var files = e.Data.GetData(DataFormats.FileDrop) as string[];
      if (files != null && files.Length > 0)
      {
        using (var reader = new StreamReader(files[0], Encoding.ASCII))
        {
          var regex = new Regex(
            @"^RPMTorque=\(\s*(?<RPM>[+-]?\d*\.?\d*),\s*(?<BackTorque>[+-]?\d*\.?\d*),\s*(?<Torque>[+-]?\d*\.?\d*)\).*",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Singleline);

          DataSource.Clear();

          string line = "";
          while ((line = reader.ReadLine()) != null)
          {
            var m = regex.Match(line);
            if (m.Success)
            {
              var rpm = double.Parse(m.Groups["RPM"].Value);
              var backtorque = double.Parse(m.Groups["BackTorque"].Value);
              var torque = double.Parse(m.Groups["Torque"].Value);

              DataSource.Add(new Data(rpm, torque, backtorque));
            }
          }

          AdjustRange();

          var maxTorque = DataSource.OrderBy(d => d.Torque).Last();
          var maxPower = DataSource.OrderBy(d => d.Power).Last();

          fileNameTB.Text = System.IO.Path.GetFileName(files[0]);
          maxPowerTB.Text = string.Format(
            "Max Power : {0:f1} KW ({1:f0} PS / {2:f0} BHP) @ {3:f0} rpm",
            maxPower.Power, maxPower.Power * 1.3596, maxPower.Power * 1.341, maxPower.RPM);
          maxTorqueTB.Text = string.Format(
            "Max Torque : {0:f1} Nm ({1:f1} Kgm) @ {2:f0} rpm",
            maxTorque.Torque, maxTorque.Torque * 0.10197, maxTorque.RPM);
        }
      }
    }

    private void AdjustRange()
    {
      var torqueRange = new DoubleRange();
      var powerRange = new DoubleRange();

      if (TorqueSeries.Visibility == Visibility.Visible)
      {
        var min = DataSource.Min(d => d.Torque);
        var max = DataSource.Max(d => d.Torque);
        if (min < torqueRange.Minimum)
          torqueRange.Minimum = min;
        if (torqueRange.Maximum < max)
          torqueRange.Maximum = max;
      }

      if (BackTorqueSeries.Visibility == Visibility.Visible)
      {
        var min = DataSource.Min(d => d.BackTorque);
        var max = DataSource.Max(d => d.BackTorque);
        if (min < torqueRange.Minimum)
          torqueRange.Minimum = min;
        if (torqueRange.Maximum < max)
          torqueRange.Maximum = max;
      }

      if (PowerSeries.Visibility == Visibility.Visible)
      {
        var min = DataSource.Min(d => d.Power);
        var max = DataSource.Max(d => d.Power);
        if (min < powerRange.Minimum)
          powerRange.Minimum = min;
        if (powerRange.Maximum < max)
          powerRange.Maximum = max;
      }

      torqueRange.Minimum -= (torqueRange.Maximum - torqueRange.Minimum) / 10.0;
      torqueRange.Maximum += (torqueRange.Maximum - torqueRange.Minimum) / 10.0;

      powerRange.Minimum -= (powerRange.Maximum - powerRange.Minimum) / 10.0;
      powerRange.Maximum += (powerRange.Maximum - powerRange.Minimum) / 10.0;

      TorqueAxis.Range = torqueRange;
      PowerAxis.Range = powerRange;
    }

    public void OnPropertyChanged(string propertyName)
    {
      if (PropertyChanged != null)
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void FileExit_Click(object sender, RoutedEventArgs e)
    {
      this.Close();
    }

    private void SaveAsImage_Executed(object sender, ExecutedRoutedEventArgs e)
    {
      double dpiX = 96.0, dpiY = 96.0;
      var ps = PresentationSource.FromVisual(this);
      if (ps != null)
      {
        dpiX = 96.0 * ps.CompositionTarget.TransformToDevice.M11;
        dpiY = 96.0 * ps.CompositionTarget.TransformToDevice.M22;
      }

      var dlg = new SaveFileDialog()
      {
        AddExtension = true,
        CheckPathExists = true,
        DefaultExt = "png",
        FileName = System.IO.Path.GetFileNameWithoutExtension(fileNameTB.Text) + ".png",
        Filter = "PNG Image (*.png)|*.png",
        InitialDirectory = Properties.Settings.Default.LastSaveLocation,
        OverwritePrompt = true,
        ValidateNames = true,
      };

      if (dlg.ShowDialog(this) == true)
      {
        using (var fs = File.Open(dlg.FileName, FileMode.OpenOrCreate))
        {
          var target = imageFrame;
          var size = target.RenderSize;
          var margin = 10;

          var render = new RenderTargetBitmap(
            (int)(size.Width + (margin * 2)),
            (int)(size.Height + (margin * 2)),
            dpiX, dpiY, PixelFormats.Pbgra32);

          var dv = new DrawingVisual();
          using (var dc = dv.RenderOpen())
          {
            var vb = new VisualBrush(target);
            dc.DrawRectangle(vb, null,
              new Rect(new Point(margin, margin), size));
          }
          render.Render(dv);

          BitmapEncoder encoder = new PngBitmapEncoder();
          encoder.Frames.Add(BitmapFrame.Create(render));
          encoder.Save(fs);

          Properties.Settings.Default.LastSaveLocation = System.IO.Path.GetDirectoryName(dlg.FileName);
        }
      }
    }

    private void SaveAsImage_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
      e.CanExecute = DataSource.Count > 0;
    }

    private void Visibility_Click(object sender, RoutedEventArgs e)
    {
      if (DataSource.Count > 0)
        AdjustRange();
    }
  }

  public enum TorqueUnits { NM, KGM }
  public enum PowerUnits { KW, PS, BHP }

  public class DataCollection : ObservableCollection<Data> { }

  public class Data : INotifyPropertyChanged
  {
    private double _torque, _backtorque;

    public double RPM { get; private set; }

    public double Torque
    {
      get { return _torque; }
      set { _torque = value; OnPropertyChanged("Torque"); }
    }

    public double BackTorque
    {
      get { return _backtorque; }
      set { _backtorque = value; OnPropertyChanged("BackTorque"); }
    }

    public double Power
    {
      get { return _torque * (RPM * (Math.PI * 2.0) / 60.0) / 1000.0; }
    }

    public Data(double rpm, double torque, double backtorque)
    {
      RPM = rpm;
      _torque = torque;
      _backtorque = backtorque;
    }

    public void OnPropertyChanged(string propertyName)
    {
      if (PropertyChanged != null)
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler PropertyChanged;
  }

  public class EnumBooleanConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      string paramString = parameter as string;
      if (paramString == null)
        return DependencyProperty.UnsetValue;
      var paramEnum = Enum.Parse(value.GetType(), paramString);
      return paramEnum.Equals(value);
    }
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      string paramString = parameter as string;
      if (paramString == null)
        return DependencyProperty.UnsetValue;
      return Enum.Parse(targetType, paramString);
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
}
