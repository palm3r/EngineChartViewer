using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Visiblox.Charts;

namespace EngineChartViewer
{
  public partial class MainWindow : Window, INotifyPropertyChanged
  {
    public static readonly ICommand OpenCommand =
      new RoutedUICommand("Open", "OpenCommand", typeof(MainWindow));

    public static readonly ICommand SaveAsImageCommand =
      new RoutedUICommand("Save as Image", "SaveAsImageCommand", typeof(MainWindow));

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
      using (var reader = new StreamReader(filePath, Encoding.ASCII))
      {
        var regex = new Regex(
          @"^RPMTorque=\(\s*(?<RPM>[+-]?\d*\.?\d*),\s*(?<BackTorque>[+-]?\d*\.?\d*),\s*(?<Torque>[+-]?\d*\.?\d*)\).*",
          RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        DataSeries.Clear();

        string line = "";
        while ((line = reader.ReadLine()) != null)
        {
          var m = regex.Match(line);
          if (m.Success)
          {
            var rpm = double.Parse(m.Groups["RPM"].Value);
            var backtorque = double.Parse(m.Groups["BackTorque"].Value);
            var torque = double.Parse(m.Groups["Torque"].Value);

            DataSeries.Add(new DataPoint(rpm, torque, backtorque));
          }
        }

        var mp = DataSeries.OrderBy(d => d.Power).Last();
        var mt = DataSeries.OrderBy(d => d.Torque).Last();

        fileNameTB.Text = System.IO.Path.GetFileName(filePath);
        maxPowerTB.Text = string.Format(
          "{0:f1} KW ({1:f1} PS / {2:f1} HP) @ {3:f0} rpm",
          mp.PowerKW, mp.PowerKW * Const.KW_PS, mp.PowerKW * Const.KW_HP, mp.RPM);
        maxTorqueTB.Text = string.Format(
          "{0:f1} Nm ({1:f1} Kgm) @ {2:f0} rpm",
          mt.TorqueNM, mt.TorqueNM * Const.NM_KGM, mt.RPM);

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
          Properties.Settings.Default.RecentFiles.RemoveAt(10);

        AdjustRange();
        OnPropertyChanged("RecentFiles");
      }
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
        var pr = new DoubleRange();
        var tr = new DoubleRange();

        if (PowerSeries.Visibility == Visibility.Visible)
        {
          var min = DataSeries.Min(dp => dp.Power);
          var max = DataSeries.Max(dp => dp.Power);
          if (min < pr.Minimum)
            pr.Minimum = min;
          if (pr.Maximum < max)
            pr.Maximum = max;
        }

        if (TorqueSeries.Visibility == Visibility.Visible)
        {
          var min = DataSeries.Min(dp => dp.Torque);
          var max = DataSeries.Max(dp => dp.Torque);
          if (min < tr.Minimum)
            tr.Minimum = min;
          if (tr.Maximum < max)
            tr.Maximum = max;
        }

        if (BackTorqueSeries.Visibility == Visibility.Visible)
        {
          var min = DataSeries.Min(dp => dp.BackTorque);
          var max = DataSeries.Max(dp => dp.BackTorque);
          if (min < tr.Minimum)
            tr.Minimum = min;
          if (tr.Maximum < max)
            tr.Maximum = max;
        }

        pr.Minimum -= (pr.Maximum - pr.Minimum) / 10.0;
        pr.Maximum += (pr.Maximum - pr.Minimum) / 10.0;
        PowerAxis.Range = pr;

        tr.Minimum -= (tr.Maximum - tr.Minimum) / 10.0;
        tr.Maximum += (tr.Maximum - tr.Minimum) / 10.0;
        TorqueAxis.Range = tr;
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

    private void ReloadFile_Clicked(object sender, RoutedEventArgs e)
    {
      Open(Properties.Settings.Default.RecentFiles[0]);
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
  }
}
