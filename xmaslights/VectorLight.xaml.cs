using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.ComponentModel;

namespace xmaslights
{
    /// <summary>
    /// Interaction logic for Light.xaml
    /// </summary>
    public partial class VectorLight : UserControl, INotifyPropertyChanged, ILight
    {
        static VectorLight()
        {
            InitializeBrushes();
        }

        private static LinearGradientBrush _onBrush;
        private static LinearGradientBrush _offBrush;

        private static void InitializeBrushes()
        {
            _onBrush = new LinearGradientBrush();
            _onBrush.GradientStops = new GradientStopCollection();
            _onBrush.GradientStops.Add(new GradientStop(Colors.Orange, 0.3));
            _onBrush.GradientStops.Add(new GradientStop(Colors.Red, 1));
            RenderOptions.SetCachingHint(_onBrush, CachingHint.Cache);
            _onBrush.Freeze();

            _offBrush = new LinearGradientBrush();
            _offBrush.GradientStops = new GradientStopCollection();
            _offBrush.GradientStops.Add(new GradientStop(Colors.Orange, 0));
            _offBrush.GradientStops.Add(new GradientStop(Colors.Yellow, 1));
            _offBrush.Opacity = 0.5;
            RenderOptions.SetCachingHint(_offBrush, CachingHint.Cache);
            _offBrush.Freeze();
        }

        public VectorLight()
        {
            InitializeComponent();
            _isOn = false;
            Binding brushBinding = new Binding("Brush");
            brushBinding.Source = this;
            brushBinding.UpdateSourceTrigger = UpdateSourceTrigger.Explicit;
            BindingOperations.SetBinding(path, Path.FillProperty, brushBinding);
        }

        private Brush _brush;
        private bool _isOn;
   
        public Brush Brush 
        { 
            get
            { 
                return _brush;
            }
            set
            {    
                _brush = value;
                OnPropertyChanged("Brush");
            }
        }

        public void Switch()
        {
            _isOn = !_isOn;
            Update();
        }

        public void On()
        {
            _isOn = true;
            Update();
        }

        public void Off()
        {
            _isOn = false;
            Update();
        }

        public void Update()
        {
            if (_isOn)
            {
               this.Brush = _onBrush;
            }
            else
            {
               this.Brush = _offBrush;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public void Rotate(int angle)
        {
            this.RenderTransform = new RotateTransform(angle, 10, 20);
        }


        public bool IsOn()
        {
            return _isOn;
        }

    }
}
