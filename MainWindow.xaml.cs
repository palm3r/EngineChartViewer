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

    public PowerUnits PowerUnit
    {
      get { return Data.PowerUnit; }
      set
      {
        Data.PowerUnit = value;

        foreach (Data d in DataSource)
        {
          d.OnPropertyChanged("Power");
        }

        chart.YAxis.Title = string.Format("Power ({0})", PowerUnit.ToString());
        OnPropertyChanged("PowerUnit");
      }
    }

    public TorqueUnits TorqueUnit
    {
      get { return Data.TorqueUnit; }
      set
      {
        Data.TorqueUnit = value;

        foreach (Data d in DataSource)
        {
          d.OnPropertyChanged("Torque");
          d.OnPropertyChanged("BackTorque");
        }

        chart.SecondaryYAxis.Title = string.Format("Torque ({0})", TorqueUnit.ToString());
        OnPropertyChanged("TorqueUnit");
      }
    }

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

      Width = Properties.Settings.Default.Width;
      Height = Properties.Settings.Default.Height;
      PowerSeries.Visibility = Properties.Settings.Default.PowerSeriesVisibility;
      TorqueSeries.Visibility = Properties.Settings.Default.TorqueSeriesVisibility;
      BackTorqueSeries.Visibility = Properties.Settings.Default.BackTorqueSeriesVisibility;
      PowerUnit = Properties.Settings.Default.PowerUnit;
      TorqueUnit = Properties.Settings.Default.TorqueUnit;

      DataContext = this;

      chart.Behaviour = new ZoomBehaviour();
      chart.Series[0].YAxis = chart.SecondaryYAxis;
      chart.Series[1].YAxis = chart.SecondaryYAxis;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
      Properties.Settings.Default.Width = this.ActualWidth;
      Properties.Settings.Default.Height = this.ActualHeight;
      Properties.Settings.Default.PowerSeriesVisibility = PowerSeries.Visibility;
      Properties.Settings.Default.TorqueSeriesVisibility = TorqueSeries.Visibility;
      Properties.Settings.Default.BackTorqueSeriesVisibility = BackTorqueSeries.Visibility;
      Properties.Settings.Default.PowerUnit = PowerUnit;
      Properties.Settings.Default.TorqueUnit = TorqueUnit;
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
              //var power = torque * (rpm * (Math.PI * 2.0) / 60.0) / 1000.0;

              DataSource.Add(new Data(rpm, torque, backtorque));
            }
          }

          AdjustRange();

          var maxTorque = DataSource.OrderBy(d => d.Torque).Last();
          var maxPower = DataSource.OrderBy(d => d.Power).Last();

          fileNameTB.Text = System.IO.Path.GetFileName(files[0]);
          maxPowerTB.Text = string.Format(
            "Max Power : {0:f1} KW ({1:f0} PS / {2:f0} BHP) @ {3:f0} rpm",
            maxPower.RawPower, maxPower.RawPower * 1.3596, maxPower.RawPower * 1.341, maxPower.RPM);
          maxTorqueTB.Text = string.Format(
            "Max Torque : {0:f1} Nm ({1:f1} Kgm) @ {2:f0} rpm",
            maxTorque.RawTorque, maxTorque.RawTorque * 0.10197, maxTorque.RPM);
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
        //Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|Bitmap Image (*.bmp)|*.bmp",
        Filter = "PNG Image (*.png)|*.png",
        //InitialDirectory = System.IO.Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]),
        InitialDirectory = Properties.Settings.Default.LastSaveLocation,
        OverwritePrompt = true,
        ValidateNames = true,
      };

      if (dlg.ShowDialog(this) == true)
      {
        var ext = System.IO.Path.GetExtension(dlg.FileName).ToLower();

        BitmapEncoder encoder = null;

        switch (ext)
        {
          case ".png":
            encoder = new PngBitmapEncoder();
            break;
          /*
          case ".jpg":
            encoder = new JpegBitmapEncoder();
            break;
          case ".bmp":
            encoder = new BmpBitmapEncoder();
            break;
          */
          default:
            break;
        }

        if (encoder == null)
          throw new ApplicationException("Invalid file extension");

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

        using (var fs = File.Open(dlg.FileName, FileMode.OpenOrCreate))
        {
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

    private void ChangeUnit_Click(object sender, RoutedEventArgs e)
    {
      if (DataSource.Count > 0)
        AdjustRange();
    }
  }

  public class DataCollection : ObservableCollection<Data> { }

  public class Data : INotifyPropertyChanged
  {
    public static TorqueUnits TorqueUnit { get; set; }
    public static PowerUnits PowerUnit { get; set; }

    private double _torque, _backtorque;

    public double RPM { get; private set; }

    public double RawTorque
    {
      get { return _torque; }
      set { _torque = value; OnPropertyChanged("Torque"); }
    }

    public double RawBackTorque
    {
      get { return _backtorque; }
      set { _backtorque = value; OnPropertyChanged("BackTorque"); }
    }

    public double RawPower
    {
      get { return _torque * (RPM * (Math.PI * 2.0) / 60.0) / 1000.0; }
    }

    public double Torque
    {
      get
      {
        switch (TorqueUnit)
        {
          case TorqueUnits.NM:
            return _torque;
          case TorqueUnits.KGM:
            return _torque * 0.10197;
        }
        return 0;
      }
      set
      {
        switch (TorqueUnit)
        {
          case TorqueUnits.NM:
            _torque = value;
            break;
          case TorqueUnits.KGM:
            _torque = value / 0.10197;
            break;
        }
        OnPropertyChanged("Torque");
      }
    }

    public double BackTorque
    {
      get
      {
        switch (TorqueUnit)
        {
          case TorqueUnits.NM:
            return _backtorque;
          case TorqueUnits.KGM:
            return _backtorque * 0.10197;
        }
        return 0;
      }
      set
      {
        switch (TorqueUnit)
        {
          case TorqueUnits.NM:
            _backtorque = value;
            break;
          case TorqueUnits.KGM:
            _backtorque = value / 0.10197;
            break;
        }
        OnPropertyChanged("BackTorque");
      }
    }

    public double Power
    {
      get
      {
        var kw = _torque * (RPM * (Math.PI * 2.0) / 60.0) / 1000.0;

        switch (PowerUnit)
        {
          case PowerUnits.KW:
            return kw;
          case PowerUnits.PS:
            return kw * 1.3596;
          case PowerUnits.BHP:
            return kw * 1.341;
          default:
            break;
        }
        return 0;
      }
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

  public enum TorqueUnits { NM, KGM }
  public enum PowerUnits { KW, PS, BHP }

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
