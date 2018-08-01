using JustClimbTrial.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace JustClimbTrial.Views.UserControls
{
    // https://docs.microsoft.com/en-us/dotnet/framework/wpf/advanced/walkthrough-enabling-drag-and-drop-on-a-user-control
    public partial class MyRockShape : UserControl
    {
        #region members

        public const string RockViewModelDataFormatName = "RockViewModel";
        private const DragDropEffects allowedDragDropEffects = DragDropEffects.Move;

        private Shape rockShape;
        private RockViewModel owner;

        #endregion


        #region constructors

        public MyRockShape(Shape aRockShape, RockViewModel rockVM)
        {
            InitializeComponent();
            rockShape = aRockShape;
            owner = rockVM;
        }

        #endregion


        #region event handlers

        protected override void OnMouseMove(MouseEventArgs e)
        {            
            base.OnMouseMove(e);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // Package the data.
                DataObject data = new DataObject();
                data.SetData(RockViewModelDataFormatName, owner);

                // Initiate the drag-and-drop operation.
                DragDrop.DoDragDrop(this, data, allowedDragDropEffects);
            }
        }

        protected override void OnGiveFeedback(GiveFeedbackEventArgs e)
        {
            base.OnGiveFeedback(e);
            // These Effects values are set in the drop target's
            // DragOver event handler.
            if (e.Effects.HasFlag(DragDropEffects.Move))
            {
                Mouse.SetCursor(Cursors.Cross);
            }
            else
            {
                Mouse.SetCursor(Cursors.No);
            }
            e.Handled = true;
        }

        #endregion


        #region draw rock shape

        public void SetWidth(double width)
        {
            rockShape.Width = width;            
        }

        public void SetHeight(double height)
        {
            rockShape.Height = height;
        }

        public void RecolorRockShape(Brush color)
        {
            rockShape.Stroke = color;
        }

        #endregion


        #region getters & setters

        public Shape GetShape()
        {
            return rockShape;
        }

        public double GetWidth()
        {
            return rockShape.Width;
        }

        public double GetHeight()
        {
            return rockShape.Height;
        }

        #endregion
    }
}
