using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace EngineChartViewer
{
  public class DataPoint : INotifyPropertyChanged
  {
    private double _rpm, _torque, _backtorque;

    public double PowerKW
    {
      get { return _torque * (RPM * (Math.PI * 2.0) / 60.0) / 1000.0; }
    }

    public double TorqueNM
    {
      get { return _torque; }
      set { _torque = value; OnPropertyChanged("TorqueNM", "Torque", "PowerKW", "Power"); }
    }

    public double BackTorqueNM
    {
      get { return _backtorque; }
      set { _backtorque = value; OnPropertyChanged("BackTorqueNM", "BackTorque"); }
    }

    public double RPM
    {
      get { return _rpm; }
    }

    public double Power
    {
      get
      {
        switch (Properties.Settings.Default.PowerUnit)
        {
          case PowerUnits.KW: return PowerKW;
          case PowerUnits.PS: return PowerKW * Const.KW_PS;
          case PowerUnits.HP: return PowerKW * Const.KW_HP;
          default:
            break;
        }
        return 0;
      }
    }

    public double Torque
    {
      get
      {
        switch (Properties.Settings.Default.TorqueUnit)
        {
          case TorqueUnits.Nm: return TorqueNM;
          case TorqueUnits.Kgm: return TorqueNM * Const.NM_KGM;
          default:
            break;
        }
        return 0;
      }
      set
      {
        switch (Properties.Settings.Default.TorqueUnit)
        {
          case TorqueUnits.Nm:
            TorqueNM = value;
            break;
          case TorqueUnits.Kgm:
            TorqueNM = value / Const.NM_KGM;
            break;
          default:
            break;
        }
      }
    }

    public double BackTorque
    {
      get
      {
        switch (Properties.Settings.Default.TorqueUnit)
        {
          case TorqueUnits.Nm: return BackTorqueNM;
          case TorqueUnits.Kgm: return BackTorqueNM * Const.NM_KGM;
          default:
            break;
        }
        return 0;
      }
      set
      {
        switch (Properties.Settings.Default.TorqueUnit)
        {
          case TorqueUnits.Nm:
            BackTorqueNM = value;
            break;
          case TorqueUnits.Kgm:
            BackTorqueNM = value / Const.NM_KGM;
            break;
          default:
            break;
        }
      }
    }

    public DataPoint(double rpm, double torque, double backtorque)
    {
      _rpm = rpm;
      _torque = torque;
      _backtorque = backtorque;
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
  }
}
