﻿using JustClimbTrial.Views.UserControls;
using System.Windows;
using System.Windows.Controls;

namespace JustClimbTrial.Views.CustomControls.MasterPages
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:JustClimbTrial.Views.CustomControls.MasterPages"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:JustClimbTrial.Views.CustomControls.MasterPages;assembly=JustClimbTrial.Views.CustomControls.MasterPages"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:MasterWithNavigation/>
    ///
    /// </summary>
    /// https://www.codeproject.com/Articles/23069/WPF-Master-Pages
    /// /Theme/Generic.xmal
    public class MasterWithNavigation : Control
    {
        static MasterWithNavigation()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MasterWithNavigation), new FrameworkPropertyMetadata(typeof(MasterWithNavigation)));
        }
        
        public HeaderRowNavigation NavHead
        {
            get
            {
                // https://docs.microsoft.com/en-us/dotnet/framework/wpf/controls/how-to-find-controltemplate-generated-elements
                // https://stackoverflow.com/questions/29094171/how-to-find-controls-in-custom-window-in-wpf
                // Note: You need to do it in the Loaded event (or later).
                return Template.FindName("navHead", this) as HeaderRowNavigation;
            }
        }

        public object Content
        {
            get { return GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register("Content", typeof(object),
                typeof(MasterWithNavigation), new UIPropertyMetadata());
    }
}
