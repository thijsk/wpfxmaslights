﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.4927
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;


namespace HueShift {
	
	public class ShiftHueEffect : ShaderEffect {
		public static readonly DependencyProperty InputProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(ShiftHueEffect), 0);
		public static readonly DependencyProperty HueShiftProperty = DependencyProperty.Register("HueShift", typeof(double), typeof(ShiftHueEffect), new PropertyMetadata(((double)(0)), PixelShaderConstantCallback(0)));
		public ShiftHueEffect() {
			PixelShader pixelShader = new PixelShader();
            pixelShader.UriSource = new Uri("pack://application:,,,/ChristmasLights;Component/ShiftHue/ShiftHue.ps", UriKind.Absolute);
			this.PixelShader = pixelShader;

			this.UpdateShaderValue(InputProperty);
			this.UpdateShaderValue(HueShiftProperty);
		}
		public Brush Input {
			get {
				return ((Brush)(this.GetValue(InputProperty)));
			}
			set {
				this.SetValue(InputProperty, value);
			}
		}
		/// <summary>Hue shift</summary>
		public double HueShift {
			get {
				return ((double)(this.GetValue(HueShiftProperty)));
			}
			set {
				this.SetValue(HueShiftProperty, value);
			}
		}
	}
}
