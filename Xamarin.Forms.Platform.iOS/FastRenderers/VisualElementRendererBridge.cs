using System;
using System.ComponentModel;
using Xamarin.Forms;

#if __MOBILE__
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
using static Xamarin.Forms.Platform.iOS.ColorExtensions;
using UIKit;
using NativeView = UIKit.UIView;
using NativeViewController = UIKit.UIViewController;
using NativeColor = UIKit.UIColor;
using NativeControl = UIKit.UIControl;

namespace Xamarin.Forms.Platform.iOS.FastRenderers
#else
using static Xamarin.Forms.Platform.MacOS.ColorExtensions;
using AppKit;
using NativeView = AppKit.NSView;
using NativeViewController = AppKit.NSViewController;
using NativeColor = AppKit.NSColor;
using NativeControl = AppKit.NSControl;

namespace Xamarin.Forms.Platform.MacOS.FastRenderers
#endif
{
	public class VisualElementRendererBridge : IDisposable
	{
#if __MOBILE__
		static readonly NativeColor _defaultColor = NativeColor.Clear;
#else
		static readonly CGColor _defaultColor = NativeColor.Clear.CGColor;
#endif

		IVisualElementRenderer _renderer;

		EventTracker _events;
		VisualElementPackager _packager;
		VisualElementTracker _tracker;
		AccessibilityProvider _accessibilityProvider;
		EffectControlProvider _effectControlProvider;

		VisualElementRendererFlags _flags = VisualElementRendererFlags.AutoPackage | VisualElementRendererFlags.AutoTrack;

#if __MOBILE__
		UIVisualEffectView _blur;
		BlurEffectStyle _previousBlur;
#endif

		public Action UpdateNativeWidget;


		public VisualElementRendererBridge(IVisualElementRenderer renderer)
		{
			_renderer = renderer;
			_renderer.ElementChanged += OnElementChanged;
			_accessibilityProvider = new AccessibilityProvider(_renderer);
			_effectControlProvider = new EffectControlProvider(_renderer?.NativeView);

#if __MOBILE__
			Control.BackgroundColor = _defaultColor;
#else
			Control.WantsLayer = true;
			Control.Layer.BackgroundColor = _defaultColor;
#endif
		}

		VisualElement Element => _renderer?.Element;

		NativeView Control => _renderer?.NativeView;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose(bool disposing)
		{
			if ((_flags & VisualElementRendererFlags.Disposed) != 0)
				return;

			_flags |= VisualElementRendererFlags.Disposed;

			if (disposing)
			{

				if (_events != null)
				{
					_events.Dispose();
					_events = null;
				}

				if (_tracker != null)
				{
					_tracker.Dispose();
					_tracker = null;
				}

				if (_packager != null)
				{
					_packager.Dispose();
					_packager = null;
				}

				if (_accessibilityProvider != null)
				{
					_accessibilityProvider.Dispose();
					_accessibilityProvider = null;
				}

				if (_effectControlProvider != null)
				{
					_effectControlProvider = null;
				}

				if (Element != null)
				{
					Platform.SetRenderer(Element, null);
					_renderer?.SetElement(null);
				}

				if (_renderer != null)
				{
					_renderer.ElementChanged -= OnElementChanged;
					_renderer = null;
				}
			}
		}

		public bool AutoPackage
		{
			get { return (_flags & VisualElementRendererFlags.AutoPackage) != 0; }
			set
			{
				if (value)
					_flags |= VisualElementRendererFlags.AutoPackage;
				else
					_flags &= ~VisualElementRendererFlags.AutoPackage;
			}
		}

		public bool AutoTrack
		{
			get { return (_flags & VisualElementRendererFlags.AutoTrack) != 0; }
			set
			{
				if (value)
					_flags |= VisualElementRendererFlags.AutoTrack;
				else
					_flags &= ~VisualElementRendererFlags.AutoTrack;
			}
		}

		void OnElementChanged(object sender, VisualElementChangedEventArgs e)
		{
			if (e.OldElement != null)
			{
				e.OldElement.PropertyChanged -= OnElementPropertyChanged;
				e.OldElement.FocusChangeRequested -= ViewOnFocusChangeRequested;
			}

			if (e.NewElement != null)
			{
				if (e.NewElement.BackgroundColor != Color.Default || (e.OldElement != null && e.NewElement.BackgroundColor != e.OldElement.BackgroundColor))
					SetBackgroundColor(e.NewElement.BackgroundColor);

				UpdateClipToBounds();

				if (_tracker == null)
				{
					_tracker = new VisualElementTracker(_renderer);
					_tracker.NativeControlUpdated += (s, ev) => UpdateNativeWidget?.Invoke();
				}

				if (AutoPackage && _packager == null)
				{
					_packager = new VisualElementPackager(_renderer);
					_packager.Load();
				}

				if (AutoTrack && _events == null)
				{
					_events = new EventTracker(_renderer);
					_events.LoadEvents(Control);
				}

				e.NewElement.PropertyChanged += OnElementPropertyChanged;
				e.NewElement.FocusChangeRequested += ViewOnFocusChangeRequested;

				SendVisualElementInitialized(e.NewElement, Control);

				UpdateIsEnabled();

#if __MOBILE__
				if (e.NewElement != null)
					SetBlur((BlurEffectStyle)e.NewElement.GetValue(PlatformConfiguration.iOSSpecific.VisualElement.BlurEffectProperty));
#endif
			}

			EffectUtilities.RegisterEffectControlProvider(_effectControlProvider, e.OldElement, e.NewElement);
		}

		void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == VisualElement.BackgroundColorProperty.PropertyName)
				SetBackgroundColor(Element.BackgroundColor);
			else if (e.PropertyName == Xamarin.Forms.Layout.IsClippedToBoundsProperty.PropertyName)
				UpdateClipToBounds();
			else if (e.PropertyName == VisualElement.IsEnabledProperty.PropertyName)
				UpdateIsEnabled();
#if __MOBILE__
			else if (e.PropertyName == PlatformConfiguration.iOSSpecific.VisualElement.BlurEffectProperty.PropertyName)
				SetBlur((BlurEffectStyle)Element.GetValue(PlatformConfiguration.iOSSpecific.VisualElement.BlurEffectProperty));
#endif
		}

		void SetBackgroundColor(Color color)
		{
			if (Element == null || Control == null)
				return;

			if (color == Color.Default)
#if __MOBILE__

				Control.BackgroundColor = _defaultColor;
			else
				Control.BackgroundColor = color.ToUIColor();

#else
				Control.Layer.BackgroundColor = _defaultColor;
			else
				Control.Layer.BackgroundColor = color.ToCGColor();
#endif
		}

		void UpdateClipToBounds()
		{
#if __MOBILE__
			var clippableLayout = Element as Layout;
			if (clippableLayout != null)
				Control.ClipsToBounds = clippableLayout.IsClippedToBounds;
#endif
		}

		void SendVisualElementInitialized(VisualElement element, NativeView nativeView)
		{
			Element.SendViewInitialized(nativeView);
		}

		void UpdateIsEnabled()
		{
			if (Element == null || Control == null)
				return;

			var uiControl = Control as NativeControl;
			if (uiControl == null)
				return;
			uiControl.Enabled = Element.IsEnabled;
		}

