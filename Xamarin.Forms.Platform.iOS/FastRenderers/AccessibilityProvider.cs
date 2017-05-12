using System;
using System.ComponentModel;

#if __MOBILE__
using UIKit;
using NativeView = UIKit.UIView;

namespace Xamarin.Forms.Platform.iOS.FastRenderers
#else
using AppKit;
using NativeView = AppKit.NSView;

namespace Xamarin.Forms.Platform.MacOS.FastRenderers
#endif
{
	public class AccessibilityProvider : IDisposable
	{
		bool _disposed;

#if __MOBILE__
		string _defaultAccessibilityLabel;
		string _defaultAccessibilityHint;
		bool? _defaultIsAccessibilityElement;
#endif

		IVisualElementRenderer _renderer;

		NativeView Control => _renderer?.NativeView;

		VisualElement Element => _renderer?.Element;


		public AccessibilityProvider(IVisualElementRenderer renderer)
		{
			_renderer = renderer;
			_renderer.ElementChanged += OnElementChanged;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose(bool disposing)
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;

			if (_renderer != null)
			{
				_renderer.ElementChanged -= OnElementChanged;

				if (Element != null)
					Element.PropertyChanged -= OnElementPropertyChanged;


				_renderer = null;
			}
		}

		void OnElementChanged(object sender, VisualElementChangedEventArgs e)
		{
			if (e.OldElement != null)
			{
				e.OldElement.PropertyChanged -= OnElementPropertyChanged;
			}

			if (e.NewElement != null)
			{
				e.NewElement.PropertyChanged += OnElementPropertyChanged;
			}


			if (Element != null && !string.IsNullOrEmpty(Element.AutomationId))
				SetAutomationId(Element.AutomationId);

#if __MOBILE__
			SetAccessibilityLabel();
			SetAccessibilityHint();
			SetIsAccessibilityElement();
#endif
		}

		void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
#if __MOBILE__
			if (e.PropertyName == AutomationProperties.HelpTextProperty.PropertyName)
				SetAccessibilityHint();
			else if (e.PropertyName == AutomationProperties.NameProperty.PropertyName)
				SetAccessibilityLabel();
			else if (e.PropertyName == AutomationProperties.IsInAccessibleTreeProperty.PropertyName)
				SetIsAccessibilityElement();
#endif
		}

		public void SetAutomationId(string id)
		{
			Control.AccessibilityIdentifier = id;
		}

#if __MOBILE__
		void SetAccessibilityHint()
		{
			if (Element == null)
				return;

			if (_defaultAccessibilityHint == null)
				_defaultAccessibilityHint = Control.AccessibilityHint;

			Control.AccessibilityHint = (string)Element.GetValue(AutomationProperties.HelpTextProperty) ?? _defaultAccessibilityHint;
		}

		void SetAccessibilityLabel()
		{
			if (Element == null)
				return;

			if (_defaultAccessibilityLabel == null)
				_defaultAccessibilityLabel = Control.AccessibilityLabel;

			Control.AccessibilityLabel = (string)Element.GetValue(AutomationProperties.NameProperty) ?? _defaultAccessibilityLabel;
		}

		void SetIsAccessibilityElement()
		{
			if (Element == null)
				return;

			if (!_defaultIsAccessibilityElement.HasValue)
				_defaultIsAccessibilityElement = Control.IsAccessibilityElement;

			Control.IsAccessibilityElement = (bool)((bool?)Element.GetValue(AutomationProperties.IsInAccessibleTreeProperty) ?? _defaultIsAccessibilityElement);
		}
#endif


	}
}


