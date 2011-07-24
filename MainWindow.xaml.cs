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

    public DataSeries DataSeries
    {
      get
      {
        return this.FindResource("DataSeries") as DataSeries;
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

    private void OpenFile(string filePath)
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

        AdjustRange();

        var maxTorque = DataSeries.OrderBy(d => d.Torque).Last();
        var maxPower = DataSeries.OrderBy(d => d.Power).Last();

        fileNameTB.Text = System.IO.Path.GetFileName(filePath);
        maxPowerTB.Text = string.Format(
          "Max Power : {0:f1} KW ({1:f0} PS / {2:f0} BHP) @ {3:f0} rpm",
          maxPower.Power, maxPower.Power * 1.3596, maxPower.Power * 1.341, maxPower.RPM);
        maxTorqueTB.Text = string.Format(
          "Max Torque : {0:f1} Nm ({1:f1} Kgm) @ {2:f0} rpm",
          maxTorque.Torque, maxTorque.Torque * 0.10197, maxTorque.RPM);

        Properties.Settings.Default.LastOpenLocation = System.IO.Path.GetDirectoryName(filePath);
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

    private void Window_Drop(object sender, DragEventArgs e)
    {
      var files = e.Data.GetData(DataFormats.FileDrop) as string[];
      if (files != null && files.Length > 0)
      {
        OpenFile(files[0]);
      }
    }

    private void AdjustRange()
    {
      var torqueRange = new DoubleRange();
      var powerRange = new DoubleRange();

      if (PowerSeries.Visibility == Visibility.Visible)
      {
        var min = DataSeries.Min(d => d.Power);
        var max = DataSeries.Max(d => d.Power);
        if (min < powerRange.Minimum)
          powerRange.Minimum = min;
        if (powerRange.Maximum < max)
          powerRange.Maximum = max;
      }

      if (TorqueSeries.Visibility == Visibility.Visible)
      {
        var min = DataSeries.Min(d => d.Torque);
        var max = DataSeries.Max(d => d.Torque);
        if (min < torqueRange.Minimum)
          torqueRange.Minimum = min;
        if (torqueRange.Maximum < max)
          torqueRange.Maximum = max;
      }

      if (BackTorqueSeries.Visibility == Visibility.Visible)
      {
        var min = DataSeries.Min(d => d.BackTorque);
        var max = DataSeries.Max(d => d.BackTorque);
        if (min < torqueRange.Minimum)
          torqueRange.Minimum = min;
        if (torqueRange.Maximum < max)
          torqueRange.Maximum = max;
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
        OpenFile(dlg.FileName);
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

    private void Visibility_Click(object sender, RoutedEventArgs e)
    {
      if (DataSeries.Count > 0)
        AdjustRange();
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
