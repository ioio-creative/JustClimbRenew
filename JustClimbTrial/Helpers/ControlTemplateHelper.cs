using System.Windows;
using System.Windows.Controls;

namespace JustClimbTrial.Helpers
{
    public class ControlTemplateHelper
    {
        public static void SetTemplateOfControlFromResource(Control ctrl, 
            FrameworkElement resourceOwner, string resourceKey)
        {
            ctrl.Template = GetControlTemplateFromResource(resourceOwner, resourceKey);
        }

        public static ControlTemplate GetControlTemplateFromResource(FrameworkElement resourceOwner, 
            string resourceKey)
        {
            return resourceOwner.Resources[resourceKey] as ControlTemplate;
        }
    }
}
