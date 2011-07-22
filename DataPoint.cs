using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace EngineChartViewer
{
  public enum TorqueUnits { NM, KGM }
  public enum PowerUnits { KW, PS, BHP }

  public class DataCollection : ObservableCollection<DataPoint> { }

  public class DataPoint : INotifyPropertyChanged
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

    public DataPoint(double rpm, double torque, double backtorque)
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
}
