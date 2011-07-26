using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Visiblox.Charts;
using System.Windows.Controls;

namespace EngineChartViewer
{
  public partial class MainWindow : Window, INotifyPropertyChanged
  {
    public static readonly ICommand OpenCommand =
      new RoutedUICommand("Open", "OpenCommand", typeof(MainWindow));

    public static readonly ICommand SaveAsImageCommand =
      new RoutedUICommand("Save as Image", "SaveAsImageCommand", typeof(MainWindow));

    public static readonly ICommand ReloadCommand =
      new RoutedUICommand("Reload", "ReloadCommand", typeof(MainWindow));

    public static readonly ICommand ExitCommand =
      new RoutedUICommand("Exit", "ExitCommand", typeof(MainWindow));

    public DataSeries DataSeries
    {
      get
      {
        return this.FindResource("DataSeries") as DataSeries;
      }
    }

    public IEnumerable<RecentFile> RecentFiles
    {
      get
      {
        return Properties.Settings.Default.RecentFiles != null
          ? Properties.Settings.Default.RecentFiles.Select((filePath, index) => new RecentFile(index, filePath))
          : null;
      }
    }

    public MainWindow()
    {
      InitializeComponent();

      DataContext = this;

      chart.Series[1].YAxis = chart.SecondaryYAxis;
      chart.Series[2].YAxis = chart.SecondaryYAxis;
      chart.Series[3].YAxis = chart.AdditionalSecondaryYAxes[0];
    }

    protected override void OnClosing(CancelEventArgs e)
    {
      Properties.Settings.Default.Width = this.ActualWidth;
      Properties.Settings.Default.Height = this.ActualHeight;
      Properties.Settings.Default.Save();
      base.OnClosing(e);
    }

