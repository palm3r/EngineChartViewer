using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Visiblox.Charts;

namespace EngineChartViewer
{
  public class DataSeries : ICollection<DataPoint>, INotifyCollectionChanged, INotifyPropertyChanged
  {
    private ObservableCollection<DataPoint> _dp = new ObservableCollection<DataPoint>();

    public Dictionary<string, dynamic> Data
    {
      get { return DataPoint.Data; }
      set
      {
        var data = DataPoint.Data = value;

        if (data.ContainsKey("EngineBoostRange") &&
          data.ContainsKey("EngineBoostSetting") &&
          data.ContainsKey("BoostTorque") &&
          data.ContainsKey("BoostPower") &&
          data.ContainsKey("BoostEffects"))
        {
          BoostMin = (int)data["EngineBoostRange"][0];
          BoostStep = (int)data["EngineBoostRange"][1];
          BoostRange = (int)data["EngineBoostRange"][2];
          BoostIndex = (int)data["EngineBoostSetting"];
          BoostTorque = data["BoostTorque"];
          BoostPower = data["BoostPower"];
          BoostRpm = data["BoostEffects"][0];
          BoostFuel = data["BoostEffects"][1];
          BoostWear = data["BoostEffects"][2];
        }
        else
        {
          BoostMin = 0;
          BoostStep = 0;
          BoostRange = 0;
          BoostIndex = 0;
          BoostTorque = 0;
          BoostPower = 0;
          BoostRpm = 0;
          BoostFuel = 0;
          BoostWear = 0;
        }

        OnPropertyChanged("Data");
      }
    }

    public int BoostMin
    {
      get { return DataPoint.BoostMin; }
      set
      {
        DataPoint.BoostMin = value;
        _dp.ToList().ForEach(dp => dp.OnPropertyChanged("Power", "Torque", "FuelConsumption"));
        OnPropertyChanged("BoostMin", "ActualBoost", "MaxPower", "MaxTorque");
      }
    }

    public int BoostStep
    {
      get { return DataPoint.BoostStep; }
      set
      {
        DataPoint.BoostStep = value;
        _dp.ToList().ForEach(dp => dp.OnPropertyChanged("Power", "Torque", "FuelConsumption"));
        OnPropertyChanged("BoostStep", "ActualBoost", "MaxPower", "MaxTorque");
      }
    }

    public int BoostRange
    {
      get { return DataPoint.BoostRange; }
      set
      {
        DataPoint.BoostRange = value;
        OnPropertyChanged("BoostRange");
      }
    }

    public int BoostIndex
    {
      get { return DataPoint.BoostIndex; }
      set
      {
        DataPoint.BoostIndex = value;
        _dp.ToList().ForEach(dp => dp.OnPropertyChanged("Power", "Torque", "FuelConsumption"));
        OnPropertyChanged("BoostIndex", "ActualBoost", "MaxPower", "MaxTorque");
      }
    }

    public int ActualBoost
    {
      get { return DataPoint.ActualBoost; }
      set
      {
        DataPoint.ActualBoost = value;
        _dp.ToList().ForEach(dp => dp.OnPropertyChanged("Power", "Torque", "FuelConsumption"));
        OnPropertyChanged("BoostIndex", "ActualBoost", "MaxPower", "MaxTorque");
      }
    }

    public double BoostRpm
    {
      get
      {
        return DataPoint.BoostRpm;
      }
      set
      {
        if (DataPoint.BoostRpm != value)
        {
          DataPoint.BoostRpm = value;
          _dp.ToList().ForEach(dp => dp.OnPropertyChanged("Rpm", "Power"));
          OnPropertyChanged("BoostRpm", "MaxPower");
        }
      }
    }

    public double BoostPower
    {
      get
      {
        return DataPoint.BoostPower;
      }
      set
      {
        if (DataPoint.BoostPower != value)
        {
          DataPoint.BoostPower = value;
          _dp.ToList().ForEach(dp => dp.OnPropertyChanged("Power"));
          OnPropertyChanged("BoostPower", "MaxPower");
        }
      }
    }

    public double BoostTorque
    {
      get
      {
        return DataPoint.BoostTorque;
      }
      set
      {
        if (DataPoint.BoostTorque != value)
        {
          DataPoint.BoostTorque = value;
          _dp.ToList().ForEach(dp => dp.OnPropertyChanged("Torque", "Power"));
          OnPropertyChanged("BoostTorque", "MaxPower", "MaxTorque");
        }
      }
    }

    public double BoostFuel
    {
      get
      {
        return DataPoint.BoostFuel;
      }
      set
      {
        if (DataPoint.BoostFuel != value)
        {
          DataPoint.BoostFuel = value;
          _dp.ToList().ForEach(dp => dp.OnPropertyChanged("FuelConsumption"));
          OnPropertyChanged("BoostFuel");
        }
      }
    }

    public double BoostWear
    {
      get
      {
        return DataPoint.BoostWear;
      }
      set
      {
        if (DataPoint.BoostWear != value)
        {
          DataPoint.BoostWear = value;
          OnPropertyChanged("BoostWear");
        }
      }
    }

    public DataPoint MaxPower
    {
      get { return _dp.OrderBy(dp => dp.Power).LastOrDefault(); }
    }

    public DataPoint MaxTorque
    {
      get { return _dp.OrderBy(dp => dp.Torque).LastOrDefault(); }
    }

    public DataSeries()
    {
    }

    public void Add(DataPoint item)
    {
      _dp.Add(item);

      if (CollectionChanged != null)
      {
        CollectionChanged(this, new NotifyCollectionChangedEventArgs(
          NotifyCollectionChangedAction.Reset));
      }
    }

    public void Clear()
    {
      _dp.Clear();

      if (CollectionChanged != null)
      {
        CollectionChanged(this, new NotifyCollectionChangedEventArgs(
          NotifyCollectionChangedAction.Reset));
      }

      /*
      Data.Clear();
      OnPropertyChanged("Data");

      BoostMin = 0;
      BoostStep = 0;
      BoostRange = 0;
      BoostIndex = 0;
      BoostTorque = 0;
      BoostPower = 0;
      BoostRpm = 0;
      BoostFuel = 0;
      BoostWear = 0;
      */
    }

    public bool Contains(DataPoint item)
    {
      return _dp.Contains(item);
    }

    public void CopyTo(DataPoint[] array, int index)
    {
      _dp.CopyTo(array, index);
    }

    public int Count
    {
      get { return _dp.Count; }
    }

    public bool IsReadOnly
    {
      get { return false; }
    }

    public bool Remove(DataPoint item)
    {
      bool b = _dp.Remove(item);

      if (CollectionChanged != null)
      {
        CollectionChanged(this, new NotifyCollectionChangedEventArgs(
          NotifyCollectionChangedAction.Remove, item));
      }

      return b;
    }

    public IEnumerator<DataPoint> GetEnumerator()
    {
      return _dp.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return _dp.GetEnumerator();
    }

    public event NotifyCollectionChangedEventHandler CollectionChanged;

    public void OnCollectionChanged(NotifyCollectionChangedAction action)
    {
      if (CollectionChanged != null)
      {
        CollectionChanged(this, new NotifyCollectionChangedEventArgs(action));
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
  }
}
