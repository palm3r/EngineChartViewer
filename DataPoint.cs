using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Collections;
using System.Collections.Generic;

namespace EngineChartViewer
{
  public class DataPoint : INotifyPropertyChanged
  {
    private static Dictionary<string, dynamic> _data;

    public static Dictionary<string, dynamic> Data
    {
      get { return _data; }
      set { _data = value; }
    }

    private static int _boostMin = 0;
    private static int _boostStep = 0;
    private static int _boostRange = 0;
    private static int _boostIndex = 0;
    // (double)(BoostMin + (BoostStep * BoostIndex))

    private static double _boostRpm = 0;
    private static double _boostPower = 0;
    private static double _boostTorque = 0;
    private static double _boostFuel = 0;
    private static double _boostWear = 0;

    public static int BoostMin
    {
      get { return _boostMin; }
      set { _boostMin = value; }
    }

    public static int BoostStep
    {
      get { return _boostStep; }
      set { _boostStep = value; }
    }

    public static int BoostRange
    {
      get { return _boostRange; }
      set { _boostRange = value; }
    }

    public static int BoostIndex
    {
      get { return _boostIndex; }
      set { _boostIndex = value; }
    }

    public static double BoostMax
    {
      get { return BoostMin + (BoostStep * BoostRange); }
    }

    public static int ActualBoost
    {
      get { return BoostMin + (BoostStep * BoostIndex); }
      set
      {
        int index = 0;
        for (int b = BoostMin; b < BoostRange; b += BoostStep)
        {
          if (b == value)
          {
            BoostIndex = index;
            return;
          }
          ++index;
        }
        throw new InvalidOperationException();
      }
    }

    public static double BoostRpm
    {
      get { return _boostRpm; }
      set { _boostRpm = value; }
    }

    public static double BoostPower
    {
      get { return _boostPower; }
      set { _boostPower = value; }
    }

    public static double BoostTorque
    {
      get { return _boostTorque; }
      set { _boostTorque = value; }
    }

    public static double BoostFuel
    {
      get { return _boostFuel; }
      set { _boostFuel = value; }
    }

    public static double BoostWear
    {
      get { return _boostWear; }
      set { _boostWear = value; }
    }

    private double _rpm;
    private double _torque, _backTorque;

    public double RPM
    {
      get { return _rpm; }
    }

    public double TorqueNM
    {
      get
      {
        return _torque * (1.0 + ActualBoost * BoostTorque);
      }
      set
      {
        _torque = value;
        OnPropertyChanged("TorqueNM", "Torque", "PowerKW", "Power");
      }
    }

    public double BackTorqueNM
    {
      get { return _backTorque; }
      set { _backTorque = value; OnPropertyChanged("BackTorqueNM", "BackTorque"); }
    }

    public double PowerKW
    {
      get
      {
        return TorqueNM * ((RPM + (1.0 + ActualBoost * BoostRpm)) * (Math.PI * 2.0) / 60.0)
          * (1.0 + (ActualBoost * BoostPower)) / 1000.0;
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

    public double FuelConsumption
    {
      get
      {
        return _data["FuelConsumption"] * ((RPM + (1.0 + ActualBoost * BoostRpm)) * (Math.PI * 2.0) / 60.0)
          * (1.0 + ActualBoost * BoostFuel) * 60.0;
      }
    }

    public DataPoint(double rpm, double torque, double backtorque)
    {
      _rpm = rpm;
      _torque = torque;
      _backTorque = backtorque;
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