    public void Open(string filePath)
    {
      var emptyRegex = new Regex(@"^\s*(?:\/\/.*)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

      var rpmTorqueRegex = new Regex(
        @"^RPMTorque=\(\s*(?<rpm>[+-]?\d*\.?\d*),\s*(?<backtorque>[+-]?\d*\.?\d*),\s*(?<torque>[+-]?\d*\.?\d*)\).*",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Singleline);

      var keyValueRegex = new Regex(
        @"^(?<key>[a-z0-9]+)=((?<number>[^\(\)""\/]+)|""(?<string>[^""\/]+)""|\((?<list>(\s*(\d+(?:\.\d+)?)\s*,?)+)\)).*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

      var lines = File.ReadAllLines(filePath, Encoding.ASCII);

      DataSeries.Data = lines.Select(line => keyValueRegex.Match(line))
        .Where(m => m.Success && m.Groups["key"].Value.ToLower() != "rpmtorque")
        .Select(m =>
          {
            var key = m.Groups["key"].Value;

            if (m.Groups["number"].Success)
            {
              var value = double.Parse(m.Groups["number"].Value);
              return new KeyValuePair<string, dynamic>(key, value);
            }
            else if (m.Groups["string"].Success)
            {
              var value = m.Groups["string"].Value;
              return new KeyValuePair<string, dynamic>(key, value);
            }
            else if (m.Groups["list"].Success)
            {
              var list = m.Groups["list"].Value
                .Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => double.Parse(value))
                .ToList();
              return new KeyValuePair<string, dynamic>(key, list);
            }
            throw new InvalidDataException();
          })
          .ToDictionary(item => item.Key, item => item.Value);

      DataSeries.Clear();

      lines.Select(line => rpmTorqueRegex.Match(line))
        .Where(m => m.Success)
        .Select(m => new DataPoint(
          double.Parse(m.Groups["rpm"].Value),
          double.Parse(m.Groups["torque"].Value),
          double.Parse(m.Groups["backtorque"].Value)
          ))
        .ToList()
        .ForEach(dp => DataSeries.Add(dp));

      Properties.Settings.Default.LastOpenLocation = System.IO.Path.GetDirectoryName(filePath);

      if (Properties.Settings.Default.RecentFiles == null)
      {
        Properties.Settings.Default.RecentFiles = new List<string>();
      }

      if (Properties.Settings.Default.RecentFiles.Contains(filePath))
      {
        Properties.Settings.Default.RecentFiles.Remove(filePath);
      }

      Properties.Settings.Default.RecentFiles.Insert(0, filePath);

      if (Properties.Settings.Default.RecentFiles.Count > 10)
      {
        Properties.Settings.Default.RecentFiles.RemoveAt(10);
      }

      fileNameTB.Text = System.IO.Path.GetFileName(filePath);

      AdjustRange();
      OnPropertyChanged("DataSeries", "RecentFiles");
    }

    private void SameAsImage(string filePath)
    {
      using (var fs = File.Open(filePath, FileMode.OpenOrCreate))
      {
        var target = imageFrame;
        var size = target.RenderSize;
        var margin = 10;

        double dpiX = 96.0, dpiY = 96.0;
        var ps = PresentationSource.FromVisual(this);
        if (ps != null)
        {
          dpiX = 96.0 * ps.CompositionTarget.TransformToDevice.M11;
          dpiY = 96.0 * ps.CompositionTarget.TransformToDevice.M22;
        }

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

        Properties.Settings.Default.LastSaveLocation = System.IO.Path.GetDirectoryName(filePath);
      }
    }

    private void AdjustRange()
    {
      if (DataSeries.Count > 0)
      {
        var power = new DoubleRange();
        var torque = new DoubleRange();
        var fuel = new DoubleRange();

        if (chart.Series[0].Element.Visibility == Visibility.Visible)
        {
          var min = DataSeries.Min(dp => dp.Power);
          var max = DataSeries.Max(dp => dp.Power);
          if (min < power.Minimum)
            power.Minimum = min;
          if (power.Maximum < max)
            power.Maximum = max;
        }

        if (chart.Series[1].Element.Visibility == Visibility.Visible)
        {
          var min = DataSeries.Min(dp => dp.Torque);
          var max = DataSeries.Max(dp => dp.Torque);
          if (min < torque.Minimum)
            torque.Minimum = min;
          if (torque.Maximum < max)
            torque.Maximum = max;
        }

        if (chart.Series[2].Element.Visibility == Visibility.Visible)
        {
          var min = DataSeries.Min(dp => dp.BackTorque);
          var max = DataSeries.Max(dp => dp.BackTorque);
          if (min < torque.Minimum)
            torque.Minimum = min;
          if (torque.Maximum < max)
            torque.Maximum = max;
        }

        if (chart.Series[3].Element.Visibility == Visibility.Visible)
        {
          var min = DataSeries.Min(dp => dp.FuelConsumption);
          var max = DataSeries.Max(dp => dp.FuelConsumption);
          if (min < fuel.Minimum)
            fuel.Minimum = min;
          if (fuel.Maximum < max)
            fuel.Maximum = max;
        }

        power.Minimum -= (power.Maximum - power.Minimum) / 10.0;
        power.Maximum += (power.Maximum - power.Minimum) / 10.0;
        chart.YAxis.Range = power;
        chart.YAxis.Element.Visibility = chart.Series[0].Element.Visibility;

        torque.Minimum -= (torque.Maximum - torque.Minimum) / 10.0;
        torque.Maximum += (torque.Maximum - torque.Minimum) / 10.0;
        chart.SecondaryYAxis.Range = torque;
        chart.SecondaryYAxis.Element.Visibility =
          chart.Series[1].Element.Visibility == Visibility.Visible ||
          chart.Series[2].Element.Visibility == Visibility.Visible
          ? Visibility.Visible : Visibility.Collapsed;

        fuel.Minimum -= (fuel.Maximum - fuel.Minimum) / 10.0;
        fuel.Maximum += (fuel.Maximum - fuel.Minimum) / 10.0;
        chart.AdditionalSecondaryYAxes[0].Range = fuel;
        chart.AdditionalSecondaryYAxes[0].Element.Visibility = chart.Series[3].Element.Visibility;
      }
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
      var files = e.Data.GetData(DataFormats.FileDrop) as string[];
      if (files != null && files.Length > 0)
      {
        Open(files[0]);
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged(params string[] properties)
    {
      if (PropertyChanged != null)
      {
        foreach (var property in properties)
        {
          PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
      }
    }

    private void Exit_Executed(object sender, ExecutedRoutedEventArgs e)
    {
      this.Close();
    }

    private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
    {
      var dlg = new OpenFileDialog()
        {
          CheckFileExists = true,
          DefaultExt = "ini",
          Filter = "INI file (*.ini)|*.ini",
          InitialDirectory = Properties.Settings.Default.LastOpenLocation,
          ValidateNames = true,
        };

      if (dlg.ShowDialog(this) == true)
      {
        Open(dlg.FileName);
      }
    }

    private void SaveAsImage_Executed(object sender, ExecutedRoutedEventArgs e)
    {
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
        SameAsImage(dlg.FileName);
      }
    }

    private void SaveAsImage_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
      e.CanExecute = DataSeries.Count > 0;
    }

    private void Reload_Executed(object sender, ExecutedRoutedEventArgs e)
    {
      //Open(Properties.Settings.Default.RecentFiles[0]);
      AdjustRange();
    }

    private void Reload_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
      e.CanExecute = DataSeries.Count > 0;
    }

    private void ReloadFile_Clicked(object sender, RoutedEventArgs e)
    {
      if (DataSeries.Count > 0)
      {
        Open(Properties.Settings.Default.RecentFiles[0]);
      }
      else
      {
        AdjustRange();
      }
    }

    public string AppTitle
    {
      get
      {
        return string.Format("Engine Chart Viewer v1.0 ({0}x{1})",
          imageFrame.RenderSize.Width + 20, imageFrame.RenderSize.Height + 20);
      }
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      OnPropertyChanged("AppTitle");
    }

    private void Boost_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      AdjustRange();
    }
  }
}
