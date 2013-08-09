using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.Charts;
using TrafficAnalysis.Util;
using System.Diagnostics.CodeAnalysis;

namespace TrafficAnalysis.ChartEx
{
    /// <summary>
    /// RangeSelectionBound.xaml 的交互逻辑
    /// </summary>
    public partial class SelectionLine : ContentGraph, INotifyPropertyChanged
    {
        public SelectionLine()
        {
            InitializeComponent();

            IsHitTestVisible = true;

            //Position = new Point(10, 10);
        }

        Vector blockShift = new Vector(3, 3);

        bool isDraging = false;

        #region Plotter

        protected override void OnPlotterAttached()
        {
            UIElement parent = (UIElement)Parent;

            parent.MouseMove += parent_MouseMove;
            parent.MouseLeftButtonDown += parent_MouseLeftButtonDown;
            parent.MouseLeftButtonUp += parent_MouseLeftButtonUp;

            UpdateVisibility();
            UpdateUIRepresentation();
        }

        protected override void OnPlotterDetaching()
        {
            UIElement parent = (UIElement)Parent;

            parent.MouseMove -= parent_MouseMove;
            parent.MouseLeftButtonDown -= parent_MouseLeftButtonDown;
            parent.MouseLeftButtonUp -= parent_MouseLeftButtonUp;
        }

        #endregion

