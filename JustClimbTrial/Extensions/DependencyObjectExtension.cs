using System.Windows;
using System.Windows.Media;

namespace JustClimbTrial.Extensions
{
    public static class DependencyObjectExtension
    {
        // https://social.msdn.microsoft.com/Forums/vstudio/en-US/c47754bd-38c7-40b3-b64a-38a48884fde8/how-to-find-a-parent-of-a-specific-type?forum=wpf
        public static bool IsContainAncestor<T>(this DependencyObject child, 
            int numOfLevelsToTry = 10)
        {
            DependencyObject myChild = child;

            while (numOfLevelsToTry > 0)
            {
                DependencyObject parent = 
                    VisualTreeHelper.GetParent(myChild);

                if (parent is T)
                {
                    return true;
                }

                myChild = parent;
                numOfLevelsToTry--;
            }

            return false;
        }
    }
}
