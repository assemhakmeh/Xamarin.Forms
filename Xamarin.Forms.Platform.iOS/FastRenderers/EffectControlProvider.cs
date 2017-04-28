using System;

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
	public class EffectControlProvider : IEffectControlProvider
	{
		readonly NativeView _control;
		readonly NativeView _container;

		public EffectControlProvider(NativeView control)
		{
			_control = control;
		}

		public EffectControlProvider(NativeView control, NativeView container)
		{
			_control = control;
			_container = container;
		}

		void IEffectControlProvider.RegisterEffect(Effect effect)
		{
			var platformEffect = effect as PlatformEffect;
			if (platformEffect == null)
			{
				return;
			}

			platformEffect.SetControl(_control);
			platformEffect.SetContainer(_container);
		}
	}
}