        private void parent_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDraging)
            {
                Rect output = Plotter2D.Viewport.Output;
                Point mousePos = Mouse.GetPosition(this);

                if (orientation == Orientation.Vertical)
                {
                    if (mousePos.X < output.Left)
                        mousePos.X = output.Left;
                    if (mousePos.X > output.Right)
                        mousePos.X = output.Right;
                }
                if (orientation == Orientation.Horizontal)
                {
                    if (mousePos.Y < output.Bottom)
                        mousePos.Y = output.Bottom;
                    if (mousePos.Y > output.Top)
                        mousePos.Y = output.Top;
                }

                Position = mousePos;
                UpdateUIRepresentation();
                e.Handled = true;
            }
        }

        private void parent_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            UIElement parent = (UIElement)Parent;
            bool hited = false;
            VisualTreeHelper.HitTest(parent, null, res =>
            {
                FrameworkElement ue = res.VisualHit as FrameworkElement;
                if(ue != null && "SelectionLine".Equals(ue.Tag))
                {
                    if (res.VisualHit.Equals(this.horizLine)
                    || res.VisualHit.Equals(this.vertLine)
                    || res.VisualHit.Equals(this.horizRect)
                    || res.VisualHit.Equals(vertRect))
                    { // self
                        hited = true;
                        return HitTestResultBehavior.Stop;
                    }
                    else // others
                    {
                        return HitTestResultBehavior.Stop;
                    }
                }
                
                return HitTestResultBehavior.Continue;
            }, new PointHitTestParameters(e.GetPosition(parent)));

            if (hited)
            {
                isDraging = true;
                CaptureMouse();
            }
        }

        private void parent_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDraging)
            {
                ReleaseMouseCapture();
                isDraging = false;
            }
        }

        protected override void OnViewportPropertyChanged(ExtendedPropertyChangedEventArgs e)
        {
            UpdateUIRepresentation();
        }

        private void UpdateUIRepresentation()
        {
            //var transform = Plotter2D.Viewport.Transform;
            //Position = Mouse.GetPosition(this).ScreenToData(transform);
            UpdateUIRepresentation(Position);
        }

        private void UpdateUIRepresentation(Point mousePos)
        {
            if (Plotter2D == null) return;

            var transform = Plotter2D.Viewport.Transform;
            DataRect visible = Plotter2D.Viewport.Visible;
            Rect output = Plotter2D.Viewport.Output;

            //Point mousePos = mousePosInData.DataToScreen(transform);

            horizLine.X1 = output.Left;
            horizLine.X2 = output.Right;
            horizLine.Y1 = mousePos.Y;
            horizLine.Y2 = mousePos.Y;

            vertLine.X1 = mousePos.X;
            vertLine.X2 = mousePos.X;
            vertLine.Y1 = output.Top;
            vertLine.Y2 = output.Bottom;

            if (UseDashOffset)
            {
                horizLine.StrokeDashOffset = (output.Right - mousePos.X) / 2;
                vertLine.StrokeDashOffset = (output.Bottom - mousePos.Y) / 2;
            }

            Point mousePosInData = mousePos.ScreenToData(transform);

            string text = null;

            if (showVerticalLine)
            {
                double xValue = mousePosInData.X;
                if (xTextMapping != null)
                    text = xTextMapping(xValue);

                // doesnot have xTextMapping or it returned null
                if (text == null)
                    text = GetRoundedValue(visible.XMin, visible.XMax, xValue);

                if (!String.IsNullOrEmpty(customXFormat))
                    text = String.Format(customXFormat, text);
                horizTextBlock.Text = text;

                double width = horizGrid.ActualWidth;
                double x = mousePos.X + blockShift.X;
                if (x + width > output.Right)
                {
                    x = mousePos.X - blockShift.X - width;
                }
                Canvas.SetLeft(horizGrid, x);
            }



            if (showHorizontalLine)
            {
                double yValue = mousePosInData.Y;
                text = null;
                if (yTextMapping != null)
                    text = yTextMapping(yValue);

                if (text == null)
                    text = GetRoundedValue(visible.YMin, visible.YMax, yValue);

                if (!String.IsNullOrEmpty(customYFormat))
                    text = String.Format(customYFormat, text);
                vertTextBlock.Text = text;

                // by default vertGrid is positioned on the top of line.
                double height = vertGrid.ActualHeight;
                double y = mousePos.Y - blockShift.Y - height;
                if (y < output.Top)
                {
                    y = mousePos.Y + blockShift.Y;
                }
                Canvas.SetTop(vertGrid, y);
            }
        }

        private string GetRoundedValue(double min, double max, double value)
        {
            double roundedValue = value;
            var log = RoundingHelper.GetDifferenceLog(min, max);
            string format = "G3";
            double diff = Math.Abs(max - min);
            if (1E3 < diff && diff < 1E6)
            {
                format = "F0";
            }
            if (log < 0)
                format = "G" + (-log + 2).ToString();

            return roundedValue.ToString(format);
        }

        #region public string CustomYFormat
        private string customXFormat = null;
        /// <summary>
        /// Gets or sets the custom format string of x label.
        /// </summary>
        /// <value>The custom X format.</value>
        public string CustomXFormat
        {
            get { return customXFormat; }
            set
            {
                if (customXFormat != value)
                {
                    customXFormat = value;
                    UpdateUIRepresentation();
                }
            }
        }
        #endregion

        #region public string CustomYFormat
        private string customYFormat = null;
        /// <summary>
        /// Gets or sets the custom format string of y label.
        /// </summary>
        /// <value>The custom Y format.</value>
        public string CustomYFormat
        {
            get { return customYFormat; }
            set
            {
                if (customYFormat != value)
                {
                    customYFormat = value;
                    UpdateUIRepresentation();
                }
            }
        }
        #endregion

        #region public Func<double, string> XTextMapping
        private Func<double, string> xTextMapping = null;
        /// <summary>
        /// Gets or sets the text mapping of x label - function that builds text from x-coordinate of mouse in data.
        /// </summary>
        /// <value>The X text mapping.</value>
        public Func<double, string> XTextMapping
        {
            get { return xTextMapping; }
            set
            {
                if (xTextMapping != value)
                {
                    xTextMapping = value;
                    UpdateUIRepresentation();
                }
            }
        }
        #endregion

        #region public Func<double, string> YTextMapping
        private Func<double, string> yTextMapping = null;
        /// <summary>
        /// Gets or sets the text mapping of y label - function that builds text from y-coordinate of mouse in data.
        /// </summary>
        /// <value>The Y text mapping.</value>
        public Func<double, string> YTextMapping
        {
            get { return yTextMapping; }
            set
            {
                if (yTextMapping != value)
                {
                    yTextMapping = value;
                    UpdateUIRepresentation();
                }
            }
        }
        #endregion

        #region public System.Windows.Controls.Orientation Orientation
        private Orientation orientation = Orientation.Vertical;
        private bool showHorizontalLine = false;
        private bool showVerticalLine = true;

        private void UpdateVisibility()
        {
            horizLine.Visibility = vertGrid.Visibility = GetHorizontalVisibility();
            vertLine.Visibility = horizGrid.Visibility = GetVerticalVisibility();
        }

        private Visibility GetHorizontalVisibility()
        {
            return showHorizontalLine ? Visibility.Visible : Visibility.Collapsed;
        }

        private Visibility GetVerticalVisibility()
        {
            return showVerticalLine ? Visibility.Visible : Visibility.Collapsed;
        }
        /// <summary>
        /// Gets or sets a value indicating whether to show horizontal or vertical line.
        /// </summary>
        public Orientation Orientation
        {
            get { return orientation; }
            set
            {
                if (orientation != value)
                {
                    orientation = value;
                    showHorizontalLine = orientation == Orientation.Horizontal;
                    showVerticalLine = orientation == Orientation.Vertical;
                    UpdateVisibility();
                }
            }
        }
        #endregion

        #region public double CurrentValue
        public double CurrentValue
        {
            get
            {
                var transform = Plotter2D.Viewport.Transform;
                Point dataPoint = Position.ScreenToData(transform);
                return orientation == Orientation.Horizontal ?
                    dataPoint.Y : dataPoint.X;
            }
        }
        #endregion


        #region Position property
        /// <summary>
        /// Gets or sets the mouse position in screen coordinates.
        /// </summary>
        /// <value>The position.</value>
        public Point Position
        {
            get { return (Point)GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }

        /// <summary>
        /// Identifies Position dependency property.
        /// </summary>
        public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
            "Position",
            typeof(Point),
            typeof(SelectionLine),
            new UIPropertyMetadata(new Point(), OnPositionChanged));

        private static void OnPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SelectionLine graph = (SelectionLine)d;
            graph.UpdateUIRepresentation((Point)e.NewValue);
            graph.OnPropertyChanged("Position");
            graph.OnPropertyChanged("CurrentValue");
        }
        #endregion

        #region UseDashOffset property

        public bool UseDashOffset
        {
            get { return (bool)GetValue(UseDashOffsetProperty); }
            set { SetValue(UseDashOffsetProperty, value); }
        }

        public static readonly DependencyProperty UseDashOffsetProperty = DependencyProperty.Register(
          "UseDashOffset",
          typeof(bool),
          typeof(SelectionLine),
          new FrameworkPropertyMetadata(true, UpdateUIRepresentation));

        private static void UpdateUIRepresentation(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SelectionLine graph = (SelectionLine)d;
            if ((bool)e.NewValue)
            {
                graph.UpdateUIRepresentation();
            }
            else
            {
                graph.vertLine.ClearValue(Line.StrokeDashOffsetProperty);
                graph.horizLine.ClearValue(Line.StrokeDashOffsetProperty);
            }
        }

        #endregion

        #region LineStroke property

        public Brush LineStroke
        {
            get { return (Brush)GetValue(LineStrokeProperty); }
            set { SetValue(LineStrokeProperty, value); }
        }

        public static readonly DependencyProperty LineStrokeProperty = DependencyProperty.Register(
          "LineStroke",
          typeof(Brush),
          typeof(SelectionLine),
          new PropertyMetadata(new SolidColorBrush(Color.FromArgb(170, 86, 86, 86))));

        #endregion

        #region LineStrokeThickness property

        public double LineStrokeThickness
        {
            get { return (double)GetValue(LineStrokeThicknessProperty); }
            set { SetValue(LineStrokeThicknessProperty, value); }
        }

        public static readonly DependencyProperty LineStrokeThicknessProperty = DependencyProperty.Register(
          "LineStrokeThickness",
          typeof(double),
          typeof(SelectionLine),
          new PropertyMetadata(2.0));

        #endregion

        #region LineStrokeDashArray property

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public DoubleCollection LineStrokeDashArray
        {
            get { return (DoubleCollection)GetValue(LineStrokeDashArrayProperty); }
            set { SetValue(LineStrokeDashArrayProperty, value); }
        }

        public static readonly DependencyProperty LineStrokeDashArrayProperty = DependencyProperty.Register(
          "LineStrokeDashArray",
          typeof(DoubleCollection),
          typeof(SelectionLine),
          new FrameworkPropertyMetadata(DoubleCollectionHelper.Create(3, 3)));

        #endregion


        #region PropertyChanged Event
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }
}
