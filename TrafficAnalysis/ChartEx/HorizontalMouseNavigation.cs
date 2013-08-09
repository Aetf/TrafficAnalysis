using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using System.Windows.Input;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.Navigation;

namespace TrafficAnalysis.ChartEx
{
    class HorizontalMouseNavigation : MouseNavigationBase
    {

        protected override void OnPlotterMouseDown(MouseButtonEventArgs e)
        {
            // dragging
            bool shouldStartDrag = ShouldStartPanning(e);
            if (shouldStartDrag)
                StartPanning(e);

            if (!Plotter.IsFocused)
            {
                Plotter.Focus();
                // this is done to prevent other tools like PointSelector from getting mouse click event when clicking on plotter
                // to activate window it's contained within
                e.Handled = true;
            }
        }

        protected override void OnPlotterMouseMove(MouseEventArgs e)
        {
            if (!isPanning) return;

            // dragging
            if (isPanning && e.LeftButton == MouseButtonState.Pressed)
            {
                if (!IsMouseCaptured)
                {
                    CaptureMouse();
                }

                Point endPoint = e.GetPosition(this).ScreenToViewport(Viewport.Transform);

                Point loc = Viewport.Visible.Location;
                Vector shift = panningStartPointInViewport - endPoint;
                shift.Y = 0; // Only move in X.
                loc += shift;

                // preventing unnecessary changes, if actually visible hasn't change.
                if (shift.X != 0 || shift.Y != 0)
                {
                    Cursor = Cursors.ScrollAll;

                    DataRect visible = Viewport.Visible;

                    visible.Location = loc;
                    Viewport.Visible = visible;
                }

                e.Handled = true;
            }
        }

        protected override void OnPlotterMouseUp(MouseButtonEventArgs e)
        {
            if (isPanning && e.ChangedButton == MouseButton.Left)
            {
                isPanning = false;
                StopPanning(e);
            }
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            if (isPanning)
            {
                Plotter2D.Viewport.PanningState = Viewport2DPanningState.NotPanning;
                isPanning = false;
            }
            ReleaseMouseCapture();
            base.OnLostFocus(e);
        }

        #region Horizontal pan

        private bool isPanning = false;
        protected bool IsPanning
        {
            get { return isPanning; }
        }

        private Point panningStartPointInViewport;
        protected Point PanningStartPointInViewport
        {
            get { return panningStartPointInViewport; }
        }

        Point panningStartPointInScreen;

        protected virtual bool ShouldStartPanning(MouseButtonEventArgs e)
        {
            return e.ChangedButton == MouseButton.Left && Keyboard.Modifiers == ModifierKeys.None;
        }

        protected virtual void StartPanning(MouseButtonEventArgs e)
        {
            panningStartPointInScreen = e.GetPosition(this);
            panningStartPointInViewport = panningStartPointInScreen.ScreenToViewport(Viewport.Transform);

            Plotter2D.UndoProvider.CaptureOldValue(Viewport, Viewport2D.VisibleProperty, Viewport.Visible);

            isPanning = true;

            // not capturing mouse because this made some tools like PointSelector not
            // receive MouseUp events on markers;
            // Mouse will be captured later, in the first MouseMove handler call.
            // CaptureMouse();

            Viewport.PanningState = Viewport2DPanningState.Panning;

            //e.Handled = true;
        }

        protected virtual void StopPanning(MouseButtonEventArgs e)
        {
            Plotter2D.UndoProvider.CaptureNewValue(Plotter2D.Viewport, Viewport2D.VisibleProperty, Viewport.Visible);

            if (!Plotter.IsFocused)
            {
                Plotter2D.Focus();
            }

            Plotter2D.Viewport.PanningState = Viewport2DPanningState.NotPanning;

            ReleaseMouseCapture();
            ClearValue(CursorProperty);
        }

        #endregion

        #region Mouse whell zoom
        private const double wheelZoomSpeed = 1.2;
        private void MouseWheelZoom(Point mousePos, double wheelRotationDelta)
        {
            Point zoomTo = mousePos.ScreenToViewport(Viewport.Transform);

            double zoomSpeed = Math.Abs(wheelRotationDelta / Mouse.MouseWheelDeltaForOneLine);
            zoomSpeed *= wheelZoomSpeed;
            if (wheelRotationDelta < 0)
            {
                zoomSpeed = 1 / zoomSpeed;
            }
            //Viewport.Visible = Viewport.Visible.Zoom(zoomTo, zoomSpeed);
            Viewport.Visible = CoordinateUtilities.RectZoomX(Viewport.Visible, zoomTo, zoomSpeed);
        }
        #endregion
    }
}
