using System.Windows;

namespace JustClimbTrial.Helpers
{
    

    public class UiHelper
    {
        private const string defaultCaption = "Just Climb";

        public static void NotifyUser(string msg, string caption = defaultCaption)
        {
            // MessageBox is modal automatically
            MessageBox.Show(msg, caption);
        }

        public static MessageBoxResult NotifyUserResult(string msg, string caption = defaultCaption)
        {
            return MessageBox.Show(msg, caption);
        }

        public static MessageBoxResult NotifyUserYesNo(string msg, string caption = defaultCaption, MessageBoxImage msgImg = MessageBoxImage.None)
        {
            return MessageBox.Show(msg, caption, MessageBoxButton.YesNo, msgImg);
        }
    }
}
