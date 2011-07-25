using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace EngineChartViewer
{
  public class RecentFile
  {
    private int _index;
    private string _filePath;

    public string Header
    {
      get
      {
        return string.Format("_{0}: {1}", _index, _filePath);
      }
    }

    public ICommand Command
    {
      get
      {
        return new RelayCommand(
          param => ((MainWindow)App.Current.MainWindow).Open(_filePath));
      }
    }

    public RecentFile(int index, string filePath)
    {
      _index = index;
      _filePath = filePath;
    }
  }
}
