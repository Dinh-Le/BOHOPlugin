using System.Windows;
using BOHO.Core.Interfaces;

namespace BOHO.Application.Util
{
    public class MessageService : IMessageService
    {
        public void ShowError(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
