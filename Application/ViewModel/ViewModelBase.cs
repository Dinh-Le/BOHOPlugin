using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BOHO.Application.ViewModel
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        protected virtual void SetProperty<T>(ref T prop, T value, [CallerMemberName] string propName = "")
        {
            prop = value;
            OnPropertyChanged(propName);
        }
    }
}