#if __MOBILE__
		void SetBlur(BlurEffectStyle blur)
		{
			if (_previousBlur == blur)
				return;

			_previousBlur = blur;

			if (_blur != null)
			{
				_blur.RemoveFromSuperview();
				_blur = null;
			}

			if (blur == BlurEffectStyle.None)
			{
				Control.SetNeedsDisplay();
				return;
			}

			UIBlurEffect blurEffect;
			switch (blur)
			{
				default:
				case BlurEffectStyle.ExtraLight:
					blurEffect = UIBlurEffect.FromStyle(UIBlurEffectStyle.ExtraLight);
					break;
				case BlurEffectStyle.Light:
					blurEffect = UIBlurEffect.FromStyle(UIBlurEffectStyle.Light);
					break;
				case BlurEffectStyle.Dark:
					blurEffect = UIBlurEffect.FromStyle(UIBlurEffectStyle.Dark);
					break;
			}

			_blur = new UIVisualEffectView(blurEffect);
			Control.LayoutSubviews();
		}

		public void LayoutSubviews()
		{
			if (_blur != null && Control.Superview != null)
			{
				_blur.Frame = Control.Frame;
				if (_blur.Superview == null)
					Control.Superview.Add(_blur);
			}
		}
#endif

		void ViewOnFocusChangeRequested(object sender, VisualElement.FocusRequestArgs focusRequestArgs)
		{
			if (Control == null)
				return;

			focusRequestArgs.Result = focusRequestArgs.Focus ? Control.BecomeFirstResponder() : Control.ResignFirstResponder();
		}
	}
}
