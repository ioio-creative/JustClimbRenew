using System.Windows;

namespace JustClimbTrial.Helpers
{
    public class UiHelper
    {
        public static void NotifyUser(string msg)
        {
            // MessageBox is modal automatically
            MessageBox.Show(msg);
        }

        public static MessageBoxResult NotifyUserResult(string msg)
        {
            return MessageBox.Show(msg);
        }
    }
}
